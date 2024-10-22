using DistributedCacheAndLockSample.Extensions;
using DistributedCacheAndLockSample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RedLockNet;
using System.Net;
using Order = DistributedCacheAndLockSample.Models.Order;

namespace DistributedCacheAndLockSample.Controllers;

[Route("api/[Controller]")]
[ApiController]
public class OrderController : ControllerBase
{
	private readonly IConfiguration _configuration;
	private readonly IDistributedCache _distributedCache;
	private readonly IDistributedLockFactory _distributedLockFactory;
	private readonly IHostApplicationLifetime _hostApplicationLifetime;
	private IRedLock _processOrderLock;

	public OrderController(IConfiguration configuration, IDistributedCache distributedCache,
		IDistributedLockFactory distributedLockFactory, IHostApplicationLifetime hostApplicationLifetime)
	{
		_configuration = configuration;
		_distributedCache = distributedCache;
		_distributedLockFactory = distributedLockFactory;
		_hostApplicationLifetime = hostApplicationLifetime;

		_hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
	}

	[HttpGet("{orderId:int}")]
	public async Task<ActionResult<Order>> GetOrder(int orderId)
	{
		//we assume here we always have the requested order, if not we create it on the fly

		var order = await _distributedCache.GetAsync<Order>(orderId.ToString());

		if (order != null)
		{
			return Ok(order);
		}
		else
		{
			var newOrder = new Order
			{
				Id = orderId,
				IsProcessed = false,
				ProcessorInstanceName = _configuration["INSTANCE_ID"]
			};

			await _distributedCache.SetAsync(orderId.ToString(), newOrder, new()
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
			});

			return Ok(newOrder);
		}
	}

	[HttpPost("process-order")]
	public async Task<ActionResult<Order>> ProcessOrder(ProcessOrderRequest request)
	{
		var resource = $"order:{request.OrderId}";
		var expiry = TimeSpan.FromSeconds(30);
		var wait = TimeSpan.FromSeconds(10);
		var retry = TimeSpan.FromSeconds(1);
		Order order = null;

		order = await _distributedCache.GetAsync<Order>(request.OrderId.ToString());

		if (order?.IsProcessed ?? false)
		{
			return Ok(order);
		}

		await using (_processOrderLock = await _distributedLockFactory.CreateLockAsync(resource, expiry, wait, retry))
		{
			// make sure we got the lock
			if (_processOrderLock.IsAcquired)
			{
				try
				{
					order = await _distributedCache.GetAsync<Order>(request.OrderId.ToString());

					if (order?.IsProcessed ?? false)
					{
						return Ok(order);
					}

					await Task.Delay(TimeSpan.FromSeconds(45));

					if (order == null)
					{
						order = new Order
						{
							Id = request.OrderId,
							ProcessorInstanceName = _configuration["INSTANCE_ID"]
						};
					}

					order.IsProcessed = true;

					await _distributedCache.SetAsync(order.Id.ToString(), order);
				}
				catch (Exception ex)
				{
					return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
				}
				finally
				{
					await _processOrderLock.DisposeAsync();
				}
			}
		}

		return Ok(order);
	}

	private void OnShutdown()
	{
		if(_processOrderLock?.IsAcquired ?? false)
		{
			_processOrderLock.Dispose();
		}
	}
}
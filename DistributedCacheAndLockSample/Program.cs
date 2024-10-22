
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace DistributedCacheAndLockSample;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		// Add services to the container.

		builder.Services.AddControllers();
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		builder.Services.AddSingleton<Random>();

		ConfigRedis(builder.Services);

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

		app.UseAuthorization();


		app.MapControllers();

		app.Run();
	}

	private static void ConfigRedis(IServiceCollection services)
	{
		services.AddStackExchangeRedisCache(options =>
		{
			options.Configuration = "localhost:6379";
		});

		var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");

		services.AddSingleton<IDistributedLockFactory>(provider =>
		{
			var multiplexers = new List<RedLockMultiplexer>
			{
				redisConnection
			};

			return RedLockFactory.Create(multiplexers);
		});
	}
}

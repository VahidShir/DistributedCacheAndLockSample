using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DistributedCacheAndLockSample.Extensions;

public static class RedisExtensions
{
	public static async Task<T> GetAsync<T>(this IDistributedCache distributedCache, string key)
	{
		var responseStr = await distributedCache.GetStringAsync(key);

		if (responseStr == null)
			return default(T);

		return JsonSerializer.Deserialize<T>(responseStr);
	}

	public static async Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value,
		DistributedCacheEntryOptions options = null)
	{
		options ??= new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20)
		};

		await distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
	}
}
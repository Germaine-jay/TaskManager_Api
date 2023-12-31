﻿using Newtonsoft.Json;
using StackExchange.Redis;

namespace TaskManager.Services.Configurations.Cache.CacheServices
{
    public class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _redis;
        private readonly IServer _server;

        public CacheService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
            _server = redis.GetServer(redis.GetEndPoints()[0]);
        }


        public async Task WriteToCache<T>(string key, T payload, CacheKeySets? cacheKeySets, TimeSpan? absoluteExpireTime)
        {
            string stringifiedJson = JsonConvert.SerializeObject(payload);
            await _redis.StringSetAsync(key, stringifiedJson, absoluteExpireTime, When.Always).ConfigureAwait(false);

            if (cacheKeySets.HasValue)            
                await _redis.SetAddAsync(cacheKeySets.Value.ToString(), key).ConfigureAwait(false);        
        }



        public async Task<T?> ReadFromCache<T>(string key) where T : class
        {
            string? serialisedPayload = await _redis.StringGetAsync(key: key);
            if (!string.IsNullOrWhiteSpace(serialisedPayload))
            {
                T? payload = JsonConvert.DeserializeObject<T>(serialisedPayload);
                return payload;
            }
            return default;
        }



        public async Task ClearFromCache(string key)
        {
            await _redis.KeyDeleteAsync(key);
        }



        public async Task ClearFromCache(CacheKeySets cacheKeySets, string key)
        {
            await _redis.KeyDeleteAsync(key);
            await _redis.SetRemoveAsync(cacheKeySets.ToString(), key);
        }



        public async Task<IEnumerable<T>> BulkReadFromCache<T>(string pattern) where T : class
        {
            List<T> records = new List<T>();
            foreach (var key in _server.Keys(pattern: pattern))
            {
                T? record = await ReadFromCache<T>(key.ToString());
                if (record != default(T))
                {
                    records.Add(record);
                }
            }
            return records;
        }



        public async Task<IEnumerable<T>> BulkReadFromCache<T>(CacheKeySets cacheKeySets) where T : class
        {
            List<T> records = new List<T>();

            var keys = await _redis.SetMembersAsync(cacheKeySets.ToString());

            foreach (var key in keys)
            {
                T? record = await ReadFromCache<T>(key.ToString());
                if (record != default(T))
                {
                    records.Add(record);
                }
                else
                {
                    await _redis.SetRemoveAsync(cacheKeySets.ToString(), key);
                }
            }
            return records;
        }

    }
}

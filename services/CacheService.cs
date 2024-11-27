using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

class CacheService
{
    private readonly IDistributedCache _cache;
    public CacheService(string TableName)
    {
        var serviceProvider = new ServiceCollection().AddDistributedSqlServerCache(o =>
        {
            o.ConnectionString = "Data Source=GIRLBOSS;Initial Catalog=IFNTUNG_SCHEDULE;Integrated Security=True;TrustServerCertificate=True;";
            o.SchemaName = "dbo";
            o.TableName = TableName;
        }).BuildServiceProvider();
        _cache = serviceProvider.GetRequiredService<IDistributedCache>();
    }
    public async Task<string> getGroupNameFromCache(string groupName)
    {
        string cachedGroupName = "";
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            try
            {
                cachedGroupName = await _cache.GetStringAsync(groupName) ?? "";
                Console.WriteLine(cachedGroupName);
                return cachedGroupName;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error with fetching group name " + ex);
                return "";

            }
        }
        return cachedGroupName;

    }



    public async Task FillCacheWithGroups(string groupId, string groupName)
    {
        var cacheOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(10),
        };
        await _cache.SetStringAsync(groupId, groupName, cacheOptions);
        var result = new List<KeyValuePair<string, string>>{
            new KeyValuePair<string , string>(groupId , groupName)
        };
    }
    public async Task FillCacheWithSchedule(string groupId, string scheduleInstance)
    {
        var cacheOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(10),
        };
        await _cache.SetStringAsync(groupId, scheduleInstance, cacheOptions);

    }

    public async Task<string> GetScheduleFromCache(string groupId)
    {
        string cachedSchedule = "";
        if (!string.IsNullOrWhiteSpace(groupId) && groupId.Length == 5)
        {
            try
            {
                cachedSchedule = await _cache.GetStringAsync(groupId) ?? "";
                return cachedSchedule;


            }
            catch (Exception ex)
            {
                return "Error with fetching schedule from cache " + ex;
            }
        }
        return cachedSchedule;
    }
}

using HttpRequestService;


namespace GroupManagerNamespace {
    class GroupManager
{
    private string groupIdEndpoint = "https://dekanat.nung.edu.ua/cgi-bin/timetable_export.cgi?req_type=obj_list&req_mode=group&show_ID=yes&req_format=json&coding_mode=UTF8&bs=ok";


    public async Task<string> getGroupValueId(string groupName)
    {
        HttpRequestClient client = new HttpRequestClient();
        var cacheService = new CacheService("CachedGroups");
        try
        {
            string groupID = await cacheService.getGroupNameFromCache(groupName);
            if (groupID != null && groupID.Length == 5)
            {
                Console.WriteLine("Got groupID from cache");
                return groupID;
            }
            else
            {
                try
                {
                    string groupId = await client.makeHttpRequest(groupIdEndpoint , groupName);
                    if (groupId.Length == 5)
                    {
                        Console.WriteLine($"{groupId} got from HTTP request groupId");
                        cacheService.FillCacheWithGroups(groupName, groupId).Wait();
                        return groupId;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while fetching the data from the GroupIDs endpoint " + ex);
                }
            }
            {


            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while accessing CacheGroups table " + ex);
        }

        throw new InvalidOperationException("No 'departments' found in 'psrozklad_export'.");


    }

}
}

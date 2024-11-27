using System.Text.Json;
using HttpRequestService;

namespace ScheduleManagerNamespace
{
    class ScheduleManager
    {


        public ScheduleManager()
        {

        }
        public async Task<List<Dictionary<string, string>>> GetScheduleValue(string groupID)
        {
            HttpRequestClient client = new HttpRequestClient();
            var cacheService = new CacheService("CachedSchedule");

            try
            {
                string scheduleStringJson = await cacheService.getGroupNameFromCache(groupID);
                if (!string.IsNullOrEmpty(scheduleStringJson))
                {
                    Console.WriteLine("Cache hit!");
                }

                string url = $"https://dekanat.nung.edu.ua/cgi-bin/timetable_export.cgi?req_type=rozklad&req_mode=group&OBJ_ID={groupID}&OBJ_name=&dep_name=&ros_text=united&begin_date=&end_date=&req_format=json&coding_mode=UTF8&bs=ok";
                List<Dictionary<string, string>> fetchedSchedule = await client.MakeHttpScheduleRequest(url);

                Console.WriteLine("Fetched and cached schedule successfully.");
                return fetchedSchedule;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching schedule for groupID {groupID}: {ex.Message}");
            }

            return new List<Dictionary<string, string>>();
        }
    }
}
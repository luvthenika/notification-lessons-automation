using System.Text.Json;
using GroupParserNamespace;
using ScheduleManagerParser;
namespace HttpRequestService
{
    class HttpRequestClient
    {
        private readonly HttpClient _client;
        public HttpRequestClient()
        {
            _client = new HttpClient();

        }
        public async Task<string> makeHttpRequest(string endpoint, string targetGroupName)
        {
            var parser = new GroupsParser();
            var groupJsonString = _client.GetStringAsync(endpoint).Result;
            await Task.Yield();
            string groupId = parser.ParseGroupsIdsJson(groupJsonString, targetGroupName);
            Console.WriteLine($"{groupId} ");
            return groupId;

        }
        public async Task<List<Dictionary<string, string>>> MakeHttpScheduleRequest(string endpoint)
        {
            try
            {
                string schedule = await _client.GetStringAsync(endpoint);
                var parser = new ScheduleParser();
                var parserObject = parser.ParseScheduleObject(schedule);

                foreach (var entry in parserObject)
                {
                    Console.WriteLine($"Time: {entry["lesson_time"]}, Description: {entry["lesson_description"]}");
                }

                return parserObject;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while fetching schedule: {ex.Message}");
                return new List<Dictionary<string, string>>();
            }
        }
    }


}
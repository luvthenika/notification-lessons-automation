using System.Text.Json;

namespace ScheduleManagerParser
{
    class ScheduleParser
    {
        public ScheduleParser()
        {

        }
        public List<Dictionary<string, string>> ParseScheduleObject(string schedule)
        {
            var scheduleList = new List<Dictionary<string, string>>();
            using (JsonDocument document = JsonDocument.Parse(schedule))
            {
                JsonElement root = document.RootElement;
                if (root.TryGetProperty("psrozklad_export", out JsonElement psRozkladExport))
                {
                    if (psRozkladExport.TryGetProperty("roz_items", out JsonElement rozItems))
                    {
                        foreach (JsonElement item in rozItems.EnumerateArray())
                        {
                            var scheduleEntry = new Dictionary<string, string>();

                            if (item.TryGetProperty("lesson_time", out JsonElement lessonTime))
                            {
                                scheduleEntry["lesson_time"] = lessonTime.GetString();
                            }

                            if (item.TryGetProperty("lesson_description", out JsonElement lessonDescription))
                            {
                                scheduleEntry["lesson_description"] = lessonDescription.GetString();
                            }

                            if (scheduleEntry.Count > 0)
                            {
                                scheduleList.Add(scheduleEntry);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("'roz_items' not found in 'psrozklad_export'.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("'psrozklad_export' not found in the provided JSON.");
                }
            }

            return scheduleList;
        }


    }


}
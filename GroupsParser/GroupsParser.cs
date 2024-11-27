using System.Text.Json;
namespace GroupParserNamespace
{
    class GroupsParser
    {
        public GroupsParser()
        {

        }
        public string ParseGroupsIdsJson(string targetObject, string targetGroupName)
        {
            using (JsonDocument document = JsonDocument.Parse(targetObject))
            {
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("psrozklad_export", out JsonElement psRozkladExport))
                {
                    if (psRozkladExport.TryGetProperty("departments", out JsonElement departments))
                    {
                        foreach (JsonElement department in departments.EnumerateArray())
                        {
                            if (department.TryGetProperty("objects", out JsonElement objects))
                            {
                                foreach (JsonElement obj in objects.EnumerateArray())
                                {
                                    if (obj.TryGetProperty("name", out JsonElement groupName) && obj.TryGetProperty("ID", out JsonElement groupID))
                                    {
                                        if (targetGroupName == groupName.ToString())
                                        {
                                            Console.WriteLine($"{groupID} parsed");
                                            return groupID.ToString();
                                        }

                                    }
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("No 'objects' found in 'departments'.");
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("No 'departments' found in 'psrozklad_export'.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("'psrozklad_export' not found in the provided JSON.");
                }
            }

            // If none of the properties matched, throw an exception indicating that a valid group ID was not found.
            throw new InvalidOperationException("No valid group ID found in the provided JSON.");
        }


    }
}

using Newtonsoft.Json;

namespace NailManager.Services;

public class Utls
{
    public static string FormatJsonString(string json)
    {
        try
        {
            var parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
        catch (Exception ex)
        {
            return $"Invalid JSON format: {ex.Message}";
        }
    }

}
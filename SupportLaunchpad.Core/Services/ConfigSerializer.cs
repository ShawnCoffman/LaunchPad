using System.Text.Json;
using System.Text.Json.Serialization;
using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public static class ConfigSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static LaunchpadConfig Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<LaunchpadConfig>(json, Options) ?? new LaunchpadConfig();
        }
        catch (JsonException)
        {
            return new LaunchpadConfig();
        }
    }

    public static LaunchpadConfig DeserializeOrDefault(string json, LaunchpadConfig fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<LaunchpadConfig>(json, Options) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    public static string Serialize(LaunchpadConfig config)
    {
        return JsonSerializer.Serialize(config, Options);
    }
}

using System.Text.Json;
using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public static class AppSettingsSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static LocalAppSettings Deserialize(string json)
    {
        return JsonSerializer.Deserialize<LocalAppSettings>(json, Options) ?? AppSettingsFactory.CreateDefault();
    }

    public static string Serialize(LocalAppSettings settings)
    {
        return JsonSerializer.Serialize(settings, Options);
    }
}

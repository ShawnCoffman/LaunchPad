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

    public static bool TryDeserialize(string json, out LaunchpadConfig config)
    {
        try
        {
            config = JsonSerializer.Deserialize<LaunchpadConfig>(json, Options) ?? new LaunchpadConfig();
            Normalize(config);
            return true;
        }
        catch (JsonException)
        {
            config = new LaunchpadConfig();
            return false;
        }
    }

    public static string Serialize(LaunchpadConfig config)
    {
        return JsonSerializer.Serialize(config, Options);
    }

    private static void Normalize(LaunchpadConfig config)
    {
        config.Title ??= "Support Launchpad";
        config.Settings ??= new LaunchpadSettings();
        config.Settings.AllowedScriptDirectories ??= [];
        config.Tabs ??= [];
        config.Tabs.RemoveAll(tab => tab is null);
        config.HiddenTabIds ??= [];
        config.HiddenButtonIds ??= [];

        foreach (var tab in config.Tabs)
        {
            tab.Id ??= string.Empty;
            tab.Name ??= string.Empty;
            tab.Buttons ??= [];
            tab.Buttons.RemoveAll(button => button is null);
            tab.ButtonOrder ??= [];
            foreach (var button in tab.Buttons)
            {
                button.Id ??= string.Empty;
                button.Name ??= string.Empty;
                button.Description ??= string.Empty;
                button.Path ??= string.Empty;
                button.Arguments ??= string.Empty;
                button.WorkingDirectory ??= string.Empty;
                button.IconPath ??= string.Empty;
            }
        }
    }
}

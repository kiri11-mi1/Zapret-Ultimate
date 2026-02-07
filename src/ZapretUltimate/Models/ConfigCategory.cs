namespace ZapretUltimate.Models;

public enum ConfigCategory
{
    Discord,
    YouTubeTwitch,
    Gaming,
    Universal
}

public static class ConfigCategoryExtensions
{
    public static string GetDisplayName(this ConfigCategory category) => category switch
    {
        ConfigCategory.Discord => "Discord",
        ConfigCategory.YouTubeTwitch => "YouTube & Twitch",
        ConfigCategory.Gaming => "Gaming",
        ConfigCategory.Universal => "Universal",
        _ => category.ToString()
    };

    public static string GetFolderName(this ConfigCategory category) => category switch
    {
        ConfigCategory.Discord => "discord",
        ConfigCategory.YouTubeTwitch => "youtube_twitch",
        ConfigCategory.Gaming => "gaming",
        ConfigCategory.Universal => "universal",
        _ => category.ToString().ToLower()
    };

    public static int GetPriority(this ConfigCategory category) => category switch
    {
        ConfigCategory.Discord => 1,
        ConfigCategory.YouTubeTwitch => 2,
        ConfigCategory.Gaming => 3,
        ConfigCategory.Universal => 4,
        _ => 99
    };
}

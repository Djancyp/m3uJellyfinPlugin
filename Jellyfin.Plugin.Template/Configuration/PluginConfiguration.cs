namespace Jellyfin.Plugin.Template.Configuration
{
    public class PluginConfiguration : MediaBrowser.Model.Plugins.BasePluginConfiguration
    {
        public string M3UUrl { get; set; } = string.Empty;
        public int CacheMinutes { get; set; } = 60;
        public bool EnableDebugLogging { get; set; } = false;
    }
}

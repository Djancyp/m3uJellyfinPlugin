using Jellyfin.Plugin.Template.Services; // Add this for the controller's namespace
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Template
{
    /// <summary>
    /// Register services.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddScoped<CollectionImportController>();
            serviceCollection.AddScoped<CustomM3uMediaSourceProvider>();
        }
    }
}

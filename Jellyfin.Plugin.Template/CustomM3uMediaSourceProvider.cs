using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding; // For IMediaSourceProvider
using MediaBrowser.Model.Dto; // For MediaSourceInfo
using MediaBrowser.Model.MediaInfo; // For MediaProtocol
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Template;

public class CustomM3uMediaSourceProvider : IMediaSourceProvider
{
    private readonly ILogger<CustomM3uMediaSourceProvider> _logger;

    public CustomM3uMediaSourceProvider(ILogger<CustomM3uMediaSourceProvider> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CustomM3uMediaSourceProvider: Processing media sources for item {ItemName}", item.Name);

        // log out the item type and ID
        _logger.LogInformation("Item Type: {ItemType}, Item ID: {ItemId}", item.GetType().Name, item.Id);
        _logger.LogInformation("Item Path: {ItemPath}", item.Path);
        // Check if the item is a LiveTvChannel and has a valid stream URL
        if (string.IsNullOrEmpty(item.Path))
        {
            var originalUrl = item.Path;
            var modifiedUrl = ModifyStreamUrl(originalUrl);

            var mediaSource = new MediaSourceInfo
            {
                Id = item.Id.ToString(),
                Path = modifiedUrl,
                Protocol = MediaProtocol.Http,
                Name = item.Name,
                RequiresOpening = false,
                SupportsDirectStream = true,
                SupportsDirectPlay = true,
                SupportsTranscoding = true,
            };

            _logger.LogInformation("Modified stream URL from {OriginalUrl} to {ModifiedUrl}", originalUrl, modifiedUrl);
            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new[] { mediaSource });
        }

        // Return empty list for non-M3U items
        return Task.FromResult<IEnumerable<MediaSourceInfo>>(Array.Empty<MediaSourceInfo>());
    }

    public Task<ILiveStream> OpenMediaSource(
        string openToken,
        List<ILiveStream> currentLiveStreams,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private string ModifyStreamUrl(string originalUrl)
    {
        // Example modification: Prepend a proxy URL
        // Customize this based on your needs
        // var proxyBaseUrl = "http://localhost:8585/hooks";
        // _logger.LogInformation("Modifying stream URL: {OriginalUrl}", originalUrl);
        // return proxyBaseUrl + Uri.EscapeDataString(originalUrl);
        // return hardcoded URL for testing
        return "https://yayin2.canlitv.fun/live/dmax.stream/chunklist_w285741828.m3u8?hash=d2356a2ac05e7a39ae5c8e454b93a149"; // Replace
    }
}

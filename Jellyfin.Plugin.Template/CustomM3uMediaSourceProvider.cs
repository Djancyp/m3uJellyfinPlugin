using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Template;

public class CustomM3uMediaSourceProvider : IMediaSourceProvider, IDisposable
{
    private readonly ILogger<CustomM3uMediaSourceProvider> _logger;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public CustomM3uMediaSourceProvider(
        ILogger<CustomM3uMediaSourceProvider> logger,
        IUserManager userManager,
        ISessionManager sessionManager
    )
    {
        _logger = logger;
        _userManager = userManager;
        _sessionManager = sessionManager;
        _httpClient = new HttpClient();
        _disposed = false;
    }

    public Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CustomM3uMediaSourceProvider: Processing media sources for item {ItemName}", item.Name);
        _logger.LogInformation("Item Type: {ItemType}, Item ID: {ItemId}", item.GetType().Name, item.Id);
        _logger.LogInformation("Item Path: {ItemPath}", item.Path ?? "null");

        // _logger.LogInformation("MediaSourceTest: {MediaSourceTest}", mediaSourcetest?.Path ?? "null");

        // Check if there is a valid media source or item path
        if (string.IsNullOrEmpty(item.Path))
        {
            var originalUrl = item.Path ?? string.Empty;
            // if (string.IsNullOrEmpty(originalUrl))
            // {
            //     _logger.LogWarning("No valid URL found for item {ItemName}; returning empty media sources", item.Name);
            //     return Task.FromResult<IEnumerable<MediaSourceInfo>>(Array.Empty<MediaSourceInfo>());
            // }

            var modifiedUrl = ModifyStreamUrl(originalUrl, item, cancellationToken).GetAwaiter().GetResult();

            var mediaSource = new MediaSourceInfo
            {
                Id = item.ExternalId.ToString(),
                Path = modifiedUrl,
                Protocol = MediaProtocol.Http,
                Name = item.Name,
                RequiresOpening = false,
                SupportsDirectStream = true,
                SupportsDirectPlay = true,
                SupportsTranscoding = true,
                UseMostCompatibleTranscodingProfile = true,
            };

            _logger.LogInformation("Modified stream URL from {OriginalUrl} to {ModifiedUrl}", originalUrl, modifiedUrl);
            return Task.FromResult<IEnumerable<MediaSourceInfo>>(new[] { mediaSource });
        }

        // Return empty list for items without a valid URL
        _logger.LogWarning(
            "No valid media source or path for item {ItemName}; returning empty media sources",
            item.Name
        );
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

    private async Task<string> ModifyStreamUrl(string originalUrl, BaseItem item, CancellationToken cancellationToken)
    {
        // Fallback to original URL if anything fails
        try
        {
            // Get user ID (try to resolve from session or fallback to "unknown")
            string userId = "unknown";
            try
            {
                // Attempt to get user ID from session context
                var sessions = _sessionManager.Sessions.Where(s => s.NowPlayingItem?.Id == item.Id).ToList();
                if (sessions.Count > 0)
                {
                    var user = _userManager.GetUserById(sessions.First().UserId);
                    userId = user?.Id.ToString() ?? "unknown";
                }
                _logger.LogInformation("Resolved user ID '{UserId}' for item {ItemName}", userId, item.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to retrieve user ID for item {ItemName}: {Error}", item.Name, ex.Message);
            }

            // Prepare POST request body with user ID and original URL
            var requestBody = new { OriginalUrl = originalUrl, UserId = userId };
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // External service URL
            var externalServiceUrl = "http://192.168.0.13:8585/hook";
            _logger.LogInformation(
                "Sending POST request to {ServiceUrl} with userId {UserId}",
                externalServiceUrl,
                userId
            );

            // Make the POST request
            var response = await _httpClient.PostAsync(externalServiceUrl, jsonContent, cancellationToken);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                var modifiedUrl = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(modifiedUrl))
                {
                    return modifiedUrl.Trim();
                }
                _logger.LogWarning(
                    "External service returned empty URL; falling back to original URL: {OriginalUrl}",
                    originalUrl
                );
                return originalUrl;
            }

            _logger.LogWarning(
                "External service returned status {StatusCode}; falling back to original URL: {OriginalUrl}",
                response.StatusCode,
                originalUrl
            );
            return originalUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying stream URL {OriginalUrl}; falling back to original", originalUrl);
            return originalUrl;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Dispose managed resources
            _httpClient?.Dispose();
        }

        // No native resources to dispose
        _disposed = true;
    }
}

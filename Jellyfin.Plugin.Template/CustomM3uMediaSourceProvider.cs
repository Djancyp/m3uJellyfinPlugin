using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Template.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Template;

public class CustomM3uMediaSourceProvider : IMediaSourceProvider, IDisposable
{
    private readonly PluginConfiguration _configuration;
    private readonly ILogger<CustomM3uMediaSourceProvider> _logger;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public CustomM3uMediaSourceProvider(
        ILogger<CustomM3uMediaSourceProvider> logger,
        IUserManager userManager,
        ISessionManager sessionManager,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _logger = logger;
        _userManager = userManager;
        _sessionManager = sessionManager;
        _httpContextAccessor = httpContextAccessor;
        _httpClient = new HttpClient();
        _configuration = Plugin.Instance!.Configuration;
        _disposed = false;
    }

    public async Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CustomM3uMediaSourceProvider: Processing media sources for item {ItemName}", item.Name);
        _logger.LogInformation("Item Type: {ItemType}, Item ID: {ItemId}", item.GetType().Name, item.Id);
        _logger.LogInformation("Item Path: {ItemPath}", item.Path ?? "null");

        var originalUrl = item.Path ?? string.Empty;
        var modifiedUrl = await ModifyStreamUrl(originalUrl, item, cancellationToken);

        // get the full item from item.id

        if (string.IsNullOrEmpty(modifiedUrl))
        {
            _logger.LogWarning("Modified URL is empty for item {ItemName}; returning empty media sources", item.Name);
            return Array.Empty<MediaSourceInfo>();
        }

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
            UseMostCompatibleTranscodingProfile = true,
        };

        _logger.LogInformation(
            "Providing modified stream URL {ModifiedUrl} for item {ItemName}",
            modifiedUrl,
            item.Name
        );
        return new[] { mediaSource };
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
            User? user = null;
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                user = _userManager.GetUserByName(username);
            }

            if (user == null)
            {
                // Fallback to session manager if user is not in http context
                var sessions = _sessionManager.Sessions.Where(s => s.NowPlayingItem?.Id == item.Id).ToList();
                if (sessions.Count > 0)
                {
                    user = _userManager.GetUserById(sessions.First().UserId);
                }
            }

            var userId = user?.Id.ToString() ?? "unknown";
            _logger.LogInformation("Resolved user ID '{UserId}' for item {ItemName}", userId, item.Name);

            // Prepare POST request body with user ID and original URL
            var requestBody = new
            {
                OriginalUrl = originalUrl,
                UserId = userId,
                ChannelId = item.Id,
                ChannelName = item.Name,
            };
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // External service URL
            var externalServiceUrl = _configuration.M3UUrl;
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

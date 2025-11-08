using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[ConfigAuthorize]
[Route("jellio/{config}/jellyseerr")]
public class RequestController : ControllerBase
{
    // Simple in-memory cache to prevent duplicate requests (userId:imdbId:type -> timestamp)
    private static readonly ConcurrentDictionary<string, DateTime> _requestCache = new();
    private static readonly ConcurrentDictionary<string, object> _requestLocks = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

    private static bool TryMarkAsProcessing(Guid userId, string identifier, string type)
    {
        var cacheKey = $"{userId}:{identifier}:{type}";
        var lockObj = _requestLocks.GetOrAdd(cacheKey, _ => new object());
        
        lock (lockObj)
        {
            if (_requestCache.TryGetValue(cacheKey, out var timestamp))
            {
                if (DateTime.UtcNow - timestamp < _cacheDuration)
                {
                    Console.WriteLine($"[Jellyseerr] Skipping duplicate request (cached {(DateTime.UtcNow - timestamp).TotalSeconds:F1}s ago)");
                    return false; // Already requested
                }
            }
            
            // Mark as being processed NOW
            _requestCache[cacheKey] = DateTime.UtcNow;
            return true; // OK to process
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, string? apiKey)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
        client.Timeout = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            // API keys from the config are stored in plain text, use directly
            Console.WriteLine($"[Jellyseerr] Using API key: {apiKey.Substring(0, Math.Min(8, apiKey.Length))}... (length: {apiKey.Length})");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
        else
        {
            Console.WriteLine("[Jellyseerr] WARNING: No API key provided!");
        }

        return client;
    }

    [HttpGet]
    public async Task<IActionResult> CreateRequest(
        [ConfigFromBase64Json] ConfigModel? config,
        [FromQuery] string type,
        [FromQuery] int? tmdbId,
        [FromQuery] string? imdbId,
        [FromQuery] string? title,
        [FromQuery] int? season,
        [FromQuery] int? episode
    )
    {
        try
        {
            Console.WriteLine($"[Jellyseerr] Request received: type={type}, tmdbId={tmdbId}, imdbId={imdbId}, title={title}");
            
            if (config is null)
            {
                Console.WriteLine("[Jellyseerr] ERROR: Config is null");
                return BadRequest("Invalid or missing configuration.");
            }

            // Get userId from context (set by ConfigAuthorize filter)
            var userId = (Guid?)HttpContext.Items["JellioUserId"];
            if (userId == null)
            {
                Console.WriteLine("[Jellyseerr] ERROR: No user ID in context");
                return Unauthorized();
            }

            // Check for duplicate request (with lock to prevent race condition)
            var identifier = imdbId ?? tmdbId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? title ?? "unknown";
            if (!TryMarkAsProcessing(userId.Value, identifier, type))
            {
                return Content("✓ Request already sent (duplicate prevented)", "text/plain");
            }

            Console.WriteLine($"[Jellyseerr] Config loaded: Enabled={config.JellyseerrEnabled}, Url={config.JellyseerrUrl}, HasApiKey={!string.IsNullOrWhiteSpace(config.JellyseerrApiKey)}");

            if (!config.JellyseerrEnabled || string.IsNullOrWhiteSpace(config.JellyseerrUrl))
            {
                Console.WriteLine("[Jellyseerr] ERROR: Jellyseerr not configured or disabled");
                return BadRequest("Jellyseerr is not configured.");
            }

            int? maybeTmdbId = tmdbId;

            using var client = CreateHttpClient(config.JellyseerrUrl!, config.JellyseerrApiKey);

            // Resolve TMDB ID via Jellyseerr search if not provided
            if (maybeTmdbId is null)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine("[Jellyseerr] ERROR: No tmdbId or title provided");
                    return Problem("Either tmdbId or title parameter is required.", statusCode: 400);
                }

                Console.WriteLine($"[Jellyseerr] Searching Jellyseerr for title: {title}");
                var searchUri = $"api/v1/search?query={Uri.EscapeDataString(title!)}";
                using var resp = await client.GetAsync(searchUri);
                Console.WriteLine($"[Jellyseerr] Search response status: {resp.StatusCode}");
                
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
                    if (doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"[Jellyseerr] Found {results.GetArrayLength()} search results");
                        foreach (var el in results.EnumerateArray())
                        {
                            var mediaType = el.TryGetProperty("mediaType", out var mt) ? mt.GetString() : null;
                            if (!string.IsNullOrEmpty(mediaType) && string.Equals(mediaType, type, StringComparison.OrdinalIgnoreCase))
                            {
                                if (el.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var idVal))
                                {
                                    maybeTmdbId = idVal;
                                    Console.WriteLine($"[Jellyseerr] Matched TMDB ID: {idVal}");
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Jellyseerr] Search failed: {errorContent}");
                }
            }

            if (maybeTmdbId is null)
            {
                Console.WriteLine("[Jellyseerr] ERROR: Could not resolve TMDB ID");
                return Problem("Unable to resolve TMDB id for request.", statusCode: 502);
            }

            int id = maybeTmdbId.Value;
            Console.WriteLine($"[Jellyseerr] Using TMDB ID: {id}");
            
            bool isTV = string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase);
            
            // Build request body - only include seasons for TV shows
            object body;
            if (isTV)
            {
                int[]? seasons = null;
                if (season.HasValue)
                {
                    seasons = new[] { season.Value };
                    Console.WriteLine($"[Jellyseerr] Requesting TV season: {season.Value}");
                }
                
                body = new
                {
                    mediaType = "tv",
                    mediaId = id,
                    seasons
                };
            }
            else
            {
                // For movies, don't include seasons field at all
                body = new
                {
                    mediaType = "movie",
                    mediaId = id
                };
            }

            Console.WriteLine($"[Jellyseerr] Sending request to Jellyseerr: {config.JellyseerrUrl}/api/v1/request");
            using var createResp = await client.PostAsJsonAsync("api/v1/request", body);
            Console.WriteLine($"[Jellyseerr] Request response status: {createResp.StatusCode}");
            
            if (createResp.IsSuccessStatusCode)
            {
                Console.WriteLine("[Jellyseerr] ✓ Request successful!");
                
                // Already marked in cache at the start, no need to mark again
                
                // Return a simple success message
                // Stremio will attempt to play this URL, fail gracefully, but the request is already sent
                return Content("✓ Content request sent to Jellyseerr successfully!", "text/plain");
            }

            var failContent = await createResp.Content.ReadAsStringAsync();
            Console.WriteLine($"[Jellyseerr] ERROR: Request failed with: {failContent}");
            return Problem($"Jellyseerr request failed with status {(int)createResp.StatusCode}.", statusCode: 502);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Jellyseerr] EXCEPTION: {ex.Message}");
            Console.WriteLine($"[Jellyseerr] Stack trace: {ex.StackTrace}");
            return Problem($"Error creating Jellyseerr request: {ex.Message}", statusCode: 500);
        }
    }
}

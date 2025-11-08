using System;
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
    private static HttpClient CreateHttpClient(string baseUrl, string? apiKey)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
        client.Timeout = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                // Decode the base64-encoded API key
                var decodedKey = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(apiKey));
                client.DefaultRequestHeaders.Add("X-Api-Key", decodedKey);
            }
            catch
            {
                // If decoding fails, use the key as-is (might already be decoded)
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            }
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
            
            int[]? seasons = null;
            if (string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase))
            {
                // Always request the entire season if a season is specified
                if (season.HasValue)
                {
                    seasons = new[] { season.Value };
                    Console.WriteLine($"[Jellyseerr] Requesting TV season: {season.Value}");
                }
            }

            var body = new
            {
                mediaType = string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase) ? "tv" : "movie",
                mediaId = id,
                seasons
            };

            Console.WriteLine($"[Jellyseerr] Sending request to Jellyseerr: {config.JellyseerrUrl}/api/v1/request");
            using var createResp = await client.PostAsJsonAsync("api/v1/request", body);
            Console.WriteLine($"[Jellyseerr] Request response status: {createResp.StatusCode}");
            
            if (createResp.IsSuccessStatusCode)
            {
                Console.WriteLine("[Jellyseerr] ✓ Request successful!");
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

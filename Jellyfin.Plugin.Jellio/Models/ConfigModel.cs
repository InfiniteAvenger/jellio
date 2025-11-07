using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Jellio.Models;

public class ConfigModel
{
    public required string ServerName { get; init; }
    public required string AuthToken { get; init; }

    public required IReadOnlyList<Guid> LibrariesGuids { get; init; }

    // Jellyseerr integration
    public bool JellyseerrEnabled { get; init; } = false;
    public string? JellyseerrUrl { get; init; }
    public string? JellyseerrApiKey { get; init; }
}

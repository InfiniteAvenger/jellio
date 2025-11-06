using System.Net.Mime;
using Jellyfin.Plugin.Jellio.Helpers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[Route("jellio")]
public class WebController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly IUserViewManager _userViewManager;
    private readonly IDtoService _dtoService;
    private readonly IServerApplicationHost _serverApplicationHost;

    public WebController(
        IUserManager userManager,
        IUserViewManager userViewManager,
        IDtoService dtoService,
        IServerApplicationHost serverApplicationHost
    )
    {
        _userManager = userManager;
        _userViewManager = userViewManager;
        _dtoService = dtoService;
        _serverApplicationHost = serverApplicationHost;
    }

    [HttpGet("server-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces(MediaTypeNames.Application.Json)]
    public IActionResult GetServerInfo()
    {
        // Get user from the X-Emby-Token header (sent by ApiClient in Jellyfin web UI)
        var userId = RequestHelpers.GetCurrentUserId(User);
        if (userId == null)
        {
            return Unauthorized(new { error = "Not authenticated. Please access this page through Jellyfin's Dashboard." });
        }

        var friendlyName = _serverApplicationHost.FriendlyName;
        var libraries = LibraryHelper.GetUserLibraries(userId.Value, _userManager, _userViewManager, _dtoService);

        return Ok(new { name = friendlyName, libraries });
    }
}

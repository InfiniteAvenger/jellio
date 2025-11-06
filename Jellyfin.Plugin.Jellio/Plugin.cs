using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Jellio;

public class Plugin : BasePlugin<BasePluginConfiguration>
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Jellio";

    public override Guid Id => Guid.Parse("e874be83-fe36-4568-abac-f5ce0574b409");

    public static Plugin? Instance { get; private set; }
}

# Jellio+
[![Release](https://img.shields.io/github/v/release/InfiniteAvenger/jellio-plus)](https://github.com/InfiniteAvenger/jellio-plus/releases)

Stream your Jellyfin library directly in Stremio with seamless integration

Jellio+ is a modern fork of [Vanchaxy's original Jellio plugin](https://github.com/vanchaxy/jellio), updated and enhanced for **Jellyfin 10.11.x** compatibility. This plugin creates a bridge between your Jellyfin media server and Stremio, allowing you to stream your personal media library directly within the Stremio interface.

## Features

This fork is updated for **Jellyfin 10.11.2**.

- **Full Library Integration** - Access your entire Jellyfin movie and TV show collection in Stremio

- **Cross-Platform** - Works on all Stremio-supported devices (Windows, macOS, Linux, Android, iOS)

- **Jellyseer Integration** - Optional integration with Jellyseer for content requests
  
- **Multiple Formats** - Supports various video codecs and quality options.


## How it Works:



### Browsing Your Library in Stremio

Jellio+ allows you to instantly stream media from your Jellyfin server through Stremio. Simply search for the media in Stremio, and if it is on your Jellyfin server, it will apear!

![Jellio Streaming in Stremio](assets/jellio-stream.PNG)

### Jellyseer Integration

Enable the optional Jellyseer functionality to be able to directly request media to be sent to Jellyseer with a simple in-app solution.

![Jellyseer Integration](assets/jellyseer-integration.PNG)  


### Installation:

NOTICE: It is recommended to have both your Jellyfin and Jellyseer instances accessilbe over HTTPS by using a Cloudflare tunnel. This is the only way I have tested it so far.

1. Go Open Jellyfin Dashboard > Plugins > Manage Repositories
2. Click "New Repository" and add "Jellio" for the name, and "https://raw.githubusercontent.com/InfiniteAvenger/jellio-plus/metadata/jellyfin-repo-manifest.json" for the repository url
3. Go back to Plugins, and under "All" find and install Jellio
4. Restart Jellyfin
5. Jellyfin Dashboard > Plugins > Installed > Jellio and then click "Settings"
6. Select which libraries you want to be included in Stremio
7. (Optional) Input your local Jellyseer url (ex. http://192.168.0.105:5055" and your Jellyseer API key. Also include your Public URL for Jellyfin (ex. https://jellyfin.yourserver.com)
8. Click "Save Configuration for Jellyfin"
9. Lastly, click "Install." Copy that link and paste it in your Stremio addons. You're all done!

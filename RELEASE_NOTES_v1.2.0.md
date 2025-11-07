# Jellio v1.2.0 Release Notes

## 🎉 New Features

### Jellyseerr Integration
- **Automatic Content Requests**: When a movie or TV episode isn't available in your Jellyfin library, Jellio now displays a "Request via Jellyseerr" option in Stremio
- **No Redirects**: Clicking the request link opens it externally without redirecting you away from Stremio
- **Smart TMDB Resolution**: Automatically resolves TMDB IDs from IMDB IDs using Jellyseerr's search API
- **Season-level Requests**: For TV shows, requests entire seasons (not just individual episodes)

### Public Base URL Configuration
- **HTTPS Proxy Support**: Added optional "Public Base URL" field for Jellyfin instances behind reverse proxies or tunnels (Cloudflare, Nginx, etc.)
- **Solves SSL Issues**: If your Jellyfin runs behind a proxy/tunnel with HTTPS but the backend sees HTTP, configure your public HTTPS URL to ensure request links work correctly
- **Example Use Case**: Jellyfin at `https://jellyfin.example.com` via Cloudflare Tunnel

## 🔧 Configuration

### Setting Up Jellyseerr
1. Enable Jellyseerr integration in plugin config
2. Enter your Jellyseerr Base URL (e.g., `https://jellyseerr.example.com`)
3. Add your Jellyseerr API Key (found in Jellyseerr Settings → API)
4. **(Optional)** If behind HTTPS proxy: Set Public Base URL to your external domain (e.g., `https://jellyfin.example.com`)

### How It Works
1. User browses content in Stremio via Jellio
2. If content isn't in Jellyfin library, "Request via Jellyseerr" option appears
3. Clicking opens request in browser, automatically creating the request in Jellyseerr
4. No need to manually search or submit requests!

## 📋 What's Changed
- Added `RequestController` for Jellyseerr API integration
- Added `PublicBaseUrl` to `ConfigModel` for proxy/tunnel support
- Updated UI with Jellyseerr configuration fields
- Modified stream endpoints to show request links when content unavailable
- Updated `GetBaseUrl` method to use PublicBaseUrl override when configured

## 🔗 Installation
Install from the Jellyfin plugin catalog or manually:
1. Download the plugin ZIP from this release
2. Extract to your Jellyfin plugins directory
3. Restart Jellyfin
4. Configure the plugin at `https://your-jellyfin/jellio`

## 🎯 Target Platform
- **Jellyfin**: 10.11.0+
- **.NET**: 9.0

---

**Full Changelog**: https://github.com/InfiniteAvenger/jellio-jellyfin10.11.x/compare/v1.1.0...v1.2.0

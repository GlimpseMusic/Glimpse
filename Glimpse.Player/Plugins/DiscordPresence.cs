using System;
using System.Threading.Tasks;
using DiscordRPC;
using Glimpse.Player.Configs;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.CoverArt;
using MetaBrainz.MusicBrainz.CoverArt.Interfaces;
using MetaBrainz.MusicBrainz.Interfaces.Searches;

namespace Glimpse.Player.Plugins;

public class DiscordPresence : Plugin
{
    private AudioPlayer _player;
    
    private static string _currentUrl;

    public static DiscordConfig Config;
    
    public static DiscordRpcClient Client;
    
    public override void Initialize(AudioPlayer player)
    {
//#if DEBUG
//        return;
//#endif

        _player = player;
        
        Client = new DiscordRpcClient("1280266653950804111");
        
        if (!IConfig.TryGetConfig("Discord", out Config))
        {
            Config = new DiscordConfig();
            IConfig.WriteConfig("Discord", Config);
        }
        
        Client.Initialize();
        
        player.TrackChanged += PlayerOnTrackChanged;
        player.StateChanged += PlayerOnStateChanged;
    }

    private void PlayerOnTrackChanged(TrackInfo info)
    {
        SetPresence(info, _player.ElapsedSeconds, _player.TrackLength);
    }

    void PlayerOnStateChanged(TrackState state)
    {
        switch (state)
        {
            case TrackState.Playing:
                SetPresence(_player.TrackInfo, _player.ElapsedSeconds, _player.TrackLength);
                break;
            
            case TrackState.Paused:
            case TrackState.Stopped:
                Client.ClearPresence();
                break;
        }
    }

    public void SetPresence(TrackInfo info, int currentSecond, int totalSeconds)
    {
        DateTime now = DateTime.UtcNow;
        
        RichPresence presence = new RichPresence()
            .WithDetails(info.Title)
            .WithState(info.Artist)
            .WithTimestamps(new Timestamps(now - TimeSpan.FromSeconds(currentSecond), now + TimeSpan.FromSeconds(totalSeconds)))
            .WithAssets(new Assets() { LargeImageText = info.Album, LargeImageKey = _currentUrl });
        
        Client.SetPresence(presence);
        
        string albumName = info.Album;

        int startIndex = albumName.IndexOf("disc", StringComparison.OrdinalIgnoreCase);

        if (startIndex != -1)
        {
            int endIndex;
            bool foundNumber = false;

            for (endIndex = startIndex + "disc".Length; endIndex < albumName.Length; endIndex++)
            {
                char c = albumName[endIndex];

                if (c is not ' ' && c is < '0' or > '9')
                    break;

                foundNumber = true;
            }

            if (foundNumber)
                albumName = albumName.Remove(startIndex, endIndex - startIndex).Replace("()", "").Replace("[]", "").Trim();
        }

        if (Config.AlbumArt.TryGetValue(albumName, out _currentUrl))
        {
            Client.UpdateLargeAsset(_currentUrl);
            return;
        }

        // Only search for new album art if the album changes or the URL is null.
        // This saves queries to musicbrainz.
        if (info.Album != TrackInfo.UnknownAlbum)
        {
            Task.Run(() =>
            {
                const string app = "GlimpseAudioPlayer";
                const string contact = "https://github.com/aquagoose";
                const string version = "0.0.0-dev";

                using Query query = new Query(app, version, contact);
                var releases = query.FindReleases(albumName, 5);
                using CoverArt art = new CoverArt(app, version, contact);

                foreach (ISearchResult<MetaBrainz.MusicBrainz.Interfaces.Entities.IRelease> release in releases.Results)
                {
                    IImage image = null;

                    try
                    {
                        foreach (IImage img in art.FetchReleaseIfAvailable(release.Item.Id)?.Images)
                        {
                            if (img.Front)
                            {
                                image = img;
                                break;
                            }
                        }
                    }
                    catch (Exception) { }

                    if (image is not null)
                    {
                        _currentUrl = image.Location?.ToString();
                        Config.AlbumArt[albumName] = _currentUrl;
                        IConfig.WriteConfig("Discord", Config);
                        break;
                    }
                }
                
                Client.UpdateLargeAsset(_currentUrl);
            });
        }
    }

    public override void Dispose()
    {
        Client.Dispose();
    }
}
using Glimpse.Api;
using TagLib;

namespace Glimpse.Player;

public static class Utils
{
    public static TrackInfo TrackInfoFromTags(string path)
    {
        using File file = File.Create(path);

        uint trackNumber = file.Tag.Track;
        string title = file.Tag.Title ?? TrackInfo.UnknownTitle;
        string artist = file.Tag.Performers is { Length: > 0 } ? file.Tag.Performers[0] : TrackInfo.UnknownArtist;
        string album = file.Tag.Album ?? TrackInfo.UnknownAlbum;

        TrackInfo.Image albumArt = null;
        
        if (file.Tag.Pictures is { Length: > 0 })
        {
            IPicture picture = file.Tag.Pictures[0];

            albumArt = new TrackInfo.Image(picture.Data?.Data, picture.Filename);
        }

        return new TrackInfo(trackNumber, title, artist, album, albumArt);
    }
}
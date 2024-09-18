namespace Glimpse.Api;

public class TrackInfo
{
    public const string UnknownTitle = "Unknown Title";
    
    public const string UnknownArtist = "Unknown Artist";
    
    public const string UnknownAlbum = "Unknown Album";

    public readonly uint? TrackNumber;
    
    public readonly string Title;

    public readonly string Artist;

    public readonly string Album;

    public readonly Image AlbumArt;

    public TrackInfo(uint? trackNumber, string title, string artist, string album, Image albumArt)
    {
        TrackNumber = trackNumber;
        Title = title;
        Artist = artist;
        Album = album;
        AlbumArt = albumArt;
    }

    public class Image
    {
        public byte[] Data;
        
        public string Location;

        public Image(byte[] data, string location)
        {
            Data = data;
            Location = location;
        }
    }
}
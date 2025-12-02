namespace Glimpse.API.Database;

public class Album
{
    public string Name;

    public List<string> Tracks;

    public Album(string name)
    {
        Name = name;
        Tracks = new List<string>();
    }
}
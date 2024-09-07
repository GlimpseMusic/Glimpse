using System.Collections.Generic;

namespace Glimpse.Database;

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
using System.Collections.Generic;

namespace Glimpse.Database;

public class Album
{
    public string Location;
    
    public string Name;

    public string[] Tracks;

    public Album(string location, string name, string[] tracks)
    {
        Location = location;
        Name = name;
        Tracks = tracks;
    }
}
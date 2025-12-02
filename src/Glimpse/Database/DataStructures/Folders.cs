using Glimpse.API;

namespace Glimpse.Database.DataStructures;

public class Folders : IConfig
{
    public List<string> AddedFolders;

    public Folders()
    {
        AddedFolders = [];
    }
    
    public Folders(List<string> addedFolders)
    {
        AddedFolders = addedFolders;
    }
}
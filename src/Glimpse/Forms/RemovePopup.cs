using Glimpse.Audio;
using Glimpse.Database;
using Glimpse.Locales;
using Hexa.NET.ImGui;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public sealed class RemovePopup : Popup
{
    private readonly string _nameOrPath;
    private readonly bool _isAlbum;
    private readonly bool _delete;

    private bool _removeSongs;
    
    public RemovePopup(string nameOrPath, bool isAlbum, bool delete)
    {
        _nameOrPath = nameOrPath;
        _isAlbum = isAlbum;
        _delete = delete;
        _removeSongs = isAlbum && delete;
    }
    
    public override void Update()
    {
        Locale locale = Glimpse.Locale;
        string popupName = locale.GetString("Popup.Remove.Name");
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);

        if (ImGui.BeginPopupModal(popupName, ImGuiWindowFlags.AlwaysAutoResize))
        {
            DatabaseManager db = Glimpse.Database;
            AudioPlayer player = Glimpse.Player;
            
            string name = _isAlbum ? _nameOrPath : (db.Tracks[_nameOrPath].Title ?? locale.GetString("UnknownTrack"));

            if (_delete)
            {
                ImGui.TextUnformatted(locale.GetString(_isAlbum ? "Popup.Remove.Menu.DeleteAlbumWarning" : "Popup.Remove.DeleteTrackWarning", name));
            }
            else
                ImGui.TextUnformatted(locale.GetString("Popup.Remove.Confirmation", name));

            if (_isAlbum && !_delete)
                ImGui.Checkbox(locale.GetString("Popup.Remove.SongsConfirmation"), ref _removeSongs);

            if (ImGui.Button(locale.GetString("Button.Yes")))
            {
                if (_isAlbum)
                {
                    if (db.Albums.Remove(_nameOrPath, out Album album) && _removeSongs)
                    {
                        foreach (string track in album.Tracks)
                        {
                            if (track == player.CurrentTrackPath)
                                player.Stop();
                            db.Tracks.Remove(track);
                            if (_delete)
                                File.Delete(track);
                        }
                    }
                }
                else
                {
                    if (_nameOrPath == player.CurrentTrackPath)
                        player.Stop();
                    
                    if (db.Tracks.Remove(_nameOrPath, out Track track) && track.Album != null)
                        db.Albums[track.Album].Tracks.Remove(_nameOrPath);
                    
                    if (_delete)
                        File.Delete(_nameOrPath);
                }
                
                db.Refresh();
                Close();
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button(locale.GetString("Button.No")))
                Close();
            
            ImGui.EndPopup();
        }
    }
}
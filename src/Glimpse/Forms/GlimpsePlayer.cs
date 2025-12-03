using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text.Json.Nodes;
using Glimpse.API;
using Glimpse.API.Database;
using Glimpse.Audio;
using Glimpse.Database;
using Glimpse.Locales;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using Newtonsoft.Json.Linq;
using Color = System.Drawing.Color;
using Image = Glimpse.Graphics.Image;
using Track = Glimpse.API.Database.Track;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private bool _init;

    private Version? _newVersion;
    private string? _newVersionURL;
    private float _newVersionBlinker;
    
    private string? _currentAlbum;
    private Album[] _albumList;
    private Track[] _currentTrackList;
    private int _seekPosition;

    private Image _playButton;
    private Image _pauseButton;
    private Image _skipButton;
    private Image _stopButton;
    private Image _plusButton;
    private Image _cogButton;
    private Image _bugButton;
    private Image _updateButton;

    private Image _defaultAlbumArt;
    private Image? _albumArt;

    private byte[]? _newAlbumArt;
    private bool _shouldDeleteArt;

    private bool _hasIncrementedPlayCount;
    
    public GlimpsePlayer()
    {
#if DEBUG
        Title = "Glimpse DEBUG";
#else
        Title = "Glimpse";
#endif
        Size = new Size(1100, 650);
    }

    protected override void Initialize()
    {
        _playButton = Renderer.CreateImage("Assets/Icons/PlayButton.png");
        _pauseButton = Renderer.CreateImage("Assets/Icons/PauseButton.png");
        _skipButton = Renderer.CreateImage("Assets/Icons/SkipButton.png");
        _stopButton = Renderer.CreateImage("Assets/Icons/StopButton.png");
        _plusButton = Renderer.CreateImage("Assets/Icons/Plus.png");
        _cogButton = Renderer.CreateImage("Assets/Icons/Cog.png");
        _bugButton = Renderer.CreateImage("Assets/Icons/Bug.png");
        _updateButton = Renderer.CreateImage("Assets/Icons/Update.png");
        
        _defaultAlbumArt = Renderer.CreateImage("Assets/Icons/Glimpse.png");
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
        Glimpse.Player.StateChanged += PlayerOnStateChanged;
        Glimpse.Platform.ButtonPressed += PlatformOnButtonPressed;
        
        ImFontPtr roboto = Renderer.ImGui.AddFont("Assets/Fonts/Roboto-Regular.ttf", 18, "Roboto-20px");
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.FontDefault = roboto;

        ImGuiStylePtr style = ImGui.GetStyle();
        int rounding = (int) (5 * Scale);
        style.FrameRounding = rounding;
        style.GrabRounding = rounding;
        style.ChildRounding = rounding;
        style.PopupRounding = rounding;
        style.DockingSeparatorSize = (int) float.Ceiling(1 * Scale);
        
        Span<Vector4> colors = style.Colors;
        colors[(int) ImGuiCol.Text]                   = new Vector4(0.93f, 0.93f, 0.93f, 1.00f);
        colors[(int) ImGuiCol.TextDisabled]           = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int) ImGuiCol.WindowBg]               = new Vector4(0.12f, 0.12f, 0.14f, 0.94f);
        colors[(int) ImGuiCol.ChildBg]                = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.PopupBg]                = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
        colors[(int) ImGuiCol.Border]                 = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
        colors[(int) ImGuiCol.BorderShadow]           = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.FrameBg]                = new Vector4(0.16f, 0.29f, 0.48f, 0.54f);
        colors[(int) ImGuiCol.FrameBgHovered]         = new Vector4(0.26f, 0.59f, 0.98f, 0.40f);
        colors[(int) ImGuiCol.FrameBgActive]          = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        colors[(int) ImGuiCol.TitleBg]                = new Vector4(0.04f, 0.04f, 0.04f, 1.00f);
        colors[(int) ImGuiCol.TitleBgActive]          = new Vector4(0.16f, 0.29f, 0.48f, 1.00f);
        colors[(int) ImGuiCol.TitleBgCollapsed]       = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);
        colors[(int) ImGuiCol.MenuBarBg]              = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int) ImGuiCol.ScrollbarBg]            = new Vector4(0.02f, 0.02f, 0.02f, 0.20f);
        colors[(int) ImGuiCol.ScrollbarGrab]          = new Vector4(0.44f, 0.53f, 0.64f, 0.71f);
        colors[(int) ImGuiCol.ScrollbarGrabHovered]   = new Vector4(0.44f, 0.53f, 0.64f, 1.00f);
        colors[(int) ImGuiCol.ScrollbarGrabActive]    = new Vector4(0.26f, 0.93f, 0.59f, 1.00f);
        colors[(int) ImGuiCol.CheckMark]              = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.SliderGrab]             = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.SliderGrabActive]       = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.Button]                 = new Vector4(1.00f, 0.69f, 0.22f, 0.78f);
        colors[(int) ImGuiCol.ButtonHovered]          = new Vector4(0.62f, 0.93f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.ButtonActive]           = new Vector4(0.06f, 0.53f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.Header]                 = new Vector4(0.23f, 0.66f, 0.87f, 0.16f);
        colors[(int) ImGuiCol.HeaderHovered]          = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.HeaderActive]           = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        //colors[(int) ImGuiCol.Separator]              = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
        colors[(int) ImGuiCol.SeparatorHovered]       = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
        colors[(int) ImGuiCol.SeparatorActive]        = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        colors[(int) ImGuiCol.ResizeGrip]             = new Vector4(0.26f, 0.59f, 0.98f, 0.20f);
        colors[(int) ImGuiCol.ResizeGripHovered]      = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        colors[(int) ImGuiCol.ResizeGripActive]       = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        colors[(int) ImGuiCol.TabHovered]             = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        colors[(int) ImGuiCol.Tab]                    = new Vector4(0.27f, 0.27f, 0.27f, 0.78f);
        colors[(int) ImGuiCol.TabSelected]            = new Vector4(0.23f, 0.66f, 0.87f, 1.00f);
        colors[(int) ImGuiCol.TabSelectedOverline]    = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TabDimmed]              = new Vector4(0.07f, 0.10f, 0.15f, 0.97f);
        colors[(int) ImGuiCol.TabDimmedSelected]      = new Vector4(0.14f, 0.26f, 0.42f, 1.00f);
        colors[(int) ImGuiCol.TabDimmedSelectedOverline]  = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int) ImGuiCol.DockingPreview]         = new Vector4(0.26f, 0.59f, 0.98f, 0.70f);
        colors[(int) ImGuiCol.DockingEmptyBg]         = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        colors[(int) ImGuiCol.PlotLines]              = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        colors[(int) ImGuiCol.PlotLinesHovered]       = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        colors[(int) ImGuiCol.PlotHistogram]          = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.PlotHistogramHovered]   = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.TableHeaderBg]          = new Vector4(0.19f, 0.19f, 0.26f, 1.00f);
        colors[(int) ImGuiCol.TableBorderStrong]      = new Vector4(0.31f, 0.31f, 0.35f, 1.00f);
        colors[(int) ImGuiCol.TableBorderLight]       = new Vector4(0.23f, 0.23f, 0.25f, 1.00f);
        colors[(int) ImGuiCol.TableRowBg]             = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.TableRowBgAlt]          = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        colors[(int) ImGuiCol.TextLink]               = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TextSelectedBg]         = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        colors[(int) ImGuiCol.DragDropTarget]         = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
        colors[(int) ImGuiCol.NavWindowingHighlight]  = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int) ImGuiCol.NavWindowingDimBg]      = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        colors[(int) ImGuiCol.ModalWindowDimBg]       = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);

        ChangeAlbum(null);
        
        if (Glimpse.Database.Folders.Count == 0)
            AddPopup(new AddFolderPopup());

#if !DEBUG
        Task.Run(CheckForNewerVersion);
#endif
    }

    protected override unsafe void Update()
    {
        Locale locale = Glimpse.Locale;
        
        if (_newAlbumArt != null)
        {
            _albumArt?.Dispose();
            try
            {
                _albumArt = Renderer.CreateImage(_newAlbumArt);
            }
            catch (Exception e)
            {
                Glimpse.Logger.Log($"Failed to load album art: {e}");
            }

            _newAlbumArt = null;
        }
        else if (_shouldDeleteArt)
        {
            _shouldDeleteArt = false;
            
            _albumArt?.Dispose();
            _albumArt = null;
        }
        
        AudioPlayer player = Glimpse.Player;
        
        Renderer.Clear(Color.Black);
        
/*#if DEBUG
        if (ImGui.BeginMainMenuBar())
        {
            ImGui.Text("DEBUG Menu");

            ImGui.Spacing();
            
            if (ImGui.MenuItem("Style Editor"))
                AddPopup(new StyleEditorPopup());
            
            if (ImGui.MenuItem("Settings"))
                AddPopup(new SettingsPopup());
            
            ImGui.EndMainMenuBar();
        }
#endif*/
        
        //ImGui.ShowStyleEditor();
        
        uint id = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode | (ImGuiDockNodeFlags) (1 << 12));
        //ImGui.SetNextWindowDockID(id, ImGuiCond.Once);
        
        if (!_init)
        {
            _init = true;
            
            ImGuiP.DockBuilderRemoveNode(id);
            ImGuiP.DockBuilderAddNode(id, ImGuiDockNodeFlags.NoUndocking);

            uint outId = id;

            ImGuiDir dir = ImGuiDir.Down;
            float sizeRatio = 0.18f;

            if (Glimpse.Config.SwapTransportControls)
            {
                dir = ImGuiDir.Up;
                sizeRatio = 0.19f;
            }
            
            uint transportId;
            uint transportDock = ImGuiP.DockBuilderSplitNode(outId, dir, sizeRatio, &transportId, &outId);

            ImGuiDockNodePtr node = ImGuiP.DockBuilderGetNode(transportId);
            node.LocalFlags |= ImGuiDockNodeFlags.NoResize;
            
            uint foldersDock = ImGuiP.DockBuilderSplitNode(outId, ImGuiDir.Left, 0.3f, null, &outId);
            
            ImGuiP.DockBuilderDockWindow("Transport", transportDock);
            ImGuiP.DockBuilderDockWindow("Albums", foldersDock);
            ImGuiP.DockBuilderDockWindow("Songs", outId);
        
            ImGuiP.DockBuilderFinish(id);
        }

        if (ImGui.Begin("Transport", ImGuiWindowFlags.NoResize))
        {
            Vector2 winSize = ImGui.GetContentRegionAvail();

            ImGui.BeginChild("AlbumArt", new Vector2(winSize.Y));
            {
                ImGui.Image(_albumArt?.ID ?? _defaultAlbumArt.ID, new Vector2(winSize.Y));
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            ImGui.BeginChild("MainThing");

            ImGui.BeginChild("TrackInfo", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
            {
                ImGui.Text(EscapeString(player.CurrentTrack?.Title) ?? locale.GetString("UnknownTrack"));
                ImGui.Text(EscapeString(player.CurrentTrack?.Artist) ?? locale.GetString("UnknownArtist"));
                ImGui.Text(EscapeString(player.CurrentTrack?.Album) ?? locale.GetString("UnknownAlbum"));

                ImGui.EndChild();
            }
            
            ImGui.SameLine();
            
            Vector2 centerPos = new Vector2(Size.Width / 2 - 40, ImGui.GetCursorScreenPos().Y + (int) (10 * Scale));
            ImGui.SetNextWindowPos(centerPos);

            ImGui.BeginChild("TransportControls", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
            {
                //Vector2 centerPos = new Vector2(Size.Width / 2, ImGui.GetCursorScreenPos().Y);
                //float padding = ImGui.GetStyle().WindowPadding.X + 10;
                
                ImGui.BeginDisabled(player.TrackState == TrackState.Stopped);
                
                Vector4 buttonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
                
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);
                
                if (ImGui.ImageButton("BackwardButton", _skipButton.ID, ScaleVec(32), new Vector2(1, 0), new Vector2(0, 1)))
                {
                    player.Previous();
                }
                
                ImGui.SameLine();
                
                if (player.TrackState == TrackState.Playing)
                {
                    if (ImGui.ImageButton("PauseButton", _pauseButton.ID, ScaleVec(32)))
                        player.Pause();
                }
                else
                {
                    if (ImGui.ImageButton("PlayButton", _playButton.ID, ScaleVec(32)))
                        player.Play();
                }
                
                ImGui.SameLine();

                if (ImGui.ImageButton("ForwardButton", _skipButton.ID, ScaleVec(32)))
                {
                    player.Next();
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("StopButton", _stopButton.ID, ScaleVec(32)))
                {
                    player.Stop();
                }
                
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                
                ImGui.EndDisabled();
                
                ImGui.EndChild();
            }

            //ImGui.BeginChild("SongPosition", ImGuiChildFlags.AutoResizeX)
            {
                float cursorPos = ImGui.GetCursorPosY() + (int) (10 * Scale);
                Vector2 contentRegion = ImGui.GetContentRegionAvail();

                float align = ImGui.GetStyle().FramePadding.Y;

                int position = player.ElapsedSeconds;
                int length = player.TrackLength;
                
                string elapsedText = $"{position / 60:0}:{position % 60:00}";
                string lengthText = $"{length / 60:0}:{length % 60:00}";

                Vector2 elapsedTextSize = ImGui.CalcTextSize(elapsedText);
                Vector2 lengthTextSize = ImGui.CalcTextSize(lengthText);
                
                ImGui.SetCursorPosY(cursorPos + align);
                ImGui.Text(elapsedText);
                ImGui.SameLine();
                ImGui.SetCursorPosY(cursorPos);
                ImGui.SetNextItemWidth(contentRegion.X - elapsedTextSize.X - lengthTextSize.X - (int) (20 * Scale));
                if (ImGui.SliderInt("##transport", ref position, 0, length, ""))
                    _seekPosition = position;

                if (ImGui.IsItemDeactivatedAfterEdit())
                    player.Seek(_seekPosition);

                ImGui.SameLine();
                ImGui.SetCursorPosY(cursorPos);
                ImGui.Text(lengthText);
                
                //ImGui.EndChild();
            }
            
            ImGui.EndChild();
            
            ImGui.End();
        }
        
        // Switches from the queue to the track list. Useful for when a new album is clicked, provides immediate
        // feedback to the user.
        bool switchFromQueueToTrackList = false;
        
        if (ImGui.Begin("Albums", ImGuiWindowFlags.HorizontalScrollbar))
        {
            /*string newDirectory = null;

            if (ImGui.Selectable(".."))
                newDirectory = Path.GetDirectoryName(_currentDirectory);
            
            foreach (string directory in _directories)
            {
                if (ImGui.Selectable(Path.GetFileName(directory)))
                    newDirectory = directory;
            }
            
            if (newDirectory != null)
                ChangeDirectory(newDirectory);*/

            ImGui.BeginChild("AlbumList", ImGuiWindowFlags.HorizontalScrollbar);
            {
                if (ImGui.Selectable(locale.GetString("Player.Albums.ShowAll"), _currentAlbum == null))
                {
                    ChangeAlbum(null);
                    switchFromQueueToTrackList = true;
                }
                
                ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                clipper.Begin(_albumList.Length);

                while (clipper.Step())
                {
                    IEnumerable<Album> albumsRange =
                        _albumList.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                    
                    foreach (Album album in albumsRange)
                    {
                        string name = album.Name;
                        
                        if (ImGui.Selectable(name, _currentAlbum == name))
                        {
                            ChangeAlbum(name);
                            switchFromQueueToTrackList = true;
                        }

                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                player.QueueTracks(album.Tracks, QueueSlot.AtEnd);
                        
                            ImGui.Separator();
                        
                            if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                AddPopup(new RemovePopup(name, true, false));
                            if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteAlbum")))
                                AddPopup(new RemovePopup(name, true, true));
                        
                            ImGui.EndPopup();
                        }
                    }
                }
                
                ImGui.EndChild();
            }

            ImGui.End();
        }
        
        if (_currentAlbum != null && ImGui.Begin("Songs"))
        {
            /*foreach (string file in _files)
            {
                if (ImGui.Selectable(Path.GetFileName(file)))
                {
                    player.ChangeTrack(file);
                    player.Play();
                }
            }*/

            if (ImGui.BeginTabBar("SongsTabs"))
            {
                Vector2 currentCursorPos = ImGui.GetCursorPos();
                Vector2 contentRegion = ImGui.GetContentRegionAvail();

                bool updateAvailable = _newVersionURL != null;
                
                ImGui.SetCursorPos(new Vector2(contentRegion.X - (int) ((updateAvailable ? 114 : 82) * Scale), (int) (5 * Scale)));
                ImGui.BeginChild("SettingsButtons");
                {
                    if (updateAvailable)
                    {
                        Vector4 buttonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
                        Vector4 highlightColor = new Vector4(1, 0, 0, 1);
                        float amount = (float.Sin(_newVersionBlinker) + 1) / 2;
                        
                        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Lerp(buttonColor, highlightColor, amount));
                        if (ImGui.ImageButton("Update", _updateButton.ID, ScaleVec(16)))
                            OpenLink(_newVersionURL);
                        
                        ImGui.SetItemTooltip(locale.GetString("Player.UpdateAvailable", _newVersion));
                        
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        // TODO: DeltaTime
                        const float dt = 1 / 60.0f;
                        _newVersionBlinker += dt * 2;
                        if (_newVersionBlinker >= float.Pi * 2)
                            _newVersionBlinker -= float.Pi * 2;
                    }
                    
                    if (ImGui.ImageButton("ReportBug", _bugButton.ID, ScaleVec(16)))
                        OpenLink("https://github.com/aquagoose/Glimpse/issues/new?template=bug_report.md");
                    ImGui.SetItemTooltip(locale.GetString("Player.ReportBug"));
                    
                    ImGui.SameLine();
                    
                    if (ImGui.ImageButton("Settings", _cogButton.ID, ScaleVec(16)))
                        AddPopup(new SettingsPopup());
                    ImGui.SetItemTooltip(locale.GetString("Player.Settings"));
            
                    ImGui.SameLine();
            
                    if (ImGui.ImageButton("AddDirs", _plusButton.ID, ScaleVec(16)))
                        AddPopup(new AddFolderPopup());
                    ImGui.SetItemTooltip(locale.GetString("Player.AddDirs"));
                    
                    ImGui.EndChild();
                }
                
                ImGui.SetCursorPos(currentCursorPos);
                
                ImGuiTabItemFlags trackFlags =
                    switchFromQueueToTrackList ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Tracks"), trackFlags))
                {
                    if (ImGui.BeginTable("SongTable", 9, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.RowBg))
                    {
                        ImGui.TableSetupColumn(locale.GetString("Track"), ImGuiTableColumnFlags.WidthFixed,  40.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Title"), ImGuiTableColumnFlags.WidthFixed, 280.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Artist"), ImGuiTableColumnFlags.WidthFixed, 160.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Album"), ImGuiTableColumnFlags.WidthFixed, 240.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Length"), ImGuiTableColumnFlags.WidthFixed, 48.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Plays"), ImGuiTableColumnFlags.WidthFixed, 40.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Rating"), ImGuiTableColumnFlags.WidthFixed, 60.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("LastPlayed"), ImGuiTableColumnFlags.WidthFixed, 160.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("FileName"), ImGuiTableColumnFlags.WidthFixed, 300.0f * Scale);
                        
                        ImGui.TableSetupScrollFreeze(0, 1);
                        
                        ImGui.TableHeadersRow();

                        string currentTrackPath = Glimpse.Player.CurrentTrackPath;
                        int songEntryHeight = (int) (25 * Scale);

                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(_currentTrackList.Length, songEntryHeight);
                        while (clipper.Step())
                        {
                            int song = clipper.DisplayStart;
                            IEnumerable<Track> visibleTracks = _currentTrackList.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                            foreach (Track track in visibleTracks)
                            {
                                ImGui.TableNextRow(songEntryHeight);

                                //Console.WriteLine(song);

                                ImGui.TableNextColumn();
                                if (track.TrackNumber is uint trackNumber)
                                    ImGui.Text(trackNumber.ToString());

                                ImGui.TableNextColumn();

                                string path = track.Path;
                                string title = EscapeString(track.Title) ?? locale.GetString("UnknownTrack");
                                string artist = EscapeString(track.Artist) ?? locale.GetString("UnknownArtist");
                                string album = EscapeString(track.Album) ?? locale.GetString("UnknownAlbum");
                                string escapedPath = EscapeString(path);

                                if (ImGui.Selectable($"{title}##{path}", path == currentTrackPath, ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    // TODO: Not a fan of this.
                                    player.QueueTracks(_currentTrackList.Select(tracks => tracks.Path), QueueSlot.Clear);
                                    player.ChangeTrack(song);
                                }

                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                        player.QueueTrack(path, QueueSlot.Queue);
                                    if (ImGui.Selectable(locale.GetString("Menu.PlayNext")))
                                        player.QueueTrack(path, QueueSlot.NextTrack);
                                    if (ImGui.Selectable(locale.GetString("Menu.AddToEnd")))
                                        player.QueueTrack(path, QueueSlot.AtEnd);

                                    ImGui.Separator();

                                    if (ImGui.Selectable(locale.GetString("Menu.ShowInExplorer")))
                                        Glimpse.Platform.OpenFileInExplorer(path);
                                    if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                        AddPopup(new RemovePopup(path, false, false));
                                    if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteFile")))
                                        AddPopup(new RemovePopup(path, false, true));

                                    ImGui.EndPopup();
                                }

                                ImGui.TableNextColumn();
                                ImGui.Text(artist);
                                ImGui.TableNextColumn();
                                ImGui.Text(album);
                                ImGui.TableNextColumn();
                                ImGui.Text(track.Length?.ToString(@"mm\:ss") ?? "");
                                ImGui.TableNextColumn();
                                ImGui.Text(track.PlayCount.ToString());
                                ImGui.TableNextColumn();
                                for (int i = 0; i < 5; i++)
                                {
                                    ImGui.Text(i < track.Rating ? "*" : "-");
                                    ImGui.SameLine(0, 7);
                                }

                                ImGui.NewLine();
                                ImGui.TableNextColumn();
                                ImGui.Text(track.LastPlayed is { } lastPlayed ? lastPlayed.ToString("yyyy-MM-dd HH:mm:ss") : "");
                                ImGui.TableNextColumn();
                                ImGui.Text(EscapeString(escapedPath));
                                ImGui.SetItemTooltip(escapedPath);

                                song++;
                            }
                        }

                        ImGui.EndTable();
                    }
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Queue")))
                {
                    ImGui.BeginChild("QueuedTracks");
                    {
                        List<string> queuedTracks = player.QueuedTracks;
                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(queuedTracks.Count);

                        while (clipper.Step())
                        {
                            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                            {
                                string path = queuedTracks[i];

                                bool selected = i == player.CurrentTrackIndex;
                                bool dark = i < player.CurrentTrackIndex;

                                if (dark)
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                                
                                if (ImGui.Selectable($"{i + 1}. {Glimpse.Database.Tracks[path].Title}", selected))
                                    player.ChangeTrack(i);
                                if (dark)
                                    ImGui.PopStyleColor();
                            }
                        }

                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }
            
            ImGui.End();
        }

        if (player.TrackState == TrackState.Playing && player.SecondsConsumed >= double.Max(30, player.TrackLength * 0.6) &&
            !_hasIncrementedPlayCount)
        {
            _hasIncrementedPlayCount = true;
            
            if (Glimpse.Database.Tracks.TryGetValue(player.CurrentTrackPath, out Track track))
            {
                track.PlayCount++;
                track.LastPlayed = DateTime.Now;
                Glimpse.Database.UpdateTrack(track);
            }
        }
    }

    public void RefreshLayout()
    {
        _init = false;
    }

    private void ChangeAlbum(string? albumName)
    {
        _currentAlbum = albumName;
        _currentTrackList = albumName == null
            ? Glimpse.Database.SelectAllTracks()
            : Glimpse.Database.SelectAlbum(albumName);
    }
    
    private void PlayerOnTrackChanged(TrackInfo info, string path)
    {
        _hasIncrementedPlayCount = false;
        TrackInfo.Image art = info.AlbumArt;

        if (art?.Data == null)
            _shouldDeleteArt = true;
        else
            _newAlbumArt = art.Data;
    }
    
    private void PlayerOnStateChanged(TrackState state)
    {
        Glimpse.Platform.SetPlayState(state, Glimpse.Player.CurrentTrack);
        
        if (state != TrackState.Stopped)
            return;

        _shouldDeleteArt = true;
    }
    
    private void PlatformOnButtonPressed(TransportButton button)
    {
        AudioPlayer player = Glimpse.Player;
        
        switch (button)
        {
            case TransportButton.Play:
                player.Play();
                break;
            case TransportButton.Pause:
                player.Pause();
                break;
            case TransportButton.Next:
                player.Next();
                break;
            case TransportButton.Previous:
                player.Previous();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(button), button, null);
        }
    }

    private Vector2 ScaleVec(float x, float y)
    {
        float scale = Scale;
        return new Vector2((int) (x * scale), (int) (y * scale));
    }

    private Vector2 ScaleVec(float scalar)
        => ScaleVec(scalar, scalar);

    private async Task CheckForNewerVersion()
    {
        Logger logger = Glimpse.Logger;
        logger.Log("Checking for update...");
        
        try
        {
            using HttpClient client = new();
            client.BaseAddress = new Uri("https://glimpseaudio.co.uk");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "download/version.json");
            using HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(json);

            Version thisVersion = Glimpse.Version;

            string? newVersionString = (string?) obj["version"];
            if (newVersionString == null)
                return;
            int posOfSuffixDash = newVersionString.IndexOf('-');
            if (posOfSuffixDash >= 0)
                newVersionString = newVersionString.Remove(posOfSuffixDash);
            Version newVersion = new Version(newVersionString);

            if (newVersion <= thisVersion)
            {
                logger.Log("Glimpse is up to date.");
                return;
            }

            string? newVersionUrl = (string?) obj["url"];
            if (newVersionUrl == null)
                return;

            _newVersion = newVersion;
            _newVersionURL = newVersionUrl;
            logger.Log($"Version {_newVersion} is available!");
        }
        catch (Exception e)
        {
            logger.Log($"Error occurred while checking for update: {e}");
        }
    }

    public static void OpenLink(string link)
    {
        Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
    }

    private static string? EscapeString(string? @string)
    {
        return @string?.Replace("%", "%%");
    }
}
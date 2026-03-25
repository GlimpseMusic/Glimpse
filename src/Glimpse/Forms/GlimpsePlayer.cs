using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text.Json.Nodes;
using Glimpse.API;
using Glimpse.Audio;
using Glimpse.Database;
using Glimpse.Locales;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using Newtonsoft.Json.Linq;
using SDL3;
using Color = System.Drawing.Color;
using Image = Glimpse.Graphics.Image;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private const string ShowAllString = "*";
    private const uint IconSize = 16;
    private const uint BigIconSize = IconSize * 2;
    
    private bool _init;
    private ImGuiStyle _defaultStyle;
    private bool _isLightModeTheme;

    private SemVer _newVersion;
    private string? _newVersionURL;
    private float _newVersionBlinker;

    private AlbumView _currentView;
    private string _currentAlbum;
    private int _seekPosition;
    private int _currentRowHover;
    private int _currentRatingHover;

    private Image _playButton;
    private Image _pauseButton;
    private Image _skipButton;
    private Image _stopButton;
    private Image _plusButton;
    private Image _star;
    private Image _starFilled;
    private Image _cogButton;
    private Image _bugButton;
    private Image _updateButton;
    private Image _shuffleButton;
    private Image _repeatButton;

    private Image _defaultAlbumArt;
    private Image? _albumArt;

    private byte[]? _newAlbumArt;
    private bool _shouldDeleteArt;

    private Timer _playCountTimer;
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

    protected override unsafe void Initialize()
    {
        _playButton = Renderer.CreateImage("Assets/Icons/PlayButton.png");
        _pauseButton = Renderer.CreateImage("Assets/Icons/PauseButton.png");
        _skipButton = Renderer.CreateImage("Assets/Icons/SkipButton.png");
        _stopButton = Renderer.CreateImage("Assets/Icons/StopButton.png");
        _plusButton = Renderer.CreateImage("Assets/Icons/Plus.png");
        _star = Renderer.CreateImage("Assets/Icons/Star.png");
        _starFilled = Renderer.CreateImage("Assets/Icons/Star-Filled.png");
        _cogButton = Renderer.CreateImage("Assets/Icons/Cog.png");
        _bugButton = Renderer.CreateImage("Assets/Icons/Bug.png");
        _updateButton = Renderer.CreateImage("Assets/Icons/Update.png");
        _shuffleButton = Renderer.CreateImage("Assets/Icons/Shuffle.png");
        _repeatButton = Renderer.CreateImage("Assets/Icons/Repeat.png");
        
        _defaultAlbumArt = Renderer.CreateImage("Assets/Icons/Glimpse.png");
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
        Glimpse.Player.StateChanged += PlayerOnStateChanged;
        Glimpse.Platform.ButtonPressed += PlatformOnButtonPressed;
        Glimpse.Platform.GetPosition += PlatformOnGetPosition;

        const uint fontSize = 18;
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/Roboto-Regular.ttf"), fontSize, "Roboto");
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/NotoSansJP-Regular.ttf"), fontSize, "NotoJP");
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/NotoSansSC-Regular.ttf"), fontSize, "NotoSC");
        Renderer.ImGui.AddFont(Glimpse.GetPath("Assets/Fonts/MaterialSymbolsOutlined-Regular.ttf"), fontSize, "Icons");
        
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigInputTrickleEventQueue = false;

        ImGuiStylePtr style = ImGui.GetStyle();
        _defaultStyle = *style.Handle;
        
        RefreshLayout();

        _currentView = AlbumView.Albums;
        _currentAlbum = ShowAllString;

        _playCountTimer = new Timer(CheckIncrementPlayCount, null, 0, 1000);
        
        if (Glimpse.Database.Tracks.Count == 0)
            AddPopup(new AddFolderPopup());

#if !DEBUG
        Task.Run(CheckForNewerVersion);
#endif
    }

    private void CheckIncrementPlayCount(object? state)
    {
        AudioPlayer player = Glimpse.Player;

        if (player.TrackState != TrackState.Playing ||
            _hasIncrementedPlayCount ||
            player.ConsumedTime.TotalSeconds < double.Max(30, player.TrackLength.TotalSeconds * 0.6))
        {
            return;
        }

        _hasIncrementedPlayCount = true;
        if (Glimpse.Database.Tracks.TryGetValue(player.CurrentTrackPath, out Track track))
        {
            track.PlayCount++;
            track.LastPlayed = DateTime.Now;
            Glimpse.Database.Tracks[player.CurrentTrackPath] = track;
        }
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
                _albumArt = null;
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
            ImGui.TextUnformatted("DEBUG Menu");

            ImGui.Spacing();
            
            if (ImGui.MenuItem("Style Editor"))
                AddPopup(new StyleEditorPopup());
            
            if (ImGui.MenuItem("Settings"))
                AddPopup(new SettingsPopup());
            
            ImGui.EndMainMenuBar();
        }
#endif*/

        const uint centralNode = 1 << 11;
        const uint noTabBar = 1 << 12;

        uint id = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(),
            ImGuiDockNodeFlags.PassthruCentralNode | (ImGuiDockNodeFlags) noTabBar);
        
        if (!_init)
        {
            _init = true;
            
            ImGuiP.DockBuilderRemoveNode(id);
            ImGuiP.DockBuilderAddNode(id, ImGuiDockNodeFlags.NoUndocking);

            ImGuiDir dir = Glimpse.Config.SwapTransportControls ? ImGuiDir.Up : ImGuiDir.Down;
            
            uint transportId;
            uint albumsSongsId;
            ImGuiP.DockBuilderSplitNode(id, dir, 0, &transportId, &albumsSongsId);

            ImGuiDockNodePtr transportNode = ImGuiP.DockBuilderGetNode(transportId);
            transportNode.LocalFlags = ImGuiDockNodeFlags.NoResize;
            transportNode.SizeRef = ScaleVec(1100, 122);
            
            uint albumsId;
            uint songsId;
            ImGuiP.DockBuilderSplitNode(albumsSongsId, ImGuiDir.Left, 0, &albumsId, &songsId);

            ImGuiDockNodePtr albumsNode = ImGuiP.DockBuilderGetNode(albumsId);
            albumsNode.SizeRef = ScaleVec(327, 650);

            ImGuiDockNodePtr songsNode = ImGuiP.DockBuilderGetNode(songsId);
            songsNode.SizeRef = ScaleVec(772, 650);
            songsNode.LocalFlags = (ImGuiDockNodeFlags) centralNode;
            
            Console.WriteLine(albumsId.ToString("x8"));
            Console.WriteLine(albumsId.ToString("x8"));
            Console.WriteLine(transportId.ToString("x8"));
            
            ImGuiP.DockBuilderDockWindow("Transport", transportId);
            ImGuiP.DockBuilderDockWindow("Albums", albumsId);
            ImGuiP.DockBuilderDockWindow("Songs", songsId);
        
            ImGuiP.DockBuilderFinish(id);
        }

        Vector4 iconsColor = _isLightModeTheme ? new Vector4(0, 0, 0, 1) : new Vector4(1, 1, 1, 1);

        bool switchToTrackList = false;
        bool switchToQueueView = false;
        AlbumView? switchView = null;
        
        #region Transport Dock
        
        if (ImGui.Begin("Transport", ImGuiWindowFlags.NoResize))
        {
            Vector2 winSize = ImGui.GetContentRegionAvail();

            ImGui.BeginChild("AlbumArt", new Vector2(winSize.Y));
            {
                ImGui.Image(_albumArt ?? _defaultAlbumArt, new Vector2(winSize.Y));
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            ImGui.BeginChild("MainView");
            {
                ImGui.BeginChild("TrackInfo", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                {
                    if (player.TrackState == TrackState.Stopped)
                    {
                        ImGui.TextUnformatted(locale.GetString("Glimpse"));
                        ImGui.TextUnformatted("");
                        ImGui.TextUnformatted("");
                    }
                    else
                    {
                        // TODO: Scroll to the selected track & album/artist
                        if (ImGuiE.TextButton(player.CurrentTrack?.Title ?? locale.GetString("UnknownTrack")) &&
                            player.CurrentTrack?.Album != null)
                        {
                            switchToQueueView = true;
                        }

                        if (ImGuiE.TextButton(player.CurrentTrack?.Artist ?? locale.GetString("UnknownArtist")) &&
                            player.CurrentTrack?.Artist != null)
                        {
                            switchToTrackList = true;
                            switchView = AlbumView.Artists;
                            _currentAlbum = player.CurrentTrack.Artist;
                        }


                        if (ImGuiE.TextButton(player.CurrentTrack?.Album ?? locale.GetString("UnknownAlbum")) &&
                            player.CurrentTrack?.Album != null)
                        {
                            switchToTrackList = true;
                            switchView = AlbumView.Albums;
                            _currentAlbum = player.CurrentTrack.Album;
                        }
                    }

                    ImGui.EndChild();
                }

                ImGui.SameLine();

                Vector2 iconSize = new Vector2(BigIconSize) * Scale;
                // Even though there are 4 icons, 3 icons makes it *feel* more centered, even though it's shifted to the right.
                const int numIcons = 3;
                float spacing = ImGui.GetStyle().ItemSpacing.X;
                float padding = ImGui.GetStyle().FramePadding.X;
                float totalButtonsWidth = (iconSize.X + spacing + padding) * numIcons;

                Vector2 centerPos = new Vector2(winSize.X / 2 - totalButtonsWidth / 2, ImGui.GetCursorScreenPos().Y + (int) (10 * Scale));
                ImGui.SetCursorScreenPos(centerPos);

                ImGui.BeginChild("TransportControls", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                {
                    //Vector2 centerPos = new Vector2(Size.Width / 2, ImGui.GetCursorScreenPos().Y);
                    //float padding = ImGui.GetStyle().WindowPadding.X + 10;

                    ImGui.BeginDisabled(player.TrackState == TrackState.Stopped);

                    Vector4 buttonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);

                    ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonColor);

                    if (ImGui.ImageButton("BackwardButton", _skipButton, iconSize, new Vector2(1, 0),
                            new Vector2(0, 1), Vector4.Zero, iconsColor))
                    {
                        player.Previous();
                    }

                    ImGui.SameLine();

                    if (player.TrackState == TrackState.Playing)
                    {
                        if (ImGui.ImageButton("PauseButton", _pauseButton, iconSize, Vector4.Zero, iconsColor))
                            player.Pause();
                    }
                    else
                    {
                        if (ImGui.ImageButton("PlayButton", _playButton, iconSize, Vector4.Zero, iconsColor))
                            player.Play();
                    }

                    ImGui.SameLine();

                    if (ImGui.ImageButton("ForwardButton", _skipButton, iconSize, Vector4.Zero, iconsColor))
                    {
                        player.Next();
                    }

                    ImGui.SameLine();
                    if (ImGui.ImageButton("StopButton", _stopButton, iconSize, Vector4.Zero, iconsColor))
                    {
                        player.Stop();
                    }

                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();

                    ImGui.EndDisabled();

                    ImGui.EndChild();
                }

                Vector2 cursorPos = ImGui.GetCursorPos();

                Vector2 contentRegion = ImGui.GetContentRegionAvail();
                ImGui.SetCursorPos(new Vector2(contentRegion.X - 150 * Scale, 20));
                ImGui.BeginChild("VolumeDock");
                {
                    ImGui.BeginDisabled();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                    ImGui.ImageButton("ShuffleButton", _shuffleButton, ScaleVec(IconSize), Vector4.Zero, iconsColor);
                    ImGui.SameLine(0, 0);
                    ImGui.ImageButton("RepeatButton", _repeatButton, ScaleVec(IconSize), Vector4.Zero, iconsColor);
                    ImGui.PopStyleColor();
                    ImGui.EndDisabled();

                    ImGui.SameLine(0, 2);

                    contentRegion = ImGui.GetContentRegionAvail();
                    int volume = (int) (Glimpse.Player.Volume * 100);
                    ImGui.SetNextItemWidth(contentRegion.X);
                    if (ImGui.SliderInt("##Volume", ref volume, 0, 100))
                    {
                        float fVol = (float) volume / 100;
                        Glimpse.Player.Volume = fVol;
                        Glimpse.Config.Volume = fVol;
                    }

                    ImGui.EndChild();
                }

                // TODO: HACK
                ImGui.SetCursorPos(cursorPos);
                ImGui.BeginChild("SongPosition");
                {
                    float cursorPosY = ImGui.GetCursorPosY() + (int) (10 * Scale);
                    contentRegion = ImGui.GetContentRegionAvail();

                    float align = ImGui.GetStyle().FramePadding.Y;

                    int position = (int) player.ElapsedTime.TotalSeconds;
                    int length = (int) player.TrackLength.TotalSeconds;

                    string elapsedText = $"{position / 60:0}:{position % 60:00}";
                    string lengthText = $"{length / 60:0}:{length % 60:00}";

                    Vector2 elapsedTextSize = ImGui.CalcTextSize(elapsedText);
                    Vector2 lengthTextSize = ImGui.CalcTextSize(lengthText);

                    ImGui.SetCursorPosY(cursorPosY + align);
                    ImGui.TextUnformatted(elapsedText);
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(cursorPosY);
                    ImGui.SetNextItemWidth(contentRegion.X - elapsedTextSize.X - lengthTextSize.X - (int) (20 * Scale));
                    if (ImGui.SliderInt("##transport", ref position, 0, length, ""))
                        _seekPosition = position;

                    if (ImGui.IsItemDeactivatedAfterEdit())
                        player.Seek(_seekPosition);

                    ImGui.SameLine();
                    ImGui.SetCursorPosY(cursorPosY);
                    ImGui.TextUnformatted(lengthText);

                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }
        }
        ImGui.End();
        
        #endregion
        
        #region Albums Dock
        
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

            Vector2 contentRegion = ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemSpacing;
            const float split = 0.6f;
            
            ImGui.BeginDisabled();
            
            string str = "";
            ImGui.SetNextItemWidth(contentRegion.X * split);
            ImGui.InputTextWithHint("##SearchBox", locale.GetString("Player.SearchBar"), ref str, 1000);
            
            ImGui.EndDisabled();
            
            ImGui.SameLine();
            
            /*ImGui.SetNextItemWidth(contentRegion.X * (1.0f - split));
            string preview = _currentView switch
            {
                AlbumView.Albums => locale.GetString("Player.ViewSelect.Albums"),
                AlbumView.Artists => locale.GetString("Player.ViewSelect.Artists"),
                _ => throw new ArgumentOutOfRangeException()
            };
            if (ImGui.BeginCombo("##DisplaySelector", preview))
            {
                if (ImGui.Selectable(locale.GetString("Player.ViewSelect.Albums")))
                {
                    _currentView = AlbumView.Albums;
                    _currentAlbum = ShowAllString;
                }

                if (ImGui.Selectable(locale.GetString("Player.ViewSelect.Artists")))
                {
                    _currentView = AlbumView.Artists;
                    _currentAlbum = ShowAllString;
                }
                //ImGui.Selectable("Playlists");
                
                ImGui.EndCombo();
            }*/

            void DrawItemList()
            {
                ImGui.BeginChild("AlbumList", ImGuiWindowFlags.HorizontalScrollbar);
                {
                    if (ImGui.Selectable(locale.GetString("Player.Albums.ShowAll"), _currentAlbum == ShowAllString))
                    {
                        _currentAlbum = ShowAllString;
                        switchToTrackList = true;
                    }

                    IReadOnlyCollection<TrackLinkData> datas = _currentView switch
                    {
                        AlbumView.Albums => Glimpse.Database.Albums.Values,
                        AlbumView.Artists => Glimpse.Database.Artists.Values,
                        AlbumView.Genres => Glimpse.Database.Genres.Values,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                    clipper.Begin(datas.Count);
                    
                    while (clipper.Step())
                    {
                        IEnumerable<TrackLinkData> albumsRange =
                            datas.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                        
                        foreach (TrackLinkData data in albumsRange)
                        {
                            string albumName = data.Name;
                            if (albumName == string.Empty)
                                albumName = locale.GetString("Player.Albums.NoAlbum");
                            
                            if (ImGui.Selectable(albumName, _currentAlbum == data.Name))
                            {
                                _currentAlbum = data.Name;
                                switchToTrackList = true;
                            }

                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                    player.QueueTracks(data.Tracks, QueueSlot.AtEnd);
                            
                                ImGui.Separator();
                            
                                if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                    AddPopup(new RemovePopup(data.Name, true, false));
                                if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteAlbum")))
                                    AddPopup(new RemovePopup(data.Name, true, true));
                            
                                ImGui.EndPopup();
                            }
                        }
                    }
                    
                    ImGui.EndChild();
                }
            }
            
            if (ImGui.BeginTabBar("AlbumTabs"))
            {
                //ImGui.PushFont(_iconsFont, 32);

                if (switchView is AlbumView view)
                    _currentView = view;

                if (ImGuiE.BeginTabItemTooltip("\ue019##Albums", locale.GetString("Player.ViewSelect.Albums"), switchView is AlbumView.Albums))
                {
                    if (switchView == null)
                        _currentView = AlbumView.Albums;
                    DrawItemList();
                    ImGui.EndTabItem();
                }

                if (ImGuiE.BeginTabItemTooltip("\ue01a##Artists", locale.GetString("Player.ViewSelect.Artists"), switchView is AlbumView.Artists))
                {
                    if (switchView == null)
                        _currentView = AlbumView.Artists;
                    DrawItemList();
                    ImGui.EndTabItem();
                }
                
                if (ImGuiE.BeginTabItemTooltip("\ue521##Genres", locale.GetString("Player.ViewSelect.Genres"), switchView is AlbumView.Genres))
                {
                    if (switchView == null)
                        _currentView = AlbumView.Genres;
                    DrawItemList();
                    ImGui.EndTabItem();
                }

                ImGui.BeginDisabled();
                if (ImGuiE.BeginTabItemTooltip("\ue05f##Playlists", locale.GetString("Player.ViewSelect.Playlists")))
                {
                    ImGui.EndTabItem();
                }
                ImGui.EndDisabled();
                
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
        
        #endregion
        
        #region Songs Dock
        
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
                        if (ImGui.ImageButton("Update", _updateButton, ScaleVec(IconSize), Vector4.Zero, iconsColor))
                            OpenLink(_newVersionURL);
                        
                        ImGuiE.SetItemTooltipUnformatted(locale.GetString("Player.UpdateAvailable", _newVersion));
                        
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        // TODO: DeltaTime
                        const float dt = 1 / 60.0f;
                        _newVersionBlinker += dt * 2;
                        if (_newVersionBlinker >= float.Pi * 2)
                            _newVersionBlinker -= float.Pi * 2;
                    }

                    if (ImGui.ImageButton("ReportBug", _bugButton, ScaleVec(IconSize), Vector4.Zero, iconsColor))
                        OpenLink("https://github.com/aquagoose/Glimpse/issues/new?template=bug_report.md");

                    ImGuiE.SetItemTooltipUnformatted(locale.GetString("Player.ReportBug"));
                    
                    ImGui.SameLine();
                    
                    if (ImGui.ImageButton("Settings", _cogButton, ScaleVec(IconSize), Vector4.Zero, iconsColor))
                        AddPopup(new SettingsPopup());
                    ImGuiE.SetItemTooltipUnformatted(locale.GetString("Player.Settings"));
            
                    ImGui.SameLine();
            
                    if (ImGui.ImageButton("AddDirs", _plusButton, ScaleVec(IconSize), Vector4.Zero, iconsColor))
                        AddPopup(new AddFolderPopup());
                    ImGuiE.SetItemTooltipUnformatted(locale.GetString("Player.AddDirs"));
                    
                    ImGui.EndChild();
                }
                
                ImGui.SetCursorPos(currentCursorPos);
                
                ImGuiTabItemFlags trackFlags =
                    switchToTrackList ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Tracks"), trackFlags))
                {
                    ICollection<string> trackList;

                    TrackLinkData? currentView = null;
                    switch (_currentView)
                    {
                        case AlbumView.Albums:
                            if (Glimpse.Database.Albums.TryGetValue(_currentAlbum, out Album currentAlbum))
                                currentView = currentAlbum;
                            break;
                        case AlbumView.Artists:
                            if (Glimpse.Database.Artists.TryGetValue(_currentAlbum, out Artist currentArtist))
                                currentView = currentArtist;
                            break;
                        case AlbumView.Genres:
                            if (Glimpse.Database.Genres.TryGetValue(_currentAlbum, out Genre currentGenre))
                                currentView = currentGenre;
                            break;
                    }
                    
                    if (_currentAlbum == ShowAllString || (currentView == null && switchView == null))
                    {
                        trackList = Glimpse.Database.Tracks.Keys;
                        _currentAlbum = ShowAllString;
                    }
                    else
                        trackList = currentView.Tracks;

                    if (ImGui.BeginTable("SongTable", 9, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.RowBg))
                    {
                        const int ratingColumn = 6;
                        ImGui.TableSetupColumn(locale.GetString("Track"), ImGuiTableColumnFlags.WidthFixed,  40.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Title"), ImGuiTableColumnFlags.WidthFixed, 265.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Artist"), ImGuiTableColumnFlags.WidthFixed, 160.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Album"), ImGuiTableColumnFlags.WidthFixed, 195.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Length"), ImGuiTableColumnFlags.WidthFixed, 48.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Plays"), ImGuiTableColumnFlags.WidthFixed, 40.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("Rating"), ImGuiTableColumnFlags.WidthFixed, 85.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("LastPlayed"), ImGuiTableColumnFlags.WidthFixed, 160.0f * Scale);
                        ImGui.TableSetupColumn(locale.GetString("FileName"), ImGuiTableColumnFlags.WidthFixed, 300.0f * Scale);
                        
                        ImGui.TableSetupScrollFreeze(0, 1);
                        
                        ImGui.TableHeadersRow();

                        string currentTrackPath = Glimpse.Player.CurrentTrackPath;
                        //int songEntryHeight = (int) (25 * Scale);

                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(trackList.Count/*, songEntryHeight*/);
                        while (clipper.Step())
                        {
                            int song = clipper.DisplayStart;
                            IEnumerable<string> visibleTracks =
                                trackList.Take(new Range(clipper.DisplayStart, clipper.DisplayEnd));
                            foreach (string path in visibleTracks)
                            {
                                Track track = Glimpse.Database.Tracks[path];

                                ImGui.TableNextRow(/*songEntryHeight*/);
                                int currentRow = ImGui.TableGetRowIndex();

                                //Console.WriteLine(song);

                                ImGui.TableNextColumn();
                                if (track.TrackNumber is uint trackNumber)
                                    ImGui.TextUnformatted(trackNumber.ToString());

                                ImGui.TableNextColumn();

                                string title = track.Title ?? locale.GetString("UnknownTrack");
                                string artist = track.Artist ?? locale.GetString("UnknownArtist");
                                string album = track.Album ?? locale.GetString("UnknownAlbum");
                                string length = track.Length?.ToString(@"mm\:ss") ?? "";
                                string playCount = track.PlayCount.ToString();
                                string lastPlayed = track.LastPlayed is { } last
                                    ? last.ToString("yyyy-MM-dd HH:mm:ss")
                                    : "";

                                // In order to allow the rating buttons to be clicked, we tell the selectable to ignore
                                // the ratings column (otherwise the buttons won't click and instead the song will play)
                                // To do this we just disable the SpanAllColumns flag when the rating column is hovered.
                                bool isRatingHovered = ImGui.TableGetHoveredColumn() == ratingColumn;
                                if (ImGui.Selectable($"{title}##{path}", path == currentTrackPath, isRatingHovered ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    player.QueueTracks(trackList, QueueSlot.Clear);
                                    player.ChangeTrack(song);
                                }
                                ImGuiE.SetColumnTooltip(title);

                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.Selectable(locale.GetString("Menu.AddToQueue")))
                                        player.QueueTrack(path, QueueSlot.AtEnd);
                                    if (ImGui.Selectable(locale.GetString("Menu.PlayNext")))
                                        player.QueueTrack(path, QueueSlot.NextTrack);

                                    ImGui.Separator();

                                    if (ImGui.Selectable(locale.GetString("Menu.ShowInExplorer", Glimpse.Platform.FileManagerName)))
                                        Glimpse.Platform.OpenFileInExplorer(path);
                                    if (ImGui.Selectable(locale.GetString("Menu.RemoveFromLibrary")))
                                        AddPopup(new RemovePopup(path, false, false));
                                    if (Glimpse.Config.EnableFileDeletion && ImGui.Selectable(locale.GetString("Menu.DeleteFile")))
                                        AddPopup(new RemovePopup(path, false, true));

                                    ImGui.EndPopup();
                                }

                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(artist);
                                ImGuiE.SetColumnTooltip(artist);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(album);
                                ImGuiE.SetColumnTooltip(album);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(length);
                                //ImGuiE.SetColumnTooltip(length);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(playCount);
                                //ImGuiE.SetColumnTooltip(playCount);
                                ImGui.TableNextColumn();
                                int rating = isRatingHovered && _currentRowHover == currentRow ? _currentRatingHover : track.Rating;
                                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0));
                                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
                                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
                                for (int i = 0; i < 5; i++)
                                {
                                    if (ImGui.ImageButton($"{path}rating{i}", i < rating ? _starFilled : _star, ScaleVec(IconSize), Vector4.Zero, iconsColor))
                                    {
                                        track.Rating = (byte) (i + 1);
                                        Glimpse.Database.Tracks[path] = track;
                                    }

                                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                    {
                                        track.Rating = 0;
                                        Glimpse.Database.Tracks[path] = track;
                                    }

                                    if (ImGui.IsItemHovered())
                                    {
                                        _currentRowHover = currentRow;
                                        _currentRatingHover = i + 1;
                                    }

                                    ImGui.SameLine(0, 0);
                                }
                                ImGui.PopStyleColor(3);
                                ImGui.PopStyleVar();

                                ImGui.NewLine();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(lastPlayed);
                                ImGuiE.SetColumnTooltip(lastPlayed);
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted(path);
                                ImGuiE.SetColumnTooltip(path);

                                song++;
                            }
                        }

                        ImGui.EndTable();
                    }
                    
                    ImGui.EndTabItem();
                }

                ImGuiTabItemFlags queueFlags =
                    switchToQueueView ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                
                if (ImGui.BeginTabItem(locale.GetString("Player.Tab.Queue"), queueFlags))
                {
                    ImGui.BeginChild("QueuedTracks");
                    {
                        List<string> queuedTracks = player.QueuedTracks;
                        ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();
                        clipper.Begin(queuedTracks.Count, (BigIconSize + 2) * Scale);

                        while (clipper.Step())
                        {
                            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        //for (int i = 0; i < queuedTracks.Count; i++)
                            {
                                string path = queuedTracks[i];

                                bool selected = i == player.CurrentTrackIndex;
                                bool dark = i < player.CurrentTrackIndex;

                                /*if (dark)
                                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

                                if (ImGui.Selectable($"{i + 1}. {Glimpse.Database.Tracks[path].Title}", selected))
                                    player.ChangeTrack(i);
                                if (dark)
                                    ImGui.PopStyleColor();*/

                                Vector2 cursorPos = ImGui.GetCursorPos();
                                int height = (int) ((BigIconSize + 6) * Scale);
                                
                                if (ImGui.Selectable($"##Queue{i}", selected, ImGuiSelectableFlags.AllowOverlap, new Vector2(0, height)))
                                    player.ChangeTrack(i);
                                ImGui.SetCursorPos(cursorPos);
                                ImGui.SameLine();
                                
                                Track track = Glimpse.Database.Tracks[path];
                                string title = track.Title ?? locale.GetString("UnknownTrack");
                                string artist = track.Artist ?? locale.GetString("UnknownArtist");
                                string album = track.Album ?? locale.GetString("UnknownAlbum");

                                ImGui.BeginChild($"QueueTrack{i}", new Vector2(0, height), ImGuiWindowFlags.NoInputs);
                                {
                                    float posY = ImGui.GetCursorPosY();
                                    ImGui.SetCursorPosY(posY + 1);
                                    ImGui.PushFont(ImFontPtr.Null, BigIconSize);
                                    ImGui.TextUnformatted($"{i + 1}");
                                    ImGui.PopFont();
                                    ImGui.SetCursorPosY(posY);
                                    ImGui.SameLine();
                                    ImGui.BeginChild($"QueueTrackInfo{i}", ImGuiWindowFlags.NoInputs);
                                    {
                                        ImGui.TextUnformatted(title);
                                        ImGui.PushFont(ImFontPtr.Null, 14);
                                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.75f, 0.75f, 0.75f, 1.0f));
                                        ImGui.TextUnformatted(
                                            $"{artist} • {album} • {track.Length.GetValueOrDefault():mm\\:ss}");
                                        ImGui.PopStyleColor();
                                        ImGui.PopFont();

                                        ImGui.EndChild();
                                    }
                                    ImGui.EndChild();
                                }
                            }
                        }

                        ImGui.EndChild();
                    }
                    
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
        
        #endregion
    }

    public void RefreshLayout()
    {
        _init = false;

        _isLightModeTheme = Glimpse.Config.Theme switch
        {
            Theme.SyncToOS => SDL.GetSystemTheme() == SDL.SystemTheme.Light ? true : false,
            Theme.Dark => false,
            Theme.Light => true,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        SetupStyle(ImGui.GetStyle());
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
        Glimpse.Platform.SetPlayState(state, Glimpse.Player.CurrentTrack, Glimpse.Player.ElapsedTime);
        
        if (state != TrackState.Stopped)
            return;

        _shouldDeleteArt = true;
    }
    
    private void PlatformOnButtonPressed(TransportButton? button, int? position)
    {
        AudioPlayer player = Glimpse.Player;

        if (button is { } transportButton)
        {
            switch (transportButton)
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
                case TransportButton.Stop:
                    player.Stop();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
        }
        
        if (position is { } pos)
            player.Seek(pos);
    }
    
    private TimeSpan PlatformOnGetPosition()
    {
        return Glimpse.Player.ElapsedTime;
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

            SemVer thisVersion = Glimpse.Version;

            string? newVersionString = (string?) obj["version"];
            if (newVersionString == null)
                return;
            SemVer newVersion = new SemVer(newVersionString);

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

    public override void Dispose()
    {
        _playCountTimer.Dispose();
        
        _playButton.Dispose();
        _pauseButton.Dispose();
        _skipButton.Dispose();
        _stopButton.Dispose();
        _plusButton.Dispose();
        _star.Dispose();
        _starFilled.Dispose();
        _cogButton.Dispose();
        _bugButton.Dispose();
        _updateButton.Dispose();
        _shuffleButton.Dispose();
        _repeatButton.Dispose();
        
        base.Dispose();
    }

    public static void OpenLink(string link)
    {
        SDL.OpenURL(link);
    }

    protected override void OnScaleChanged()
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        SetupStyle(style);
        RefreshLayout();
    }

    private unsafe void SetupStyle(ImGuiStylePtr style)
    {
        *style.Handle = _defaultStyle;
        
        int rounding = (int) (5 * Scale);
        style.FrameRounding = rounding;
        style.GrabRounding = rounding;
        style.ChildRounding = rounding;
        style.PopupRounding = rounding;
        style.DockingSeparatorSize = (int) float.Ceiling(1 * Scale);
        style.ScaleAllSizes(Scale);
        style.FontScaleDpi = Scale;
        
        if (_isLightModeTheme)
            LightMode(style);
        else
            DarkMode(style);
    }

    private void DarkMode(ImGuiStylePtr style)
    {
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
    }

    private void LightMode(ImGuiStylePtr style)
    {
        Span<Vector4> colors = style.Colors;
        colors[(int) ImGuiCol.Text]                   = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.TextDisabled]           = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
        colors[(int) ImGuiCol.WindowBg]               = new Vector4(0.94f, 0.94f, 0.94f, 1.00f);
        colors[(int) ImGuiCol.ChildBg]                = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.PopupBg]                = new Vector4(1.00f, 1.00f, 1.00f, 0.98f);
        colors[(int) ImGuiCol.Border]                 = new Vector4(0.00f, 0.00f, 0.00f, 0.30f);
        colors[(int) ImGuiCol.BorderShadow]           = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.FrameBg]                = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int) ImGuiCol.FrameBgHovered]         = new Vector4(0.26f, 0.59f, 0.98f, 0.40f);
        colors[(int) ImGuiCol.FrameBgActive]          = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        colors[(int) ImGuiCol.TitleBg]                = new Vector4(0.96f, 0.96f, 0.96f, 1.00f);
        colors[(int) ImGuiCol.TitleBgActive]          = new Vector4(0.82f, 0.82f, 0.82f, 1.00f);
        colors[(int) ImGuiCol.TitleBgCollapsed]       = new Vector4(1.00f, 1.00f, 1.00f, 0.51f);
        colors[(int) ImGuiCol.MenuBarBg]              = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
        colors[(int) ImGuiCol.ScrollbarBg]            = new Vector4(0.98f, 0.98f, 0.98f, 0.53f);
        colors[(int) ImGuiCol.ScrollbarGrab]          = new Vector4(0.69f, 0.69f, 0.69f, 0.80f);
        colors[(int) ImGuiCol.ScrollbarGrabHovered]   = new Vector4(0.49f, 0.49f, 0.49f, 0.80f);
        colors[(int) ImGuiCol.ScrollbarGrabActive]    = new Vector4(0.49f, 0.49f, 0.49f, 1.00f);
        colors[(int) ImGuiCol.CheckMark]              = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.SliderGrab]             = new Vector4(0.26f, 0.59f, 0.98f, 0.78f);
        colors[(int) ImGuiCol.SliderGrabActive]       = new Vector4(0.46f, 0.54f, 0.80f, 0.60f);
        colors[(int) ImGuiCol.Button]                 = new Vector4(1.00f, 0.69f, 0.22f, 0.78f);
        colors[(int) ImGuiCol.ButtonHovered]          = new Vector4(0.62f, 0.93f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.ButtonActive]           = new Vector4(0.06f, 0.53f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.Header]                 = new Vector4(0.26f, 0.59f, 0.98f, 0.31f);
        colors[(int) ImGuiCol.HeaderHovered]          = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        colors[(int) ImGuiCol.HeaderActive]           = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        //colors[(int) ImGuiCol.Separator]              = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
        colors[(int) ImGuiCol.SeparatorHovered]       = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
        colors[(int) ImGuiCol.SeparatorActive]        = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        colors[(int) ImGuiCol.ResizeGrip]             = new Vector4(0.35f, 0.35f, 0.35f, 0.17f);
        colors[(int) ImGuiCol.ResizeGripHovered]      = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
        colors[(int) ImGuiCol.ResizeGripActive]       = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        colors[(int) ImGuiCol.InputTextCursor]        = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.TabHovered]             = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        colors[(int) ImGuiCol.Tab]                    = new Vector4(0.76f, 0.80f, 0.84f, 0.93f);
        colors[(int) ImGuiCol.TabSelected]            = new Vector4(0.60f, 0.73f, 0.88f, 1.00f);
        colors[(int) ImGuiCol.TabSelectedOverline]    = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TabDimmed]              = new Vector4(0.92f, 0.93f, 0.94f, 0.99f);
        colors[(int) ImGuiCol.TabDimmedSelected]      = new Vector4(0.74f, 0.82f, 0.91f, 1.00f);
        colors[(int) ImGuiCol.TabDimmedSelectedOverline] = new Vector4(0.26f, 0.59f, 1.00f, 0.00f);
        colors[(int) ImGuiCol.DockingPreview]         = new Vector4(0.26f, 0.59f, 0.98f, 0.22f);
        colors[(int) ImGuiCol.DockingEmptyBg]         = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        colors[(int) ImGuiCol.PlotLines]              = new Vector4(0.39f, 0.39f, 0.39f, 1.00f);
        colors[(int) ImGuiCol.PlotLinesHovered]       = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        colors[(int) ImGuiCol.PlotHistogram]          = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.PlotHistogramHovered]   = new Vector4(1.00f, 0.45f, 0.00f, 1.00f);
        colors[(int) ImGuiCol.TableHeaderBg]          = new Vector4(0.78f, 0.87f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TableBorderStrong]      = new Vector4(0.57f, 0.57f, 0.64f, 1.00f);
        colors[(int) ImGuiCol.TableBorderLight]       = new Vector4(0.68f, 0.68f, 0.74f, 1.00f);
        colors[(int) ImGuiCol.TableRowBg]             = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int) ImGuiCol.TableRowBgAlt]          = new Vector4(0.30f, 0.30f, 0.30f, 0.09f);
        colors[(int) ImGuiCol.TextLink]               = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.TextSelectedBg]         = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
        colors[(int) ImGuiCol.TreeLines]              = new Vector4(0.00f, 0.00f, 0.00f, 0.30f);
        colors[(int) ImGuiCol.DragDropTarget]         = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
        colors[(int) ImGuiCol.NavCursor]              = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
        colors[(int) ImGuiCol.NavWindowingHighlight]  = new Vector4(0.70f, 0.70f, 0.70f, 0.70f);
        colors[(int) ImGuiCol.NavWindowingDimBg]      = new Vector4(0.20f, 0.20f, 0.20f, 0.20f);
        colors[(int) ImGuiCol.ModalWindowDimBg]       = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
    }

    private enum AlbumView
    {
        Albums,
        Artists,
        Genres
    }
}
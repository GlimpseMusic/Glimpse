using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Glimpse.Database;
using Glimpse.Player;
using Glimpse.Player.Configs;
using Hexa.NET.ImGui;
using Image = Glimpse.Graphics.Image;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private bool _init;
    
    private string _currentAlbum;

    private List<string> _queue;
    private int _currentSong;

    private Image _playButton;
    private Image _pauseButton;
    private Image _skipButton;
    private Image _stopButton;

    private Image _defaultAlbumArt;
    private Image _albumArt;
    
    public GlimpsePlayer()
    {
#if DEBUG
        Title = "Glimpse DEBUG";
#else
        Title = "Glimpse";
#endif
        Size = new Size(1100, 650);

        _queue = new List<string>();
    }

    protected override unsafe void Initialize()
    {
        _playButton = Renderer.CreateImage("Assets/Icons/PlayButton.png");
        _pauseButton = Renderer.CreateImage("Assets/Icons/PauseButton.png");
        _skipButton = Renderer.CreateImage("Assets/Icons/SkipButton.png");
        _stopButton = Renderer.CreateImage("Assets/Icons/StopButton.png");
        
        _defaultAlbumArt = Renderer.CreateImage("Assets/Icons/Glimpse.png");
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
        Glimpse.Player.StateChanged += PlayerOnStateChanged;
        
        ImFontPtr roboto = Renderer.ImGui.AddFont("Assets/Fonts/Roboto-Regular.ttf", 18, "Roboto-20px");
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.FontDefault = roboto;
        
        Span<Vector4> colors = ImGui.GetStyle().Colors;
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
        colors[(int) ImGuiCol.Separator]              = new Vector4(1.00f, 0.34f, 0.43f, 1.00f);
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
        colors[(int) ImGuiCol.NavHighlight]           = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int) ImGuiCol.NavWindowingHighlight]  = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int) ImGuiCol.NavWindowingDimBg]      = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        colors[(int) ImGuiCol.ModalWindowDimBg]       = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);

        if (Glimpse.Database.Albums.Count > 0)
            _currentAlbum = Glimpse.Database.Albums.First().Key;
    }

    protected override unsafe void Update()
    {
        AudioPlayer player = Glimpse.Player;
        
        Renderer.Clear(Color.Black);
        
#if DEBUG
        if (ImGui.BeginMainMenuBar())
        {
            ImGui.Text("DEBUG Menu");
            ImGui.EndMainMenuBar();
        }
#endif
        
        //ImGui.ShowStyleEditor();
        
        uint id = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode | (ImGuiDockNodeFlags) (1 << 12));
        //ImGui.SetNextWindowDockID(id, ImGuiCond.Once);
        
        if (!_init)
        {
            _init = true;
            
            ImGui.DockBuilderRemoveNode(id);
            ImGui.DockBuilderAddNode(id, ImGuiDockNodeFlags.None);

            uint outId = id;

            uint transportDock = ImGui.DockBuilderSplitNode(outId, ImGuiDir.Down, 0.2f, null, &outId);

            uint foldersDock = ImGui.DockBuilderSplitNode(outId, ImGuiDir.Left, 0.3f, null, &outId);
            
            ImGui.DockBuilderDockWindow("Transport", transportDock);
            ImGui.DockBuilderDockWindow("Albums", foldersDock);
            ImGui.DockBuilderDockWindow("Songs", outId);
        
            ImGui.DockBuilderFinish(id);
        }

        if (ImGui.Begin("Transport"))
        {
            Vector2 winSize = ImGui.GetContentRegionAvail();
            
            if (ImGui.BeginChild("AlbumArt", new Vector2(winSize.Y)))
            {
                ImGui.Image((IntPtr) (_albumArt?.ID ?? _defaultAlbumArt.ID), new Vector2(winSize.Y));
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            ImGui.BeginChild("MainThing");
            
            if (ImGui.BeginChild("TrackInfo", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
            {
                ImGui.Text(player.TrackInfo.Title);
                ImGui.Text(player.TrackInfo.Artist);

                Vector2 size = ImGui.CalcTextSize(player.TrackInfo.Album);
                ImGui.Text(player.TrackInfo.Album);

                ImGui.EndChild();
            }
            
            ImGui.SameLine();
            
            Vector2 centerPos = new Vector2(Size.Width / 2 - 80, ImGui.GetCursorScreenPos().Y);
            ImGui.SetNextWindowPos(centerPos);
            
            if (ImGui.BeginChild("TransportControls", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
            {
                //Vector2 centerPos = new Vector2(Size.Width / 2, ImGui.GetCursorScreenPos().Y);
                //float padding = ImGui.GetStyle().WindowPadding.X + 10;
                
                ImGui.BeginDisabled(player.TrackState == TrackState.Stopped);
                
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                
                if (ImGui.ImageButton("BackwardButton", (IntPtr) _skipButton.ID, new Vector2(32), new Vector2(1, 0), new Vector2(0, 1)))
                {
                    _currentSong--;
                    if (_currentSong < 0)
                        _currentSong = 0;

                    Glimpse.Player.ChangeTrack(_queue[_currentSong]);
                    Glimpse.Player.Play();
                }
                
                ImGui.SameLine();
                
                if (player.TrackState == TrackState.Playing)
                {
                    if (ImGui.ImageButton("PauseButton", (IntPtr) _pauseButton.ID, new Vector2(32)))
                        player.Pause();
                }
                else
                {
                    if (ImGui.ImageButton("PlayButton", (IntPtr) _playButton.ID, new Vector2(32)))
                        player.Play();
                }
                
                ImGui.SameLine();

                if (ImGui.ImageButton("ForwardButton", (IntPtr) _skipButton.ID, new Vector2(32)))
                {
                    _currentSong++;
                    if (_currentSong >= _queue.Count)
                    {
                        Glimpse.Player.Stop();
                        _queue.Clear();
                    }
                    else
                    {
                        Glimpse.Player.ChangeTrack(_queue[_currentSong]);
                        Glimpse.Player.Play();
                    }
                }

                ImGui.SameLine();
                if (ImGui.ImageButton("StopButton", (IntPtr) _stopButton.ID, new Vector2(32)))
                {
                    _queue.Clear();
                    player.Stop();
                }
                
                ImGui.PopStyleColor();
                
                ImGui.EndDisabled();
                
                ImGui.EndChild();
            }

            //if (ImGui.BeginChild("SongPosition", ImGuiChildFlags.AutoResizeX))
            {
                int position = player.ElapsedSeconds;
                int length = player.TrackLength;
                ImGui.Text($"{position / 60:0}:{position % 60:00}");
                ImGui.SameLine();
                ImGui.SliderInt("##transport", ref position, 0, length, "");
                ImGui.SameLine();
                ImGui.Text($"{length / 60:0}:{length % 60:00}");
                
                //ImGui.EndChild();
            }
            
            ImGui.EndChild();
            
            ImGui.End();
        }
        
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

            if (ImGui.Button("+"))
            {
                AddPopup(new AddFolderPopup());
            }

            if (ImGui.BeginChild("AlbumList", ImGuiWindowFlags.HorizontalScrollbar))
            {
                foreach ((string name, Album album) in Glimpse.Database.Albums)
                {
                    if (ImGui.Selectable(name, _currentAlbum == name))
                        _currentAlbum = name;
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
            
            Album album = Glimpse.Database.Albums[_currentAlbum];

            if (ImGui.BeginTable("SongTable", 4, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX))
            {
                ImGui.TableSetupColumn("Track", ImGuiTableColumnFlags.WidthStretch,  1.0f);
                ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthStretch, 5.0f);
                ImGui.TableSetupColumn("Artist", ImGuiTableColumnFlags.WidthStretch, 3.0f);
                ImGui.TableSetupColumn("Album", ImGuiTableColumnFlags.WidthStretch, 4.0f);
                ImGui.TableHeadersRow();

                int song = 0;
                foreach (string path in album.Tracks)
                {
                    Track track = Glimpse.Database.Tracks[path];
                    TrackInfo info = Glimpse.Player.TrackInfo;
                    
                    ImGui.TableNextRow();
                    
                    ImGui.TableNextColumn();
                    if (track.TrackNumber is uint trackNumber)
                        ImGui.Text(trackNumber.ToString());

                    ImGui.TableNextColumn();
                    
                    if (ImGui.Selectable(track.Title, info.Title == track.Title && info.Album == album.Name, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        _queue.Clear();
                        _currentSong = song;

                        _queue.AddRange(album.Tracks);
                    
                        player.ChangeTrack(path);
                        player.Play();
                    }
                    
                    ImGui.TableNextColumn();
                    ImGui.Text(track.Artist);
                    ImGui.TableNextColumn();
                    ImGui.Text(track.Album);

                    song++;
                }
                
                ImGui.EndTable();
            }
            
            ImGui.End();
        }

        if (Glimpse.Player.TrackState == TrackState.Stopped && _queue.Count > 0)
        {
            _currentSong++;

            if (_currentSong >= _queue.Count)
            {
                Glimpse.Player.Stop();
                _queue.Clear();
            }
            else
            {
                Glimpse.Player.ChangeTrack(_queue[_currentSong]);
                Glimpse.Player.Play();
            }
        }
    }
    
    private void PlayerOnTrackChanged(TrackInfo info)
    {
        _albumArt?.Dispose();
        _albumArt = null;
        TrackInfo.Image art = info.AlbumArt;

        if (art == null)
            return;

        _albumArt = Renderer.CreateImage(art.Data);
    }
    
    private void PlayerOnStateChanged(TrackState state)
    {
        if (state != TrackState.Stopped)
            return;
        
        _albumArt?.Dispose();
        _albumArt = null;
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Glimpse.Database;
using Glimpse.Player;
using Hexa.NET.ImGui;
using Image = Glimpse.Graphics.Image;
using Track = Glimpse.Database.Track;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private bool _init;
    
    private string _currentDirectory;

    private List<string> _queue;
    private int _currentSong;

    private Image _defaultAlbumArt;
    private Image _albumArt;
    
    public GlimpsePlayer()
    {
        Title = "Glimpse";
        Size = new Size(1100, 650);

        _queue = new List<string>();
    }

    protected override unsafe void Initialize()
    {
        //ChangeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));

        _defaultAlbumArt = Renderer.CreateImage("Assets/Icons/Glimpse.png");
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
        Glimpse.Player.StateChanged += PlayerOnStateChanged;
        
        ImFontPtr roboto = Renderer.ImGui.AddFont("Assets/Fonts/Roboto-Regular.ttf", 20, "Roboto-20px");
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        _currentDirectory = Glimpse.Database.Albums.First().Key;
    }

    protected override unsafe void Update()
    {
        AudioPlayer player = Glimpse.Player;
        
        Renderer.Clear(Color.Black);
        
        ImGui.PushFont(Renderer.ImGui.Fonts["Roboto-20px"]);
        
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

            if (ImGui.BeginChild("TrackInfo", new Vector2(0, 0)))
            {
                ImGui.Text(player.TrackInfo.Title);
                ImGui.Text(player.TrackInfo.Artist);
                ImGui.Text(player.TrackInfo.Album);
             
                int position = player.ElapsedSeconds;
                int length = player.TrackLength;
                ImGui.Text($"{position / 60:0}:{position % 60:00}");
                ImGui.SameLine();
                ImGui.SliderInt("##transport", ref position, 0, length, "");
                ImGui.SameLine();
                ImGui.Text($"{length / 60:0}:{length % 60:00}");
                
                ImGui.SameLine();
                if (ImGui.Button("Play"))
                    player.Play();
                
                ImGui.SameLine();
                if (ImGui.Button("Pause"))
                    player.Pause();
                
                ImGui.SameLine();
                if (ImGui.Button("Prev"))
                {
                    _currentSong--;
                    if (_currentSong < 0)
                        _currentSong = 0;
                    
                    Glimpse.Player.ChangeTrack(_queue[_currentSong]);
                    Glimpse.Player.Play();
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Next"))
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
                
                ImGui.EndChild();
            }
            
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

            foreach ((string name, Album album) in Glimpse.Database.Albums)
            {
                if (ImGui.Selectable(name, _currentDirectory == name))
                    _currentDirectory = name;
            }
            
            ImGui.End();
        }
        
        if (ImGui.Begin("Songs"))
        {
            /*foreach (string file in _files)
            {
                if (ImGui.Selectable(Path.GetFileName(file)))
                {
                    player.ChangeTrack(file);
                    player.Play();
                }
            }*/

            Album album = Glimpse.Database.Albums[_currentDirectory];

            if (ImGui.BeginTable("SongTable", 4, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
            {
                ImGui.TableSetupColumn("Track");
                ImGui.TableSetupColumn("Title");
                ImGui.TableSetupColumn("Artist");
                ImGui.TableSetupColumn("Album");
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
        
        ImGui.PopFont();

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

    /*private void ChangeDirectory(string directory)
    {
        _currentDirectory = directory;
        _directories.Clear();
        _files.Clear();
        
        foreach (string directories in Directory.GetDirectories(directory))
        {
            _directories.Add(directories);
        }
        
        foreach (string file in Directory.GetFiles(directory))
        {
            _files.Add(file);
        }
    }*/
    
    private void PlayerOnTrackChanged(TrackInfo info)
    {
        _albumArt?.Dispose();
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
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using Glimpse.Player;
using Hexa.NET.ImGui;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    private string _currentDirectory;
    private List<string> _directories;
    private List<string> _files;

    private Image _albumArt;
    
    public GlimpsePlayer()
    {
        Title = "Glimpse";
        Size = new Size(800, 450);

        _directories = new List<string>();
        _files = new List<string>();
    }

    protected override void Initialize()
    {
        ChangeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        
        Glimpse.Player.TrackChanged += PlayerOnTrackChanged;
    }

    protected override void Update()
    {
        AudioPlayer player = Glimpse.Player;
        
        Renderer.Clear(Color.Black);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        ImGui.DockSpaceOverViewport();

        if (ImGui.Begin("Transport"))
        {
            Vector2 winSize = ImGui.GetContentRegionAvail();
            
            if (ImGui.BeginChild("AlbumArt", new Vector2(winSize.Y)))
            {
                if (_albumArt != null)
                    ImGui.Image((IntPtr) _albumArt.ID, new Vector2(winSize.Y));
                
                ImGui.EndChild();
            }
            
            ImGui.SameLine();

            if (ImGui.BeginChild("TrackInfo", ImGuiChildFlags.AlwaysAutoResize))
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
                
                ImGui.EndChild();
            }
            
            ImGui.End();
        }

        //if (ImGui.Begin("Albums"))
        if (ImGui.Begin("Folders"))
        {
            string newDirectory = null;

            if (ImGui.Selectable(".."))
                newDirectory = Path.GetDirectoryName(_currentDirectory);
            
            foreach (string directory in _directories)
            {
                if (ImGui.Selectable(Path.GetFileName(directory)))
                    newDirectory = directory;
            }
            
            if (newDirectory != null)
                ChangeDirectory(newDirectory);
            
            ImGui.End();
        }

        //if (ImGui.Begin("Songs"))
        if (ImGui.Begin("Files"))
        {
            foreach (string file in _files)
            {
                if (ImGui.Selectable(Path.GetFileName(file)))
                {
                    player.ChangeTrack(file);
                    player.Play();
                }
            }
            
            ImGui.End();
        }
    }

    private void ChangeDirectory(string directory)
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
    }
    
    private void PlayerOnTrackChanged(TrackInfo info)
    {
        _albumArt?.Dispose();
        TrackInfo.Image art = info.AlbumArt;

        if (art == null)
            return;

        _albumArt = Renderer.CreateImage(art.Data);
    }
}
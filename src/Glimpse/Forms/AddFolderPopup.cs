﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Glimpse.Player.Configs;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class AddFolderPopup : Popup
{
    private DirectorySource _baseDirectory;
    private Task _currentTask;

    public string Selected;
    
    public override void Update()
    {
        if (!ImGui.IsPopupOpen("Add Folder"))
        {
            _baseDirectory = new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            {
                SubDirectories = 
                [
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)),
                    new DirectorySource(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
                ]
            };

            foreach (DriveInfo info in DriveInfo.GetDrives())
            {
                _baseDirectory.SubDirectories.Add(new DirectorySource(info.Name));
            }

            Selected = "";
            
            ImGui.OpenPopup("Add Folder");
        }
        
        if (ImGui.BeginPopupModal("Add Folder", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            if (ImGui.BeginChild("FoldersList", new Vector2(300, 300), ImGuiChildFlags.AutoResizeY))
            {
                _baseDirectory.Update(ref Selected);
                ImGui.EndChild();
            }

            ImGui.InputText("##FolderPath", ref Selected, 5000);

            ImGui.BeginDisabled(string.IsNullOrWhiteSpace(Selected) || _currentTask != null);
            
            if (ImGui.Button("Add"))
            {
                ImGui.OpenPopup("Adding Folders...");
                
                Glimpse.Player.Stop();

                Glimpse.Database.AddDirectory(Selected, Glimpse.Player);
                IConfig.WriteConfig("Database/MusicDatabase", Glimpse.Database);
                
                Close();
            }
            
            ImGui.EndDisabled();
            
            ImGui.EndPopup();
        }
    }

    private class DirectorySource
    {
        public string Path;
        
        public List<DirectorySource> SubDirectories;

        public DirectorySource(string path)
        {
            Path = path;
        }

        public void Update(ref string selected)
        {
            if (SubDirectories == null)
            {
                SubDirectories = new List<DirectorySource>();
                foreach (string dir in Directory.GetDirectories(Path))
                    SubDirectories.Add(new DirectorySource(dir));
            }
            
            foreach (DirectorySource directory in SubDirectories)
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow;
                if (directory.Path == selected)
                    flags |= ImGuiTreeNodeFlags.Selected;

                string dirName = System.IO.Path.GetFileName(directory.Path);
                if (string.IsNullOrWhiteSpace(dirName))
                    dirName = directory.Path;
                
                if (ImGui.TreeNodeEx(dirName, flags))
                    directory.Update(ref selected);
                
                if (ImGui.IsItemClicked())
                    selected = directory.Path;
            }
            
            ImGui.TreePop();
        }
    }
}
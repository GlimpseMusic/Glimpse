using System;
using System.Threading;
using Glimpse.Player;

string file = args[0];

AudioPlayer player = new AudioPlayer(new PlayerSettings(48000));
player.ChangeTrack(file);
player.Play();

while (player.TrackState == TrackState.Playing)
{
    int elapsed = player.ElapsedSeconds;
    int total = player.TrackLength;
    
    Console.WriteLine($"{elapsed / 60}:{elapsed % 60:00} / {total / 60}:{total % 60:00}");
    Thread.Sleep(250);
}
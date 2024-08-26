using System.Threading;
using Glimpse.Player;

string file = args[0];

AudioPlayer player = new AudioPlayer(new PlayerSettings(48000));
player.PlayTrack(file);

while (true)
{
    Thread.Sleep(1000);
}
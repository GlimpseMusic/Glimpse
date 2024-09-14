using System;
using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Media.Playback;
using Glimpse.Player;
using TerraFX.Interop.Windows;

namespace Glimpse.Platforms;

public unsafe class WindowsPlatform : Platform
{
    //private ISystemMediaTransportControls* _transportControls;
    private SystemMediaTransportControls _transportControls;

    public override void InitializeMainWindow(IntPtr hwnd)
    {
        _transportControls = SystemMediaTransportControlsInterop.GetForWindow(hwnd);
        _transportControls.ButtonPressed += MediaButtonPressed;
        
        _transportControls.IsEnabled = true;
        _transportControls.IsPlayEnabled = true;
        _transportControls.IsPauseEnabled = true;
        _transportControls.IsNextEnabled = true;
        _transportControls.IsPreviousEnabled = true;
    }

    private void MediaButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        TransportButton button = args.Button switch
        {
            SystemMediaTransportControlsButton.Play => TransportButton.Play,
            SystemMediaTransportControlsButton.Pause => TransportButton.Pause,
            SystemMediaTransportControlsButton.Stop => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.Record => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.FastForward => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.Rewind => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.Next => TransportButton.Next,
            SystemMediaTransportControlsButton.Previous => TransportButton.Previous,
            SystemMediaTransportControlsButton.ChannelUp => throw new NotSupportedException(),
            SystemMediaTransportControlsButton.ChannelDown => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException()
        };

        InvokeButtonPressed(button);
    }

    public override void EnableDPIAwareness()
    {
        SetProcessDPIAware();
    }

    public override void EnableDarkWindow(nint hwnd)
    {
        BOOL value = true;
        TerraFX.Interop.Windows.Windows.DwmSetWindowAttribute((HWND) hwnd, 20, &value, (uint) sizeof(BOOL));
    }

    public override void SetPlayState(TrackState state, TrackInfo info)
    {
        _transportControls.DisplayUpdater.Type = MediaPlaybackType.Music;
        _transportControls.DisplayUpdater.MusicProperties.Title = info.Title;
        _transportControls.DisplayUpdater.MusicProperties.Artist = info.Artist;
        _transportControls.DisplayUpdater.MusicProperties.AlbumTitle = info.Album;
        
        _transportControls.DisplayUpdater.Update();
        
        switch (state)
        {
            case TrackState.Stopped:
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                break;
            case TrackState.Paused:
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                break;
            case TrackState.Playing:
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();
}
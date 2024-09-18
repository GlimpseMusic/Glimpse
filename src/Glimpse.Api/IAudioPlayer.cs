namespace Glimpse.Api;

public interface IAudioPlayer
{
    public void Play();

    public void Pause();

    public void Stop();

    public void Next();

    public void Previous();

    public void Seek(int second);
}
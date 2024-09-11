namespace Glimpse.Forms;

public abstract class Popup
{
    public bool IsRemoved;
    
    public abstract void Update();

    public void Close()
    {
        IsRemoved = true;
    }
}
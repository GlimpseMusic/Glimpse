using Pie;
using Pie.Windowing;
using Pie.Windowing.Events;

namespace Glimpse;

public static class Glimpse
{
    private static bool _isOpen;
    
    public static Window Window;

    public static void Run()
    {
        Window = new WindowBuilder()
            .Size(1280, 720)
            .Title("Glimpse")
            .Resizable()
            .Build(out GraphicsDevice device);

        _isOpen = true;

        while (_isOpen)
        {
            while (Window.PollEvent(out IWindowEvent winEvent))
            {
                switch (winEvent)
                {
                    case QuitEvent:
                        _isOpen = false;
                        break;
                }
            }
            
            device.Present(1);
        }
        
        device.Dispose();
        Window.Dispose();
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Glimpse.API.UI;

public interface IImmediateGUI
{
    public void Separator();

    public void Separator(string heading);

    public void Text(string text);

    public void Text(string text, uint fontSize);

    public bool Button(string text, string? tooltip = null, [CallerLineNumber] int id = 0,
        [CallerMemberName] string caller = "");

    public bool Button(string text, Size size, string? tooltip = null, [CallerLineNumber] int id = 0,
        [CallerMemberName] string caller = "");

    public bool Checkbox(string text, ref bool ticked, string? tooltip = null, [CallerLineNumber] int id = 0,
        [CallerMemberName] string caller = "");

    public bool Dropdown(string label, ref int value, ReadOnlySpan<string> items, string? tooltip = null,
        [CallerLineNumber] int id = 0, [CallerMemberName] string caller = "");

    public bool Input(string? hint, ref string text, string? label = null, string? tooltip = null,
        bool returnTrueOnlyOnEnter = true, [CallerLineNumber] int id = 0, [CallerMemberName] string caller = "");

    public bool Slider(string label, ref int number, int min, int max, string? tooltip = null,
        [CallerLineNumber] int id = 0, [CallerMemberName] string caller = "");

    public bool Slider(string label, ref float number, float min, float max, string? tooltip = null,
        [CallerLineNumber] int id = 0, [CallerMemberName] string caller = "");
}
using Glimpse.API;
using Glimpse.API.UI;
using OpenMPT.NET;

namespace Glimpse.OpenMPT;

public class MptPlugin : IPlugin
{
    private IAudioPlayer _player;
    private bool _initialized;

    private MptCodec _codec;

    public bool IsInitialized => _initialized;

    public IConfig? Config
    {
        get => _codec.Config;
        set => _codec.Config = (MptConfig) value!;
    }

    public void Initialize(IGlimpse glimpse)
    {
        _player = glimpse.Player;
        
        if (!glimpse.ConfigManager.TryGetConfig("MPT", out MptConfig config))
        {
            config = new MptConfig();
            glimpse.ConfigManager.WriteConfig("MPT", config);
        }

        _codec = new MptCodec(config);
        _player.RegisterCodec(_codec);

        _initialized = true;
    }

    public void OnGUI(IImmediateGUI ui)
    {
        ui.Checkbox("Emulate Amiga Resampler", ref _codec.Config.EmulateAmigaResampler, tooltip: "If enabled, the amiga resampler will be used for .mod files.\nOtherwise, the current Resampler Mode will be used.");
        //ui.Checkbox("Fade Out at End", ref _codec.Config.FadeOutAtEnd); todo this doesn't actually work

        int resamplerFilter = (int) _codec.Config.ResamplerFilter;
        if (ui.Dropdown("Resampler Mode", ref resamplerFilter, ["Default", "None", "Linear", "Cubic", "Sinc"], tooltip: "Change the resampling quality."))
            _codec.Config.ResamplerFilter = (Filter) resamplerFilter;
    }

    public void Dispose()
    {
        if (!_initialized)
            return;
        _initialized = false;
        
        _player.DeregisterCodec(_codec);
    }
}
#include "AudioDevice.h"

#include <fmt/format.h>

#include <stdexcept>

void AudioCallback(void *userdata, SDL_AudioStream *stream, int additional_amount, int total_amount)
{

}

namespace gmp
{
    AudioDevice::AudioDevice(sl::Context& context, uint32_t sampleRate) : _context(context), _sampleRate(sampleRate) {}

    AudioDevice::~AudioDevice()
    {
        // stop will close the audio device, so we can just call that here
        Stop();
    }

    void AudioDevice::Start()
    {
        if (_device)
            return;

        SDL_AudioSpec spec
        {
            .format = SDL_AUDIO_F32,
            .channels = 2,
            .freq = static_cast<int>(48000)
        };

        _device = SDL_OpenAudioDeviceStream(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec, AudioCallback, &_context);
        if (!_device)
            throw std::runtime_error(fmt::format("Failed to open audio device! {}", SDL_GetError()));

        if (!SDL_ResumeAudioStreamDevice(_device))
            throw std::runtime_error(fmt::format("Failed to resume audio device! {}", SDL_GetError()));
    }

    void AudioDevice::Stop()
    {
        if (!_device)
            return;

        SDL_DestroyAudioStream(_device);
        _device = nullptr;
    }
}

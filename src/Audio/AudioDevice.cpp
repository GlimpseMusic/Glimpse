#include "AudioDevice.h"

#include <stdexcept>
#include <format>
#include <iostream>

void AudioCallback(void *userdata, SDL_AudioStream *stream, int additional_amount, int total_amount)
{
    auto context = static_cast<mixr::Context*>(userdata);
    std::cout << "Callback" << std::endl;
}

namespace Glimpse
{
    AudioDevice::AudioDevice(mixr::Context* context, const uint32_t sampleRate) : _context(context)
    {
        if (!SDL_Init(SDL_INIT_AUDIO))
            throw std::runtime_error(std::format("Failed to initialize SDL: {}", SDL_GetError()));

        const SDL_AudioSpec spec
        {
            .format = SDL_AUDIO_F32LE,
            .channels = 2,
            .freq = static_cast<int32_t>(sampleRate)
        };

        _stream = SDL_OpenAudioDeviceStream(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec, AudioCallback, _context);
        if (!_stream)
            throw std::runtime_error(std::format("Failed to open audio device: {}", SDL_GetError()));

        SDL_ResumeAudioStreamDevice(_stream);
    }

    AudioDevice::~AudioDevice()
    {
        SDL_DestroyAudioStream(_stream);
    }
}
#include "AudioDevice.h"

#include <stdexcept>
#include <format>
#include <iostream>

void AudioCallback(void *userdata, SDL_AudioStream *stream, int additional_amount, int total_amount)
{
    const auto context = static_cast<mixr::Context*>(userdata);

    constexpr int bufferSize = 512;
    float buffer[bufferSize];

    while (additional_amount > 0)
    {
        const int total = std::min(additional_amount, bufferSize);
        context->MixToStereoF32Buffer(buffer, bufferSize / 4);
        SDL_PutAudioStreamData(stream, buffer, total);
        additional_amount -= total;
    }
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
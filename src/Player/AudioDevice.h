#pragma once

#include "Common.h"
#include <SDL3/SDL.h>
#include <cstdint>

namespace gmp
{
    class AudioDevice final
    {
        sl::Context& _context;
        uint32_t _sampleRate;
        SDL_AudioStream* _device{};

    public:
        explicit AudioDevice(sl::Context& context, uint32_t sampleRate);
        ~AudioDevice();

        // Start the audio device playback
        void Start();

        // Stop the audio device playback
        void Stop();
    };
}
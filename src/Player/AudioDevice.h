#pragma once

#include "Common.h"
#include <SDL3/SDL_audio.h>
#include <cstdint>

namespace gmp
{
    class AudioDevice final
    {
        sl::Context& _context;
        uint32_t _sample_rate;
        SDL_AudioStream* _device{};

    public:
        explicit AudioDevice(sl::Context& context, uint32_t sample_rate);
        ~AudioDevice();

        // start the audio device playback
        void start();

        // stop the audio device playback
        void stop();
    };
}
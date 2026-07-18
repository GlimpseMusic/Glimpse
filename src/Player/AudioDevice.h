#pragma once

#include <SDL3/SDL_audio.h>
#include <cstdint>

namespace gmp
{
    class AudioDevice final
    {
    public:
        explicit AudioDevice(uint32_t sample_rate);
        ~AudioDevice();

        void start();
        void stop();
    };
}
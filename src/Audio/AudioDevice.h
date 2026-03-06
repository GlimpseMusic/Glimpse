#pragma once

#include <cstdint>

#include <SDL3/SDL.h>
#include <mixr/mixr.hpp>

namespace Glimpse
{
    class AudioDevice final
    {
        SDL_AudioStream* _stream;
        mixr::Context* _context;

    public:
        explicit AudioDevice(mixr::Context* context, uint32_t sampleRate);
        ~AudioDevice();
    };
}
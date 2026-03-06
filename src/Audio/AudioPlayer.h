#pragma once

#include <cstdint>

#include <mixr/mixr.hpp>

#include "AudioDevice.h"

namespace Glimpse
{
    struct AudioPlayerConfig
    {
        uint32_t SampleRate;
        float Volume;
        double Speed;
    };

    class AudioPlayer final
    {
        std::unique_ptr<mixr::Context> _context;
        std::unique_ptr<AudioDevice> _device;

    public:
        explicit AudioPlayer(const AudioPlayerConfig& config);
    };
}
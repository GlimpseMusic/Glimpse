#pragma once

#include <cstdint>

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
    public:
        explicit AudioPlayer(const AudioPlayerConfig& config);
    };
}
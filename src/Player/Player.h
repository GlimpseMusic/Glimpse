#pragma once

#include "Common.h"
#include "AudioDevice.h"

#include <cstdint>

// gmp, short for Glimpse Music Player
// todo i don't like gmp. reminds me of GIMP.
namespace gmp
{
    struct PlayerConfig
    {
        uint32_t sample_rate;
    };

    class Player final
    {
        std::unique_ptr<sl::Context> _context;
        std::unique_ptr<AudioDevice> _device;

    public:
        explicit Player(const PlayerConfig& config);
    };
}
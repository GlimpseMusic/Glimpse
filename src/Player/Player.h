#pragma once

#include <Slant++/Slant.h>

#include <cstdint>

namespace sl = Slant;

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

    public:
        explicit Player(const PlayerConfig& config);
    };
}
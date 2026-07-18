#pragma once

#include <Slant++/Slant.h>

#include <cstdint>

namespace sl = Slant;

namespace gmp
{
    struct PlayerConfig
    {
        uint32_t SampleRate;
    };

    class Player final
    {
        std::unique_ptr<sl::Context> _context;

    public:
        explicit Player(const PlayerConfig& config);
    };
}
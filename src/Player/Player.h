#pragma once

#include "Common.h"
#include "AudioDevice.h"

#include <cstdint>
#include <vector>
#include <filesystem>

// gmp, short for Glimpse Music Player
// todo i don't like gmp. reminds me of GIMP.
namespace gmp
{
    enum class PlayState
    {
        Stopped,
        Paused,
        Playing
    };

    struct PlayerConfig
    {
        uint32_t SampleRate;
    };

    class Player final
    {
        std::unique_ptr<sl::Context> _context;
        std::unique_ptr<AudioDevice> _device;

    public:
        explicit Player(const PlayerConfig& config);

        // Get the current playback state.
        PlayState State() { return PlayState::Stopped; }
    };
}
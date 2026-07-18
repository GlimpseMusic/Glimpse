#include "Player.h"

namespace gmp
{
    Player::Player(const PlayerConfig& config)
    {
        _context = std::make_unique<sl::Context>(config.SampleRate);
        _device = std::make_unique<AudioDevice>(*_context, config.SampleRate);
    }
}

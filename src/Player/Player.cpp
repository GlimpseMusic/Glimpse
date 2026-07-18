#include "Player.h"

namespace gmp
{
    Player::Player(const PlayerConfig& config)
    {
        _context = std::make_unique<sl::Context>(config.sample_rate);
        _device = std::make_unique<AudioDevice>(*_context, config.sample_rate);
    }
}

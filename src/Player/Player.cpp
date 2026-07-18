#include "Player.h"

namespace gmp
{
    Player::Player(const PlayerConfig& config)
    {
        _context = std::make_unique<sl::Context>(config.sample_rate);
    }
}

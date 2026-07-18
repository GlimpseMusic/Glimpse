#include "Player/Player.h"

int main()
{
    gmp::PlayerConfig config
    {
        .sample_rate = 48000
    };

    auto player = std::make_unique<gmp::Player>(config);

    return 0;
}

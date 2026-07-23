#include <thread>

#include "Player/Player.h"

int main(int argc, char* argv[])
{
    gmp::PlayerConfig config
    {
        .SampleRate = 48000
    };

    auto player = std::make_unique<gmp::Player>(config);
    auto _ = player->PlayTrack(argv[1]);

    while (player->State() != gmp::PlayState::Stopped)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(1000));
    }

    return 0;
}

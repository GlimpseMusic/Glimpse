#include <thread>

#include "Player/Player.h"

int main(int argc, char* argv[])
{
    gmp::PlayerConfig config
    {
        .SampleRate = 48000
    };

    auto player = std::make_unique<gmp::Player>(config);
    for (int i = 1; i < argc; i++)
        player->QueueTrack(argv[i]);
    auto _ = player->PlayTrack(0);

    while (player->State() != gmp::PlayState::Stopped)
    {
        std::this_thread::sleep_for(std::chrono::milliseconds(1000));
    }

    return 0;
}

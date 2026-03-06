#include <thread>

#include "Audio/AudioPlayer.h"

using namespace Glimpse;

int main(int argc, char* argv[])
{
    constexpr AudioPlayerConfig config
    {
        .SampleRate = 48000,
        .Volume = 1.0f,
        .Speed = 1.0
    };

    AudioPlayer player(config);

    std::this_thread::sleep_for(std::chrono::seconds(1));

    return 0;
}
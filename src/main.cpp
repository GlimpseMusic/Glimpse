#include "Audio/AudioPlayer.h"

using namespace Glimpse;

int main(int argc, char* argv[])
{
    AudioPlayerConfig config
    {
        .SampleRate = 48000,
        .Volume = 1.0f,
        .Speed = 1.0
    };

    AudioPlayer player(config);

    return 0;
}
#include "AudioPlayer.h"

namespace Glimpse
{
    AudioPlayer::AudioPlayer(const AudioPlayerConfig& config)
    {
        _context = std::make_unique<mixr::Context>(config.SampleRate);

        // TODO: Can this be done using *_context?
        _device = std::make_unique<AudioDevice>(_context.get(), config.SampleRate);
    }
}
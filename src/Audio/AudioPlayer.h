#pragma once

#include "Glimpse/IAudioPlayer.h"
#include "AudioDevice.h"

#include <mixr/mixr.hpp>

#include <cstdint>

namespace Glimpse
{
    struct AudioPlayerConfig
    {
        uint32_t SampleRate;
        float Volume;
        double Speed;
    };

    class AudioPlayer final : public IAudioPlayer
    {
        std::unique_ptr<mixr::Context> _context;
        std::unique_ptr<AudioDevice> _device;

    public:
        explicit AudioPlayer(const AudioPlayerConfig& config);
        [[nodiscard]] TrackState State() const override { return TrackState::Stopped; }
    };
}
#pragma once

namespace Glimpse
{
    enum class TrackState
    {
        Stopped,
        Paused,
        Playing
    };

    class IAudioPlayer
    {
    public:
        virtual ~IAudioPlayer() = default;
        [[nodiscard]] virtual TrackState State() const = 0;
    };
}
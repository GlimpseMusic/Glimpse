#pragma once

#include "Common.h"
#include "AudioDevice.h"

#include <Slant++/Stream/AudioStream.h>

#include <cstdint>
#include <vector>
#include <string>

// gmp, short for Glimpse Music Player
// todo i don't like gmp. reminds me of GIMP.
namespace gmp
{
    enum class PlayState
    {
        Stopped,
        Paused,
        Playing
    };

    enum class QueueSlot
    {
        // Insert at the end of the queue.
        AtEnd,

        // Insert after the current track.
        Next
    };

    struct PlayerConfig
    {
        uint32_t SampleRate;
    };

    class Player final
    {
        std::unique_ptr<sl::Context> _context;
        std::unique_ptr<AudioDevice> _device;

        std::vector<std::string> _queuedTracks; // the queued tracks, in order of queue.
        std::vector<size_t> _queueOrder; // the queue/play order, used for shuffle without affecting the original queue.

        std::vector<uint8_t> _workBuffer;
        std::vector<std::unique_ptr<sl::AudioBuffer>> _buffers;

        std::unique_ptr<sls::AudioStream> _stream{};
        std::unique_ptr<sl::AudioSource> _streamSource{};

    public:
        explicit Player(const PlayerConfig& config);

        // Get the current playback state.
        PlayState State();

        void QueueTrack(const std::string& path, QueueSlot slot = QueueSlot::AtEnd);
        [[nodiscard]] bool PlayTrack(size_t queueIndex);
    };
}
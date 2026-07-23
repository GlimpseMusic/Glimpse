#pragma once

#include "Common.h"
#include "AudioDevice.h"

#include <Slant++/Stream/AudioStream.h>

#include <cstdint>
#include <vector>
#include <queue>
#include <string>
#include <thread>
#include <mutex>
#include <condition_variable>

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
        Next,

        // Clear the queue, then insert.
        Clear
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
        size_t _currentTrackIndex{};

        std::vector<uint8_t> _workBuffer;
        std::vector<std::unique_ptr<sl::AudioBuffer>> _buffers;
        size_t _currentBuffer{};

        std::unique_ptr<sls::AudioStream> _stream{};
        std::unique_ptr<sl::AudioSource> _streamSource{};

        std::thread _bufferProcessThread;
        std::mutex _lockMutex;
        std::condition_variable _cv;
        bool _shouldExit{};

        static void BufferProcessThread(void* userData);
        static void StreamCallback(void* userData);

        static void SourceStateChangedCallback(sl::SourceState state, void* userData);

        void NextTrack();

    public:
        explicit Player(const PlayerConfig& config);
        ~Player();

        // Get the current playback state.
        PlayState State();

        // Add a track to the queue, at the given queue slot.
        void QueueTrack(const std::string& path, QueueSlot slot = QueueSlot::AtEnd);

        // Play a track at the given index.
        [[nodiscard]] bool PlayTrack(size_t queueIndex);

        // Play a single track from the given path.
        [[nodiscard]] bool PlayTrack(const std::string& path);
    };
}
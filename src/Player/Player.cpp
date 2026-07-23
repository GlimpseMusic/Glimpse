#include "Player.h"

#include "Codecs/FLACCodec.h"
#include "Codecs/MP3Codec.h"

#include <cassert>
#include <filesystem>

namespace gmp
{
    void Player::BufferProcessThread(void* userData)
    {
        auto player = static_cast<Player*>(userData);

        while (true)
        {
            std::unique_lock lock(player->_lockMutex);
            player->_cv.wait(lock); // wait until the thread is notified to continue

            if (player->_shouldExit)
                return;

            // todo i really hate this
            sls::AudioStream& stream = *player->_stream;
            auto& workBuffer = player->_workBuffer;
            auto& buffers = player->_buffers;
            sl::AudioSource& source = *player->_streamSource;
            size_t* currentBuffer = &player->_currentBuffer;

            size_t gotBytes = stream.GetBuffer(workBuffer.data(), workBuffer.size());
            if (gotBytes == 0)
            {
                source.SetLooping(false); // once there's no more data, disable looping so the source can fully stop.
                continue;
            }

            // update with the number of got bytes, in case it returns less than the work buffer size
            buffers[*currentBuffer]->Update(workBuffer.data(), gotBytes);
            source.SubmitBuffer(buffers[*currentBuffer].get());
            *currentBuffer = (*currentBuffer + 1) % buffers.size();
        }
    }

    void Player::StreamCallback(void* userData)
    {
        auto player = static_cast<Player*>(userData);
        player->_cv.notify_all();
    }

    void Player::SourceStateChangedCallback(sl::SourceState state, void* userData)
    {
        if (state != sl::SourceState::Stopped)
            return;

        auto player = static_cast<Player*>(userData);
        player->NextTrack();
    }

    void Player::NextTrack()
    {
        // continuously increment the track index until PlayTrack returns true
        // causes the player to skip over invalid tracks instead of displaying an error
        do
        {
            _currentTrackIndex++;
            if (_currentTrackIndex >= _queuedTracks.size())
                // Stop();
                return;
        }
        while (!PlayTrack(_currentTrackIndex));
    }

    Player::Player(const PlayerConfig& config)
    {
        _context = std::make_unique<sl::Context>(config.SampleRate);
        _device = std::make_unique<AudioDevice>(*_context, config.SampleRate);

        _codecs.emplace_back(std::make_unique<FLACCodec>());
        _codecs.emplace_back(std::make_unique<MP3Codec>());

        // 1 second long buffer. multiply by 2 for 2 channels, and since it is in bytes, multiply by 4 for 32-bit
        _workBuffer = std::vector<uint8_t>(config.SampleRate * 2 * 4);

        constexpr size_t numBuffers = 2;
        _buffers.reserve(numBuffers);
        for (size_t i = 0; i < numBuffers; i++)
            _buffers.push_back(_context->CreateBuffer(nullptr, 0));

        _bufferProcessThread = std::thread(BufferProcessThread, this);
    }

    Player::~Player()
    {
        {
            std::unique_lock lock(_lockMutex);
            _shouldExit = true;
        }
        _cv.notify_all();
        _bufferProcessThread.join(); // we must join the thread as not doing so causes crashes! yay?
    }

    PlayState Player::State()
    {
        if (!_streamSource)
            return PlayState::Stopped;

        switch (_streamSource->State())
        {
            case Slant::SourceState::Stopped:
                //Unreachable();
                return PlayState::Stopped;
            case Slant::SourceState::Paused:
                return PlayState::Paused;
            case Slant::SourceState::Playing:
                return PlayState::Playing;
        }

        Unreachable();
    }

    void Player::QueueTrack(const std::string& path, QueueSlot slot)
    {
        switch (slot)
        {
            case QueueSlot::AtEnd:
            {
                auto index = _queuedTracks.size();
                _queuedTracks.push_back(path);
                _queueOrder.push_back(index);
                break;
            }
            case QueueSlot::Next:
                throw std::logic_error("Not yet implemented.");
            case QueueSlot::Clear:
                _queuedTracks.clear();
                _queueOrder.clear();
                _queuedTracks.push_back(path);
                _queueOrder.push_back(0); // since we just cleared it the index will always be 0
        }
    }

    bool Player::PlayTrack(size_t queueIndex)
    {
        assert(_queuedTracks.size() == _queueOrder.size());

        if (queueIndex >= _queuedTracks.size())
            return false;

        _currentTrackIndex = queueIndex;
        auto trackPath = _queuedTracks[_queueOrder[_currentTrackIndex]];
        if (!std::filesystem::exists(trackPath))
            return false;

        std::unique_ptr<sls::AudioStream> stream{};
        for (const auto& codec : _codecs)
        {
            if (codec->CheckFileSupport(trackPath))
                stream = codec->CreateStream(trackPath);
        }

        // no suitable codec was found so the track cannot be played
        if (!stream)
            return false;

        if (_streamSource)
            _streamSource->Stop();

        // force the stream thread to finish processing stuff before replacing the stream its reading from
        {
            std::unique_lock lock(_lockMutex);
            _stream = std::move(stream);
        }

        sl::SourceDescription sourceDesc
        {
            .Type = sl::SourceType::PCM,
            .Format = _stream->Format()
        };
        _streamSource = _context->CreateSource(sourceDesc);
        _streamSource->SetBufferFinishedCallback(StreamCallback, this);
        _streamSource->SetStateChangedCallback(SourceStateChangedCallback, this);
        _streamSource->SetLooping(true); // enable looping to ensure the source keeps playing in case of slowdowns. not elegant but it works

        _currentBuffer = 0;
        for (const auto& buffer : _buffers)
        {
            size_t gotBytes = _stream->GetBuffer(_workBuffer.data(), _workBuffer.size());
            // if a track is less than 1-2 seconds then it might not fill the initial buffers
            // in that case, set looping to false and don't upload to the buffer.
            if (gotBytes == 0)
            {
                _streamSource->SetLooping(false);
                break;
            }

            buffer->Update(_workBuffer.data(), gotBytes);
            _streamSource->SubmitBuffer(buffer.get());
        }

        _device->Start();
        _streamSource->Play();

        return true;
    }

    bool Player::PlayTrack(const std::string& path)
    {
        QueueTrack(path, QueueSlot::Clear);
        return PlayTrack(0);
    }
}

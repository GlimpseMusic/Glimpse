#include "Player.h"

#include <Slant++/Stream/Flac.h>

#include <cassert>
#include <filesystem>

namespace gmp
{
    // todo threading !!!!!!!!!
    void Player::StreamCallback(void* userData)
    {
        auto player = static_cast<Player*>(userData);
        sls::AudioStream& stream = *player->_stream;
        auto& workBuffer = player->_workBuffer;
        auto& buffers = player->_buffers;
        sl::AudioSource& streamSource = *player->_streamSource;
        size_t* currentBuffer = &player->_currentBuffer;

        stream.GetBuffer(workBuffer.data(), workBuffer.size());
        buffers[*currentBuffer]->Update(workBuffer.data(), workBuffer.size());
        streamSource.SubmitBuffer(buffers[*currentBuffer].get());
        *currentBuffer = (*currentBuffer + 1) % buffers.size();
    }

    Player::Player(const PlayerConfig& config)
    {
        _context = std::make_unique<sl::Context>(config.SampleRate);
        _device = std::make_unique<AudioDevice>(*_context, config.SampleRate);

        // 1 second long buffer. multiply by 2 for 2 channels, and since it is in bytes, multiply by 4 for 32-bit
        _workBuffer = std::vector<uint8_t>(config.SampleRate * 2 * 4);

        constexpr size_t numBuffers = 2;
        _buffers.reserve(numBuffers);
        for (size_t i = 0; i < numBuffers; i++)
            _buffers.push_back(_context->CreateBuffer(nullptr, 0));
    }

    PlayState Player::State()
    {
        if (!_streamSource)
            return PlayState::Stopped;

        switch (_streamSource->State())
        {
            case Slant::SourceState::Stopped:
                Unreachable();
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
        }
    }

    bool Player::PlayTrack(size_t queueIndex)
    {
        assert(_queuedTracks.size() == _queueOrder.size());

        if (queueIndex >= _queuedTracks.size())
            return false;

        auto trackPath = _queuedTracks[_queueOrder[queueIndex]];
        if (!std::filesystem::exists(trackPath))
            return false;

        if (_streamSource)
            _streamSource->Stop();

        _stream = std::make_unique<sls::Flac>(trackPath);

        sl::SourceDescription sourceDesc
        {
            .Type = sl::SourceType::PCM,
            .Format = _stream->Format()
        };
        _streamSource = _context->CreateSource(sourceDesc);
        _streamSource->SetBufferFinishedCallback(StreamCallback, this);

        for (const auto& buffer : _buffers)
        {
            _stream->GetBuffer(_workBuffer.data(), _workBuffer.size());
            buffer->Update(_workBuffer.data(), _workBuffer.size());
            _streamSource->SubmitBuffer(buffer.get());
        }

        _streamSource->Play();
        _device->Start();

        return true;
    }
}

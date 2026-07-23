#pragma once

#include "Codec.h"

namespace gmp
{
    class MP3Codec final : public Codec
    {
    public:
        std::unique_ptr<sls::AudioStream> CreateStream(const std::filesystem::path& path) override;
        bool CheckFileSupport(const std::filesystem::path& path) override;
    };
}

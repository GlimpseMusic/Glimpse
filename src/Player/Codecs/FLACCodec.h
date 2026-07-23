#pragma once

#include "Codec.h"

namespace gmp
{
    class FLACCodec final : public Codec
    {
    public:
        std::unique_ptr<sls::AudioStream> CreateStream(const std::filesystem::path& path) override;
        bool CheckFileSupport(const std::filesystem::path& path) override;
    };
}
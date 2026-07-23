#pragma once

#include "../Common.h"

#include <filesystem>
#include <memory>

namespace gmp
{
    class Codec
    {
    public:
        virtual ~Codec() = default;

        virtual std::unique_ptr<sls::AudioStream> CreateStream(const std::filesystem::path& path) = 0;
        virtual bool CheckFileSupport(const std::filesystem::path& path) = 0;
    };
}
#pragma once

#include "../Common.h"

#include <string>
#include <memory>

namespace gmp
{
    class Codec
    {
    public:
        virtual ~Codec() = default;

        virtual std::unique_ptr<sls::AudioStream> CreateStream(const std::string& path) = 0;
        virtual bool CheckFileSupport(const std::string& path) = 0;
    };
}
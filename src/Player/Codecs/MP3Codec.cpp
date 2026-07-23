#include "MP3Codec.h"

#include "../Common.h"
#include <Slant++/Stream/Mp3.h>

#include <algorithm>

namespace gmp
{
    std::unique_ptr<sls::AudioStream> MP3Codec::CreateStream(const std::filesystem::path& path)
    {
        return std::make_unique<sls::Mp3>(path);
    }

    bool MP3Codec::CheckFileSupport(const std::filesystem::path& path)
    {
        auto ext = path.extension().string();
        std::ranges::transform(ext, ext.begin(), tolower);
        return ext == ".mp3";
    }
}

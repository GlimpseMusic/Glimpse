#include "FLACCodec.h"

#include "../Common.h"
#include <Slant++/Stream/Flac.h>

#include <algorithm>

namespace gmp
{
    std::unique_ptr<sls::AudioStream> FLACCodec::CreateStream(const std::filesystem::path& path)
    {
        return std::make_unique<sls::Flac>(path);
    }

    bool FLACCodec::CheckFileSupport(const std::filesystem::path& path)
    {
        auto ext = path.extension().string();
        std::ranges::transform(ext, ext.begin(), tolower);
        return path.extension() == ".flac";
    }
}

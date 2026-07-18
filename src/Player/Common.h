#pragma once

#include <Slant++/Slant.h>

#include "Slant++/Stream/AudioStream.h"

// todo probably should upstream this
namespace sl = Slant;
namespace sls = Slant::Stream;

namespace gmp
{
    inline void Unreachable()
    {
        // https://mahmoudimus.com/til/2026/01/c20-doesnt-have-stdunreachable-heres-what-to-use-instead/
#if defined(_MSC_VER) && !defined(__clang__)
        __assume(false);
#else
        __builtin_unreachable();
#endif
    }
}

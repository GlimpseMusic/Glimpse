cmake_minimum_required(VERSION 4.2)
project(Glimpse)

set(CMAKE_CXX_STANDARD 20)

file(GLOB_RECURSE GLIMPSE_SRC CONFIGURE_DEPENDS
        src/*.h
        src/*.cpp
)

add_executable(Glimpse ${GLIMPSE_SRC})

add_subdirectory(external/Slant)
target_link_libraries(Glimpse PRIVATE Slant++)

add_subdirectory(external/SDL)
target_link_libraries(Glimpse PRIVATE SDL3::SDL3)

add_subdirectory(external/fmt)
target_link_libraries(Glimpse PRIVATE fmt::fmt)
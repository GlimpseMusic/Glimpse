cmake_minimum_required(VERSION 4.2)
project(Glimpse)

set(CMAKE_CXX_STANDARD 20)

add_executable(Glimpse main.cpp)

add_subdirectory(external/Slant)
target_link_libraries(Glimpse PRIVATE Slant++)

add_subdirectory(external/SDL)
target_link_libraries(Glimpse PRIVATE SDL3::SDL3)
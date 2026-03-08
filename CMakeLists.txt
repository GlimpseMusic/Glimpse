cmake_minimum_required(VERSION 3.20)
project(Glimpse)

set(CMAKE_CXX_STANDARD 20)

file(GLOB_RECURSE GLIMPSE_API CONFIGURE_DEPENDS include/*.h)
file(GLOB_RECURSE GLIMPSE_SRC CONFIGURE_DEPENDS src/*.h src/*.cpp)

add_executable(Glimpse ${GLIMPSE_API} ${GLIMPSE_SRC})
target_include_directories(Glimpse PRIVATE include)

add_subdirectory(lib/SDL)
target_link_libraries(Glimpse PRIVATE SDL3::SDL3)

add_subdirectory(lib/mixr)
target_link_libraries(Glimpse PRIVATE mixr)

add_subdirectory(lib/glad)
target_link_libraries(Glimpse PRIVATE glad)
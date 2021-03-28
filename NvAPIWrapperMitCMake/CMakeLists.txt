cmake_minimum_required(VERSION 3.10)

# set the project name
project(NvAPIWrapper)
set(default_build_type "Release")
set (CMAKE_BUILD_TYPE Release)

# specify the C++ standard
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED True)
#set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /Yustdafx.h /FIstdafx.h")

include_directories (include ${PROJECT_BINARY_DIR})

file (GLOB SOURCES "./*.cpp")

add_library(NvAPIWrapper SHARED ${SOURCES})
target_link_libraries (NvAPIWrapper LINK_PUBLIC ../lib/nvapi)


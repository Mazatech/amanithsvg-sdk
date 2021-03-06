set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_VERSION 3)
set(CMAKE_SYSTEM_PROCESSOR mipsel)
# the 2 following variables are used internally by building scripts (i.e. not related to CMake variables)
set(OS_LINUX true CACHE STRING "Linux operating system (used internally, not related to CMake variables)")
set(ARCH_MIPS true CACHE STRING "mips little endian architecture (used internally, not related to CMake variables)")

set(CMAKE_C_COMPILER /usr/bin/mipsel-linux-gnu-gcc-6)
set(CMAKE_CXX_COMPILER /usr/bin/mipsel-linux-gnu-g++-6)
set(CMAKE_AR /usr/bin/mipsel-linux-gnu-ar CACHE FILEPATH "Archiver for Linux mipsel")
set(CMAKE_LD /usr/bin/mipsel-linux-gnu-ld CACHE FILEPATH "Linker for Linux mipsel")
set(CMAKE_STRIP /usr/bin/mipsel-linux-gnu-strip CACHE FILEPATH "Stripper for Linux mipsel")

set(CMAKE_FIND_ROOT_PATH /usr/mipsel-linux-gnu)
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)

set(CMAKE_IGNORE_PATH /usr/lib/x86_64-linux-gnu/ /usr/lib/x86_64-linux-gnu/lib/)

set(CC_NO_UNINITIALIZED_WARNING "-Wno-uninitialized" CACHE STRING "No uninitialized variable warning for Linux mipsel compiler")

# common flags
set(LINUX_COMMON_C_FLAGS "-O2 -ffast-math -fno-exceptions -fno-strict-aliasing -fomit-frame-pointer -Wall -W")
set(LINUX_COMMON_CXX_FLAGS "${LINUX_COMMON_C_FLAGS} -fno-rtti")

# architecture specific flags
set(LINUX_ARCH_C_FLAGS "-march=mips32r2 -mtune=mips32r2 -EL -mabi=32")

# flags for Release build type or configuration
set(CMAKE_C_FLAGS_RELEASE "${LINUX_COMMON_C_FLAGS} ${LINUX_ARCH_C_FLAGS}" CACHE STRING "Compiler C flags used by release builds for Linux mipsel")
set(CMAKE_CXX_FLAGS_RELEASE "${LINUX_COMMON_CXX_FLAGS} ${LINUX_ARCH_C_FLAGS}" CACHE STRING "Compiler C++ flags used by release builds for Linux mipsel")

# linker flags to be used to create shared libraries
set(CMAKE_SHARED_LINKER_FLAGS "-Wl,--no-undefined" CACHE STRING "Shared libraries linker flags for Linux mipsel")
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_VERSION 3)
set(CMAKE_SYSTEM_PROCESSOR ppc64le)
# the 2 following variables are used internally by building scripts (i.e. not related to CMake variables)
set(OS_LINUX true CACHE STRING "Linux operating system (used internally, not related to CMake variables)")
set(ARCH_PPC64 true CACHE STRING "PowerPC 64 little endian architecture (used internally, not related to CMake variables)")

# assembler
set(CMAKE_ASM_COMPILER /usr/bin/powerpc64le-linux-gnu-as)
set(CMAKE_ASM_PREPROCESSOR /usr/bin/powerpc64le-linux-gnu-gcc CACHE FILEPATH "Preprocessor for Linux ppc64el")
set(CMAKE_ASM_COMPILE_OBJECT
    "${CMAKE_ASM_PREPROCESSOR} <DEFINES> <INCLUDES> $(C_FLAGS) $(C_DEFINES) -o <OBJECT>_pp.s -E <SOURCE>"
    "${CMAKE_ASM_COMPILER} ${CMAKE_ASM_FLAGS} -o <OBJECT> <OBJECT>_pp.s")
set(CMAKE_C_COMPILER /usr/bin/powerpc64le-linux-gnu-gcc)
set(CMAKE_CXX_COMPILER /usr/bin/powerpc64le-linux-gnu-g++)
set(CMAKE_AR /usr/bin/powerpc64le-linux-gnu-ar CACHE FILEPATH "Archiver for Linux ppc64el")
set(CMAKE_LD /usr/bin/powerpc64le-linux-gnu-ld CACHE FILEPATH "Linker for Linux ppc64el")
set(CMAKE_STRIP /usr/bin/powerpc64le-linux-gnu-strip CACHE FILEPATH "Stripper for Linux ppc64el")

# in order to check compilers, cmake will try (as default) to build test applications
# which is not always possible (especially in cross mobile environments); so we skip
# the generation of test executables
set(CMAKE_TRY_COMPILE_TARGET_TYPE "STATIC_LIBRARY")

set(CMAKE_FIND_ROOT_PATH /usr/powerpc64le-linux-gnu)
set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)

set(CMAKE_IGNORE_PATH /usr/lib/x86_64-linux-gnu/ /usr/lib/x86_64-linux-gnu/lib/)

# common flags
set(LINUX_COMMON_C_FLAGS "-O2 -ffast-math -fno-exceptions -fno-strict-aliasing -fomit-frame-pointer -Wall -W")
set(LINUX_COMMON_CXX_FLAGS "${LINUX_COMMON_C_FLAGS} -fno-rtti")

# architecture specific flags
set(LINUX_ARCH_C_FLAGS "-mcpu=powerpc64le -mtune=powerpc64le -mlittle-endian")

# flags for Release build type or configuration
set(CMAKE_C_FLAGS_RELEASE "${LINUX_COMMON_C_FLAGS} ${LINUX_ARCH_C_FLAGS}" CACHE STRING "Compiler C flags used by release builds for Linux ppc64el")
set(CMAKE_CXX_FLAGS_RELEASE "${LINUX_COMMON_CXX_FLAGS} ${LINUX_ARCH_C_FLAGS}" CACHE STRING "Compiler C++ flags used by release builds for Linux ppc64el")

# linker flags to be used to create shared libraries
set(CMAKE_SHARED_LINKER_FLAGS "-Wl,--no-undefined" CACHE STRING "Shared libraries linker flags for Linux ppc64el")
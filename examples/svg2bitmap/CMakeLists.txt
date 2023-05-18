#/****************************************************************************
#** Copyright (c) 2013-2023 Mazatech S.r.l.
#** All rights reserved.
#** 
#** This file is part of AmanithSVG software, an SVG rendering library.
#** 
#** Redistribution and use in source and binary forms, with or without
#** modification, are permitted (subject to the limitations in the disclaimer
#** below) provided that the following conditions are met:
#** 
#** - Redistributions of source code must retain the above copyright notice,
#**   this list of conditions and the following disclaimer.
#** 
#** - Redistributions in binary form must reproduce the above copyright notice,
#**   this list of conditions and the following disclaimer in the documentation
#**   and/or other materials provided with the distribution.
#** 
#** - Neither the name of Mazatech S.r.l. nor the names of its contributors
#**   may be used to endorse or promote products derived from this software
#**   without specific prior written permission.
#** 
#** NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED
#** BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
#** CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT
#** NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
#** A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER
#** OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
#** EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
#** PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
#** OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
#** WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
#** OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
#** ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#** 
#** For any information, please contact info@mazatech.com
#** 
#****************************************************************************/
#
# Build instruction:
#
# Windows (Visual Studio Solution)
# <open x64 Native Tools Command Prompt for VS 2022>
# cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/win_x86_64.cmake --no-warn-unused-cli -G "Visual Studio 17 2022" -A x64 .
# <open the generated .sln solution>
#
#
# Windows (NMAKE)
# <open x86 / x64 Native Tools Command Prompt for VS 2017 / 2019 / 2022>
# cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/win_x86_64.cmake --no-warn-unused-cli -G "NMake Makefiles" .
# nmake
#
#
# macOS Universal Binary (arm64, x64)
# cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/osx_ub.cmake --no-warn-unused-cli -G "Unix Makefiles" .
# make
#
#
# Linux
# cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/linux_x86_64.cmake --no-warn-unused-cli -G "Unix Makefiles" .
# make
#
#
cmake_minimum_required(VERSION 3.2 FATAL_ERROR)
project(svg2bitmap)
set(CMAKE_BUILD_TYPE Release)

# AmanithSVG SDK
function (get_os_subdir ret)
    if (DEFINED OS_WINDOWSCE)
        set(${ret} "windowsce" PARENT_SCOPE)
    elseif (DEFINED OS_WINDOWS)
        set(${ret} "windows" PARENT_SCOPE)
    elseif (DEFINED OS_MACOSX)
        set(${ret} "macosx" PARENT_SCOPE)
    elseif (DEFINED OS_LINUX)
        set(${ret} "linux" PARENT_SCOPE)
    elseif (DEFINED OS_IOS)
        set(${ret} "ios" PARENT_SCOPE)
    elseif (DEFINED OS_ANDROID)
        set(${ret} "android" PARENT_SCOPE)
    elseif (DEFINED OS_QNX)
        set(${ret} "qnx" PARENT_SCOPE)
    else()
        set(${ret} "" PARENT_SCOPE)
    endif()
endfunction()

function (get_arch_subdir ret)

    if (DEFINED ARCH_X86_64)
        set(${ret} "x86_64" PARENT_SCOPE)
    elseif (DEFINED ARCH_X86)
        set(${ret} "x86" PARENT_SCOPE)
    elseif (DEFINED ARCH_UB)
        set(${ret} "ub" PARENT_SCOPE)
    elseif (DEFINED ARCH_PPC64)
        # supported by Linux only
        set(${ret} "ppc64el" PARENT_SCOPE)
    elseif (DEFINED ARCH_ARMV5)
        # supported by Linux and Android
        if (DEFINED OS_ANDROID)
            set(${ret} "armeabi" PARENT_SCOPE)
        else()
            set(${ret} "arm-gnueabi" PARENT_SCOPE)
        endif()
    elseif (DEFINED ARCH_ARMV7)
        # supported by Linux, Android and QNX
        if (DEFINED OS_ANDROID)
            set(${ret} "armeabi-v7a" PARENT_SCOPE)
        elseif (DEFINED OS_QNX)
            set(${ret} "armv7-le" PARENT_SCOPE)
        else()
            set(${ret} "arm-gnueabi-v7a" PARENT_SCOPE)
        endif()
    elseif (DEFINED ARCH_ARMV8)
        # supported by Linux and Android
        if (DEFINED OS_ANDROID)
            set(${ret} "arm64-v8a" PARENT_SCOPE)
        else()
            set(${ret} "aarch64" PARENT_SCOPE)
        endif()
    elseif (DEFINED ARCH_MIPS64)
        # supported by Linux and Android
        if (DEFINED OS_ANDROID)
            set(${ret} "mips64" PARENT_SCOPE)
        else()
            set(${ret} "mips64el" PARENT_SCOPE)
        endif()
    elseif (DEFINED ARCH_MIPS)
        # supported by Linux and Android
        if (DEFINED OS_ANDROID)
            set(${ret} "mips" PARENT_SCOPE)
        else()
            set(${ret} "mipsel" PARENT_SCOPE)
        endif()
    else()
        set(${ret} "" PARENT_SCOPE)
    endif()
endfunction()

function (get_backend_subdir ret)
    set(${ret} "sre" PARENT_SCOPE)
endfunction()

function (get_dependency_subdir ret)
    set(${ret} "standalone" PARENT_SCOPE)
endfunction()

# base output directories for binaries
set(OUTPUT_BASE_DIR ${CMAKE_SOURCE_DIR}/../../bin)

# get target operating system, architecture and backend engine type
get_os_subdir(TARGET_OS)
get_arch_subdir(TARGET_ARCH)
get_backend_subdir(TARGET_ENGINE)
get_dependency_subdir(TARGET_DEPENDENCY)

# AmanithSVG SDK
set(AMANITHSVG_BASE_DIR ${CMAKE_SOURCE_DIR}/../..)
set(AMANITHSVG_INC_DIR ${AMANITHSVG_BASE_DIR}/include)
set(AMANITHSVG_LIBS_DIR ${AMANITHSVG_BASE_DIR}/lib/${TARGET_OS}/${TARGET_ARCH}/${TARGET_ENGINE}/${TARGET_DEPENDENCY})
if (MSVC)
    # use the 'lib' prefix for the import library, in order to match *nix systems
    set(AMANITHSVG_LIB "libAmanithSVG")
else()
    set(AMANITHSVG_LIB "AmanithSVG")
endif()

# application output directory
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_BASE_DIR}/${TARGET_OS}/${TARGET_ARCH}/${TARGET_ENGINE}/standalone)

# application source code directory
set(APP_SRC_DIR ${CMAKE_SOURCE_DIR}/src)

# application source code files
set(APP_SOURCE_FILES
    ${APP_SRC_DIR}/arg_parser.c
    ${APP_SRC_DIR}/atlas_export.c
    ${APP_SRC_DIR}/config.c
    ${APP_SRC_DIR}/file_utils.c
    ${APP_SRC_DIR}/json_utils.c
    ${APP_SRC_DIR}/main.c
    ${APP_SRC_DIR}/png_utils.c
    ${APP_SRC_DIR}/rendering.c
    ${APP_SRC_DIR}/str_hashmap.c
    ${APP_SRC_DIR}/str_utils.c
    ${APP_SRC_DIR}/xml_utils.c)

link_directories(${AMANITHSVG_LIBS_DIR})
if (${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    # generate the executable such that the loader will search for AmanithSVG library within the same executable folder
    set(CMAKE_INSTALL_RPATH "$ORIGIN")
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
endif()

add_executable(${PROJECT_NAME} ${APP_SOURCE_FILES})

# include directories
target_include_directories(${PROJECT_NAME}
                           PUBLIC ${AMANITHSVG_INC_DIR}
                           PUBLIC ${APP_SRC_DIR})
# link libraries
target_link_libraries(${PROJECT_NAME}
                      PUBLIC ${AMANITHSVG_LIB})

if (${CMAKE_SYSTEM_NAME} MATCHES "Windows")

    if (MSVC)
        # let Visual Studio generate an automatic manifest file
        set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS "/MANIFEST:NO /SUBSYSTEM:CONSOLE /SAFESEH:NO")
        # specify the default startup project in Visual Studio solution
        set_property(DIRECTORY PROPERTY VS_STARTUP_PROJECT ${PROJECT_NAME})
    elseif(MINGW)
        # '-s' option will strip final executable
        set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS "-s")
    endif()

    # fix the application bundle/executable: copy the AmanithSVG library in order to
    # run the application as standalone, directly within its directory
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
                       COMMAND ${CMAKE_COMMAND} -E copy_if_different
                       ${AMANITHSVG_LIBS_DIR}/libAmanithSVG${CMAKE_SHARED_LIBRARY_SUFFIX}
                       $<TARGET_FILE_DIR:${PROJECT_NAME}>)

elseif (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")

    # compiling for a Darwin-based system (MacOS X, iOS) and using a Makefile-based generator
    if ("${CMAKE_GENERATOR}" MATCHES "Make")

        # fix the application bundle/executable: copy the AmanithSVG library in order to
        # run the application as standalone, directly within its directory
        # NB: on a native MacOS X system this step is not required: the Xcode generator
        # will take care of AmanithSVG "dependency fix" (i.e. install_name_tool)
        add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD COMMAND ${CMAKE_COMMAND}
                           -DCMAKE_TOOLCHAIN_FILE=${CMAKE_TOOLCHAIN_FILE}
                           -DTARGET_EXECUTABLE_DIR=${CMAKE_RUNTIME_OUTPUT_DIRECTORY}
                           -DTARGET_EXECUTABLE_FNAME=${PROJECT_NAME}${CMAKE_EXECUTABLE_SUFFIX}
                           -DAMANITHSVG_LIBS_DIR=${AMANITHSVG_LIBS_DIR}
                           -P CMake/fix-bundle.cmake)

    endif()

elseif (${CMAKE_SYSTEM_NAME} MATCHES "Linux")

    # fix the application bundle/executable: copy the AmanithSVG library in order to
    # run the application as standalone, directly within its directory
    add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
                       COMMAND ${CMAKE_COMMAND} -E copy_if_different
                       ${AMANITHSVG_LIBS_DIR}/libAmanithSVG${CMAKE_SHARED_LIBRARY_SUFFIX}.?
                       $<TARGET_FILE_DIR:${PROJECT_NAME}>)

endif()

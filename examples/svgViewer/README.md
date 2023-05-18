# SVG viewer

A simple SVG viewer example that shows how to use AmanithSVG API on several platforms/architectures.

# How to build

If you don't have `CMake` installed on your system, please install it by following instructions at: [https://cmake.org/install](https://cmake.org/install/).
Then, according to your platform, select a toolchain file and generate the build project.

## Windows

```
// Windows x86_64, Visual Studio 2022 solution
<open x64 Native Tools Command Prompt for VS 2022>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/win_x86_64.cmake --no-warn-unused-cli -G "Visual Studio 17 2022" -A x64 .
<open the generated .sln solution>
```

## MacOS X

```
// MacOS X, Xcode project
<open a command prompt>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/osx_ub.cmake --no-warn-unused-cli -G "Xcode" .
<open the generated .xcodeproj project>
```

## Linux

```
// Linux x86_64, standard Makefile
<open a command prompt>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/linux_x86_64.cmake --no-warn-unused-cli -G "Unix Makefiles" .
make
```

## Android

The viewer for Android can be compiled directly with [Android Studio](https://developer.android.com/studio) (you don't need to use CMake).
Open project located in `platform/android` subdirectory.

## iOS

The viewer for iOS can be compiled using Xcode only.

```
// iOS, Xcode project
<open a command prompt>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/ios_ub.cmake --no-warn-unused-cli -G "Xcode" .
<open the generated .xcodeproj project>
```

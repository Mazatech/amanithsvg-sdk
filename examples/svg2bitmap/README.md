# svg2bitmap command line utility

An utility to convert SVG files to bitmaps. This tools makes use of AmanithSVG API to convert single or multiple SVG files into PNG; it supports the generation of atlas too.

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

or 

```
// Windows x86_64, Visual Studio 2022 nmake
<open x64 Native Tools Command Prompt for VS 2022>
cmake-DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/win_x86_64.cmake --no-warn-unused-cli -G "NMake Makefiles" .
nmake
```

## MacOS X

```
// MacOS X, Xcode project
<open a command prompt>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/osx_ub.cmake --no-warn-unused-cli -G "Xcode" .
<open the generated .xcodeproj project>
```

or

```
// MacOS X, standard Makefile
<open a command prompt>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/osx_ub.cmake --no-warn-unused-cli -G "Unix Makefiles" .
make
```

## Linux

```
// Linux x86_64, standard Makefile
<open a command prompt>
cmake -DCMAKE_TOOLCHAIN_FILE=./CMake/toolchain/linux_x86_64.cmake --no-warn-unused-cli -G "Unix Makefiles" .
make
```


# How to use it

Here is how to use it and the list of available options:

```
USAGE:
    svg2bitmap [OPTIONS] --input=<input path/file> --output-path=<output_path>
    svg2bitmap [OPTIONS] --atlas-input=icons.svg,1,true --atlas-output=icons_atlas,xml --output-path=<output_path>

OPTIONS:
    -h, --help                    show this help message and exit
    --dpi=<flt>                   dpi resolution, must be a positive number; default is 72
    --screen-width=<int>          screen/display width, in pixels; default is 1024
    --screen-height=<int>         screen/display width, in pixels; default is 768
    --language=<str>              user-agent language (used during the 'systemLanguage' attribute resolving); a list of languages separated by semicolon (e.g.en-US;en-GB;it;fr), default is 'en'
    --fonts-path=<str>            optional fonts path, the location where font resources are searched for
    --images-path=<str>           optional images path, the location where bitmap resources (png and jpg) are searched for
    --clear-color=<str>           clear (background) color, expressed as #RRGGBBAA; default is transparent white #FFFFFF00
    --output-width=<int>          set the output width, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
    --output-height=<int>         set the output height, in pixel; a negative number will cause the value to be taken directly from the SVG file(s)
    --scale=<flt>                 additional scale to be applied to all SVG files (also in atlas mode), must be a positive number; default is 1.0
    --rendering-quality=<int>     rendering quality, must be a number between 1 and 100 (where 100 represents the best quality)
    --filter=<str>                optional post-rendering filter, valid values are: 'none' (default), 'dilate'
    --pixel-format=<str>          pixel format of produced PNG, valid values are: 'rgba' (default), 'bgra'
    --compression-level=<int>     compression level used to generate output PNG files, must be a number between 0 and 9 (default is 6)
    --atlas-mode=<str>            atlas mode parameters: <max bitmap dimensions>, <pad between each sprite in pixels>, <constraint: pow2 / npot (default)>; e.g. '2048,1,npot'
    --atlas-output=<str>          atlas output format: <prefix>,<data format: xml (default), cocos2d, json-array, json-hash, phaser2, phaser3, pixijs, godot3-sheet, godot3-tset, libgdx, spine, code-c, code-libgdx>,<map format: array (default), hash>; e.g. 'myatlas,xml,array'
    --atlas-input=<str>           in atlas mode, add the given SVG to the packing; format is: <filename>,<scale>,<explode groups> (e.g. icons.svg,0.8,true)
    -o, --output-path=<str>       optional output path, the location where output files will be written to (default is current directory)
    -q, --quiet                   quiet mode, it disables information messages and warnings

ARGUMENTS:
    -i, --input=<str>             a list of SVG files (separated by comma), or an input path that will be scanned, looking for SVG files
```
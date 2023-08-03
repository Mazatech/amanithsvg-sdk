# amanithsvg-sdk

[AmanithSVG](http://www.amanithsvg.com) is a middleware for reading and rendering [SVG](https://it.wikipedia.org/wiki/Scalable_Vector_Graphics) files, available as a standalone 
native library and supporting bindings for other languages and engines (C#, Java, Unity, libGDX); AmanithSVG is based on static features of [SVG Full 
1.1](https://www.w3.org/TR/SVG/), plus some static features of [SVG Tiny 1.2](https://www.w3.org/TR/SVGTiny12/).

Some headlines about AmanithSVG:

 * Standalone: the library does not require external libraries, it can be downloaded and used just as is

 * Cross platform: the API, and so the rendering, remains consistent across all supported platforms

 * Really fast rendering and high antialiasing quality (analytical pixel coverage)

## What does the public AmanithSVG SDK include?

This public AmanithSVG SDK includes:

 * the header files (`.h` file extension) to build native applications for all the supported platforms
 * the binary library files (`.dll`, `.dylib`, `.so`, `.a` file extensions) of **AmanithSVG Lite**, for all the supported platforms.
 * the SVG viewer example for Desktop (Windows, MacOS X, Linux) and mobile (iOS, Android) platforms
 * the SVG to bitmap command line utility for Desktop platforms
 * AmanithSVG bindings for the [libGDX](http://libgdx.com) game development framework
 * AmanithSVG bindings for the [Unity](http://unity3d.com/) game engine

## What does the public AmanithSVG SDK not include?

The public AmanithSVG SDK does not include:

 * the binary library files (`.dll`, `.dylib`, `.so`, `.a` file extensions) of **AmanithSVG Full**
 * the source code of any version of AmanithSVG

## Lite vs Full

AmanithSVG exists in two different versions: Lite and Full. **Both support the same platforms/architectures and the same API**. The difference between the two lies in the license, and in some limitations of the Lite version compared to the Full. Specifically, the Lite version does not support the following SVG elements: [\<linearGradient\>](https://www.w3.org/TR/SVG11/pservers.html#LinearGradients), [\<radialGradient\>](https://www.w3.org/TR/SVG11/pservers.html#RadialGradients), [\<pattern\>](https://www.w3.org/TR/SVG11/pservers.html#Patterns), [\<image\>](https://www.w3.org/TR/SVG11/struct.html#ImageElement), [\<mask\>](https://www.w3.org/TR/SVG11/masking.html#Masking), [\<filter\>](https://www.w3.org/TR/SVG11/filters.html).

More information on the [relative AmanithSVG project page](https://www.amanithsvg.com/docs/desc/004-licensing.html).

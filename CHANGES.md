# CHANGES BETWEEN 1.3.1 and 2.0.1

- AmanithSVG is now thread safe: all the exposed functions can be called from multiple threads at the same time

- Some parts of the underlying rasterizer have been rewritten by implementing better data structures, increasing efficiency in terms of memory consumption and performance

- Support for new SVG elements: `<pattern>`, `<image>`, `<a>`, `<text>`, `<tspan>`, `<textPath>`, `<feBlend>`, `<feComposite>`, `<feDiffuseLighting>`, `<feDisplacementMap>`, `<feFlood>`, `<feGaussianBlur>`, `<feMerge>`, `<feMorphology>`, `<feSpecularLighting>`, `<feTurbulence>`, `<switch>`

- Removed builds relative to deprecated Android ABI (ARMv5, 32 and 64 bit MIPS)

- Removed builds relative to 32 bit architectures on iOS (ARMv7) and MacOS X (x86): now both iOS and MacOS X builds are Universal Binaries for arm64 (Apple Silicon) and x86_64 architectures

- Removed deprecated Direct3D 9 backend from Unity builds

- `svgViewer` example have been improved from the point of view of compatibility with various building systems (CMake, Gradle)

- `svg2bitmap` a new command line utility for Desktop platforms. It uses AmanithSVG API to convert single or multiple SVG files into PNG; it supports the generation of atlas too.

- Lot of minor fixes and improvements

## Lite vs Full

AmanithSVG now exists in two different versions: Lite and Full. **Both support the same platforms/architectures and the same API**. The difference between the two lies in the license, and in some limitations of the Lite version compared to the Full. Specifically, the Lite version does not support the following SVG elements: [\<linearGradient\>](https://www.w3.org/TR/SVG11/pservers.html#LinearGradients), [\<radialGradient\>](https://www.w3.org/TR/SVG11/pservers.html#RadialGradients), [\<pattern\>](https://www.w3.org/TR/SVG11/pservers.html#Patterns), [\<image\>](https://www.w3.org/TR/SVG11/struct.html#ImageElement), [\<mask\>](https://www.w3.org/TR/SVG11/masking.html#Masking), [\<filter\>](https://www.w3.org/TR/SVG11/filters.html).

# libGDX memory game example

A simple memory-like game example for [libGDX](https://libgdx.com), showing how to use AmanithSVG API with this awesome open source cross platform game development framework.

# How to build and run

Install Android Studio (or IntelliJ IDEA) and import the project in this directory.
Please follow the [official libGDX documentation](https://libgdx.com/wiki/start/import-and-running)

## Notes for Desktop platforms

In order to run/debug the example on desktop platforms (Windows, MacOS X, Linux):

- open the project in Android Studio
- go to `Settings` -> `Experimental`
- unselect `Only include test tasks in the Gradle task list generated during Gradle Sync`
- click `Apply` button
- close the project, and reopen it
- then follow [official documentation](https://libgdx.com/wiki/start/import-and-running#desktop) for running libGDX applications on desktop platforms

## Notes for Android

In order to run/debug the example on Android devices, please:

- install [Android Studio](https://developer.android.com/studio)
- open the project in Android Studio
- go to `Settings` -> `Appearance & Behavior` -> `System Settings` -> `Android SDK`
   - select the tab `SDK Platforms` and be sure to install `Android 12L (Sv2), API Level 32`
   - select the tab `SDK Tools` and be sure to install `Android SDK Build-Tools 33.0.1`
   - select the tab `SDK Tools`  and be sure to install `Android SDK Command-line Tools (latest)`
- then follow [official documentation](https://libgdx.com/wiki/start/import-and-running#android) for running libGDX applications on Android

## Notes for iOS

In order to run the example on iOS devices, be sure to have installed:

- Apple Xcode
- [RoboVM plugin for IntelliJ IDEA](http://robovm.mobidevelop.com/downloads/releases/idea/idea-2.3.18.zip) by following the [official documentation](http://robovm.mobidevelop.com)
- then follow [official documentation](https://libgdx.com/wiki/start/import-and-running#ios) for running libGDX applications on iOS
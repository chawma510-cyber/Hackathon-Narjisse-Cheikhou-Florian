# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.11.1] - 2025-08-06

### Fixed
- Vision Pro OS >=2.5 Compatibility
- Static mesh display when autoplayback is disabled
- Material allocation leak in editor 

## [3.11.0] - 2025-08-06

### Added
- Decryption of 4ds files previously encrypted with 4DCoder
- Handle devices using old Android sdk <= API29
- Handle new android sdk API35 with 16kb memory pages

### Fixed
- Tracking and Look At features broken in v3.10

## [3.10.1] - 2025-01-15

### Fixed

- Vision OS error at runtime due bad marshalling

## [3.10.0] - 2025-01-02

### Added

- Unity Package Manager Compatibility
- Better performances using new mesh API

### Fixed

- Texture inside-out issue with some rendering pipelines
- Packaging on Linux with recent Ubuntu version

## [3.9.1] - 2024-05-23

### Fixed

- Package import error on some platforms.

## [3.9.0] - 2024-04-09

### Added

- New Graph4D feature to create 4D animation graph (see documentation).
- Vision Pro support.

## [3.8.4] - 2024-01-04

### Changed

- Update for new macOS version.

## [3.8.3] - 2023-07-25

### Fixed

- Application build for iOS.

## [3.8.2] - 2023-05-15

### Added

- Tracking and LookAt merge functionality.

## [3.8.1] - 2022-12-06

### Fixed

- Bounding box in L!ve.

## [3.8.0] - 2022-11-15

### Changed

- Renamed version 3.7.4 to 3.8.0

## [3.7.4] - 2022-11-08

### Added

- Magic Leap 2 support.

### Fixed

- Look At meta data test.
- Potential thread lock.

## [3.7.3] - 2022-10-04

### Fixed

- Bounding box computation with l!ve data.
- Missing DLL for UWP ARM64.
- Remove the pause on focus lost in editor.

## [3.7.2] - 2022-06-20

### Fixed

- HTTPS: streaming on Windows.

## [3.7.1] - 2022-06-01

### Fixed

- UTF-16 filenames handling.
- Possible conflict with other libraries on play function.

## [3.7.0] - 2022-05-06

### Added

- Bounding box collision.

### Fixed

- Loading error on macOS.

## [3.6.6] - 2022-04-05

### Fixed

- Play when not initialised.
- Thread consumption when not initialised.

## [3.6.5] - 2022-03-18

### Fixed

- Modifying playback speed issue.
- Android file copy memory issue.

## [3.6.4] - 2022-01-25

### Added

- HTTPS support for streaming.

## [3.6.3] - 2022-01-18

### Fixed

- Colour replacement behaviour.
- L!ve performances.

## [3.6.2] - 2021-11-30

### Fixed

- Streaming performance issue.
- Timeline audio synchronization issue.
- Audio auto position issue.
- Event generation issue.

## [3.6.1] - 2021-10-28

### Fixed

- Potential crash closing the sequence.

## [3.6.0] - 2021-10-26

### Added

- Events handling form 4DS files.
- Motion vectors from 4DS files for HDRP.

## [3.5.2] - 2021-10-26

### Fixed

- Timeline behaviour synchronization.

## [3.5.1] - 2021-10-26

### Fixed

- iOS packaging error.

## [3.5.0] - 2021-10-26

### Added

- Look At feature.
- Tracking inside 4ds files handling.

## [3.4.3] - 2021-10-26

### Added

- PlayAudio checkbox option to avoid playing internal audio.

### Fixed

- Improved Timeline behaviour for a more accurate playback.

## [3.4.2] - 2021-10-26

### Fixed

- iOS: potential crash on older devices.

## [3.4.1] - 2021-10-26

### Added

- App focus handling on mobiles.

### Fixed

- Audio support with long sequence.
- iOS packaging bug with bitcode.

## [3.4.0] - 2021-10-26

### Added

- Live! support.

## [3.3.0] - 2021-10-26

### Added

- Integrated audio in 4DS handling (since Holosys software 3.5).
- Live Handling (preview, future feature coming in Holosys).
- CPU decompression to RGBA of DXT and ASTC texture when not supported on=
  current platform.

## [3.2.4]

### Fixed

- iOS: build with bitcode enabled for app archiving.

## [3.2.3]

### Changed

- Cache Managing when Keep In cacje option is enabled with HTTP streaming.

## [3.2.2]

### Fixed

- HTTP streaming with Keep In cache option issue.

## [3.2.1]

### Fixed

- Thread decoding speed.

## [3.2.0]

### Added

- Better plugin performance.
- Unity Timeline compatibility.
- Checkbox to choose sequence looping.
- More access to plugin functions.
- Alpha handling in color replacement shaders.
- Hololens 2 support.

### Fixed

- Minor bugs.

## [3.1.4]

### Fixed

- Some normals computing error.

## [3.1.3]

### Added

- Event on last frame reached.
- Linux support.

### Fixed

- Empty frames handling.
- Better error messages.

## [3.1.2]

### Fixed

- Seek function bug when subrange.

## [3.1.1]

### Fixed

- Playing sub range.
- Magic Leap One support.

## [3.1.0]

### Added

- Shader example to replace a color in the texture.
- Magic Leap One support.

## [3.0.1]

### Added

- Hololens 1 support.

## [3.0.0]

### Added

- Streaming support from HTTP.
- Compatibility with HTTP.
- Compatible with redirections.
- Better downloading performances.
- Option to keep downloaded data in RAM and avoid to dowload it again when
  looping the sequence.
- Access to mesh and chunks buffer max size and to download payload size to
  optimize performances (via script).
- Loading bar UI prefab to help see buffers states.

## [2.2.1]

### Fixed

- Android build.

## [2.2.0]

### Added

- Possibility to have meshes with more than 65535 vertices.

### Fixed

- Reverse playback issues.
- Some mesh update issue in editor.

## [2.1.0]

### Added

- Support for new Unity rendering pipelines.

## [2.0.8]

### Fixed

- Android ARMv7 crash.

## [2.0.7]

### Fixed

- Crash on iOS.

## [2.0.6]

### Fixed

- Random crash on sequence destroy.

## [2.0.5]

### Fixed

- Bug relative to outOfRange event.
- Crash on preview.

## [2.0.4]

### Changed

- Update for macOS and iOS.

## [2.0.3]

### Fixed

- Memory leak.

## [2.0.2]

### Fixed

- Unity 2019: compatibility error.

## [2.0.1]

### Fixed

- Android: ARMv7 compatibility.

## [2.0.0]

### Changed

- Plugin now based on the new SDK4DS.

### Added

- Possibility to read 4ds files V2 (with post process).

## [1.2.0]

### Added

- Access to playback speed ratio.

## [1.1.2]

### Fixed

- Hololens DLL export compatibility.

### Removed

- Deprecated script code.

## [1.1.1]

### Fixed

- Magic Leap One build (script define).
- Deprecated script code.

## [1.1.0]

### Added

- Magic Leap One support.

## [1.0.3]

### Changed

- Normals computation.
- Texture filter mode.

## [1.0.2]

### Added

- macOS: post build script for adding automatically Xcode dependencies.

## [1.0.1]

### Fixed

- macOS: native libraries not found for built application.

## [1.0.0]

### Added

- Compatibility with Unity 2017 on:
  - Windows >= 7 x86_64
  - macOS >= 10.12 x86_64
  - Linux >= Ubuntu 12.04 LTS x86_64
  - Android >= 4.4
  - iOS >= 10.0
  - DirectX & OpenGL

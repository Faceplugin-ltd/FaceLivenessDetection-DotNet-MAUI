# iOS frameworks

Six bundles belong here and **none of them are in git**. All six are downloads; nothing has to be
built, so you do not need a Mac to use this repository.

| Bundle | What it is | Download |
| ------ | ---------- | -------- |
| `FaceLivenessBridge.xcframework` | The Objective-C face of the native liveness screens | `bridge.zip` on the [FaceLivenessDetection-iOS release](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS/releases/latest) |
| `FaceLivenessKit.xcframework` | The engine lifecycle the bridge calls | the same `bridge.zip` |

A usable `FaceLivenessBridge.xcframework` must contain
`ios-arm64/FaceLivenessBridge.framework/FaceLivenessBridge` (the Mach-O). An Info.plist-only
stub (from a failed `xcodebuild -create-xcframework` without `-allow-internal-distribution`)
causes MT158 at link time — delete the stub folders and re-fetch after a good `bridge.zip` is
published.
| `facerecognitionsdk.framework` | The engine wrapper | `runtime.zip` on the [FaceRecognition-LivenessDetection-iOS release](https://github.com/Faceplugin-ltd/FaceRecognition-LivenessDetection-iOS/releases/tag/latest) |
| `FacePluginSDK.xcframework` | Vendor runtime the wrapper links | the same `runtime.zip` |
| `FacePlugin.xcframework` | Vendor runtime the wrapper links | the same `runtime.zip` |
| `FaceDetect.xcframework` | Vendor detector the wrapper links | the same `runtime.zip` |

Unzip both archives into this folder.

## Why a bridge at all

The liveness screens in
[FaceLivenessDetection-iOS](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS) are
Swift, and .NET for iOS can only bind Objective-C. That repository therefore publishes
**FaceLivenessBridge**, a framework target whose only job is to put an `@objc` face on the same
screens — it compiles the very same source files the app does, so the two cannot drift.

<details>
<summary>Rebuilding the bridge yourself instead</summary>

Requires macOS with Xcode:

```bash
cd FaceLivenessDetection-iOS
./scripts/build_bridge.sh
cp -R build/xcframeworks/FaceLivenessBridge.xcframework \
      build/xcframeworks/FaceLivenessKit.xcframework \
      <this repo>/src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/
```
</details>

**Device only** (`ios-arm64`). The engine ships no simulator slice, which is the same reason the
app only runs on a physical phone.

`src/FaceLivenessBridge.Binding/` binds the bridge; the app's build fails with a named list if any
of the six is missing, so a missing runtime is reported before the compiler runs.

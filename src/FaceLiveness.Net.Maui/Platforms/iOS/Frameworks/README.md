# iOS frameworks

Six bundles belong here and **none of them are in git**.

| Bundle | What it is | Where it comes from |
| ------ | ---------- | ------------------- |
| `FaceLivenessBridge.xcframework` | The Objective-C face of the native liveness screens | built from `Face-LivenessDetection-iOS` |
| `FaceLivenessKit.xcframework` | The engine lifecycle the bridge calls | built from `Face-LivenessDetection-iOS` |
| `facerecognitionsdk.framework` | The engine wrapper (FacePlugin engine build) | Face-Recognition-LivenessDetection-iOS release |
| `FacePluginSDK.xcframework` | Vendor runtime the wrapper links | same release |
| `FacePlugin.xcframework` | Vendor runtime the wrapper links | same release |
| `FaceDetect.xcframework` | Vendor detector the wrapper links | same release |

## Building the first two

The liveness screens in
[Face-LivenessDetection-iOS](https://github.com/Faceplugin-ltd/Face-LivenessDetection-iOS) are
Swift, and .NET for iOS can only bind Objective-C. That repository therefore publishes
**FaceLivenessBridge**, a framework target whose only job is to put an `@objc` face on the same
screens — it compiles the very same source files the app does, so the two cannot drift.

```bash
cd Face-LivenessDetection-iOS
./scripts/build_bridge.sh
cp -R build/xcframeworks/FaceLivenessBridge.xcframework \
      build/xcframeworks/FaceLivenessKit.xcframework \
      <this repo>/src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/
```

Then the four engine bundles from that repository's root, which it needs too.

**Device only** (`ios-arm64`). The engine ships no simulator slice, which is the same reason the
app only runs on a physical phone.

`src/FaceLivenessBridge.Binding/` binds the bridge; the app's build fails with a named list if any
of the six is missing, so a missing runtime is reported before the compiler runs.

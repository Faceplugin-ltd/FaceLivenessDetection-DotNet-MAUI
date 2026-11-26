# Android runtime

Three AARs belong here and **none of them are in git** — together they are about 80 MB plus the
two libraries. All three are downloads; nothing has to be built.

| File | What it is | Download |
| ---- | ---------- | -------- |
| `facerecognitionsdk.aar` | The engine | [FaceRecognition-LivenessDetection-Android release](https://github.com/Faceplugin-ltd/FaceRecognition-LivenessDetection-Android/releases/tag/latest) |
| `face-liveness-kit.aar` | Engine lifecycle, camera frames, per-frame face attributes | `liveness-libs.zip` on the [FaceLivenessDetection-Android release](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Android/releases/latest) |
| `face-liveness-active.aar` | The whole guided active-liveness screen | the same `liveness-libs.zip` |

Unzip `liveness-libs.zip` and drop both AARs here, beside the engine:

```text
src/FaceLiveness.Net.Maui/Platforms/Android/libs/facerecognitionsdk.aar
src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-kit.aar
src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-active.aar
```

The two libraries are the ones the native Android app itself is built from, so this app launches
the real native screens rather than carrying a second copy of the liveness logic.

<details>
<summary>Building them yourself instead</summary>

```bash
cd FaceLivenessDetection-Android
./gradlew :face-liveness-kit:assembleRelease :face-liveness-active:assembleRelease
cp face-liveness-kit/build/outputs/aar/face-liveness-kit-release.aar \
   <this repo>/src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-kit.aar
cp face-liveness-active/build/outputs/aar/face-liveness-active-release.aar \
   <this repo>/src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-active.aar
```
</details>

The build fails with a named message if any of the three is absent, so a missing runtime is
reported before the compiler runs rather than as a missing-type error.

A licence key is bound to the `ApplicationId`, so your own build needs its own key — contact
FacePlugin. A key issued for the previous engine reports "Invalid license" on this runtime.

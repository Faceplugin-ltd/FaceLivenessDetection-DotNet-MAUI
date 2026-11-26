# Android runtime

Three AARs belong here and **none of them are in git** — together they are about 80 MB plus the
two libraries.

| File | What it is | Where it comes from |
| ---- | ---------- | ------------------- |
| `facerecognitionsdk.aar` | The engine (LX / Luxand build) | `Face-LivenessDetection-Android/libs/` |
| `face-liveness-kit.aar` | Engine lifecycle, camera frames, per-frame face attributes | built from `Face-LivenessDetection-Android` |
| `face-liveness-active.aar` | The whole guided active-liveness screen | built from `Face-LivenessDetection-Android` |

The two libraries are the ones the native app itself is built from, so this app launches the real
native screens rather than carrying a second copy of the liveness logic. Build them with:

```bash
cd Face-LivenessDetection-Android
./gradlew :face-liveness-kit:assembleRelease :face-liveness-active:assembleRelease
cp face-liveness-kit/build/outputs/aar/face-liveness-kit-release.aar \
   <this repo>/src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-kit.aar
cp face-liveness-active/build/outputs/aar/face-liveness-active-release.aar \
   <this repo>/src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-active.aar
cp libs/facerecognitionsdk.aar \
   <this repo>/src/FaceLiveness.Net.Maui/Platforms/Android/libs/
```

They are also published to Maven Central as `com.faceplugin:face-liveness-kit` and
`com.faceplugin:face-liveness-active` — see that repository's `PUBLISHING.md`.

The build fails with a named message if any of the three is absent, so a missing runtime is
reported before the compiler runs rather than as a missing-type error.

A licence key is bound to the `ApplicationId`, so your own build needs its own key — contact
FacePlugin. A key issued for the previous engine reports "Invalid license" on this runtime.

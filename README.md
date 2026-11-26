<div align="center">
<img alt="FacePlugin" src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/brand/logo.png" width="400"/>
</div>

#### 🌐 Company Site - [Here](https://faceplugin.com)

#### 🤗 Hugging Face - [Here](https://huggingface.co/FacePlugin-Ltd)

#### 🛟 Help Center - [Here](https://doc.faceplugin.com)

#### 🐳 Docker Hub - [Here](https://hub.docker.com/u/faceplugin)

# FacePlugin Face Liveness Detection — .NET MAUI (Fully On-Premise)

> The camera screens are the **native** FacePlugin ones, launched from C#.
> Jump: [Quick start](#quick-start) · [How it works](#how-it-works) · [Get the runtime](#get-the-runtime) · [Run the demo](#run-the-demo) · [SDK License](#sdk-license) · [Setup on your own app](#setup-on-your-own-app) · [About SDK](#about-sdk)

## Quick start

- [ ] Clone [FaceLivenessDetection-DotNet-MAUI](https://github.com/Faceplugin-ltd/FaceLivenessDetection-DotNet-MAUI)
- [ ] Install the .NET 8 SDK and `dotnet workload install maui`
- [ ] Put three AARs in `src/FaceLiveness.Net.Maui/Platforms/Android/libs/` — [Get the runtime](#get-the-runtime)
- [ ] `dotnet build -f net8.0-android -t:Run` with a **physical** phone plugged in
- [ ] Home status shows **Ready** → **Active liveness** / **Passive liveness** unlock

## Introduction

FacePlugin **Face Liveness Detection SDK for .NET MAUI** is a fully on-device anti-spoofing demo
for KYC and remote identity verification. It detects printed photos, video replay, 3D masks and
deepfake-style attacks — no extra hardware, no cloud.

It runs liveness two ways, and this app ships both:

- **Passive** — the engine scores a single frame. Nothing is asked of the user.
- **Active** — the face is scored for spoofing **first**, and only a face that clears that gate is
  asked for a short sequence of actions (blink, smile, turn your head). A recorded video cannot
  answer a sequence it has never seen, so the two together are the stronger check against replay.

All processing stays on the phone. **No** biometric data leaves the device.

## How it works

This app does **not** re-implement liveness in C#. On both platforms it launches the screens the
native apps run, and the engine is the same `facerecognitionsdk` binary — so the camera preview,
the anti-spoofing gate, the shuffled challenge sequence, the progress ring and the result card are
that code, not a copy of it.

| Layer | Where it lives | What it is |
| ----- | -------------- | ---------- |
| MAUI UI | `src/FaceLiveness.Net.Maui/` | Home, Settings and About — rebuilt to match the native screens |
| C# API | `FaceLiveness.cs`, `LivenessModels.cs` | `FaceLivenessEngine`, `FaceLivenessScreens`, and the typed options/result models |
| Android | `Platforms/Android/` | The bridge to the bound libraries, and the passive screen |
| iOS | `Platforms/iOS/`, `src/FaceLivenessBridge.Binding/` | The bridge framework's binding, and the translation to the shared model |

**Android** references the two libraries that
[FaceLivenessDetection-Android](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Android)
is itself built from — `face-liveness-kit` and `face-liveness-active` — and launches
`ActiveLivenessActivity` for a result. The passive screen is a port, because it lives in that app's
own module rather than in a library: this app carries `PassiveLivenessActivity` and
`PassiveFaceView`, still running on the kit's `CameraPreview`, `CameraFrameUtils` and
`FaceEngine.detect`, so only the drawing is C#.

**iOS** goes through `FaceLivenessBridge.xcframework`, published by
[FaceLivenessDetection-iOS](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS) as part of
`bridge.zip` on its Releases page. The screens there are Swift, and .NET for iOS reads Objective-C, so that
framework puts an `@objc` face — `FPLEngine`, `FPLActiveOptions`, `FPLActiveResult`, `FPLLiveness`
— on the *same source files* the FaceLivenessSDK app compiles. `src/FaceLivenessBridge.Binding/`
binds it; `Platforms/iOS/FaceLiveness.iOS.cs` translates it to the shared model.

Both sides report an attempt with the same wire values, which is why `LivenessModels.cs` is shared
rather than written twice.

### Main Functionalities

| Screen | What it does | Built with |
| ------ | ------------ | ---------- |
| **Active liveness** | Anti-spoofing gate, then a guided challenge sequence — face frame, progress ring, per-step instructions, and a pass/fail result listing every check | Native |
| **Passive liveness** | Live camera anti-spoofing with a face box and a liveness / quality / pose HUD | Native (iOS); native kit with a C# screen (Android) |
| **Home** | Engine status, the two liveness demos, settings and about | MAUI |
| **Settings** | Camera lens, passive score threshold, the challenge pool, how many are asked, random order and every engine threshold | MAUI |
| **About** | FacePlugin Face Liveness SDK — on-device anti-spoofing | MAUI |

### Product List

| Platform | Repository |
| -------- | ---------- |
| Android | [FaceLivenessDetection-Android](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Android) |
| iOS | [FaceLivenessDetection-iOS](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS) |
| Flutter | [FaceLivenessDetection-Flutter](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Flutter) |
| React Native | [FaceLivenessDetection-ReactNative](https://github.com/Faceplugin-ltd/FaceLivenessDetection-ReactNative) |
| Ionic Capacitor | [FaceLivenessDetection-Ionic-Capacitor](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Ionic-Capacitor) |
| Ionic Cordova | [FaceLivenessDetection-Ionic-Cordova](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Ionic-Cordova) |
| **.NET MAUI** | **[FaceLivenessDetection-DotNet-MAUI](https://github.com/Faceplugin-ltd/FaceLivenessDetection-DotNet-MAUI)** **(this repo)** |
| Linux / Docker | [FaceLivenessDetection-Docker](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Docker) |
| Windows | [FaceLivenessDetection-Windows](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Windows) |

### Related products

- **Face Recognition + Liveness** — [FaceRecognition-LivenessDetection-DotNet-MAUI](https://github.com/Faceplugin-ltd/FaceRecognition-LivenessDetection-DotNet-MAUI) — Recognition + liveness + attributes

Product hubs: [Face Recognition](https://github.com/Faceplugin-ltd/Face-Recognition-SDK) · [ID Document Recognition](https://github.com/Faceplugin-ltd/ID-Document-Recognition-SDK) · [ID Document Liveness](https://github.com/Faceplugin-ltd/ID-Document-LivenessDetection-SDK)

## Before you start

| Step | What you need |
| ---- | ------------- |
| 1 | .NET 8 SDK (`global.json` pins `8.0.100`), `dotnet workload install maui`, and a **physical** phone |
| 2 | Three AARs in `Platforms/Android/libs/` — see [Get the runtime](#get-the-runtime) |
| 3 | For iOS: macOS with Xcode, and six bundles in `Platforms/iOS/Frameworks/` |
| 4 | A licence key for your ids. The demo ships one for `com.faceplugin.faceliveness` |

### System requirements

| Item | Minimum | Recommended |
| ---- | ------- | ----------- |
| Android | API 24 (Android 7.0) | Android 10 or newer |
| iOS | 15.0 | 16 or newer |
| ABI | `arm64-v8a` or `armeabi-v7a` | `arm64-v8a` |
| Device | Physical device | Same; neither the emulator nor the simulator is for camera work |

## Get the runtime

Three AARs, none of them in git:

```text
src/FaceLiveness.Net.Maui/Platforms/Android/libs/facerecognitionsdk.aar
src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-kit.aar
src/FaceLiveness.Net.Maui/Platforms/Android/libs/face-liveness-active.aar
```

All three are downloads — nothing has to be built:

| File | Download |
| ---- | -------- |
| `facerecognitionsdk.aar` | [FaceRecognition-LivenessDetection-Android release](https://github.com/Faceplugin-ltd/FaceRecognition-LivenessDetection-Android/releases/tag/latest) |
| `face-liveness-kit.aar` | `liveness-libs.zip` on the [FaceLivenessDetection-Android release](https://github.com/Faceplugin-ltd/FaceLivenessDetection-Android/releases/latest) |
| `face-liveness-active.aar` | the same `liveness-libs.zip` |

Unzip `liveness-libs.zip` and put both AARs in the folder above, beside the engine.
`Platforms/Android/libs/README.md` has the exact paths.

On iOS, six bundles:

```text
src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/FaceLivenessBridge.xcframework
src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/FaceLivenessKit.xcframework
src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/facerecognitionsdk.framework
src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/FacePluginSDK.xcframework
src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/FacePlugin.xcframework
src/FaceLiveness.Net.Maui/Platforms/iOS/Frameworks/FaceDetect.xcframework
```

The first two ship as `bridge.zip` on the
[FaceLivenessDetection-iOS release](https://github.com/Faceplugin-ltd/FaceLivenessDetection-iOS/releases/latest).
The other four are the engine, as `runtime.zip` on the
[FaceRecognition-LivenessDetection-iOS release](https://github.com/Faceplugin-ltd/FaceRecognition-LivenessDetection-iOS/releases/tag/latest).
Unzip both into the folder above.

Because the bridge is a download, **you do not need a Mac to use this repository** — only to
rebuild the bridge yourself, which the iOS repository documents.

The build fails with a named message listing whatever is absent on either platform, so a missing
runtime is reported before the compiler runs rather than as a missing-type error.

## Run the demo

```bash
dotnet build src/FaceLiveness.Net.Maui/FaceLiveness.Net.Maui.csproj -f net8.0-android -t:Run
dotnet build src/FaceLiveness.Net.Maui/FaceLiveness.Net.Maui.csproj -f net8.0-ios -t:Run     # macOS
```

| Platform | Identifier |
| -------- | ---------- |
| Android `ApplicationId` | `com.faceplugin.faceliveness` |
| iOS bundle id | `com.faceplugin.faceliveness.app` |

1. Wait until the home status shows **Ready**.
2. **Active liveness** runs the gate, then the challenges, and ends on a result card.
3. **Passive liveness** scores every frame: a green box is Real, a red box is Spoof, at the
   threshold from Settings.

### Screenshots

| Active liveness(Result) | Passive liveness |
| ----------------- | ------------------ |
| <p align="center"><img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/face-liveness/mobile/active_result.png" alt="FacePlugin Face Liveness — Active liveness result: anti-spoofing gate and challenges passed" width="220"/></p> | <p align="center"><img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/face-liveness/mobile/passive.png" alt="FacePlugin Face Liveness — Passive liveness: live camera anti-spoofing with liveness, quality and pose" width="220"/></p> |

## SDK License

Licences are **offline** and bound to your `ApplicationId` / bundle id, so your own app needs its
own key. The demo ships the key for `com.faceplugin.faceliveness`.

> **This build changed engines.** The app now runs on `facerecognitionsdk`, which checks licences
> with its own library. If the home status shows **Invalid license!** or **No activated!** on a
> clean install, the bundled key predates the change — [contact us](#contact) for one issued for
> these ids on this runtime.

The keys live in [`src/FaceLiveness.Net.Maui/LicenseKeys.cs`](src/FaceLiveness.Net.Maui/LicenseKeys.cs)
and are the same ones the native apps activate with. After activation,
`FaceLivenessEngine.License` reports what the key unlocks: both liveness screens stay locked unless
it grants **Liveness**.

## Setup on your own app

1. Copy `Platforms/Android/libs/` into your project and reference the three AARs:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0-android'">
  <AndroidLibrary Include="Platforms\Android\libs\facerecognitionsdk.aar" Bind="false" />
  <AndroidLibrary Include="Platforms\Android\libs\face-liveness-kit.aar" />
  <AndroidLibrary Include="Platforms\Android\libs\face-liveness-active.aar" />
</ItemGroup>
```

   The engine is referenced with `Bind="false"`: your code never calls it — the kit does — and one
   of its classes overrides `finalize()`, which the C# binding generator cannot express.

2. Set `SupportedOSPlatformVersion` to 24 for Android, add the camera permission, and copy
   `Platforms/Android/Liveness/` for the passive screen.
3. For iOS, copy `src/FaceLivenessBridge.Binding/` and reference it from your iOS target, put the
   six bundles in `Platforms/iOS/Frameworks/`, and add `NSCameraUsageDescription`.
4. Start the engine once, then launch a screen:

```csharp
await FaceLivenessEngine.StartAsync(yourLicenseKey);

var result = await FaceLivenessScreens.StartActiveLivenessAsync(new ActiveLivenessOptions
{
    Count = 3,
});
if (result?.Passed == true)
{
    // live person
}
```

## About SDK

```csharp
// Engine lifecycle
await FaceLivenessEngine.StartAsync(licenseKey);   // activate + load the models
FaceLivenessEngine.State;                          // Loading | Ready | LicenseInvalid | …
FaceLivenessEngine.License;                        // LicenseStatus: Liveness / Recognition / Label
FaceLivenessEngine.StateChanged += OnStateChange;
await FaceLivenessEngine.StatusMessageAsync();     // what the home status pill shows

// Active liveness — the gate, then the challenges
var result = await FaceLivenessScreens.StartActiveLivenessAsync(options);
result?.Passed;          // gate cleared and every challenge completed
result?.PassivePassed;   // did the anti-spoofing gate clear?
result?.PassiveScore;    // averaged score, or -1 when never measured
result?.Outcomes;        // one StepState per challenge
result?.FailedCheck;     // the challenge that ended the attempt

// Passive liveness — one frame at a time, with a HUD
await FaceLivenessScreens.StartPassiveLivenessAsync(
    useFrontCamera: true,
    livenessThreshold: 0.5f);
```

`ActiveLivenessOptions` carries every threshold the native session judges against — the challenge
pool, how many are asked, random order, head-turn and tilt angles, smile and eye-closed confidence,
blinks required, the move-closer growth, the start alignment tolerance, the minimum face size and
the minimum image quality. Its defaults are the native defaults exactly, so an attempt is judged
the same way here as on Android and iOS.

> **Head-pose direction.** Which way yaw and pitch count as positive is a property of the engine
> build. If a turn challenge accepts the opposite direction on your device, flip
> **Settings → Invert left / right** (or **Invert up / down**) — no rebuild needed.

## Contact

<div align="left">
<a target="_blank" href="mailto:info@faceplugin.com"><img src="https://img.shields.io/badge/email-info@faceplugin.com-blue.svg?logo=gmail" alt="faceplugin.com"></a>&emsp;
<a target="_blank" href="https://t.me/FacePluginSupport"><img src="https://img.shields.io/badge/telegram-@FacePluginSupport-blue.svg?logo=telegram" alt="Telegram @FacePluginSupport"></a>&emsp;
<a target="_blank" href="https://wa.me/+14692784822"><img src="https://img.shields.io/badge/whatsapp-faceplugin-blue.svg?logo=whatsapp" alt="faceplugin.com"></a>
</div>

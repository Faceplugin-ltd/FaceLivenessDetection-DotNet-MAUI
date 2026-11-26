using System;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace FaceLivenessBridge;

/*
 * Mirrors FaceLivenessBridge-Swift.h, the header Xcode generates from
 * Face-LivenessDetection-iOS/FaceLivenessBridge/Public/FaceLivenessBridge.swift.
 *
 * Only the @objc surface of that file appears in the header, so this binding is the whole of it:
 * activation and licence state (FPLEngine), the attempt's configuration (FPLActiveOptions) and
 * outcome (FPLActiveResult), and the two screens (FPLLiveness). Everything behind it — the engine,
 * the camera, the anti-spoofing gate, the challenge sequence — is the native code the
 * FaceLivenessSDK app runs.
 *
 * The string values in `Pool`, `Challenges`, `Outcomes` and `Failure` are the same wire values
 * Android uses, which is why the C# model layer can be shared across both platforms.
 */

/// <summary>What the activated key unlocks.</summary>
[BaseType(typeof(NSObject), Name = "FPLLicenseStatus")]
[DisableDefaultCtor]
interface FPLLicenseStatus
{
    [Export("licensed")]
    bool Licensed { get; }

    [Export("level")]
    nint Level { get; }

    [Export("levelName")]
    string LevelName { get; }

    [Export("recognition")]
    bool Recognition { get; }

    [Export("liveness")]
    bool Liveness { get; }

    [Export("label")]
    string Label { get; }
}

/// <summary>Activation and licence state. Process-wide, so every member is static.</summary>
[BaseType(typeof(NSObject), Name = "FPLEngine")]
[DisableDefaultCtor]
interface FPLEngine
{
    /// <summary>
    /// Activate with <paramref name="license"/> and load the models. The completion runs on the
    /// main queue with the engine's own return code; 0 is success.
    /// </summary>
    [Static]
    [Export("startWithLicense:completion:")]
    void Start(string license, [NullAllowed] Action<nint> completion);

    [Static]
    [Export("state")]
    FPLEngineState State { get; }

    [Static]
    [Export("isReady")]
    bool IsReady { get; }

    /// <summary>Reads the licence on the engine's worker; the completion runs on the main queue.</summary>
    [Static]
    [Export("licenseStatusWithCompletion:")]
    void LicenseStatus(Action<FPLLicenseStatus> completion);

    /// <summary>The engine's own message for the last activation failure, or an empty string.</summary>
    [Static]
    [Export("lastLicenseError")]
    string LastLicenseError { get; }

    /// <summary>This device's machine code, for issuing a node-locked key.</summary>
    [Static]
    [Export("machineCodeWithCompletion:")]
    void MachineCode(Action<NSString> completion);

    /// <summary>The short message the native home screen shows in its status pill.</summary>
    [Static]
    [Export("stateMessage")]
    string StateMessage { get; }
}

/// <summary>
/// How one active-liveness attempt is run. Every property starts at the native default, so a
/// caller only sets what it means to change.
/// </summary>
[BaseType(typeof(NSObject), Name = "FPLActiveOptions")]
interface FPLActiveOptions
{
    /// <summary>
    /// Challenge keys the sequence may be drawn from: <c>blink</c>, <c>smile</c>,
    /// <c>turn_left</c>, <c>turn_right</c>, <c>turn_up</c>, <c>turn_down</c>, <c>perspective</c>.
    /// An empty or unrecognised pool falls back to the native default.
    /// </summary>
    [Export("pool", ArgumentSemantic.Copy)]
    string[] Pool { get; set; }

    [Export("count")]
    nint Count { get; set; }

    [Export("randomize")]
    bool Randomize { get; set; }

    [Export("livenessThreshold")]
    float LivenessThreshold { get; set; }

    [Export("passiveSamples")]
    nint PassiveSamples { get; set; }

    [Export("framesWait")]
    nint FramesWait { get; set; }

    [Export("yawThreshold")]
    float YawThreshold { get; set; }

    [Export("pitchThreshold")]
    float PitchThreshold { get; set; }

    [Export("smileThreshold")]
    float SmileThreshold { get; set; }

    [Export("blinkThreshold")]
    float BlinkThreshold { get; set; }

    [Export("blinksNumber")]
    nint BlinksNumber { get; set; }

    [Export("perspectiveThreshold")]
    float PerspectiveThreshold { get; set; }

    [Export("alignAngle")]
    float AlignAngle { get; set; }

    /// <summary>Smallest the face may be, as a fraction of the frame's short side, to be scored.</summary>
    [Export("minFaceFraction")]
    float MinFaceFraction { get; set; }

    /// <summary>Lowest image quality the engine's own score may report and still be judged.</summary>
    [Export("qualityThreshold")]
    float QualityThreshold { get; set; }

    [Export("invertTurn")]
    bool InvertTurn { get; set; }

    [Export("invertTilt")]
    bool InvertTilt { get; set; }

    /// <summary>Which lens the screen opens on. The passive screen reads this too.</summary>
    [Export("useFrontCamera")]
    bool UseFrontCamera { get; set; }
}

/// <summary>
/// What one attempt came to. <see cref="Passed"/> is true when the passive anti-spoofing gate
/// cleared and every challenge was completed.
/// </summary>
[BaseType(typeof(NSObject), Name = "FPLActiveResult")]
[DisableDefaultCtor]
interface FPLActiveResult
{
    [Export("passed")]
    bool Passed { get; }

    /// <summary><c>NONE</c>, <c>SPOOF</c>, <c>CHECK</c>, <c>TIMEOUT</c> or <c>CANCELLED</c>.</summary>
    [Export("failure")]
    string Failure { get; }

    /// <summary>Averaged passive anti-spoofing score, or -1 when the engine never returned one.</summary>
    [Export("passiveScore")]
    float PassiveScore { get; }

    [Export("passivePassed")]
    bool PassivePassed { get; }

    /// <summary>The challenge keys asked, in the order they were asked.</summary>
    [Export("challenges")]
    string[] Challenges { get; }

    /// <summary>
    /// One outcome per entry of <see cref="Challenges"/>: <c>PENDING</c>, <c>ACTIVE</c>,
    /// <c>PASSED</c> or <c>FAILED</c>.
    /// </summary>
    [Export("outcomes")]
    string[] Outcomes { get; }

    [Export("durationMs")]
    long DurationMs { get; }

    /// <summary>The challenge key that ended the attempt, or null when none did.</summary>
    [NullAllowed]
    [Export("failedCheck")]
    string FailedCheck { get; }
}

/// <summary>
/// Opens the native liveness screens. Both present modally over whatever is on screen, so a MAUI
/// host does not have to produce a view controller of its own.
/// </summary>
[BaseType(typeof(NSObject), Name = "FPLLiveness")]
[DisableDefaultCtor]
interface FPLLiveness
{
    /// <summary>
    /// Opens the guided active-liveness screen and waits for the attempt to finish. The completion
    /// runs on the main queue with null when the user closed the screen first.
    /// </summary>
    [Static]
    [Export("presentActiveLivenessWithOptions:completion:")]
    void PresentActiveLiveness(FPLActiveOptions options, Action<FPLActiveResult> completion);

    [Static]
    [Export("presentActiveLivenessFrom:options:completion:")]
    void PresentActiveLiveness(
        UIViewController host,
        FPLActiveOptions options,
        Action<FPLActiveResult> completion);

    /// <summary>
    /// Opens the passive-liveness screen: every camera frame is scored on its own, with no action
    /// asked of the user. It has no result; the user closes it.
    /// </summary>
    [Static]
    [Export("presentPassiveLivenessWithFrontCamera:livenessThreshold:")]
    void PresentPassiveLiveness(bool useFrontCamera, float livenessThreshold);

    [Static]
    [Export("presentPassiveLivenessFrom:frontCamera:livenessThreshold:")]
    void PresentPassiveLiveness(
        UIViewController host,
        bool useFrontCamera,
        float livenessThreshold);
}

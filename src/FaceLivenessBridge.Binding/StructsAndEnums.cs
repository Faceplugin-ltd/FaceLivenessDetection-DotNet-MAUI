using ObjCRuntime;

namespace FaceLivenessBridge;

/// <summary>
/// What <c>FPLEngine.State</c> reports. The raw values are the bridge's <c>FPLEngineState</c>,
/// which mirrors Android <c>FaceEngine.State</c>.
/// </summary>
[Native]
public enum FPLEngineState : long
{
    Loading = 0,
    Ready = 1,
    NotActivated = 2,
    LicenseInvalid = 3,
    LicenseExpired = 4,
    Failed = 5,
}

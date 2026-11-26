using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace FaceLiveness.Net.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
        | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    const int ReqActiveLiveness = 0x4C56; // "LV"

    /// <summary>The foreground activity the native screens are launched from.</summary>
    public static MainActivity? Current { get; private set; }

    TaskCompletionSource<Intent?>? _pending;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Current = this;
    }

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Current, this)) Current = null;
        base.OnDestroy();
    }

    /// <summary>
    /// Launches <paramref name="intent"/> for a result and completes with the data it returns, or
    /// <c>null</c> when the screen was closed without one.
    /// </summary>
    public Task<Intent?> StartForResultAsync(Intent intent)
    {
        // One liveness screen at a time: a second request cancels the first rather than leaving it
        // waiting for a result that will never arrive.
        _pending?.TrySetResult(null);
        var pending = new TaskCompletionSource<Intent?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = pending;
        StartActivityForResult(intent, ReqActiveLiveness);
        return pending.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != ReqActiveLiveness) return;
        var pending = _pending;
        _pending = null;
        pending?.TrySetResult(data);
    }
}

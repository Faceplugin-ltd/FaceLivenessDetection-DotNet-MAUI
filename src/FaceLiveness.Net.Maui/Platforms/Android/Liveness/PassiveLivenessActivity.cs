using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Com.Faceplugin.Faceliveness.Kit;
using Java.Util.Concurrent;

using AndroidColor = Android.Graphics.Color;
using NativeEngine = Com.Faceplugin.Faceliveness.Kit.FaceEngine;

// MAUI's implicit usings bring Microsoft.Maui.Controls and .Graphics in, which collide with
// the Android types by name. These screens are Android views, so the Android types win here.
using Paint = Android.Graphics.Paint;
using RectF = Android.Graphics.RectF;
using Size = Android.Util.Size;
using View = Android.Views.View;
using ImageButton = Android.Widget.ImageButton;
using TextView = Android.Widget.TextView;
using FrameLayout = Android.Widget.FrameLayout;

namespace FaceLiveness.Net.Maui;

/// <summary>
/// Passive liveness: every camera frame is scored on its own, with no action asked of the user.
/// The engine reads the anti-spoofing score, quality and head pose from the single frame.
/// <para>
/// A port of the native demo's <c>ui/CameraActivity</c>. The camera binding, the frame conversion
/// and the detection are the kit's — <c>CameraPreview.bind</c>, <c>CameraFrameUtils</c> and
/// <c>FaceEngine.detect</c> — so only the screen itself is C#. The lens and the score threshold
/// arrive as intent extras instead of being read from preferences.
/// </para>
/// </summary>
[Activity(
    Exported = false,
    ScreenOrientation = ScreenOrientation.Portrait,
    Theme = "@style/Theme.AppCompat.NoActionBar")]
public class PassiveLivenessActivity : AppCompatActivity, ImageAnalysis.IAnalyzer
{
    const string ExtraFront = "com.faceplugin.faceliveness.maui.FRONT";
    const string ExtraThreshold = "com.faceplugin.faceliveness.maui.THRESHOLD";
    const int ReqCamera = 1;

    PreviewView _viewFinder = null!;
    PassiveFaceView _faceView = null!;
    TextView _hint = null!;
    IExecutorService _cameraExecutor = null!;

    ProcessCameraProvider? _cameraProvider;
    int _lensFacing = CameraSelector.LensFacingFront;
    volatile bool _busy;
    volatile bool _hasFaces;

    /// <summary>Intent that opens the passive screen with the host's lens and score threshold.</summary>
    public static Intent NewIntent(Context context, bool useFrontCamera, float livenessThreshold) =>
        new Intent(context, typeof(PassiveLivenessActivity))
            .PutExtra(ExtraFront, useFrontCamera)
            .PutExtra(ExtraThreshold, livenessThreshold);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        _lensFacing = Intent?.GetBooleanExtra(ExtraFront, true) ?? true
            ? CameraSelector.LensFacingFront
            : CameraSelector.LensFacingBack;
        var threshold = Intent?.GetFloatExtra(ExtraThreshold, 0.5f) ?? 0.5f;

        SetContentView(BuildLayout(threshold));
        _cameraExecutor = Executors.NewFixedThreadPool(1)!;

        if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera)
            == Permission.Denied)
        {
            ActivityCompat.RequestPermissions(
                this, [Android.Manifest.Permission.Camera], ReqCamera);
        }
        else
        {
            _viewFinder.Post(SetUpCamera);
        }
        UpdateHint();
    }

    /// <summary>
    /// The native screen's <c>fp_pl_activity.xml</c>, built in code: preview, overlay, a title
    /// pill, close and switch-camera chips, and the hint pill. The chip backgrounds and icons are
    /// the active library's own <c>fp_al_*</c> resources, so the two screens look alike.
    /// </summary>
    View BuildLayout(float threshold)
    {
        var root = new FrameLayout(this);
        root.SetBackgroundColor(AndroidColor.Black);

        _viewFinder = new PreviewView(this)
        {
            LayoutParameters = Match(),
        };
        // The binding annotates these as nullable because they are Java static fields; they are
        // the library's own constants and are never null.
        _viewFinder.SetImplementationMode(PreviewView.ImplementationMode.Performance!);
        _viewFinder.SetScaleType(PreviewView.ScaleType.FillCenter!);
        root.AddView(_viewFinder);

        _faceView = new PassiveFaceView(this)
        {
            LayoutParameters = Match(),
            LivenessThreshold = threshold,
        };
        _faceView.SetMirrorX(_lensFacing == CameraSelector.LensFacingFront);
        root.AddView(_faceView);

        root.AddView(Pill(
            "Passive liveness", 13f, GravityFlags.Top | GravityFlags.CenterHorizontal,
            topMarginDp: 26, padXDp: 16, padYDp: 7));

        _hint = Pill(
            "Position your face in the frame", 14f,
            GravityFlags.Bottom | GravityFlags.CenterHorizontal,
            bottomMarginDp: 34, padXDp: 20, padYDp: 11);
        root.AddView(_hint);

        root.AddView(Chip(
            Resource.Drawable.fp_al_ic_close, "Close",
            GravityFlags.Top | GravityFlags.Start, paddingDp: 11, () => Finish()));
        root.AddView(Chip(
            Resource.Drawable.fp_al_ic_switch_camera, "Switch camera",
            GravityFlags.Top | GravityFlags.End, paddingDp: 10, ToggleCamera));

        return root;
    }

    static ViewGroup.LayoutParams Match() => new FrameLayout.LayoutParams(
        ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

    TextView Pill(
        string text,
        float sizeSp,
        GravityFlags gravity,
        int topMarginDp = 0,
        int bottomMarginDp = 0,
        int padXDp = 16,
        int padYDp = 8)
    {
        var view = new TextView(this)
        {
            Text = text,
            TextSize = sizeSp,
            Gravity = GravityFlags.Center,
        };
        view.SetTextColor(new AndroidColor(ContextCompat.GetColor(this, Resource.Color.fp_al_text)));
        view.SetBackgroundResource(Resource.Drawable.fp_al_bg_pill);
        view.SetPadding(Dp(padXDp), Dp(padYDp), Dp(padXDp), Dp(padYDp));
        var layout = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = gravity,
            TopMargin = Dp(topMarginDp),
            BottomMargin = Dp(bottomMarginDp),
            LeftMargin = Dp(24),
            RightMargin = Dp(24),
        };
        view.LayoutParameters = layout;
        return view;
    }

    ImageButton Chip(int iconRes, string description, GravityFlags gravity, int paddingDp, Action onClick)
    {
        var button = new ImageButton(this) { ContentDescription = description };
        button.SetImageResource(iconRes);
        button.SetBackgroundResource(Resource.Drawable.fp_al_bg_chip);
        button.SetPadding(Dp(paddingDp), Dp(paddingDp), Dp(paddingDp), Dp(paddingDp));
        button.LayoutParameters = new FrameLayout.LayoutParams(Dp(44), Dp(44))
        {
            Gravity = gravity,
            TopMargin = Dp(16),
            LeftMargin = Dp(16),
            RightMargin = Dp(16),
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    int Dp(int value) => (int)Math.Round(value * Resources!.DisplayMetrics!.Density);

    public override void OnRequestPermissionsResult(
        int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == ReqCamera
            && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.Camera)
                == Permission.Granted)
        {
            _viewFinder.Post(SetUpCamera);
        }
    }

    protected override void OnDestroy()
    {
        _cameraProvider?.UnbindAll();
        _cameraExecutor.Shutdown();
        base.OnDestroy();
    }

    void ToggleCamera()
    {
        _lensFacing = _lensFacing == CameraSelector.LensFacingBack
            ? CameraSelector.LensFacingFront
            : CameraSelector.LensFacingBack;
        _faceView.SetMirrorX(_lensFacing == CameraSelector.LensFacingFront);
        _faceView.SetFaces(null);
        _hasFaces = false;
        UpdateHint();
        _cameraProvider?.UnbindAll();
        _viewFinder.Post(SetUpCamera);
    }

    void SetUpCamera()
    {
        var future = ProcessCameraProvider.GetInstance(this);
        future.AddListener(new Java.Lang.Runnable(() =>
        {
            try
            {
                _cameraProvider = (ProcessCameraProvider?)future.Get();
                BindCameraUseCases();
            }
            catch (Java.Lang.Exception)
            {
                // The provider can fail on a device whose camera is held by another app.
            }
        }), ContextCompat.GetMainExecutor(this));
    }

    void BindCameraUseCases()
    {
        if (_cameraProvider is not { } provider) return;
        try
        {
            CameraPreview.Bind(this, provider, _viewFinder, _lensFacing, _cameraExecutor, this);
        }
        catch (Java.Lang.Exception)
        {
            // Same: a camera that cannot be bound leaves the screen on its hint.
        }
    }

    /// <summary>One analyzed frame. The engine is not concurrent, so a frame arriving while the
    /// last one is still being scored is dropped rather than queued.</summary>
    public void Analyze(IImageProxy imageProxy)
    {
        Bitmap? frame = null;
        try
        {
            if (NativeEngine.Instance.IsReady)
            {
                var back = _lensFacing == CameraSelector.LensFacingBack;
                frame = CameraFrameUtils.FromImageProxy(imageProxy, back);
            }
        }
        catch (Java.Lang.Exception)
        {
            // A frame that cannot be converted is simply skipped.
        }
        finally
        {
            imageProxy.Close();
        }

        if (frame is null) return;
        if (_busy)
        {
            if (!frame.IsRecycled) frame.Recycle();
            return;
        }
        _busy = true;

        var size = new Size(frame.Width, frame.Height);
        NativeEngine.Instance.Async(new Java.Lang.Runnable(() =>
        {
            IList<DetectedFace> faces;
            try
            {
                faces = NativeEngine.Instance.Detect(frame, Passive);
            }
            catch (Java.Lang.Exception)
            {
                faces = [];
            }
            finally
            {
                if (!frame.IsRecycled) frame.Recycle();
            }

            _hasFaces = faces.Count > 0;
            RunOnUiThread(() =>
            {
                _faceView.SetFrameSize(size);
                _faceView.SetFaces([.. faces]);
                UpdateHint();
            });
            _busy = false;
        }));
    }

    /// <summary>Score, quality and pose for the HUD — the kit's <c>DetectOptions.PASSIVE</c>.</summary>
    static DetectOptions Passive => new(
        liveness: true, quality: true, pose: true,
        eyes: false, expression: false, points: false, ageGender: false);

    void UpdateHint() =>
        _hint.Text = _hasFaces ? "Analyzing liveness…" : "Position your face in the frame";
}

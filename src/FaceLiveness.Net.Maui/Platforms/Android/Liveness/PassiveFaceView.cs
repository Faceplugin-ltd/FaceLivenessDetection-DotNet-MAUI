using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using AndroidX.Core.Content;
using Com.Faceplugin.Faceliveness.Kit;

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
/// Face boxes and the passive-liveness read-out, drawn over the camera preview.
/// <para>
/// A port of the native demo's <c>ui/FaceView</c> — the same geometry, the same three HUD lines
/// and the same verdict rule. The only change is that the threshold is supplied by the host
/// through <see cref="LivenessThreshold"/> instead of being read from that app's preferences.
/// </para>
/// </summary>
sealed class PassiveFaceView : View
{
    readonly Paint _realPaint;
    readonly Paint _spoofPaint;
    readonly Paint _neutralPaint;
    readonly Paint _textPaint;
    readonly Paint _textBgPaint;
    readonly float _lineHeight;
    readonly RectF _drawRect = new();

    Size? _frameSize;
    bool _mirrorX;
    IReadOnlyList<DetectedFace> _faces = [];

    /// <summary>Lowest score that counts as a live face.</summary>
    public float LivenessThreshold { get; set; } = 0.5f;

    public PassiveFaceView(Context context) : base(context)
    {
        _realPaint = StrokePaint(Color(Resource.Color.fp_pl_real));
        _spoofPaint = StrokePaint(Color(Resource.Color.fp_pl_spoof));
        _neutralPaint = StrokePaint(Color(Resource.Color.fp_pl_neutral));

        _textPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = Color(Resource.Color.fp_al_text),
            TextSize = TypedValue.ApplyDimension(
                ComplexUnitType.Sp, 13f, Resources!.DisplayMetrics!),
        };
        _textPaint.SetTypeface(Typeface.Create("sans-serif-medium", TypefaceStyle.Normal));

        _textBgPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = new Android.Graphics.Color(0, 0, 0, 0xB3),
        };
        _textBgPaint.SetStyle(Paint.Style.Fill);

        _lineHeight = _textPaint.TextSize * 1.25f;
    }

    public void SetFrameSize(Size size) => _frameSize = size;

    public void SetMirrorX(bool mirror) => _mirrorX = mirror;

    public void SetFaces(IReadOnlyList<DetectedFace>? faces)
    {
        _faces = faces ?? [];
        Invalidate();
    }

    /// <summary>The same rule the native app applies: a spoof label wins over any score.</summary>
    bool LivenessPassed(float score, string? label)
    {
        var lower = (label ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("spoof") || lower.Contains("fake")) return false;
        return score >= LivenessThreshold;
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        if (_frameSize is not { } frame || _faces.Count == 0) return;

        float frameW = frame.Width;
        float frameH = frame.Height;
        float viewW = Width;
        float viewH = Height;
        if (frameW <= 0f || frameH <= 0f || viewW <= 0f || viewH <= 0f) return;

        // PreviewView is FILL_CENTER, so the frame is scaled up and cropped the same way.
        var scale = Math.Max(viewW / frameW, viewH / frameH);
        var dx = (viewW - frameW * scale) / 2f;
        var dy = (viewH - frameH * scale) / 2f;

        foreach (var face in _faces)
        {
            var bounds = face.Bounds;
            var left = MapX(bounds.Left * scale, dx, viewW);
            var right = MapX(bounds.Right * scale, dx, viewW);
            if (left > right) (left, right) = (right, left);
            var top = bounds.Top * scale + dy;
            var bottom = bounds.Bottom * scale + dy;

            var known = face.HasLiveness;
            var live = known && LivenessPassed(face.Liveness, face.LivenessLabel);
            var paint = !known ? _neutralPaint : live ? _realPaint : _spoofPaint;

            var corner = Math.Min(right - left, bottom - top) * 0.22f;
            _drawRect.Set(left, top, right, bottom);
            canvas.DrawRoundRect(_drawRect, corner, corner, paint);
            DrawInfoBlock(canvas, face, left, top, live, known);
        }
    }

    void DrawInfoBlock(
        Canvas canvas,
        DetectedFace face,
        float boxLeft,
        float boxTop,
        bool live,
        bool livenessKnown)
    {
        var lines = BuildLines(face, live, livenessKnown);
        const float padX = 10f;
        const float padY = 7f;

        var maxWidth = 0f;
        foreach (var line in lines) maxWidth = Math.Max(maxWidth, _textPaint.MeasureText(line));
        var blockW = maxWidth + padX * 2f;
        var blockH = lines.Count * _lineHeight + padY * 2f;

        var left = boxLeft;
        var top = boxTop - blockH - 8f;
        if (top < 4f) top = Math.Min(boxTop + 8f, Height - blockH - 4f);
        if (left + blockW > Width - 4f) left = Math.Max(4f, Width - blockW - 4f);

        _drawRect.Set(left, top, left + blockW, top + blockH);
        canvas.DrawRoundRect(_drawRect, 14f, 14f, _textBgPaint);
        var textY = top + padY + _textPaint.TextSize;
        foreach (var line in lines)
        {
            canvas.DrawText(line, left + padX, textY, _textPaint);
            textY += _lineHeight;
        }
    }

    List<string> BuildLines(DetectedFace face, bool live, bool livenessKnown) =>
    [
        livenessKnown
            ? $"Liveness : {(live ? "Real" : "Spoof")} {Percent(face.Liveness)}%"
            : "Liveness : —",
        float.IsNaN(face.Quality)
            ? $"Quality : {Percent(face.Score)}%"
            : $"Quality : {Percent(face.Quality)}%",
        face.HasPose
            ? $"Pose : {face.Yaw:0}° / {face.Pitch:0}°"
            : "Pose : —",
    ];

    static int Percent(float value) =>
        float.IsNaN(value) ? 0 : (int)Math.Round(Math.Clamp(value, 0f, 1f) * 100f);

    float MapX(float scaled, float dx, float viewW)
    {
        var vx = scaled + dx;
        return _mirrorX ? viewW - vx : vx;
    }

    static Paint StrokePaint(Android.Graphics.Color color)
    {
        var paint = new Paint(PaintFlags.AntiAlias) { Color = color, StrokeWidth = 4f };
        paint.SetStyle(Paint.Style.Stroke);
        paint.StrokeJoin = Paint.Join.Round;
        return paint;
    }

    Android.Graphics.Color Color(int resId) =>
        new(ContextCompat.GetColor(Context!, resId));
}

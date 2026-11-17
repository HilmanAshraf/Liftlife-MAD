#if IOS
using CoreHaptics;
using Foundation;
#endif

namespace LiftLife.Services;

/// <summary>
/// Strong "rest is over" alert. On iPhone it plays three firm buzzes with Core Haptics
/// (much stronger than a normal haptic tap, and it still works in silent mode).
/// On other platforms it falls back to the MAUI Vibration API.
/// </summary>
public static class RestAlert
{
#if IOS
    private static CHHapticEngine? _engine;
#endif

    public static void Play()
    {
#if IOS
        if (TryPlayCoreHaptics())
            return;
#endif
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(800)); } catch { /* no vibration motor */ }
    }

#if IOS
    /// <summary>Three strong buzzes, 0.45 s each, 0.25 s apart.</summary>
    private static bool TryPlayCoreHaptics()
    {
        try
        {
            if (!CHHapticEngine.GetHardwareCapabilities().SupportsHaptics)
                return false;

            _engine ??= new CHHapticEngine(out NSError? createError);
            if (_engine is null) return false;

            // The engine stops when the app goes to the background, so start it every time.
            if (!_engine.Start(out NSError? startError))
            {
                _engine = null;
                return false;
            }

            var events = new List<CHHapticEvent>();
            for (var i = 0; i < 3; i++)
            {
                var time = i * 0.7;
                var parameters = new[]
                {
                    new CHHapticEventParameter(CHHapticEventParameterId.HapticIntensity, 1.0f),
                    new CHHapticEventParameter(CHHapticEventParameterId.HapticSharpness, 0.6f),
                };
                events.Add(new CHHapticEvent(CHHapticEventType.HapticContinuous, parameters, time, 0.45));
            }

            var pattern = new CHHapticPattern(events.ToArray(), Array.Empty<CHHapticDynamicParameter>(), out NSError? patternError);
            var player = _engine.CreatePlayer(pattern, out NSError? playerError);
            if (player is null) return false;

            return player.Start(0, out NSError? playError);
        }
        catch
        {
            _engine = null;
            return false;
        }
    }
#endif
}

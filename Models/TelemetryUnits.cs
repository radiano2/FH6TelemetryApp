namespace FH6TelemetryApp.Models;

public static class TelemetryUnits
{
    public static float SpeedKph(float ms)  => ms * 3.6f;
    public static float SpeedMph(float ms)  => ms * 2.237f;

    public static float AccelToG(float ms2) => ms2 / 9.80665f;

    public static float PowerKw(float watts)  => watts / 1000f;
    public static float PowerHp(float watts)  => watts / 745.7f;

    public static float TorqueLbFt(float nm) => nm * 0.7376f;

    public static float TireTempCelsius(float fahrenheit) => (fahrenheit - 32f) * (5f / 9f);

    public static float AngleDegrees(float radians) => radians * (180f / MathF.PI);

    public static float ThrottlePercent(byte raw) => raw / 255f * 100f;
    public static float BrakePercent(byte raw)    => raw / 255f * 100f;

    public static string FormatLapTime(float seconds)
    {
        if (seconds <= 0) return "--:--.---";
        var m  = (int)(seconds / 60);
        var s  = seconds % 60;
        return $"{m}:{s:00.000}";
    }

    public static string FormatDelta(float current, float best)
    {
        if (current <= 0 || best <= 0) return "--";
        var d = current - best;
        return d >= 0 ? $"+{d:F3}" : $"{d:F3}";
    }
}

namespace FH6TelemetryApp.Models;

public sealed class LapSnapshot
{
    public int LapNumber { get; init; }

    // Tire temps per corner — °C
    public float TireTempFLMin { get; init; } public float TireTempFLAvg { get; init; } public float TireTempFLMax { get; init; }
    public float TireTempFRMin { get; init; } public float TireTempFRAvg { get; init; } public float TireTempFRMax { get; init; }
    public float TireTempRLMin { get; init; } public float TireTempRLAvg { get; init; } public float TireTempRLMax { get; init; }
    public float TireTempRRMin { get; init; } public float TireTempRRAvg { get; init; } public float TireTempRRMax { get; init; }

    // Suspension bottom-out counts (NormalizedSuspensionTravel >= 0.95)
    public int SuspBottomFL { get; init; }
    public int SuspBottomFR { get; init; }
    public int SuspBottomRL { get; init; }
    public int SuspBottomRR { get; init; }

    // Peak G-forces
    public float PeakLateralG  { get; init; }
    public float PeakBrakingG  { get; init; }
    public float PeakAccelG    { get; init; }

    // Handling balance (0–1 fraction of samples)
    public float UnderSteerRatio { get; init; }
    public float OverSteerRatio  { get; init; }

    // Braking: mean(decel-G / brake_input) during braking zones
    public float BrakeEfficiencyAvg { get; init; }

    // RPM usage
    public int   LimiterHitCount   { get; init; }
    public float AvgUpshiftRpmRatio { get; init; }

    // Throttle discipline
    public float CoastingCornerFraction { get; init; }  // fraction of cornering samples with no pedal

    // Traction on corner exit
    public float WheelspinExitFraction  { get; init; }  // fraction of corner-exit events with wheelspin

    // Gear range usage
    public int LuggingCount          { get; init; }   // samples below 35% RPM with throttle
    public int OverRevSustainedCount { get; init; }   // samples 90-99% RPM while still accelerating

    public float LapTimeSeconds { get; init; }
    public int   SampleCount    { get; init; }
}

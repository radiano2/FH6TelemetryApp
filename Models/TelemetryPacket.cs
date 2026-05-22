using System.Text.Json.Serialization;

namespace FH6TelemetryApp.Models;

/// <summary>
/// FH6 combined sled+dash UDP telemetry packet — 323+ bytes, little-endian, no header.
/// FH6-exclusive fields (CarGroup, SmashableVelDiff, SmashableMass) sit after NumCylinders.
/// </summary>
public record TelemetryPacket
{
    // ── Sled fields (offsets 0–231) ────────────────────────────────────────────

    /// <summary>1 when player is in a race, 0 in menus.</summary>
    public int IsRaceOn { get; init; }

    /// <summary>Milliseconds since game started.</summary>
    public uint TimestampMS { get; init; }

    public float EngineMaxRpm    { get; init; }
    public float EngineIdleRpm   { get; init; }
    public float CurrentEngineRpm { get; init; }

    /// <summary>Lateral acceleration (m/s²). Positive = right.</summary>
    public float AccelerationX { get; init; }
    /// <summary>Vertical acceleration (m/s²).</summary>
    public float AccelerationY { get; init; }
    /// <summary>Longitudinal acceleration (m/s²). Positive = forward.</summary>
    public float AccelerationZ { get; init; }

    public float VelocityX { get; init; }
    public float VelocityY { get; init; }
    public float VelocityZ { get; init; }

    public float AngularVelocityX { get; init; }
    public float AngularVelocityY { get; init; }
    public float AngularVelocityZ { get; init; }

    public float Yaw   { get; init; }
    public float Pitch { get; init; }
    public float Roll  { get; init; }

    /// <summary>0 = fully extended, 1 = fully compressed.</summary>
    public float NormalizedSuspensionTravelFL { get; init; }
    public float NormalizedSuspensionTravelFR { get; init; }
    public float NormalizedSuspensionTravelRL { get; init; }
    public float NormalizedSuspensionTravelRR { get; init; }

    public float TireSlipRatioFL { get; init; }
    public float TireSlipRatioFR { get; init; }
    public float TireSlipRatioRL { get; init; }
    public float TireSlipRatioRR { get; init; }

    /// <summary>Radians/second.</summary>
    public float WheelRotationSpeedFL { get; init; }
    public float WheelRotationSpeedFR { get; init; }
    public float WheelRotationSpeedRL { get; init; }
    public float WheelRotationSpeedRR { get; init; }

    public int WheelOnRumbleStripFL { get; init; }
    public int WheelOnRumbleStripFR { get; init; }
    public int WheelOnRumbleStripRL { get; init; }
    public int WheelOnRumbleStripRR { get; init; }

    public float WheelInPuddleDepthFL { get; init; }
    public float WheelInPuddleDepthFR { get; init; }
    public float WheelInPuddleDepthRL { get; init; }
    public float WheelInPuddleDepthRR { get; init; }

    public float SurfaceRumbleFL { get; init; }
    public float SurfaceRumbleFR { get; init; }
    public float SurfaceRumbleRL { get; init; }
    public float SurfaceRumbleRR { get; init; }

    /// <summary>Radians. Positive = slip toward outside of turn.</summary>
    public float TireSlipAngleFL { get; init; }
    public float TireSlipAngleFR { get; init; }
    public float TireSlipAngleRL { get; init; }
    public float TireSlipAngleRR { get; init; }

    public float TireCombinedSlipFL { get; init; }
    public float TireCombinedSlipFR { get; init; }
    public float TireCombinedSlipRL { get; init; }
    public float TireCombinedSlipRR { get; init; }

    /// <summary>Meters of suspension travel from fully extended.</summary>
    public float SuspensionTravelMetersFL { get; init; }
    public float SuspensionTravelMetersFR { get; init; }
    public float SuspensionTravelMetersRL { get; init; }
    public float SuspensionTravelMetersRR { get; init; }

    /// <summary>Unique ordinal for the current car model.</summary>
    public int CarOrdinal { get; init; }

    /// <summary>0=D, 1=C, 2=B, 3=A, 4=S1, 5=S2, 6=X</summary>
    public int CarClass { get; init; }

    /// <summary>100–999</summary>
    public int CarPerformanceIndex { get; init; }

    /// <summary>0=FWD, 1=RWD, 2=AWD</summary>
    public int DrivetrainType { get; init; }

    public int NumCylinders { get; init; }

    // ── FH6-exclusive fields (offsets 232–243) ─────────────────────────────────

    public int   CarGroup        { get; init; }
    public float SmashableVelDiff { get; init; }
    public float SmashableMass   { get; init; }

    // ── Dash fields (offsets 244–322) ──────────────────────────────────────────

    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }

    /// <summary>m/s — convert with TelemetryUnits.</summary>
    public float Speed  { get; init; }
    /// <summary>Watts.</summary>
    public float Power  { get; init; }
    /// <summary>N·m.</summary>
    public float Torque { get; init; }

    /// <summary>°F — convert with TelemetryUnits.TireTempCelsius.</summary>
    public float TireTempFL { get; init; }
    public float TireTempFR { get; init; }
    public float TireTempRL { get; init; }
    public float TireTempRR { get; init; }

    public float Boost           { get; init; }
    /// <summary>0–1 fuel level.</summary>
    public float Fuel            { get; init; }
    public float DistanceTraveled { get; init; }

    /// <summary>Seconds. -1 if no best lap set.</summary>
    public float BestLap        { get; init; }
    public float LastLap        { get; init; }
    public float CurrentLap     { get; init; }
    public float CurrentRaceTime { get; init; }

    public ushort LapNumber     { get; init; }
    public byte   RacePosition  { get; init; }

    /// <summary>0–255 raw pedal input.</summary>
    public byte Accel     { get; init; }
    public byte Brake     { get; init; }
    public byte Clutch    { get; init; }
    public byte HandBrake { get; init; }

    /// <summary>0=Reverse, 1–10=forward gears.</summary>
    public byte Gear  { get; init; }

    /// <summary>Signed: −128=full left, 127=full right.</summary>
    public sbyte Steer                      { get; init; }
    public sbyte NormalizedDrivingLine      { get; init; }
    public sbyte NormalizedAIBrakeDifference { get; init; }
}

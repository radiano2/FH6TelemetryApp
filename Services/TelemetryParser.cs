using System.Buffers.Binary;
using FH6TelemetryApp.Models;

namespace FH6TelemetryApp.Services;

public static class TelemetryParser
{
    // FH6 combined sled+dash packet — no 12-byte Horizon header.
    // 3 FH6-exclusive fields after NumCylinders push dash fields to offset 244.
    public const int MinPacketSize = 323;

    public static bool TryParse(ReadOnlySpan<byte> data, out TelemetryPacket packet)
    {
        if (data.Length < MinPacketSize)
        {
            packet = default!;
            return false;
        }

        packet = new TelemetryPacket
        {
            // ── Sled ──────────────────────────────────────────────────────────
            IsRaceOn          = BinaryPrimitives.ReadInt32LittleEndian(data[0..]),
            TimestampMS       = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]),
            EngineMaxRpm      = BinaryPrimitives.ReadSingleLittleEndian(data[8..]),
            EngineIdleRpm     = BinaryPrimitives.ReadSingleLittleEndian(data[12..]),
            CurrentEngineRpm  = BinaryPrimitives.ReadSingleLittleEndian(data[16..]),

            AccelerationX = BinaryPrimitives.ReadSingleLittleEndian(data[20..]),
            AccelerationY = BinaryPrimitives.ReadSingleLittleEndian(data[24..]),
            AccelerationZ = BinaryPrimitives.ReadSingleLittleEndian(data[28..]),

            VelocityX = BinaryPrimitives.ReadSingleLittleEndian(data[32..]),
            VelocityY = BinaryPrimitives.ReadSingleLittleEndian(data[36..]),
            VelocityZ = BinaryPrimitives.ReadSingleLittleEndian(data[40..]),

            AngularVelocityX = BinaryPrimitives.ReadSingleLittleEndian(data[44..]),
            AngularVelocityY = BinaryPrimitives.ReadSingleLittleEndian(data[48..]),
            AngularVelocityZ = BinaryPrimitives.ReadSingleLittleEndian(data[52..]),

            Yaw   = BinaryPrimitives.ReadSingleLittleEndian(data[56..]),
            Pitch = BinaryPrimitives.ReadSingleLittleEndian(data[60..]),
            Roll  = BinaryPrimitives.ReadSingleLittleEndian(data[64..]),

            NormalizedSuspensionTravelFL = BinaryPrimitives.ReadSingleLittleEndian(data[68..]),
            NormalizedSuspensionTravelFR = BinaryPrimitives.ReadSingleLittleEndian(data[72..]),
            NormalizedSuspensionTravelRL = BinaryPrimitives.ReadSingleLittleEndian(data[76..]),
            NormalizedSuspensionTravelRR = BinaryPrimitives.ReadSingleLittleEndian(data[80..]),

            TireSlipRatioFL = BinaryPrimitives.ReadSingleLittleEndian(data[84..]),
            TireSlipRatioFR = BinaryPrimitives.ReadSingleLittleEndian(data[88..]),
            TireSlipRatioRL = BinaryPrimitives.ReadSingleLittleEndian(data[92..]),
            TireSlipRatioRR = BinaryPrimitives.ReadSingleLittleEndian(data[96..]),

            WheelRotationSpeedFL = BinaryPrimitives.ReadSingleLittleEndian(data[100..]),
            WheelRotationSpeedFR = BinaryPrimitives.ReadSingleLittleEndian(data[104..]),
            WheelRotationSpeedRL = BinaryPrimitives.ReadSingleLittleEndian(data[108..]),
            WheelRotationSpeedRR = BinaryPrimitives.ReadSingleLittleEndian(data[112..]),

            WheelOnRumbleStripFL = BinaryPrimitives.ReadInt32LittleEndian(data[116..]),
            WheelOnRumbleStripFR = BinaryPrimitives.ReadInt32LittleEndian(data[120..]),
            WheelOnRumbleStripRL = BinaryPrimitives.ReadInt32LittleEndian(data[124..]),
            WheelOnRumbleStripRR = BinaryPrimitives.ReadInt32LittleEndian(data[128..]),

            WheelInPuddleDepthFL = BinaryPrimitives.ReadSingleLittleEndian(data[132..]),
            WheelInPuddleDepthFR = BinaryPrimitives.ReadSingleLittleEndian(data[136..]),
            WheelInPuddleDepthRL = BinaryPrimitives.ReadSingleLittleEndian(data[140..]),
            WheelInPuddleDepthRR = BinaryPrimitives.ReadSingleLittleEndian(data[144..]),

            SurfaceRumbleFL = BinaryPrimitives.ReadSingleLittleEndian(data[148..]),
            SurfaceRumbleFR = BinaryPrimitives.ReadSingleLittleEndian(data[152..]),
            SurfaceRumbleRL = BinaryPrimitives.ReadSingleLittleEndian(data[156..]),
            SurfaceRumbleRR = BinaryPrimitives.ReadSingleLittleEndian(data[160..]),

            TireSlipAngleFL = BinaryPrimitives.ReadSingleLittleEndian(data[164..]),
            TireSlipAngleFR = BinaryPrimitives.ReadSingleLittleEndian(data[168..]),
            TireSlipAngleRL = BinaryPrimitives.ReadSingleLittleEndian(data[172..]),
            TireSlipAngleRR = BinaryPrimitives.ReadSingleLittleEndian(data[176..]),

            TireCombinedSlipFL = BinaryPrimitives.ReadSingleLittleEndian(data[180..]),
            TireCombinedSlipFR = BinaryPrimitives.ReadSingleLittleEndian(data[184..]),
            TireCombinedSlipRL = BinaryPrimitives.ReadSingleLittleEndian(data[188..]),
            TireCombinedSlipRR = BinaryPrimitives.ReadSingleLittleEndian(data[192..]),

            SuspensionTravelMetersFL = BinaryPrimitives.ReadSingleLittleEndian(data[196..]),
            SuspensionTravelMetersFR = BinaryPrimitives.ReadSingleLittleEndian(data[200..]),
            SuspensionTravelMetersRL = BinaryPrimitives.ReadSingleLittleEndian(data[204..]),
            SuspensionTravelMetersRR = BinaryPrimitives.ReadSingleLittleEndian(data[208..]),

            CarOrdinal           = BinaryPrimitives.ReadInt32LittleEndian(data[212..]),
            CarClass             = BinaryPrimitives.ReadInt32LittleEndian(data[216..]),
            CarPerformanceIndex  = BinaryPrimitives.ReadInt32LittleEndian(data[220..]),
            DrivetrainType       = BinaryPrimitives.ReadInt32LittleEndian(data[224..]),
            NumCylinders         = BinaryPrimitives.ReadInt32LittleEndian(data[228..]),

            // ── FH6-exclusive ─────────────────────────────────────────────────
            CarGroup         = BinaryPrimitives.ReadInt32LittleEndian(data[232..]),
            SmashableVelDiff = BinaryPrimitives.ReadSingleLittleEndian(data[236..]),
            SmashableMass    = BinaryPrimitives.ReadSingleLittleEndian(data[240..]),

            // ── Dash ──────────────────────────────────────────────────────────
            PositionX = BinaryPrimitives.ReadSingleLittleEndian(data[244..]),
            PositionY = BinaryPrimitives.ReadSingleLittleEndian(data[248..]),
            PositionZ = BinaryPrimitives.ReadSingleLittleEndian(data[252..]),

            Speed  = BinaryPrimitives.ReadSingleLittleEndian(data[256..]),
            Power  = BinaryPrimitives.ReadSingleLittleEndian(data[260..]),
            Torque = BinaryPrimitives.ReadSingleLittleEndian(data[264..]),

            TireTempFL = BinaryPrimitives.ReadSingleLittleEndian(data[268..]),
            TireTempFR = BinaryPrimitives.ReadSingleLittleEndian(data[272..]),
            TireTempRL = BinaryPrimitives.ReadSingleLittleEndian(data[276..]),
            TireTempRR = BinaryPrimitives.ReadSingleLittleEndian(data[280..]),

            Boost            = BinaryPrimitives.ReadSingleLittleEndian(data[284..]),
            Fuel             = BinaryPrimitives.ReadSingleLittleEndian(data[288..]),
            DistanceTraveled = BinaryPrimitives.ReadSingleLittleEndian(data[292..]),

            BestLap         = BinaryPrimitives.ReadSingleLittleEndian(data[296..]),
            LastLap         = BinaryPrimitives.ReadSingleLittleEndian(data[300..]),
            CurrentLap      = BinaryPrimitives.ReadSingleLittleEndian(data[304..]),
            CurrentRaceTime = BinaryPrimitives.ReadSingleLittleEndian(data[308..]),

            LapNumber    = BinaryPrimitives.ReadUInt16LittleEndian(data[312..]),
            RacePosition = data[314],

            Accel     = data[315],
            Brake     = data[316],
            Clutch    = data[317],
            HandBrake = data[318],
            Gear      = data[319],

            Steer                       = (sbyte)data[320],
            NormalizedDrivingLine       = (sbyte)data[321],
            NormalizedAIBrakeDifference = (sbyte)data[322],
        };

        return true;
    }
}

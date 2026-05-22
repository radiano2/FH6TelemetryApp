using FH6TelemetryApp.Models;
using static FH6TelemetryApp.Services.SessionRepository;

namespace FH6TelemetryApp.Services;

/// <summary>
/// Singleton that always listens to the telemetry stream.
///
/// RACE    — auto-starts when RacePosition > 0 (in a structured race),
///           auto-ends when RacePosition drops back to 0 or packets stop for 5 s.
/// FREE ROAM — never auto-starts; call StartManualAsync() from the UI button.
///
/// ForceEndAsync() stops any active session early from the UI.
/// </summary>
public sealed class DiagnosticsRecorder : IDisposable
{
    private readonly TelemetryBroadcaster _broadcaster;
    private readonly SessionRepository    _sessionRepo;

    // ── Public state ─────────────────────────────────────────────────────────
    public bool          IsRecording   { get; private set; }
    public bool          IsManual      { get; private set; }   // true = free-roam manual recording
    public RaceSession?  ActiveSession { get; private set; }

    /// Fired on the thread-pool when a session ends (auto or manual).
    public event Action? SessionEnded;

    // ── Race auto-detection ───────────────────────────────────────────────────
    private DateTime _lastPacketAt   = DateTime.MinValue;
    private bool     _prevInRace     = false;
    // 30 s gives plenty of room for mid-race pauses; covers game crash too
    private const double EndGapSec   = 30.0;

    // ── Per-lap accumulator ──────────────────────────────────────────────────
    private LapAccumulator _currentLap       = new();
    private int            _currentLapNumber = -1;
    private int            _prevLapNumber    = -1;

    // ── Raw packet buffer (bulk-written every 50 packets) ────────────────────
    private readonly List<RawPacketDocument> _packetBuffer = [];
    private const int FlushEvery = 50;

    // ── Watchdog timer — detects packet silence ───────────────────────────────
    private readonly PeriodicTimer _watchdog;
    private readonly Task          _watchdogTask;

    public DiagnosticsRecorder(TelemetryBroadcaster broadcaster, SessionRepository sessionRepo)
    {
        _broadcaster = broadcaster;
        _sessionRepo = sessionRepo;

        // Always subscribed — we decide whether to record inside OnPacket
        _broadcaster.PacketBroadcast += OnPacket;

        _watchdog     = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _watchdogTask = RunWatchdogAsync();
    }

    // ── Watchdog — ends any active session after sustained silence ────────────
    // Covers: game crash, alt-tab, or manual free-roam recording left running.
    private async Task RunWatchdogAsync()
    {
        try
        {
            while (await _watchdog.WaitForNextTickAsync())
            {
                if (!IsRecording) continue;
                if (_lastPacketAt == DateTime.MinValue) continue;

                var silenceSec = (DateTime.UtcNow - _lastPacketAt).TotalSeconds;
                if (silenceSec >= EndGapSec)
                    await EndSessionCoreAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── Packet handler ────────────────────────────────────────────────────────
    private void OnPacket(TelemetryPacket p)
    {
        _lastPacketAt = DateTime.UtcNow;

        // RacePosition is 0 in free roam and >0 in structured races.
        // CurrentRaceTime and LapNumber are not reliable (CurrentRaceTime runs in free roam too).
        bool inRace = p.RacePosition > 0;

        // ── Auto race detection (never triggers during free roam) ────────────
        if (!IsManual)
        {
            if (!IsRecording && inRace && !_prevInRace)
            {
                // Just entered a race
                _ = StartSessionCoreAsync(manual: false);
            }
            else if (IsRecording && !inRace && _prevInRace)
            {
                // Just left a race (crossed finish / back to free roam)
                _ = EndSessionCoreAsync();
            }
            else if (IsRecording && inRace && _prevLapNumber > 1 && p.LapNumber <= 1)
            {
                // Player restarted the race mid-session
                _ = EndSessionCoreAsync().ContinueWith(_ => StartSessionCoreAsync(manual: false));
            }
        }

        _prevInRace    = inRace;
        _prevLapNumber = p.LapNumber;

        if (!IsRecording) return;
        AccumulatePacket(p);
    }

    // ── Session lifecycle ─────────────────────────────────────────────────────
    private async Task StartSessionCoreAsync(bool manual)
    {
        if (IsRecording) return;

        IsManual      = manual;
        ActiveSession = new RaceSession { StartedAt = DateTime.UtcNow };
        await _sessionRepo.InsertSessionAsync(ActiveSession);

        _currentLap       = new LapAccumulator();
        _currentLapNumber = -1;
        _prevLapNumber    = -1;
        _packetBuffer.Clear();

        IsRecording = true;
    }

    /// <summary>Start free-roam recording manually from the UI.</summary>
    public Task StartManualAsync() => StartSessionCoreAsync(manual: true);

    private async Task EndSessionCoreAsync()
    {
        if (!IsRecording || ActiveSession is null) return;
        IsRecording = false;

        await FlushPacketsAsync();

        if (_currentLap.SampleCount > 0)
            ActiveSession.Laps.Add(_currentLap.Build(_currentLapNumber));

        ActiveSession.EndedAt = DateTime.UtcNow;
        ActiveSession.Advice  = TuningAdvisor.Analyse(ActiveSession);
        await _sessionRepo.UpdateSessionAsync(ActiveSession);

        SessionEnded?.Invoke();
    }

    /// <summary>Manual override — end the session early from the UI.</summary>
    public Task ForceEndAsync() => EndSessionCoreAsync();

    // ── Data accumulation ─────────────────────────────────────────────────────
    private void AccumulatePacket(TelemetryPacket p)
    {
        if (ActiveSession is null) return;

        var lapNum = p.LapNumber;
        if (_currentLapNumber == -1) _currentLapNumber = lapNum;

        if (lapNum != _currentLapNumber)
        {
            ActiveSession.Laps.Add(_currentLap.Build(_currentLapNumber));
            _currentLap       = new LapAccumulator();
            _currentLapNumber = lapNum;
        }

        _currentLap.Feed(p);

        _packetBuffer.Add(new RawPacketDocument
        {
            SessionId     = ActiveSession.Id!,
            TimestampMs   = p.TimestampMS,
            LapNumber     = lapNum,
            Speed         = p.Speed,
            CurrentRpm    = p.CurrentEngineRpm,
            AccelerationX = p.AccelerationX,
            AccelerationY = p.AccelerationY,
            AccelerationZ = p.AccelerationZ,
            TireTempFL    = p.TireTempFL,
            TireTempFR    = p.TireTempFR,
            TireTempRL    = p.TireTempRL,
            TireTempRR    = p.TireTempRR,
            SuspFL        = p.NormalizedSuspensionTravelFL,
            SuspFR        = p.NormalizedSuspensionTravelFR,
            SuspRL        = p.NormalizedSuspensionTravelRL,
            SuspRR        = p.NormalizedSuspensionTravelRR,
            Accel         = p.Accel,
            Brake         = p.Brake,
            Gear          = p.Gear,
            Fuel          = p.Fuel,
            CurrentLap    = p.CurrentLap,
            BestLap       = p.BestLap,
            EngineMaxRpm  = p.EngineMaxRpm,
        });

        if (_packetBuffer.Count >= FlushEvery)
            _ = FlushPacketsAsync();
    }

    private async Task FlushPacketsAsync()
    {
        if (_packetBuffer.Count == 0 || ActiveSession is null) return;
        var batch = _packetBuffer.ToList();
        _packetBuffer.Clear();
        await _sessionRepo.BulkInsertPacketsAsync(ActiveSession.Id!, batch);
    }

    public void Dispose()
    {
        _broadcaster.PacketBroadcast -= OnPacket;
        _watchdog.Dispose();
    }

    // ── Per-lap accumulator ───────────────────────────────────────────────────
    private sealed class LapAccumulator
    {
        public int SampleCount { get; private set; }

        private Stat _fl = new(), _fr = new(), _rl = new(), _rr = new();
        private int  _sFL, _sFR, _sRL, _sRR;
        private float _peakLatG, _peakBrakeG, _peakAccelG;
        private int   _underSamples, _overSamples, _cornerSamples;
        private float _brakeEffSum;
        private int   _brakeZoneSamples;
        private int   _limiterHits;
        private float _upshiftRatioSum;
        private int   _upshiftCount;
        private byte  _prevGear;
        private float _prevRpmRatio;
        private float _lapTimeSeconds;

        public void Feed(TelemetryPacket p)
        {
            SampleCount++;

            var tc_fl = TelemetryUnits.TireTempCelsius(p.TireTempFL);
            var tc_fr = TelemetryUnits.TireTempCelsius(p.TireTempFR);
            var tc_rl = TelemetryUnits.TireTempCelsius(p.TireTempRL);
            var tc_rr = TelemetryUnits.TireTempCelsius(p.TireTempRR);
            _fl.Add(tc_fl); _fr.Add(tc_fr); _rl.Add(tc_rl); _rr.Add(tc_rr);

            if (p.NormalizedSuspensionTravelFL >= 0.95f) _sFL++;
            if (p.NormalizedSuspensionTravelFR >= 0.95f) _sFR++;
            if (p.NormalizedSuspensionTravelRL >= 0.95f) _sRL++;
            if (p.NormalizedSuspensionTravelRR >= 0.95f) _sRR++;

            var latG  = MathF.Abs(TelemetryUnits.AccelToG(p.AccelerationX));
            var longG = TelemetryUnits.AccelToG(p.AccelerationZ);
            if (latG   > _peakLatG)   _peakLatG   = latG;
            if (-longG > _peakBrakeG) _peakBrakeG = -longG;
            if (longG  > _peakAccelG) _peakAccelG = longG;

            if (latG > 0.3f)
            {
                _cornerSamples++;
                var steerNorm = MathF.Abs(p.Steer) / 127f;
                if (steerNorm > 0.3f && latG < steerNorm * 2.5f) _underSamples++;
                else if (steerNorm < 0.2f && latG > 0.5f)        _overSamples++;
            }

            if (p.Brake > 30)
            {
                var brakeInput = p.Brake / 255f;
                var decelG     = MathF.Max(0f, -TelemetryUnits.AccelToG(p.AccelerationZ));
                if (brakeInput > 0f) { _brakeEffSum += decelG / brakeInput; _brakeZoneSamples++; }
            }

            if (p.EngineMaxRpm > 0f && p.CurrentEngineRpm / p.EngineMaxRpm > 0.99f)
                _limiterHits++;

            var rpmRatio = p.EngineMaxRpm > 0f ? p.CurrentEngineRpm / p.EngineMaxRpm : 0f;
            if (_prevGear > 0 && p.Gear > _prevGear) { _upshiftRatioSum += _prevRpmRatio; _upshiftCount++; }
            _prevGear     = p.Gear;
            _prevRpmRatio = rpmRatio;

            _lapTimeSeconds = p.CurrentLap;
        }

        public LapSnapshot Build(int lapNumber) => new()
        {
            LapNumber          = lapNumber,
            TireTempFLMin      = _fl.Min, TireTempFLAvg = _fl.Avg, TireTempFLMax = _fl.Max,
            TireTempFRMin      = _fr.Min, TireTempFRAvg = _fr.Avg, TireTempFRMax = _fr.Max,
            TireTempRLMin      = _rl.Min, TireTempRLAvg = _rl.Avg, TireTempRLMax = _rl.Max,
            TireTempRRMin      = _rr.Min, TireTempRRAvg = _rr.Avg, TireTempRRMax = _rr.Max,
            SuspBottomFL       = _sFL,    SuspBottomFR  = _sFR,
            SuspBottomRL       = _sRL,    SuspBottomRR  = _sRR,
            PeakLateralG       = _peakLatG,
            PeakBrakingG       = _peakBrakeG,
            PeakAccelG         = _peakAccelG,
            UnderSteerRatio    = _cornerSamples > 0 ? (float)_underSamples / _cornerSamples : 0f,
            OverSteerRatio     = _cornerSamples > 0 ? (float)_overSamples  / _cornerSamples : 0f,
            BrakeEfficiencyAvg = _brakeZoneSamples > 0 ? _brakeEffSum / _brakeZoneSamples : 0f,
            LimiterHitCount    = _limiterHits,
            AvgUpshiftRpmRatio = _upshiftCount > 0 ? _upshiftRatioSum / _upshiftCount : 0f,
            LapTimeSeconds     = _lapTimeSeconds,
            SampleCount        = SampleCount,
        };

        private struct Stat
        {
            private float _sum; private int _n;
            public  float Min { get; private set; }
            public  float Max { get; private set; }
            public  float Avg => _n > 0 ? _sum / _n : 0f;
            public void Add(float v)
            {
                if (_n == 0) { Min = Max = v; }
                else { if (v < Min) Min = v; if (v > Max) Max = v; }
                _sum += v; _n++;
            }
        }
    }
}

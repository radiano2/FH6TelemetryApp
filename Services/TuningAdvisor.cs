using FH6TelemetryApp.Models;

namespace FH6TelemetryApp.Services;

/// <summary>
/// Pure static analysis — takes a completed RaceSession and returns tuning advice.
/// Rules sourced from ForzaTune, ForzaFire, SimRacingSetup, and Forza community forums.
/// </summary>
public static class TuningAdvisor
{
    public static List<TuningAdvice> Analyse(RaceSession session)
    {
        var laps   = session.Laps;
        var advice = new List<TuningAdvice>();

        if (laps.Count == 0)
            return advice;

        AnalyseTireTemps(laps, advice);
        AnalyseSuspension(laps, advice);
        AnalyseHandlingBalance(laps, advice);
        AnalyseBraking(laps, advice);
        AnalyseGearing(laps, advice);
        AnalyseTraction(laps, advice);
        AnalyseThrottleDiscipline(laps, advice);

        advice.Sort((a, b) => a.Severity.CompareTo(b.Severity));
        advice.Reverse();
        return advice;
    }

    // ── Tire Temperatures ────────────────────────────────────────────────────
    private static void AnalyseTireTemps(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var corners = new (string Name, Func<LapSnapshot, float> Avg)[]
        {
            ("Front-Left",  l => l.TireTempFLAvg),
            ("Front-Right", l => l.TireTempFRAvg),
            ("Rear-Left",   l => l.TireTempRLAvg),
            ("Rear-Right",  l => l.TireTempRRAvg),
        };

        foreach (var (name, avg) in corners)
        {
            var mean = laps.Average(avg);

            if (mean > 112f)
            {
                out_.Add(new TuningAdvice
                {
                    Severity   = AdviceSeverity.Critical,
                    Category   = AdviceCategory.TireTemps,
                    Message    = $"{name} tires critically overheating (avg {mean:F0}°C).",
                    SliderHint = "Increase tire pressure 2–3 PSI. Consider a harder compound or reduce negative camber on that corner."
                });
            }
            else if (mean > 95f)
            {
                out_.Add(new TuningAdvice
                {
                    Severity   = AdviceSeverity.Warning,
                    Category   = AdviceCategory.TireTemps,
                    Message    = $"{name} tires running hot (avg {mean:F0}°C). Target: 78–95°C.",
                    SliderHint = "Increase tire pressure 1–2 PSI. Street/rally target: 26–28 PSI; semi-slicks: 30–33 PSI."
                });
            }
            else if (mean < 65f)
            {
                out_.Add(new TuningAdvice
                {
                    Severity   = AdviceSeverity.Warning,
                    Category   = AdviceCategory.TireTemps,
                    Message    = $"{name} tires running cold (avg {mean:F0}°C) — not reaching optimal grip.",
                    SliderHint = "Lower tire pressure 1–2 PSI, or soften spring on that corner to increase load."
                });
            }
        }

        // Front-rear axle split (spring/ARB/differential imbalance signal)
        var frontAvg = laps.Average(l => (l.TireTempFLAvg + l.TireTempFRAvg) / 2f);
        var rearAvg  = laps.Average(l => (l.TireTempRLAvg + l.TireTempRRAvg) / 2f);
        var axleDiff = frontAvg - rearAvg;

        if (axleDiff > 20f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.TireTemps,
                Message    = $"Front tires significantly hotter than rears (+{axleDiff:F0}°C). Front springs/ARB may be too stiff.",
                SliderHint = "Tuning › Springs › Front — soften slightly. Tuning › Anti-Roll Bars › Front — soften. If RWD, check Diff › Acceleration isn't causing cold rears."
            });
        }
        else if (axleDiff < -20f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.TireTemps,
                Message    = $"Rear tires significantly hotter than fronts (+{-axleDiff:F0}°C). Common in high-power RWD with soft rear springs.",
                SliderHint = "Tuning › Springs › Rear — stiffen. Tuning › Diff › Acceleration — reduce if excessive wheelspin is overheating driven wheels."
            });
        }
        else if (axleDiff > 15f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.TireTemps,
                Message    = $"Front tires running warmer than rears (+{axleDiff:F0}°C). Front end is carrying more load.",
                SliderHint = "Increase front tire pressure 1–2 PSI, or add 0.1–0.2° positive camber to fronts. Check front anti-roll bar stiffness."
            });
        }
        else if (axleDiff < -15f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.TireTemps,
                Message    = $"Rear tires running warmer than fronts (+{-axleDiff:F0}°C). Rear overloaded.",
                SliderHint = "Check rear camber and differential settings. Soften rear spring or increase rear tire pressure 1–2 PSI."
            });
        }

        // Left-right imbalance across both axles — indicates camber/alignment mismatch
        var leftAvg  = laps.Average(l => (l.TireTempFLAvg + l.TireTempRLAvg) / 2f);
        var rightAvg = laps.Average(l => (l.TireTempFRAvg + l.TireTempRRAvg) / 2f);
        var sideDiff = MathF.Abs(leftAvg - rightAvg);
        if (sideDiff > 10f)
        {
            var hotter = leftAvg > rightAvg ? "left" : "right";
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.TireTemps,
                Message    = $"{char.ToUpper(hotter[0])}{hotter[1..]} tires running {sideDiff:F0}°C hotter — possible camber imbalance side-to-side.",
                SliderHint = "Tuning › Alignment › Camber — add 0.1–0.2° more negative camber on the hotter side. Also check toe; excessive toe-in on one side can cause asymmetric wear."
            });
        }
    }

    // ── Suspension ───────────────────────────────────────────────────────────
    private static void AnalyseSuspension(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var corners = new (string Name, Func<LapSnapshot, int> Count)[]
        {
            ("Front-Left",  l => l.SuspBottomFL),
            ("Front-Right", l => l.SuspBottomFR),
            ("Rear-Left",   l => l.SuspBottomRL),
            ("Rear-Right",  l => l.SuspBottomRR),
        };

        foreach (var (name, count) in corners)
        {
            var meanPerLap = laps.Average(l => (float)count(l));

            if (meanPerLap > 5f)
            {
                out_.Add(new TuningAdvice
                {
                    Severity   = AdviceSeverity.Critical,
                    Category   = AdviceCategory.Suspension,
                    Message    = $"Suspension bottoming out at {name} (avg {meanPerLap:F1}× per lap).",
                    SliderHint = "Increase ride height first. If already at maximum, raise spring rate by 10–15% on that corner."
                });
            }
            else if (meanPerLap > 1f)
            {
                out_.Add(new TuningAdvice
                {
                    Severity   = AdviceSeverity.Warning,
                    Category   = AdviceCategory.Suspension,
                    Message    = $"Occasional {name} suspension bottoming (avg {meanPerLap:F1}× per lap).",
                    SliderHint = "Consider raising ride height or increasing spring stiffness at that corner."
                });
            }
        }
    }

    // ── Understeer / Oversteer ───────────────────────────────────────────────
    private static void AnalyseHandlingBalance(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var underRatio = laps.Average(l => l.UnderSteerRatio);
        var overRatio  = laps.Average(l => l.OverSteerRatio);

        if (underRatio > 0.70f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Critical,
                Category   = AdviceCategory.Understeer,
                Message    = $"Severe understeer detected ({underRatio:P0} of cornering samples).",
                SliderHint = "Soften front anti-roll bar, reduce front acceleration differential by 10–15 points, or increase negative front camber by 0.3–0.5°."
            });
        }
        else if (underRatio > 0.55f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Understeer,
                Message    = $"Persistent understeer ({underRatio:P0} of cornering samples).",
                SliderHint = "Reduce front acceleration differential by 5–10 points, or soften front springs relative to rear."
            });
        }

        if (overRatio > 0.55f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Oversteer,
                Message    = $"Persistent oversteer ({overRatio:P0} of cornering samples).",
                SliderHint = "Increase rear deceleration differential by 5 points, or stiffen rear springs relative to front. Check rear camber — too much negative camber reduces straight-line traction."
            });
        }
    }

    // ── Braking ──────────────────────────────────────────────────────────────
    private static void AnalyseBraking(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var brakeEff = laps.Where(l => l.BrakeEfficiencyAvg > 0f)
                           .Select(l => l.BrakeEfficiencyAvg)
                           .DefaultIfEmpty(1f)
                           .Average();

        if (brakeEff < 0.5f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Braking,
                Message    = $"Low brake efficiency (avg {brakeEff:P0}) — high pedal input yields limited deceleration.",
                SliderHint = "Tuning › Brakes › Pressure — increase. If already at max, check Brake Balance: too far rearward causes rear-lockup and reduces total stopping force. Target 52–57% front balance for most cars."
            });
        }

        var peakBrakeG = laps.Max(l => l.PeakBrakingG);
        if (peakBrakeG < 0.8f && laps.Average(l => l.BrakeEfficiencyAvg) < 0.5f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.Braking,
                Message    = $"Peak braking G is low ({peakBrakeG:F1}G). You may be under-braking or braking too early.",
                SliderHint = "Try braking later and more firmly. Tuning › Brakes › Pressure — increase if pedal feels weak. ABS-on: higher pressure is generally better since ABS prevents lock-up."
            });
        }
    }

    // ── Gearing / RPM ────────────────────────────────────────────────────────
    private static void AnalyseGearing(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var limiterHits = laps.Average(l => (float)l.LimiterHitCount);
        if (limiterHits > 10f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Gearing,
                Message    = $"Engine hitting rev limiter frequently (avg {limiterHits:F0}× per lap). Gears are too short.",
                SliderHint = "Tuning › Gearing › Final Drive — move toward top speed (right). Or lengthen the top gear(s). Target: just barely reaching max RPM at the end of the longest straight."
            });
        }

        var upshiftRatio = laps.Where(l => l.AvgUpshiftRpmRatio > 0f)
                               .Select(l => l.AvgUpshiftRpmRatio)
                               .DefaultIfEmpty(0.8f)
                               .Average();

        if (upshiftRatio < 0.65f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.Gearing,
                Message    = $"Upshifting early (avg {upshiftRatio:P0} of max RPM at gear change).",
                SliderHint = "Try upshifting at 80–85% of max RPM for best acceleration. Turbo engines may benefit from holding slightly longer; most NA engines do not."
            });
        }
        else if (upshiftRatio > 0.92f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.Gearing,
                Message    = $"Upshifting very late (avg {upshiftRatio:P0} of max RPM). Spending time near the ceiling with little power gain.",
                SliderHint = "Shift at 85–90% RPM for most engines. If rev limiter hits are also frequent, both point to a final drive that is too short — lengthen it in Tuning › Gearing."
            });
        }

        // Gear lugging — engine below power band with throttle applied
        // At ~60 fps, 90 samples ≈ 1.5 s of lugging per lap average
        var avgLugging = laps.Average(l => (float)l.LuggingCount);
        if (avgLugging > 90f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Gearing,
                Message    = $"Engine frequently lugging below 35% RPM with throttle applied (avg {avgLugging / 60f:F1}s per lap).",
                SliderHint = "Tuning › Gearing › Final Drive — shorten (move left) so gears keep the engine in its power band. Or downshift earlier on slow corners."
            });
        }

        // Over-rev while still accelerating — final drive is too short for the track
        var avgOverRev = laps.Average(l => (float)l.OverRevSustainedCount);
        if (avgOverRev > 60f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.Gearing,
                Message    = $"Car is near the rev ceiling while still accelerating (avg {avgOverRev / 60f:F1}s per lap above 90% RPM). Top speed is being left on the table.",
                SliderHint = "Tuning › Gearing › Final Drive — lengthen (move right) to let the engine pull further before hitting the ceiling. Or add a gear if the transmission allows."
            });
        }
    }

    // ── Traction (wheelspin on corner exit) ──────────────────────────────────
    private static void AnalyseTraction(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var spinFraction = laps.Average(l => l.WheelspinExitFraction);

        if (spinFraction > 0.40f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Critical,
                Category   = AdviceCategory.Traction,
                Message    = $"Severe wheelspin on corner exit ({spinFraction:P0} of exits losing drive).",
                SliderHint = "Tuning › Differential › Acceleration — increase toward 40–65% for RWD road racing. Also check rear tire pressure (+1–2 PSI) and avoid full-throttle before the apex."
            });
        }
        else if (spinFraction > 0.25f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Traction,
                Message    = $"Wheelspin detected on corner exit ({spinFraction:P0} of exits). Drive is being lost under acceleration.",
                SliderHint = "Tuning › Differential › Acceleration — increase by 5–10 points. If AWD, increase front torque split slightly. Roll on the throttle progressively — avoid stabbing it mid-corner."
            });
        }
    }

    // ── Throttle discipline (coasting mid-corner) ────────────────────────────
    private static void AnalyseThrottleDiscipline(List<LapSnapshot> laps, List<TuningAdvice> out_)
    {
        var coastFraction = laps.Average(l => l.CoastingCornerFraction);

        if (coastFraction > 0.20f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Throttle,
                Message    = $"Excessive mid-corner coasting detected ({coastFraction:P0} of cornering samples with no pedal input).",
                SliderHint = "Coasting upsets weight balance and wastes time. Trail-brake into corners and get back to throttle before the apex. If coasting stems from oversteer fear, increase Tuning › Diff › Deceleration for a more stable lift-off."
            });
        }
        else if (coastFraction > 0.12f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Info,
                Category   = AdviceCategory.Throttle,
                Message    = $"Some mid-corner coasting ({coastFraction:P0} of cornering time). Aim to overlap braking and throttle transitions.",
                SliderHint = "Practice trail braking: gradually release the brake as you reach the apex rather than releasing it completely before turn-in. This keeps the car balanced throughout the corner."
            });
        }
    }
}

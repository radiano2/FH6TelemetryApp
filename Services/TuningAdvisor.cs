using FH6TelemetryApp.Models;

namespace FH6TelemetryApp.Services;

/// <summary>
/// Pure static analysis — takes a completed RaceSession and returns tuning advice.
/// Rules sourced from FH6 community tuning guides, Forza support docs, and fh6-tel reference.
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

        // Sort: Critical first, then Warning, then Info
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

        // Front vs rear balance
        var frontAvg = laps.Average(l => (l.TireTempFLAvg + l.TireTempFRAvg) / 2f);
        var rearAvg  = laps.Average(l => (l.TireTempRLAvg + l.TireTempRRAvg) / 2f);

        if (frontAvg - rearAvg > 15f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.TireTemps,
                Message    = $"Front tires significantly hotter than rears (+{frontAvg - rearAvg:F0}°C). Indicates front overload.",
                SliderHint = "Increase front tire pressure 1–2 PSI, or add 0.1–0.2° positive camber to fronts. Check front anti-roll bar stiffness."
            });
        }
        else if (rearAvg - frontAvg > 15f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.TireTemps,
                Message    = $"Rear tires significantly hotter than fronts (+{rearAvg - frontAvg:F0}°C). Rear overloaded.",
                SliderHint = "Check rear camber and differential settings. Soften rear spring or increase rear tire pressure 1–2 PSI."
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
                SliderHint = "Increase rear deceleration differential by 5 points, or stiffen rear springs relative to front."
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

        if (brakeEff < 0.6f)
        {
            out_.Add(new TuningAdvice
            {
                Severity   = AdviceSeverity.Warning,
                Category   = AdviceCategory.Braking,
                Message    = $"Low brake efficiency (avg {brakeEff:P0}) — high pedal input for limited deceleration.",
                SliderHint = "Increase brake pressure in tuning. If already at max, check brake bias — too far forward can cause front lockup and reduce effective stopping force."
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
                Message    = $"Engine hitting rev limiter frequently (avg {limiterHits:F0}× per lap).",
                SliderHint = "Gears are too short — move final drive ratio toward top speed, or lengthen the top gear(s)."
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
                SliderHint = "Try upshifting at 80–85% of max RPM for better acceleration. Consider enabling automatic clutch if using manual gears."
            });
        }
    }
}

namespace FH6TelemetryApp.Models;

public enum AdviceSeverity { Info, Warning, Critical }

public enum AdviceCategory
{
    TireTemps,
    Suspension,
    Understeer,
    Oversteer,
    Braking,
    Gearing,
    General
}

public sealed class TuningAdvice
{
    public AdviceSeverity Severity   { get; init; }
    public AdviceCategory Category   { get; init; }
    public string         Message    { get; init; } = "";
    public string         SliderHint { get; init; } = "";
}

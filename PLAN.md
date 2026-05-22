# FH6 Telemetry App — Implementation Plan

## Source of Truth

`telemetry-dashboard.html` is the authoritative UI design. It is a fully-working prototype with simulated data, the complete CSS design system, all 11 widget layouts, SVG gauges, the grid/resize system, and all animations. The Blazor app must faithfully reproduce this visual design — no redesign, no component-library override of the look.

---

## How FH6 Telemetry Works

Forza Horizon 6 broadcasts a **324-byte little-endian UDP packet** at ~60 fps when "Data Out" is enabled in-game (`Settings → HUD and Gameplay → Data Out`). The format is byte-for-byte identical to FH5 with three FH6-exclusive fields inserted after `NumCylinders`:

- `CarGroup` (s32)
- `SmashableVelDiff` (f32)
- `SmashableMass` (f32)

Default port: **20440** (configurable via `appsettings.json`).

---

## Technology Stack

| Layer | Technology | Reason |
|---|---|---|
| **Framework** | **Blazor Server (.NET 9)** | UDP listener and SignalR hub run in the same process as the UI — zero cross-origin friction, direct in-memory communication |
| **Real-time push** | **ASP.NET Core SignalR** | Built into Blazor Server; pushes `TelemetryPacket` to all connected clients throttled to 30 fps |
| **UDP listener** | `System.Net.Sockets.UdpClient` as `BackgroundService` | Runs for app lifetime, receives at game rate (~60 fps) |
| **Packet parsing** | `System.Buffers.Binary.BinaryPrimitives` | Zero-allocation little-endian reads from a `ReadOnlySpan<byte>` |
| **Styling** | **Vanilla CSS from the prototype** | The HTML prototype already has a complete, polished design system — copy it verbatim; do NOT introduce MudBlazor/Radzen (they would override styles) |
| **SVG widgets** | Inline SVG (from prototype) | Speed/RPM gauges, G-force plot, Track map, Car damage diagram are already drawn as SVG — port them 1-to-1 |
| **Layout persistence** | `Blazored.LocalStorage` | Serialize each widget's col/row span to localStorage, rehydrate on load |
| **JS interop** | Minimal `IJSRuntime` calls | Only needed for: resize-handle drag logic (mousedown/mousemove/mouseup), `requestAnimationFrame` loop, localStorage |

> **Why no MudBlazor?** The prototype already defines every visual component (cards, buttons, progress bars, badges). Adding a component library would fight the existing CSS and balloon the bundle. All needed UI is already in the design file.

---

## Design System (from prototype)

```css
--bg: #edf2f7          /* page background */
--surface: #ffffff      /* widget background */
--surface2: #f4f8fb    /* inner card / track backgrounds */
--mint: #00c9a7        /* primary accent */
--mint-light: #3ddfc7
--mint-dark: #009e84
--text: #1a2332
--text-secondary: #6b7f96
--border: #dde6ef
--red: #ff4757  --orange: #ffa502  --yellow: #ffdd59
--blue: #4a90d9  --green: #2ed573
--radius: 16px  --radius-sm: 10px
Grid: 12 columns, 64px row height, 14px gap
```

Every widget has a 3px mint gradient top bar (`::before`), a title with a mint dot, S/M/L size preset buttons, and a corner resize handle that appears on hover (always visible in edit mode).

---

## 11 Widgets (from prototype → Blazor components)

| # | Widget | HTML id | Default size | Blazor component |
|---|---|---|---|---|
| 1 | Speed gauge | `w-speed` | col-3 row-4 | `SpeedWidget.razor` |
| 2 | RPM gauge | `w-rpm` | col-3 row-4 | `RpmWidget.razor` |
| 3 | Gear + shift lights | `w-gear` | col-2 row-4 | `GearWidget.razor` |
| 4 | Driver inputs (THR/BRK/CLT/H-B) | `w-inputs` | col-2 row-4 | `InputsWidget.razor` |
| 5 | G-Force circle plot | `w-gforce` | col-2 row-4 | `GForceWidget.razor` |
| 6 | Tire temps & wear (4-corner heatmap) | `w-tires` | col-4 row-4 | `TireWidget.razor` |
| 7 | Lap times (current/best/last/Δ/sectors) | `w-laps` | col-3 row-4 | `LapTimingWidget.razor` |
| 8 | Track map (SVG path + car dot) | `w-track` | col-3 row-4 | `TrackMapWidget.razor` |
| 9 | Session stats (position/lap/fuel) | `w-session` | col-6 row-2 | `SessionWidget.razor` |
| 10 | Suspension travel (4 bidirectional bars) | `w-susp` | col-4 row-4 | `SuspensionWidget.razor` |
| 11 | Car damage (SVG top-down car view) | `w-damage` | col-4 row-4 | `DamageWidget.razor` |

### Tire temperature color scale (from prototype)
```
< 65°  → #4a90d9 (blue — too cold)
65–78° → #00b8a0 (teal — warming)
78–95° → #00c9a7 (mint — optimal)
95–112°→ #ffa502 (orange — hot)
> 112° → #ff4757 (red — overheating)
```

### Shift light sequence (from prototype, 8 lights)
- Lights 1–3: green (rpm/maxRpm ≥ threshold)
- Lights 4–6: yellow
- Lights 7–8: red; flash animation at > 94% of max RPM

---

## Project Structure

```
FH6TelemetryApp/
├── FH6TelemetryApp.csproj
├── appsettings.json                  ← UdpPort, ListenAddress
├── Program.cs                        ← DI, SignalR, BackgroundService
│
├── Models/
│   └── TelemetryPacket.cs            ← All 80+ fields as a C# record
│
├── Services/
│   ├── UdpListenerService.cs         ← BackgroundService; receives at 60 fps
│   ├── TelemetryParser.cs            ← Parse 324-byte span into TelemetryPacket
│   └── TelemetryBroadcaster.cs       ← Throttle to 30 fps; call hub method
│
├── Hubs/
│   └── TelemetryHub.cs               ← SignalR hub; clients subscribe here
│
├── Components/
│   ├── Layout/
│   │   ├── WidgetShell.razor         ← Shared wrapper: top bar, title, size btns, resize handle
│   │   └── Dashboard.razor           ← CSS grid host; mounts all widgets
│   │
│   └── Widgets/
│       ├── SpeedWidget.razor
│       ├── RpmWidget.razor
│       ├── GearWidget.razor
│       ├── InputsWidget.razor
│       ├── GForceWidget.razor
│       ├── TireWidget.razor
│       ├── LapTimingWidget.razor
│       ├── TrackMapWidget.razor
│       ├── SessionWidget.razor
│       ├── SuspensionWidget.razor
│       └── DamageWidget.razor
│
├── Pages/
│   └── Index.razor                   ← Single page; mounts Dashboard
│
└── wwwroot/
    ├── css/
    │   └── dashboard.css             ← Verbatim copy of <style> from prototype
    └── js/
        └── dashboard.js              ← Resize handle drag + localStorage interop
```

---

## Telemetry Packet Fields → Widget Mapping

**SpeedWidget** — `Speed` (f32, m/s → km/h ×3.6 or mph ×2.237)

**RpmWidget** — `CurrentEngineRpm`, `EngineMaxRpm`, `EngineIdleRpm` (f32)

**GearWidget** — `Gear` (u8: 0=R, 1–10); shift lights derive from `CurrentEngineRpm / EngineMaxRpm`

**InputsWidget** — `Accel`, `Brake`, `Clutch`, `HandBrake` (u8, 0–255 → 0–100%)

**GForceWidget** — `AccelerationX` (lateral), `AccelerationZ` (longitudinal) (f32, m/s² ÷ 9.80665 = G)

**TireWidget** — `TireTempFL/FR/RL/RR` (f32, °F); `WheelRotationSpeedFL/FR/RL/RR` for wear proxy

**LapTimingWidget** — `CurrentLap`, `BestLap`, `LastLap` (f32, seconds); `LapNumber` (u16); delta = CurrentLap − BestLap

**TrackMapWidget** — `PositionX`, `PositionZ` (f32, world coords) mapped to SVG viewport; car dot moves in real time

**SessionWidget** — `LapNumber`, `Fuel` (f32, 0–1); race position (if available from packet)

**SuspensionWidget** — `NormalizedSuspensionTravelFL/FR/RL/RR` (f32, 0=fully extended, 1=fully compressed); center = 0.5, bars grow left (extension) or right (compression), colored orange/mint respectively

**DamageWidget** — `SmashableVelDiff`, `SmashableMass` (f32, FH6-exclusive); map impact velocity difference to damage zone coloring on the SVG car diagram

---

## Data Flow

```
FH6 Game  ──UDP :20440──►  UdpListenerService (BackgroundService)
                                    │  byte[324]
                                    ▼
                            TelemetryParser.Parse(span)
                                    │  TelemetryPacket record
                                    ▼
                            TelemetryBroadcaster
                            (gate to 30 fps via Stopwatch)
                                    │  SignalR
                                    ▼
                            TelemetryHub.BroadcastAsync()
                                    │  WebSocket
                                    ▼
                            Dashboard.razor  (HubConnection client)
                            ├── receives TelemetryPacket
                            ├── updates shared state
                            └── calls StateHasChanged()
                                    │
                                    ▼  re-render
                            All 11 widget components
```

---

## Grid & Resize System (porting prototype JS to Blazor)

The prototype already has a complete, working grid system. In Blazor:

1. **CSS grid** stays identical — 12 columns, 64px rows, 14px gap. Copied verbatim from the prototype's `<style>` block into `dashboard.css`.

2. **Widget state** — each widget has a `WidgetConfig` record: `{ string Id, int Cols, int Rows }`. Dashboard holds a `List<WidgetConfig>` and passes the config as a parameter to each `WidgetShell`.

3. **Size buttons (S/M/L)** — each button calls `SetSize(widgetId, cols, rows)` in Dashboard which updates the config and persists to localStorage via `Blazored.LocalStorage`. No JS needed for this.

4. **Drag resize handle** — this requires JS because it listens to `mousemove`/`mouseup` on `document`. Port the prototype's resize handle JS into `wwwroot/js/dashboard.js`; call it via `IJSRuntime.InvokeVoidAsync("initResizeHandles", DotNetObjectReference)` with a .NET callback `UpdateWidgetSize(id, cols, rows)`.

5. **Edit mode** — `body.edit-mode` class toggled by "Edit Layout" button; shows dashed outline and keeps resize handles visible. Toggle the class via `IJSRuntime`.

6. **Layout persistence** — on every size change, serialize `List<WidgetConfig>` to JSON and write to localStorage key `fh6-layout`. On `OnAfterRenderAsync(firstRender)` read it back.

---

## Packet Parser — Key Implementation Notes

```csharp
// FH6 324-byte packet layout (little-endian, sequential)
// Offset 0:  s32 IsRaceOn
// Offset 4:  u32 TimestampMS
// Offset 8:  f32 EngineMaxRpm
// Offset 12: f32 EngineIdleRpm
// Offset 16: f32 CurrentEngineRpm
// ... (sled fields) ...
// After NumCylinders — FH6-exclusive:
//   s32 CarGroup
//   f32 SmashableVelDiff
//   f32 SmashableMass
// Then: f32 PositionX/Y/Z, Speed, Power, Torque, ...
// Dash fields follow sled fields with no header offset needed
// (Horizon 12-byte header is NOT present in FH6 — packet starts at IsRaceOn)
```

Use `BinaryPrimitives.ReadSingleLittleEndian(span.Slice(offset))` for f32,  
`BinaryPrimitives.ReadInt32LittleEndian` for s32, etc.  
Parse into a `TelemetryPacket` record with all fields named to match the official documentation.

---

## Unit Conversions (in a `TelemetryUnits` static helper)

| Raw | Display |
|---|---|
| Speed m/s | km/h: `× 3.6` / mph: `× 2.237` |
| AccelerationX/Z m/s² | G: `÷ 9.80665` |
| Power watts | kW: `÷ 1000` / hp: `÷ 745.7` |
| Torque N·m | lb-ft: `× 0.7376` |
| TireTemp °F | °C: `(F−32)×5/9` |
| Angles rad | degrees: `× 180/π` |

User preference (mph vs km/h, °C vs °F) stored in localStorage.

---

## Build Phases

| Phase | Deliverable |
|---|---|
| **1** | Blazor Server project scaffold; copy `dashboard.css` from prototype; `Index.razor` renders static dashboard shell |
| **2** | `TelemetryPacket.cs` model + `TelemetryParser.cs` with unit tests against a captured 324-byte packet |
| **3** | `UdpListenerService` + `TelemetryBroadcaster` + `TelemetryHub` wired up; verify data flows to browser console |
| **4** | `WidgetShell.razor` + `WidgetConfig` state + S/M/L buttons + localStorage persistence |
| **5** | Port resize handle JS to `dashboard.js`; wire `IJSRuntime` callback to update `WidgetConfig` |
| **6** | Port all 11 widgets as Blazor components, replacing `S.xxx` simulation values with `TelemetryPacket` fields |
| **7** | G-force trail, track map car dot, shift light flash animation — port simulation-derived rendering to real data |
| **8** | Connection status badge (live/waiting), unit toggle (mph↔km/h, °F↔°C), edit mode polish |
| **9** | **Race Diagnostics Mode** — session recorder, per-lap snapshot accumulator, post-race tuning advisor with evidence-based recommendations |

---

## Phase 9 — Race Diagnostics Mode

### Overview

Race Diagnostics Mode captures a full session's worth of telemetry, accumulates per-lap statistical snapshots, and produces a post-race tuning report with concrete, FH6-specific slider advice.

A single "Record" toggle in the dashboard header starts/stops a session. When stopped, the Diagnostics panel (a 12th widget, `DiagnosticsWidget.razor`) switches from live capture indicators to the full report.

---

### New Files

```
Models/
  RaceSession.cs          ← Immutable record: metadata + List<LapSnapshot>
  LapSnapshot.cs          ← Per-lap aggregated stats (min/max/avg per channel)
  TuningAdvice.cs         ← Severity (Info/Warning/Critical), Category, Message, Slider

Services/
  DiagnosticsRecorder.cs  ← Receives every TelemetryPacket; accumulates LapSnapshots
  TuningAdvisor.cs        ← Analyses a RaceSession; returns List<TuningAdvice>

Components/Widgets/
  DiagnosticsWidget.razor ← 12th widget; live capture status + post-race report panel
```

---

### Data Model

```csharp
public record LapSnapshot(
    int     LapNumber,
    // Tire temps — per corner, min/avg/max in °C (converted from °F)
    float   TireTempFLMin, TireTempFLAvg, TireTempFLMax,
    float   TireTempFRMin, TireTempFRAvg, TireTempFRMax,
    float   TireTempRLMin, TireTempRLAvg, TireTempRLMax,
    float   TireTempRRMin, TireTempRRAvg, TireTempRRMax,
    // Suspension — how often each corner bottomed out (NormalizedSuspensionTravel ≥ 0.95)
    int     SuspBottomOutCountFL, SuspBottomOutCountFR,
    int     SuspBottomOutCountRL, SuspBottomOutCountRR,
    // G-forces — peak lateral and longitudinal (m/s² ÷ 9.80665)
    float   PeakLateralG, PeakBrakingG, PeakAccelG,
    // Understeer proxy — samples where |TireSlipAngleFL+FR| > |TireSlipAngleRL+RR|
    float   UnderSteerRatio,   // 0–1
    float   OverSteerRatio,    // 0–1
    // Braking efficiency — mean(decel G / brake_input_0–1) per braking zone
    float   BrakeEfficiencyAvg,
    // RPM limiter hits — count of packets where CurrentEngineRpm / EngineMaxRpm > 0.99
    int     LimiterHitCount,
    // Throttle application — mean throttle % when exiting corners (lateral G > 0.3 G falling)
    float   CornerExitThrottleAvg,
    // Lap time
    float   LapTimeSeconds
);

public enum AdviceSeverity { Info, Warning, Critical }
public enum AdviceCategory { TirePressure, Camber, Springs, Dampers, Differential, BrakeBias, Gearing, General }

public record TuningAdvice(
    AdviceSeverity Severity,
    AdviceCategory Category,
    string         Message,
    string         SliderHint   // e.g. "Reduce front brake bias by 3–5 points"
);
```

---

### Diagnostic Rules (TuningAdvisor)

Rules are evaluated against the **mean across all laps** of each metric. Each rule produces a `TuningAdvice` item shown in the report panel.

#### Tire Pressure / Camber

| Condition | Advice |
|---|---|
| Any corner avg temp < 65 °C | **Warning** — "Front-left tires running cold (avg {T}°C). Lower tire pressure by 1–2 PSI or soften springs to load the tire." |
| Any corner avg temp > 95 °C | **Warning** — "Front-right tires overheating (avg {T}°C). Increase tire pressure by 1–2 PSI. Targets: street/rally 26–28 PSI, semi-slicks 30–33 PSI." |
| Any corner avg temp > 112 °C | **Critical** — Same message with "Consider a harder compound or reduce camber." |
| Front avg temp − Rear avg temp > 15 °C (fronts hotter) | **Warning** — "Front tires significantly hotter than rears. Increase front tire pressure or add 0.1–0.2° positive camber to fronts." |
| Rear avg temp > Front avg temp + 15 °C | **Warning** — "Rear tires overloaded vs fronts. Check rear camber and spring balance." |

#### Suspension

| Condition | Advice |
|---|---|
| Any corner SuspBottomOutCount > 5 per lap (mean) | **Critical** — "Suspension bottoming out at {corner}. Increase ride height first; if at max, stiffen spring rate." |
| Any corner SuspBottomOutCount 1–5 | **Warning** — "Occasional {corner} bottoming. Consider raising ride height or increasing spring stiffness." |

#### Understeer / Oversteer / Differential

| Condition | Advice |
|---|---|
| UnderSteerRatio > 0.55 | **Warning** — "Persistent understeer detected. Reduce front acceleration differential by 5–10 points, or soften front springs relative to rear." |
| OverSteerRatio > 0.55 | **Warning** — "Persistent oversteer. Increase rear deceleration differential by 5 points or stiffen rear springs." |
| UnderSteerRatio > 0.70 | **Critical** — "Severe understeer. Consider negative front camber increase, softer front anti-roll bar, and lower front differential." |

#### Braking

| Condition | Advice |
|---|---|
| BrakeEfficiencyAvg < 0.6 | **Warning** — "Brake efficiency is low — high pedal input for limited deceleration G. Check brake pressure setting; increase if below max." |
| `NormalizedAIBrakeDifference` mean < −0.15 | **Info** — "Rear locking tendency observed. Move brake bias toward front by 3–5 points." |
| `NormalizedAIBrakeDifference` mean > 0.15 | **Info** — "Front locking tendency. Move brake bias toward rear by 3–5 points." |

#### Gearing / RPM

| Condition | Advice |
|---|---|
| LimiterHitCount > 10 per lap (mean) | **Warning** — "Engine hitting rev limiter frequently. Gears are too short — move final drive ratio toward top speed." |
| Mean `CurrentEngineRpm / EngineMaxRpm` at gear change < 0.65 | **Info** — "Shifting too early. Try upshifting closer to 80–85% of max RPM for better acceleration." |

---

### DiagnosticsWidget Layout

```
┌─────────────────────────────────────────────────────────────┐
│ ● RACE DIAGNOSTICS              [Record ●] [Clear] [Export] │
├──────────────────────┬──────────────────────────────────────┤
│ SESSION              │ TUNING REPORT                        │
│ Laps: 12             │ ⚠ Tires overheating (FL avg 101°C)  │
│ Distance: 48.3 km    │   Increase FL pressure by 2 PSI     │
│ Best lap: 2:14.331   │                                      │
│ Recording: 00:32:14  │ ✕ Suspension bottoming (RL, 8×/lap) │
│                      │   Raise ride height or stiffen RL   │
│ LIVE INDICATORS      │   spring rate                       │
│ Tire temp: WARN FL   │                                      │
│ Susp: OK             │ ℹ Early shifting detected           │
│ Understeer: MILD     │   Upshift at 80–85% RPM             │
│ Brake eff: OK        │                                      │
└──────────────────────┴──────────────────────────────────────┘
```

Default size: col-6 row-4. Placed after Session widget in the default grid layout.

---

### Export

"Export" button serializes the `RaceSession` to JSON and triggers a browser download via JS interop:  
`IJSRuntime.InvokeVoidAsync("downloadJson", fileName, jsonString)`

---

## References

- [Forza Horizon 6 Data Out Documentation](https://support.forza.net/hc/en-us/articles/51744149102611-Forza-Horizon-6-Data-Out-Documentation)
- [fh6-tel reference implementation](https://github.com/TheBanHammer/fh6-tel)
- [FH4 packet format dat file](https://github.com/richstokes/Forza-data-tools/blob/master/FH4_packetformat.dat)
- [FH5 telemetry community thread](https://forums.forza.net/t/data-out-telemetry-variables-and-structure/535984)
- UI design: `telemetry-dashboard.html` (this directory)

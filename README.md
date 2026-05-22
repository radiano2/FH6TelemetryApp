# FH6 Telemetry Dashboard

A real-time telemetry dashboard for **Forza Horizon 6**, built with Blazor Server (.NET 10), SignalR, and MongoDB.

Receives the game's UDP data-out stream and displays live car telemetry on a browser-based dashboard — speed, RPM, tires, G-forces, lap times, suspension, and more. After each race it automatically generates a tuning report with concrete setup advice.

---

## Features

### 11 Live Widgets
| Widget | Data shown |
|---|---|
| Speed gauge | Speed (km/h or mph), arc + needle |
| RPM gauge | Current / max RPM, redline zone |
| Gear + shift lights | Current gear, 8-light shift indicator with flash |
| Driver inputs | Throttle / Brake / Clutch / Handbrake bars |
| G-Force plot | Lateral + longitudinal G with 45-point trail |
| Tire temps & wear | Per-corner heatmap (blue → mint → orange → red) |
| Lap timing | Current / best / last lap + Δ best |
| Track map | Real-time car dot from world position |
| Session stats | Race position, lap number, fuel % |
| Suspension travel | 4-corner bidirectional bars (mm from centre) |
| Car damage | Impact G-force indicator (All OK → Severe) |

### Race Diagnostics (auto-recording)
- Session starts automatically when the first UDP packet arrives; ends 5 s after the last packet (race over / back to menu)
- Raw telemetry stored in MongoDB per session
- Per-lap snapshots: tire temps (min/avg/max per corner), suspension bottom-outs, peak G-forces, understeer/oversteer ratio, brake efficiency, RPM limiter hits, upshift timing
- **Post-race tuning advisor** — 12 rules across 5 categories with concrete slider advice:
  - Tire temperatures & pressure
  - Suspension bottoming
  - Understeer / oversteer balance
  - Braking efficiency
  - Gearing & rev usage
- Session history — browse past sessions, compare laps, export to JSON

### Dashboard UX
- Responsive 12-column CSS grid; each widget has S / M / L size presets
- Drag-resize handle on every widget (synced back to Blazor via JS interop)
- Layout persisted to MongoDB (survives browser cache clears)
- Unit toggle: km/h ↔ mph, °C ↔ °F (persisted to localStorage)
- LIVE / WAITING connection badge
- Edit mode with dashed outlines

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [MongoDB](https://www.mongodb.com/try/download/community) running locally (default `mongodb://localhost:27017`)
- Forza Horizon 6 with **Data Out** enabled

---

## Quick Start

```bash
git clone https://github.com/radiano2/FH6TelemetryApp.git
cd FH6TelemetryApp
dotnet run
```

Open **http://localhost:5050** in your browser.

---

## Forza Setup

In-game: **Settings → HUD and Gameplay → Data Out**

| Setting | Value |
|---|---|
| Data Out | On |
| Data Out IP Address | IP of the machine running this app |
| Data Out IP Port | `20440` |
| Data Out Packet Format | **Car Dash** |

> Running Forza on the same PC? Use `127.0.0.1` as the IP.

---

## Configuration

`appsettings.json`:

```json
{
  "Urls": "http://0.0.0.0:5050",
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "fh6telemetry"
  },
  "Telemetry": {
    "UdpPort": 20440,
    "ListenAddress": "0.0.0.0"
  }
}
```

Set `ListenAddress` to `0.0.0.0` to receive from any network interface (required when Forza runs on a different PC on the same LAN).

---

## Architecture

```
FH6 Game ──UDP :20440──► UdpListenerService (BackgroundService)
                                  │  byte[324]
                                  ▼
                          TelemetryParser
                                  │  TelemetryPacket record
                                  ▼
                          TelemetryBroadcaster (30 fps gate)
                            ├── event → Dashboard.razor → 11 widgets
                            └── event → DiagnosticsRecorder → MongoDB
```

**Stack:** Blazor Server · ASP.NET Core SignalR · `System.Net.Sockets.UdpClient` · `BinaryPrimitives` · MongoDB.Driver 3.x

---

## MongoDB Collections

| Collection | Contents |
|---|---|
| `widget_layout` | Widget positions & sizes |
| `telemetry_packets` | Raw telemetry per session (indexed by sessionId + timestamp) |
| `race_sessions` | Session metadata, per-lap snapshots, tuning advice |

---

## License

MIT

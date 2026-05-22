// ═══════════════════════════════════════════════════════════
//  FH6 TELEMETRY — dashboard.js
//  Simulation removed in Phase 6; all data now comes from
//  Blazor via TelemetryPacket → SignalR → component state.
// ═══════════════════════════════════════════════════════════

// ── LOCAL STORAGE ──────────────────────────────────────────
function fh6SaveLayout(json) { localStorage.setItem('fh6-layout', json); }
function fh6LoadLayout()     { return localStorage.getItem('fh6-layout'); }
function fh6SavePref(key, val) { localStorage.setItem(key, val); }
function fh6LoadPref(key)      { return localStorage.getItem(key); }

// ── EDIT MODE (toggled by Dashboard.razor) ─────────────────
function fh6SetEditMode(on) {
  document.body.classList.toggle('edit-mode', on);
}

// ── SIZE BUTTONS — no-op; Blazor owns them ─────────────────
function initSizeButtons() {}

// ── DRAG RESIZE ────────────────────────────────────────────
// dotNetRef: DotNetObjectReference<Dashboard> from OnAfterRenderAsync.
// Provides live DOM feedback during drag; calls UpdateWidgetSize on drop.
function initResizeHandles(dotNetRef) {
  const COL_COUNT = 12;
  const ROW_H     = 64 + 14;
  const clampI    = (v, a, b) => Math.max(a, Math.min(b, v));

  document.querySelectorAll('.rh').forEach(rh => {
    const widget = rh.closest('.widget');
    if (!widget) return;

    let sx, sy, sc, sr;

    rh.addEventListener('mousedown', e => {
      e.preventDefault();
      sx = e.clientX;
      sy = e.clientY;

      const dashRect = document.getElementById('dash').getBoundingClientRect();
      const colW = dashRect.width / COL_COUNT;

      const cm = widget.className.match(/col-(\d+)/);
      const rm = widget.className.match(/row-(\d+)/);
      sc = cm ? +cm[1] : 3;
      sr = rm ? +rm[1] : 3;

      const onMove = ev => {
        const nc = clampI(sc + Math.round((ev.clientX - sx) / colW), 2, 12);
        const nr = clampI(sr + Math.round((ev.clientY - sy) / ROW_H), 2, 8);
        widget.className = widget.className
          .replace(/\bcol-\d+\b/g, '')
          .replace(/\brow-\d+\b/g, '')
          .trim();
        widget.classList.add(`col-${nc}`, `row-${nr}`);
      };

      const onUp = () => {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup',   onUp);

        const fcm = widget.className.match(/col-(\d+)/);
        const frm = widget.className.match(/row-(\d+)/);
        const fc  = fcm ? +fcm[1] : sc;
        const fr  = frm ? +frm[1] : sr;

        if (dotNetRef && (fc !== sc || fr !== sr)) {
          dotNetRef.invokeMethodAsync('UpdateWidgetSize', widget.id, fc, fr)
            .catch(err => console.warn('UpdateWidgetSize failed:', err));
        }
      };

      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup',   onUp);
    });
  });
}

// ── JSON EXPORT (called from DiagnosticsModal) ──────────────
function downloadJson(fileName, json) {
  const blob = new Blob([json], { type: 'application/json' });
  const url  = URL.createObjectURL(blob);
  const a    = document.createElement('a');
  a.href     = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}

// ── INIT ───────────────────────────────────────────────────
// Edit mode and size buttons are Blazor-owned from Phase 6 on.
// initResizeHandles(dotNetRef) is called separately by Dashboard.razor.
function initDashboard() {
  initSizeButtons();
}

# Moon Display — Verification and Validation

KSP-like moon level-of-detail: planets only at solar zoom; moons appear when entering a host SOI context (zoom, body focus, vessel reference, or selection).

## Acceptance criteria

| ID | Criterion | Pass condition |
|----|-----------|----------------|
| AC-M01 | Solar zoom | Mun, Ike, and other moons **not** rendered |
| AC-M02 | Focus Kerbin | Mun + Minmus visible, separated, distinct labels |
| AC-M03 | Focus Ike | Sun + Duna + Ike visible; physical scale (no 0.15 overlap) |
| AC-M04 | Vessel in Mun orbit | Kerbin moon system visible (`vesselSoi`) |
| AC-M05 | Mun–Kerbin telemetry | ‖Δpos‖ ≈ 80–90 Mm (±10%) |
| AC-M06 | Ike–Duna telemetry | ‖Δpos‖ ≈ 2.5–4.5 Mm (±15%) |
| AC-M07 | Mun trail | `trailRenderMode` is `samples` or `analytic` |
| AC-M08 | Zoom into Kerbin SOI | Moons appear without clicking (SOI hysteresis) |
| AC-M09 | Radius ratio | Mun mesh : Kerbin mesh ≈ `radiusMeters` ratio in scene |

## HUD / debug

- Enable **Moon LOD debug** in the solar map HUD.
- Banner shows: `Moon LOD: {reason} | host={planet} | visible={count}`.

## Automated tests

| Layer | Command |
|-------|---------|
| Unit (web) | `cd web && npm test` — `moonVisibility.test.ts`, `bodyHierarchy.test.ts`, `bodyVisualScale.test.ts` |
| Moon positions (flight) | `scripts/verify-body-positions.mjs` — moon–parent distance gates |
| Body trails | `scripts/verify-telemetry.ps1` |

## Flight checklist

1. Build/install with KSP closed; restart; open `http://127.0.0.1:8750/?v=43` + Ctrl+F5.
2. **Full system:** only planets + Sun; no moon icons on Kerbin/Duna.
3. **Focus Duna:** Ike visible, offset from Duna, tan/orange colors distinct.
4. **Focus Ike:** frame moon SOI; unfocus returns to prior camera mode.
5. **Kerbin orbit around Mun:** Mun + Minmus trails/icons without focusing manually.
6. Compare spacing to KSP tracking map at similar context.

## Architecture references

- Visibility: `web/src/scene/moonVisibility.ts`
- Physical scale: `web/src/scene/bodyVisualScale.ts`
- Hierarchy: `web/src/model/bodyHierarchy.ts`
- Trail capture order: `TelemetrySnapshotService.BuildBodyOrbitPathCaptureOrder`

# KspWebMap documentation index

Read in this order when onboarding to the modular solar map:

1. **[MAP_V3_STRUCTURE.md](MAP_V3_STRUCTURE.md)** — Repository layout, principles, data flow.
2. **[MAP_V3_PROGRAM_STATE.md](MAP_V3_PROGRAM_STATE.md)** — As-built phases, element matrix, debt.
3. **[MAP_V3_DECOUPLE_PLAN.md](MAP_V3_DECOUPLE_PLAN.md)** — V3 standalone foundation (decoupled from v2 `TrajectoryPlanner`).
4. **[ORBIT_TRAIL_DRAWING_GUIDE.md](ORBIT_TRAIL_DRAWING_GUIDE.md)** — KSP trail rendering (motion tail §2; sample/vertex tuning §12; heliocentric frame §13).
5. **[HELIOCENTRIC_ORBIT_FRAME.md](HELIOCENTRIC_ORBIT_FRAME.md)** — **Sun-child plane / inclination fix** (symptoms, mixed-frame root cause, diagnostics).
6. **[PLANET_ORBIT_COLOR_GUIDE.md](PLANET_ORBIT_COLOR_GUIDE.md)** — Orbit **colors** (stock table, **Customize Map** dev HUD, **planet mod packs**).
7. **[MAP_V3_RENDERING_GUIDE.md](MAP_V3_RENDERING_GUIDE.md)** — Per-element draw contracts (living).
8. **[MAP_V3_MODULES.md](MAP_V3_MODULES.md)** — Module I/O and phase flags.
9. **[MAP_V3_ACCEPTANCE.md](MAP_V3_ACCEPTANCE.md)** — Manual + automated pass criteria.
10. **Phase plans** — `MAP_V3_PHASE0_PLAN.md` … `MAP_V3_PHASE2_PLAN.md`.
11. **Planet orbit spec** — [MAP_V3_PLANET_ORBIT_SPEC.md](MAP_V3_PLANET_ORBIT_SPEC.md).
12. **Planet body spec** — [MAP_V3_PLANET_BODY_SPEC.md](MAP_V3_PLANET_BODY_SPEC.md).
13. **Planet body textures** — [MAP_V3_PLANET_BODY_TEXTURE_SPEC.md](MAP_V3_PLANET_BODY_TEXTURE_SPEC.md).
14. **Planet body orientation (tilt/spin)** — [MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md](MAP_V3_PLANET_BODY_ORIENTATION_SPEC.md).
15. **Phase 3 guide (operator + developer)** — [MAP_V3_PHASE3_GUIDE.md](MAP_V3_PHASE3_GUIDE.md) — bodies, textures, orientation, lab, lessons learned, troubleshooting.

**Phase 3 status:** Complete (3.1 + 3.3 + 3.4). See [MAP_V3_PROGRAM_STATE.md](MAP_V3_PROGRAM_STATE.md). Next: Phase 4 moon orbits.

**V2 reference (regression):** `MAP_V2_MODULES.md`, `MAP_V2_ACCEPTANCE.md` (if present).

**Canonical map mode:** View → **3D Map V3** (`3d-v3`).

# Phase 10.5: 3D Solar Map Polish

Polish pass on the Phase 10 WebGL solar map. Closes Phase 10 exit criteria in [ROADMAP.md](ROADMAP.md).

## Goals

- Single control surface (`MapHud` in 3D; legacy `#solarControls` hidden)
- Extended `window.KspSolarMap` API for shell integration
- Camera framing ported from Phase 8 2D bounds
- Selection, labels, atmosphere, apsis markers, SOI LOD
- Performance: one model build per poll, line decimation, quality presets
- Minimal Sun/Kerbin procedural textures

## Verification

See [OPERATIONS.md](OPERATIONS.md) § Phase 10.5 WebGL verification.

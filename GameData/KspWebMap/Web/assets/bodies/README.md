# Body texture exports (runtime)

JPEG maps are written here by the KspWebMap plugin from in-game ScaledSpace materials (`SCANsat`-style export). Files are **gitignored** and must not be committed (Squad/mod texture rights).

After loading a flight, expect one `{BodyName}.jpg` per heliocentric planet plus a `{BodyName}.jpg.meta` fingerprint sidecar.

| Doc | Purpose |
|-----|---------|
| [`docs/MAP_V3_PHASE3_GUIDE.md`](../../../../docs/MAP_V3_PHASE3_GUIDE.md) | How export + browser load work |
| [`docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md`](../../../../docs/MAP_V3_PLANET_BODY_TEXTURE_SPEC.md) | Formal spec |

**Quick check:** load a flight, then open `http://127.0.0.1:8750/assets/bodies/Kerbin.jpg` (200 = export OK).

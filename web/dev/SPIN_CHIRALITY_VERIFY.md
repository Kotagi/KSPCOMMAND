# Spin chirality diagnostic — operator verification

Use this after **v139** with KSP running and Kerbin in **mesh LOD** (zoom in).

## Run the diagnostic

1. Hard refresh the map with `?v=139` (or check `window.KspSolarMapUiVersion`).
2. Dev HUD → enable **Collect Kerbin spin samples** (wait ≥1 s at 1× or low warp so two telemetry ticks arrive).
3. Click **Run spin chirality diagnostic**, or in the console:
   ```js
   KspSolarMap.runSpinChiralityDiagnostic()
   ```
4. Open the console group `[KspWebMap] spin chirality diagnostic Kerbin`.

## What we are testing

| Check | Meaning |
|--------|---------|
| `rotationAngle` sign | KSP stock scalar increasing or decreasing over the interval |
| `quaternionAboutNorth` sign | DLL `bodyOrientationRootRelative` delta about geographic north |
| `production.signEquatorCrossThree` | Current web mesh (v135 path) equator marker motion in Three |

**Verdict `mesh-matches-ksp-rotation-angle`:** mesh math agrees with KSP over the sample — if the tracking map still looks backward, the bug is likely **texture/export chirality**, not attitude.

**Verdict `mesh-opposes-ksp-rotation-angle`:** fix mesh/basis/pole before touching texture.

## Your visual check (required)

1. Pick one obvious Kerbin feature on **both** the in-game tracking map and the web mesh.
2. At **1×** time (warp off), watch ~30–60 s game time.
3. Note whether the feature drifts the **same** or **opposite** direction on map vs web.
4. Compare to the console signs (`rotationAngle` vs `production`).

| You see | Console says | Next step |
|--------|----------------|-----------|
| Opposite drift | `mesh-matches-ksp-rotation-angle` | Try experiment **texture-mirror-u** (we will ship as a toggle after you confirm) |
| Opposite drift | `mesh-opposes-ksp-rotation-angle` | Try **pole-plus-90** experiment |
| Same drift | either | Spin is aligned; if it still “feels” wrong, describe what you compared |

5. Reply with: verdict string, your same/opposite answer, and `bodyTextureRevision` from the log.

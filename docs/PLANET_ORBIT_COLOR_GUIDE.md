# Planet orbit colors — stock table, Customize Map, and planet mod packs

**Audience:** Anyone tuning heliocentric **planet** orbit ring colors on **3D Map V3**, especially when using **planet mod packs** (OPM, Beyond Home, custom systems) where body names are not in the stock palette.

**Related:** Motion-tail opacity and geometry are separate — see [`ORBIT_TRAIL_DRAWING_GUIDE.md`](ORBIT_TRAIL_DRAWING_GUIDE.md). Planet orbit layer contract: [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md) § Color.

**UI build tag:** `92-v3-planet-orbit-native` (`KSP_WEB_MAP_UI_VERSION` in `web/src/mount.tsx`; hard-refresh `?v=92` on the dashboard).

---

## 1. How orbit color is chosen

Colors are **not** assigned by planet order or draw order. Each trail uses the telemetry **`bodyName`** string (e.g. `Duna`, `Sarnus`, `Tellumo`).

Resolution order:

| Priority | Source | When it applies |
|----------|--------|-----------------|
| 1 | **Preview override** | Customize Map **on**, planet selected, color changed in picker but **Set color** not clicked yet |
| 2 | **Saved default** | Dev **Set color** (persisted in browser `localStorage`) |
| 3 | **Shipped stock table** | `web/src/scene/kspBodyMapColorTable.ts` → `KSP_BODY_MAP_COLORS` |
| 4 | **Fallback** | `DEFAULT_BODY_MAP_COLOR` (`#9db1c3`) for unknown bodies |

Runtime wiring:

- `getKspBodyMapColor` / `useKspBodyMapColor` — `web/src/scene/bodyMapColors.ts`
- V3 drawer — `OrbitTrailV3` → `GradientDirectionalOrbitTrail` with `lineColor` from `useKspBodyMapColor(bodyName)`

**Scope today:** **Heliocentric planet orbits only** (bodies in `hierarchy.planetNames` with `planetOrbit` layer). Moons, vessels, and SOI rings use other paths and are not editable via Customize Map yet.

---

## 2. Shipped stock colors (repo)

Edit the table when you want defaults **for everyone** who installs the mod (no browser step):

**File:** `web/src/scene/kspBodyMapColorTable.ts`

```ts
export const KSP_BODY_MAP_COLORS: Record<string, string> = {
  Kerbin: "#4fc3f7",
  Duna: "#c00202",
  // ...
};
```

Rules:

- Keys must match **exact** `body.name` from telemetry (case-sensitive, as KSP reports it).
- Values are `#rrggbb` lowercase hex.
- Rebuild web UI and install to KSP (see §6).

`bodyMapColors.ts` re-exports the table and merges dev saved defaults; do not duplicate the table there.

---

## 3. Customize Map (in-game dev HUD)

Use this when iterating colors on a **specific save** (stock or modded) without editing TypeScript each time.

### 3.1 Enable and select

1. Open the solar map dashboard in flight (`http://127.0.0.1:8750/`).
2. Set view to **3D Map V3**.
3. In the HUD, check **Dev: Customize Map**.
4. **Click a planet orbit ring** on the map (heliocentric planet trail only). The selected name appears in the panel (e.g. `Selected: Duna`).
5. Adjust the **color picker**. The ring updates immediately (**preview**).

Orbit picking uses a widened hit target on the trail and ray-to-segment tests (`pickPlanetOrbitTrail.ts`, `SelectionController.tsx`, `PlanetOrbitLayer` pick lines).

### 3.2 Buttons

| Control | Action |
|---------|--------|
| **Set color** | Saves the current picker hex as this planet’s **default**. Clears preview override. Persists in browser; still applies when Customize Map is **off**. |
| **Revert planet** | Removes preview and saved default for this planet → back to **shipped** `KSP_BODY_MAP_COLORS` (or grey fallback if unlisted). |
| **Revert all planets** | Clears all previews and all saved defaults. |
| **Copy hex** | Copies `#rrggbb` to clipboard for pasting into `kspBodyMapColorTable.ts`. |

Hint text in the panel distinguishes **preview overrides** vs **saved defaults**.

### 3.3 Browser persistence (`localStorage`)

| Key | Content |
|-----|---------|
| `ksp-web-map-dev-customize-map` | `1` / `0` — Customize Map enabled |
| `ksp-web-map-dev-planet-orbit-colors` | JSON map of **preview** overrides |
| `ksp-web-map-dev-planet-orbit-stock-defaults` | JSON map of **Set color** defaults |
| `ksp-web-map-dev-customize-selected-planet` | Last selected body name |

Legacy keys from the old Duna-only picker are migrated on load (`duna` → saved default for `Duna`).

Saved defaults are **per browser profile** (CEF cache). They do not travel with the mod zip until you copy hex into `kspBodyMapColorTable.ts`.

---

## 4. Workflow — planet mod packs

Mod packs add planets (and sometimes rename or duplicate bodies). Trails still render if telemetry includes `bodyOrbitPaths`; color may be wrong or grey until you assign a hue.

### 4.1 Recommended flow

1. **Load the modded save in flight** so telemetry lists all planets you care about.
2. **3D Map V3** → enable **Customize Map**.
3. For each mod planet whose ring is hard to read or grey:
   - Click its **orbit ring**.
   - Match the in-game KSP map color (or pick a distinct color so rings don’t clash).
   - Click **Set color**.
4. When the pack looks right, **Copy hex** per body (or read the panel code) and add entries to `KSP_BODY_MAP_COLORS` in `kspBodyMapColorTable.ts` using the **exact** body name from the HUD selection label.
5. **Revert all planets** in the HUD (optional) after shipping table updates, or keep local defaults for personal saves only.
6. Rebuild and install (§6); bump `KSP_WEB_MAP_UI_VERSION` and `index.html` `?v=` if you changed only docs/table in a release.

### 4.2 Finding body names

- **Customize Map** selection label (`Selected: …`) — authoritative for the web map.
- HUD **Show body orbit QA** (when available) — lists paths and body names from telemetry.
- Telemetry JSON: `bodyOrbitPaths[].bodyName` (or equivalent in your snapshot).

If a mod uses a typo or alias in cfg vs display name, the **telemetry name** wins for the color key.

### 4.3 Multiple packs / saves

- **Per-save tuning:** use **Set color** only (localStorage).
- **Pack profile in the repo:** maintain a section in `kspBodyMapColorTable.ts` (comment by pack name) or a future data file; today the table is one flat map — last duplicate key wins, so use unique body names only.
- Unknown bodies from two packs with the same name share one color entry.

### 4.4 Mod bodies not showing a ring

Color customization does not create orbits. If there is no ring:

- Body may be classified as a **moon** in hierarchy (Customize Map is planet-only).
- `bodyOrbitPaths` may be missing or hidden (`resolveTrailRenderMode`).
- See [`BODY_ORBIT_VNV.md`](BODY_ORBIT_VNV.md) and [`MAP_V3_PLANET_ORBIT_SPEC.md`](MAP_V3_PLANET_ORBIT_SPEC.md).

---

## 5. Source files (quick map)

| File | Role |
|------|------|
| `web/src/scene/kspBodyMapColorTable.ts` | Shipped `KSP_BODY_MAP_COLORS` + `DEFAULT_BODY_MAP_COLOR` |
| `web/src/scene/bodyMapColors.ts` | `getKspBodyMapColor`, `useKspBodyMapColor` |
| `web/src/settings/customizeMapDev.ts` | Load/save dev state, `getStockPlanetOrbitColor`, `getShippedPlanetOrbitColor` |
| `web/src/store/viewStore.ts` | `planetOrbitColorOverrides`, `planetOrbitStockDefaults`, actions |
| `web/src/components/CustomizeMapDevPanel.tsx` | HUD |
| `web/src/selection/pickPlanetOrbitTrail.ts` | Orbit click picking |
| `web/src/scene/SelectionController.tsx` | Orbit pick when Customize Map on |
| `web/src/scene/v3/layers/PlanetOrbitLayer.tsx` | Registers pick polylines |
| `web/src/scene/GradientDirectionalOrbitTrail.tsx` | Pickable trail + highlight width |

---

## 6. Build, install, and cache bust

Web-only color changes:

```powershell
cd web
npm test
npm run build
```

Install into KSP (package path, not only `MyMods/GameData`):

```powershell
.\scripts\build.ps1 -KspRoot "C:\Path\To\Kerbal Space Program"
.\scripts\install.ps1 -KspRoot "C:\Path\To\Kerbal Space Program"
```

`install.ps1` copies from `artifacts/package/GameData/KspWebMap`, which `build.ps1` fills from `web/dist`.

After install, hard-refresh the dashboard, e.g. `http://127.0.0.1:8750/?v=90`, so CEF loads `ksp-solar-map.js` with the new query string.

**Common mistake:** Updating only `MyMods/GameData/KspWebMap` in the repo without running `install.ps1` leaves the game folder on an old bundle (still showing **Dev: Duna orbit color**).

---

## 7. Checklist — shipping a mod-pack color profile

- [ ] Flight with target save; all target planets show orbit rings on V3.
- [ ] Colors tuned via Customize Map; **Set color** for each body.
- [ ] Hex values copied into `kspBodyMapColorTable.ts` with correct `bodyName` keys.
- [ ] `npm test` && `npm run build` green.
- [ ] `build.ps1` + `install.ps1` to KSP install.
- [ ] `KSP_WEB_MAP_UI_VERSION` and `GameData/KspWebMap/Web/index.html` `?v=` bumped if releasing to others.
- [ ] Side-by-side check: mod rings distinguishable; motion tail still readable (§2 orbit guide).

---

## 8. FAQ

**Why is my mod planet grey?**  
Not in `KSP_BODY_MAP_COLORS` and no saved default → `DEFAULT_BODY_MAP_COLOR`.

**Set color vs preview?**  
Picker alone is temporary. **Set color** writes the persistent default (localStorage) used even when Customize Map is unchecked.

**Revert planet vs shipped table?**  
Revert removes your saved default; it does not edit `kspBodyMapColorTable.ts`.

**Does this change the 2D Canvas map or v1/v2 WebGL?**  
Customize Map HUD is on the shared `MapHud`; color resolution applies wherever `useKspBodyMapColor` is used for planet trails. Primary workflow doc’d here is **3D Map V3** planet orbits.

**Permanent fix for all users?**  
Add rows to `kspBodyMapColorTable.ts` and release a new web bundle.

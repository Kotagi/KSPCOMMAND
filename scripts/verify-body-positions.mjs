/**
 * Phase 11: body icon / trail / live-vs-true position gates.
 * Usage: node scripts/verify-body-positions.mjs [baseUrl]
 */
const baseUrl = process.argv[2] ?? "http://127.0.0.1:8750";
const LIVE_TO_SAMPLE0_MAX = 1;
const ICON_TRAIL0_MAX = 1;
const LIVE_VS_TRUE_MAX = 1e3;
const VALIDATION_SAME_UT_MAX = 1e6;
const SUN_CHILDREN = new Set(["Moho", "Eve", "Kerbin", "Duna", "Jool", "Dres", "Eeloo"]);

/** [moon, parent, minSepM, maxSepM] — order-of-magnitude stock orbits at map scale */
const MOON_PARENT_CHECKS = [
  ["Mun", "Kerbin", 70e6, 100e6],
  ["Minmus", "Kerbin", 4e6, 6e6],
  ["Ike", "Duna", 2e6, 5e6],
  ["Gilly", "Eve", 1e5, 5e7],
  ["Laythe", "Jool", 1e7, 5e8],
];

function stripBom(text) {
  return text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;
}

function dist(a, b) {
  const dx = a.x - b.x;
  const dy = a.y - b.y;
  const dz = a.z - b.z;
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

function trailFrameResidual(v) {
  return v?.maxSampleToTrailFrameMeters ?? v?.maxSampleToTrueMeters ?? 0;
}

const res = await fetch(`${baseUrl}/api/telemetry`);
if (!res.ok) {
  console.error(`HTTP ${res.status} from ${baseUrl}/api/telemetry`);
  process.exit(2);
}

const t = JSON.parse(stripBom(await res.text()));
const failures = [];

if (t.iconTrailSample0ResidualMeters > ICON_TRAIL0_MAX) {
  failures.push(`iconTrailSample0Residual=${t.iconTrailSample0ResidualMeters} > ${ICON_TRAIL0_MAX}`);
}

if (t.ephemerisValidationResidualMeters > VALIDATION_SAME_UT_MAX) {
  failures.push(
    `ephemerisValidationResidual=${t.ephemerisValidationResidualMeters} > ${VALIDATION_SAME_UT_MAX}`,
  );
}

for (const path of t.bodyOrbitPaths ?? []) {
  const v = path.validation;
  if (!v) continue;
  const liveS0 = v.liveToSample0Meters;
  if (liveS0 != null && liveS0 > LIVE_TO_SAMPLE0_MAX) {
    failures.push(`${path.bodyName}: liveToSample0=${liveS0} m`);
  }
  const body = t.bodies?.find((b) => b.name === path.bodyName);
  if (body?.positionRootRelativeMeters && path.samples?.[0]?.positionRootRelativeMeters) {
    const d = dist(body.positionRootRelativeMeters, path.samples[0].positionRootRelativeMeters);
    if (d > ICON_TRAIL0_MAX) {
      failures.push(`${path.bodyName}: icon↔trail[0]=${d.toFixed(3)} m`);
    }
  }
}

console.log("UT", t.gameUniversalTimeSeconds);
console.log(
  "validation(same-UT)",
  t.ephemerisValidationResidualMeters,
  "icon↔trail0",
  t.iconTrailSample0ResidualMeters,
  "60s sep(diag)",
  t.ephemerisLivePropagationResidualMeters,
);

console.log("\nSun-children live vs true (m):");
for (const name of [...SUN_CHILDREN].sort()) {
  const b = t.bodies?.find((x) => x.name === name);
  if (!b) continue;
  const d = b.liveVsTrueDeltaMeters;
  const clock = b.eclipticLongitudeDegrees;
  console.log(
    `  ${name.padEnd(6)} liveVsTrue=${d != null && !Number.isNaN(d) ? d.toFixed(3) : "—"} clock=${clock != null && !Number.isNaN(clock) ? clock.toFixed(1) : "—"}°`,
  );
  if (d != null && !Number.isNaN(d) && d > LIVE_VS_TRUE_MAX) {
    failures.push(`${name}: liveVsTrue=${d.toFixed(1)} m > ${LIVE_VS_TRUE_MAX}`);
  }
}

console.log("\nMoon–parent separation (m):");
for (const [moon, parent, minSep, maxSep] of MOON_PARENT_CHECKS) {
  const moonBody = t.bodies?.find((b) => b.name === moon);
  const parentBody = t.bodies?.find((b) => b.name === parent);
  if (!moonBody?.positionRootRelativeMeters || !parentBody?.positionRootRelativeMeters) {
    console.log(`  ${moon.padEnd(8)} parent ${parent}: — (missing)`);
    continue;
  }
  const sep = dist(
    moonBody.positionRootRelativeMeters,
    parentBody.positionRootRelativeMeters,
  );
  console.log(`  ${moon.padEnd(8)} ↔ ${parent.padEnd(6)} ${sep.toExponential(3)}`);
  if (sep < minSep || sep > maxSep) {
    failures.push(
      `${moon}↔${parent}: separation ${sep.toExponential(2)} m outside [${minSep}, ${maxSep}]`,
    );
  }
}

if (t.positionValidation?.worstBodyName) {
  console.log(
    "\nWorst check:",
    t.positionValidation.worstBodyName,
    t.positionValidation.worstCheck,
    t.positionValidation.worstResidualMeters,
  );
}

if (failures.length > 0) {
  console.error("\nVERIFY BODY POSITIONS FAILED:");
  failures.forEach((f) => console.error(`  - ${f}`));
  process.exit(1);
}

console.log("\nVERIFY BODY POSITIONS PASS");
process.exit(0);

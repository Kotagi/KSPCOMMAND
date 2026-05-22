const baseUrl = process.argv[2] ?? "http://127.0.0.1:8750";
function stripBom(text) {
  return text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;
}

const res = await fetch(`${baseUrl}/api/telemetry`);
if (!res.ok) throw new Error(`HTTP ${res.status}`);
const t = JSON.parse(stripBom(await res.text()));

function clockDeg(p) {
  return (Math.atan2(p.x, p.z) * 180) / Math.PI;
}
function dist(a, b) {
  const dx = a.x - b.x;
  const dy = a.y - b.y;
  const dz = a.z - b.z;
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

const planets = ["Moho", "Eve", "Kerbin", "Duna", "Jool", "Dres", "Eeloo"];
console.log("UT", t.gameUniversalTimeSeconds);
console.log(
  "validation(same-UT)",
  t.ephemerisValidationResidualMeters,
  "icon↔trail0",
  t.iconTrailSample0ResidualMeters,
  "60s sep(diag)",
  t.ephemerisLivePropagationResidualMeters,
  "flip vs trail(diag)",
  t.bodyOrbitFlipPropagationResidualMeters ?? t.bodyOrbitPropagationResidualMeters,
);
console.log("\nBody icon (bodies[]) — clock atan2(x,z), KSP ecliptic XZ:");
for (const name of planets) {
  const b = t.bodies?.find((x) => x.name === name);
  const p = b?.positionRootRelativeMeters;
  if (!p) continue;
  const liveVsTrue = b?.liveVsTrueDeltaMeters;
  console.log(
    `  ${name.padEnd(6)} x=${(p.x / 1e9).toFixed(2)}Gm z=${(p.z / 1e9).toFixed(2)}Gm y=${(p.y / 1e9).toFixed(3)}Gm clock=${clockDeg(p).toFixed(1)}° liveVsTrue=${liveVsTrue != null && !Number.isNaN(liveVsTrue) ? liveVsTrue.toFixed(1) : "—"} m`,
  );
}

console.log("\nTrail sample[0] vs body icon (m):");
for (const name of planets) {
  const path = t.bodyOrbitPaths?.find((x) => x.bodyName === name);
  const b = t.bodies?.find((x) => x.name === name);
  if (!path?.samples?.[0] || !b) continue;
  const d = dist(path.samples[0].positionRootRelativeMeters, b.positionRootRelativeMeters);
  const v = path.validation ?? {};
  console.log(
    `  ${name.padEnd(6)} icon↔trail0=${d.toFixed(1)} liveToS0=${v.liveToSample0Meters ?? "—"} live↔ana=${v.liveToAnalyticMeters != null ? (v.liveToAnalyticMeters / 1e6).toFixed(1) + " Mm" : "—"} plane=${v.planeAngleToAnalyticDegrees != null ? v.planeAngleToAnalyticDegrees.toFixed(2) + "°" : "—"} mode=${v.trailRenderMode}`,
  );
}

console.log("\nWorst flip-recompute vs trail (maxSampleToRecomputed):");
const sorted = [...(t.bodyOrbitPaths ?? [])].sort(
  (a, b) =>
    (b.validation?.maxSampleToRecomputedMeters ?? 0) -
    (a.validation?.maxSampleToRecomputedMeters ?? 0),
);
for (const p of sorted.slice(0, 8)) {
  const v = p.validation;
  if (!v) continue;
  console.log(
    `  ${(p.bodyName ?? "?").padEnd(8)} ${v.maxSampleToRecomputedMeters?.toExponential(3)} m  trail=${(v.maxSampleToTrailFrameMeters ?? v.maxSampleToTrueMeters)?.toFixed(1)} m`,
  );
}

console.log("\nKSP map clock reference (from user screenshot):");
console.log("  Eve ~2 o'clock, Kerbin ~7, Duna ~12, Jool ~3");
console.log("  (clock: 12 o'clock = +Z, 3 = +X, 6 = -Z, 9 = -X in atan2(x,z))");

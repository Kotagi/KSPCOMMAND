import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { validateVesselFrameAlignment } from "../web/src/coords/vesselFrameValidation.ts";
import {
  planeNormalFromPolyline,
  angleBetweenNormalsDegrees,
} from "../web/src/coords/orbitPlaneValidation.ts";
import { conicToInertialSegments } from "../web/src/math/buildConicGeometry.ts";
import { translateReferenceInertialToRoot } from "../web/src/coords/referenceBodyFrames.ts";
import { kspRootToThree } from "../web/src/coords/kspToThree.ts";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const jsonPath =
  process.argv[2] ??
  path.join(__dirname, "..", "telemetry-live-flight.json");
const t = JSON.parse(fs.readFileSync(jsonPath, "utf8"));

const vResult = validateVesselFrameAlignment(t);
console.log("=== Vessel frame validation ===");
console.log(JSON.stringify(vResult, null, 2));

const kerbin = t.bodyOrbitPaths.find((p) => p.bodyName === "Kerbin");
const kerbinPts = kerbin.samples.map((s) => s.positionRootRelativeMeters);
const kerbinN = planeNormalFromPolyline(kerbinPts);
console.log("\nKerbin trail normal (from samples):", kerbinN);
console.log("Kerbin diag normal:", kerbin.validation.planeNormalRootRelative);
console.log("Kerbin mode:", kerbin.validation.trailRenderMode);

const vPts = t.activeVessel.rootPathSamples.map(
  (s) => s.positionRootRelativeMeters,
);
const vN = planeNormalFromPolyline(vPts);
console.log("\nVessel rootPath normal:", vN);
if (vN && kerbinN) {
  console.log(
    "Vessel vs Kerbin trail deg:",
    angleBetweenNormalsDegrees(vN, kerbinN).toFixed(2),
  );
}

const active = t.orbitPatches.find((p) => p.isActivePatch);
const bodyRadius = active.referenceBodyRadiusMeters ?? 600000;
const segs = conicToInertialSegments(active, bodyRadius, false);
const anchor = active.referenceBodyPositionRootRelativeMeters;
const conicPts = translateReferenceInertialToRoot(segs[0] ?? [], anchor);
const cN = planeNormalFromPolyline(conicPts);
console.log("\nActive patch conic normal (root):", cN);
console.log(
  "Patch ref=",
  active.referenceBody,
  "i=",
  active.inclinationDegrees,
  "LAN=",
  active.longitudeOfAscendingNodeDegrees,
);
if (cN && kerbinN) {
  console.log(
    "Patch conic vs Kerbin trail deg:",
    angleBetweenNormalsDegrees(cN, kerbinN).toFixed(2),
  );
}
if (cN && vN) {
  console.log(
    "Patch conic vs vessel path deg:",
    angleBetweenNormalsDegrees(cN, vN).toFixed(2),
  );
}

const cNThree = planeNormalFromPolyline(
  conicPts.map((p) => {
    const [x, y, z] = kspRootToThree(p);
    return { x, y, z };
  }),
);
const kNThree = planeNormalFromPolyline(
  kerbinPts.map((p) => {
    const [x, y, z] = kspRootToThree(p);
    return { x, y, z };
  }),
);
console.log("\nAfter kspRootToThree:");
console.log("Kerbin trail normal (Three):", kNThree);
console.log("Patch conic normal (Three):", cNThree);
if (cNThree && kNThree) {
  console.log(
    "Patch vs Kerbin in Three deg:",
    angleBetweenNormalsDegrees(cNThree, kNThree).toFixed(2),
  );
}

console.log("\nVisible body trails vs Kerbin:");
for (const p of t.bodyOrbitPaths) {
  if (p.validation.trailRenderMode !== "samples") continue;
  const n = planeNormalFromPolyline(
    p.samples.map((s) => s.positionRootRelativeMeters),
  );
  const ang =
    n && kerbinN ? angleBetweenNormalsDegrees(n, kerbinN).toFixed(2) : "—";
  console.log(
    `  ${p.bodyName}: angle to Kerbin ${ang}°, diag normal`,
    p.validation.planeNormalRootRelative,
  );
}

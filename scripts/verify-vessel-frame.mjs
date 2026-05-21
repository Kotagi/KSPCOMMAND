import { validateVesselFrameAlignment } from "../web/src/coords/vesselFrameValidation.ts";
import {
  planeNormalFromPolyline,
  angleBetweenNormalsDegrees,
} from "../web/src/coords/orbitPlaneValidation.ts";
import { conicToInertialSegments } from "../web/src/math/buildConicGeometry.ts";
import { translateReferenceInertialToRoot } from "../web/src/coords/referenceBodyFrames.ts";

const baseUrl = process.argv[2] ?? "http://127.0.0.1:8750";
const maxLiveToPath0 = Number(process.argv[3] ?? 1);
const maxVesselToPatchDeg = Number(process.argv[4] ?? 5);
const maxPatchToRefTrailDeg = Number(process.argv[5] ?? 15);

let telemetry;
try {
  const res = await fetch(`${baseUrl}/api/telemetry`);
  if (!res.ok) {
    throw new Error(`HTTP ${res.status}`);
  }
  telemetry = await res.json();
} catch (err) {
  console.error(
    `Failed to GET ${baseUrl}/api/telemetry. Is KSP in flight with mod loaded?`,
    err,
  );
  process.exit(2);
}

const ref = telemetry.orbit?.referenceBody ?? null;
const fd = telemetry.frameDiagnostics ?? {};
console.log(
  `Schema v${telemetry.schemaVersion} | body offset: ${fd.orbitOffsetMode} | vessel offset: ${fd.vesselOffsetMode}`,
);
console.log(
  `Vessel: ${telemetry.activeVessel?.name} | ref: ${ref} | root: ${telemetry.rootBody}`,
);

const vesselResult = validateVesselFrameAlignment(telemetry, {
  maxLiveToPath0Meters: maxLiveToPath0,
  maxPlaneAngleDegrees: 90,
});

const fail = [];
if (
  vesselResult.liveToPath0Meters != null &&
  vesselResult.liveToPath0Meters > maxLiveToPath0
) {
  fail.push(`liveToPath0=${vesselResult.liveToPath0Meters} m`);
}

console.log(
  `liveToPath0: ${vesselResult.liveToPath0Meters != null ? vesselResult.liveToPath0Meters.toFixed(3) : "—"} m`,
);

const refPath = telemetry.bodyOrbitPaths?.find((p) => p.bodyName === ref);
const kerbinTrailN = refPath
  ? planeNormalFromPolyline(
      refPath.samples.map((s) => s.positionRootRelativeMeters),
    )
  : null;

const vPts =
  telemetry.activeVessel?.rootPathSamples?.map(
    (s) => s.positionRootRelativeMeters,
  ) ?? [];
const vN = planeNormalFromPolyline(vPts);

const active = telemetry.orbitPatches?.find((p) => p.isActivePatch);
let patchN = null;
if (active?.referenceBodyPositionRootRelativeMeters) {
  const segs = conicToInertialSegments(
    active,
    active.referenceBodyRadiusMeters ?? 600000,
    false,
  );
  const anchor = active.referenceBodyPositionRootRelativeMeters;
  const rootPts = translateReferenceInertialToRoot(segs[0] ?? [], anchor);
  patchN = planeNormalFromPolyline(rootPts);
}

const skipVesselPatchPlane =
  active?.classification === "HyperbolicEscape" ||
  telemetry.activeVessel?.situation === "ESCAPING";

if (vN && patchN) {
  const angle = angleBetweenNormalsDegrees(vN, patchN);
  console.log(`vessel path vs patch conic plane: ${angle.toFixed(2)} deg`);
  if (!skipVesselPatchPlane && angle > maxVesselToPatchDeg) {
    fail.push(`vesselToPatchPlane=${angle.toFixed(2)} deg`);
  } else if (skipVesselPatchPlane && angle > maxVesselToPatchDeg) {
    console.log(
      "(skipped vessel↔patch plane gate: escape trajectory spans root-frame curvature)",
    );
  }
}

if (patchN && kerbinTrailN && ref === "Kerbin") {
  const angle = angleBetweenNormalsDegrees(patchN, kerbinTrailN);
  console.log(`patch conic vs ${ref} heliocentric trail: ${angle.toFixed(2)} deg`);
  if (angle > maxPatchToRefTrailDeg) {
    fail.push(`patchToRefTrailPlane=${angle.toFixed(2)} deg`);
  }
}

if (vesselResult.planeAngleDegrees != null && refPath) {
  console.log(
    `vessel path vs ${ref} trail (heliocentric): ${vesselResult.planeAngleDegrees.toFixed(2)} deg (informational for escape/non-coplanar flights)`,
  );
}

if (fail.length > 0) {
  console.error(`VESSEL FRAME VERIFY FAILED: ${fail.join("; ")}`);
  process.exit(1);
}

console.log("VESSEL FRAME VERIFY PASS");
process.exit(0);

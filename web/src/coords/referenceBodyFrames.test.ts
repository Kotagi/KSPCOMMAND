import { describe, expect, it } from "vitest";
import { planeNormalFromPolyline } from "./orbitPlaneValidation";
import { perifocalToInertial } from "../math/buildConicGeometry";
import { orbitalMathInertialToKspReferenceBody } from "./referenceBodyFrames";

describe("referenceBodyFrames", () => {
  it("maps zero-inclination orbit normal from +Z math to +Y KSP", () => {
    const orbit = {
      eccentricity: 0,
      inclinationDegrees: 0,
      longitudeOfAscendingNodeDegrees: 0,
      argumentOfPeriapsisDegrees: 0,
    };
    const pts = [0, 90, 180, 270].map((deg) => {
      const nu = (deg * Math.PI) / 180;
      const math = perifocalToInertial(
        { x: Math.cos(nu), y: Math.sin(nu), z: 0 },
        orbit,
      );
      return orbitalMathInertialToKspReferenceBody(math);
    });
    const normal = planeNormalFromPolyline(pts);
    expect(normal).not.toBeNull();
    expect(Math.abs(normal!.y)).toBeGreaterThan(0.99);
    expect(Math.abs(normal!.z)).toBeLessThan(0.01);
  });
});

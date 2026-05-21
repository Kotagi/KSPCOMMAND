/**
 * Body colors aligned with KSP's in-game map orbit lines (stock solar system).
 * Used for planet icons and heliocentric orbit trails.
 */
export const KSP_BODY_MAP_COLORS: Record<string, string> = {
  Sun: "#e8c85c",
  Moho: "#b8865a",
  Eve: "#6b3fa8",
  Gilly: "#b8b8b8",
  Kerbin: "#4fc3f7",
  Mun: "#b0b0b0",
  Minmus: "#9ee8e8",
  Duna: "#c73b3b",
  Ike: "#c9a87c",
  Dres: "#b8b8b8",
  Jool: "#6ee62e",
  Laythe: "#3d9fd4",
  Vall: "#c8e0e0",
  Tylo: "#a8a8a8",
  Bop: "#e8a8c8",
  Pol: "#e8a8c8",
  Eeloo: "#d4d4d4",
};

const DEFAULT_BODY_COLOR = "#9db1c3";

export function getKspBodyMapColor(bodyName: string | undefined): string {
  if (!bodyName) {
    return DEFAULT_BODY_COLOR;
  }
  return KSP_BODY_MAP_COLORS[bodyName] ?? DEFAULT_BODY_COLOR;
}

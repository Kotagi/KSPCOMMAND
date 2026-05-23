import { createRoot } from "react-dom/client";
import { PlanetTextureLab } from "./PlanetTextureLab";

const rootEl = document.getElementById("planet-texture-lab-root");

if (!rootEl) {
  throw new Error("Missing #planet-texture-lab-root — use dev/planet-texture-lab.html");
}

createRoot(rootEl).render(<PlanetTextureLab />);

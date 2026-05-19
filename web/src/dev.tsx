import { createRoot } from "react-dom/client";
import { useEffect } from "react";
import { SolarMapPanel } from "./components/SolarMapPanel";
import { useViewStore } from "./store/viewStore";
import { isSupportedSchemaVersion, VISIBLE_POLL_MS } from "./telemetry/constants";
import type { TelemetrySnapshot } from "./telemetry/schema-v6";

function DevApp() {
  const setTelemetry = useViewStore((s) => s.setTelemetry);

  useEffect(() => {
    let cancelled = false;
    const poll = async () => {
      try {
        const res = await fetch("/api/telemetry");
        if (!res.ok) {
          return;
        }
        const data = (await res.json()) as TelemetrySnapshot;
        if (!cancelled && isSupportedSchemaVersion(data.schemaVersion)) {
          setTelemetry(data);
        }
      } catch {
        /* KSP server may be offline during dev */
      }
    };
    poll();
    const id = window.setInterval(poll, VISIBLE_POLL_MS);
    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, [setTelemetry]);

  return <SolarMapPanel />;
}

const el = document.getElementById("root");
if (el) {
  createRoot(el).render(<DevApp />);
}

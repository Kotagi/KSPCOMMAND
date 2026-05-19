export const MIN_SCHEMA_VERSION = 6;
export const EXPECTED_SCHEMA_VERSION = 7;

export function isSupportedSchemaVersion(version: number | undefined): boolean {
  return typeof version === "number" && version >= MIN_SCHEMA_VERSION;
}
export const VISIBLE_POLL_MS = 1000;
export const HIDDEN_POLL_MS = 5000;

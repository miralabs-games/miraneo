type Row = Record<string, unknown>;

const isPlainObject = (v: unknown): v is Row =>
  typeof v === "object" && v !== null && !Array.isArray(v);

const isTableable = (v: unknown): v is Row[] =>
  Array.isArray(v) && v.length > 0 && v.every(isPlainObject);

const cell = (v: unknown): string => {
  if (v === null || v === undefined) return "";
  if (isPlainObject(v)) {
    const vals = Object.values(v);
    if (vals.length > 0 && vals.every((x) => typeof x === "number")) return vals.join(",");
  }
  const s = typeof v === "string" ? v : JSON.stringify(v);
  return s.replace(/\|/g, "\\|").replace(/\r?\n/g, " ");
};

function table(label: string, rows: Row[]): string {
  const keys: string[] = [];
  for (const r of rows) for (const k of Object.keys(r)) if (!keys.includes(k)) keys.push(k);
  const constant: string[] = [];
  const cols: string[] = [];
  for (const k of keys) {
    const first = cell(rows[0][k]);
    if (rows.length > 1 && rows.every((r) => k in r && cell(r[k]) === first)) constant.push(`${k}=${first}`);
    else cols.push(k);
  }
  const lines = [`${label} (${rows.length})${constant.length ? ` — all rows: ${constant.join(", ")}` : ""}`];
  if (cols.length > 0) {
    lines.push(`|${cols.join("|")}|`, `|${cols.map(() => "-").join("|")}|`);
    for (const r of rows) lines.push(`|${cols.map((k) => cell(r[k])).join("|")}|`);
  }
  return lines.join("\n");
}

export function toMarkdown(name: string, result: unknown): string {
  if (Array.isArray(result)) return result.length === 0 ? `${name}: none` : isTableable(result) ? table(name, result) : JSON.stringify(result);
  if (!isPlainObject(result)) return JSON.stringify(result);
  const head: string[] = [];
  const blocks: string[] = [];
  for (const [k, v] of Object.entries(result)) {
    if (isTableable(v)) blocks.push(table(k, v));
    else if (Array.isArray(v) && v.length === 0) head.push(`${k}: none`);
    else head.push(`${k}: ${cell(v)}`);
  }
  return [head.join("\n"), ...blocks].filter(Boolean).join("\n\n");
}

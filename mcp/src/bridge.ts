export interface RpcErrorShape {
  code: number;
  message: string;
  data?: unknown;
}

interface RpcResponse {
  jsonrpc: "2.0";
  id: unknown;
  result?: unknown;
  error?: RpcErrorShape;
}

export class MiraBridge {
  private nextId = 1;

  constructor(private readonly baseUrl: string = process.env.MIRASTUDIO_URL ?? "http://localhost:6472") {}

  async call(method: string, params: Record<string, unknown> = {}, timeoutMs = 15000): Promise<unknown> {
    let res: Response;
    try {
      res = await fetch(`${this.baseUrl}/rpc`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ jsonrpc: "2.0", id: this.nextId++, method, params }),
        signal: AbortSignal.timeout(timeoutMs),
      });
    } catch (e) {
      const cause = e instanceof Error ? e.message : String(e);
      throw new Error(
        `MiraStudio plugin unreachable at ${this.baseUrl} — is Studio NEO V2 running with the MiraStudio plugin loaded? (${cause})`,
      );
    }
    const body = (await res.json()) as RpcResponse;
    if (body.error) throw new Error(`Studio error ${body.error.code}: ${body.error.message}`);
    return body.result;
  }

  async health(timeoutMs = 3000): Promise<{ ok: boolean; studio: boolean; version: string }> {
    const res = await fetch(`${this.baseUrl}/health`, { signal: AbortSignal.timeout(timeoutMs) });
    return (await res.json()) as { ok: boolean; studio: boolean; version: string };
  }
}

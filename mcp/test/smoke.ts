import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const transport = new StdioClientTransport({
  command: "bun",
  args: ["run", "src/index.ts"],
  cwd: `${import.meta.dir}/..`,
});

const client = new Client({ name: "miraneo-smoke", version: "0.0.0" });
await client.connect(transport);

const tools = await client.listTools();
console.log(`tools (${tools.tools.length}):`, tools.tools.map((t) => t.name).join(", "));

const res = await client.callTool({ name: "studio_get_info", arguments: {} });
console.log("studio_get_info:", JSON.stringify(res, null, 2));

await client.close();

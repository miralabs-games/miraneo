import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { CallToolRequestSchema, ListToolsRequestSchema } from "@modelcontextprotocol/sdk/types.js";
import { TOOLS, runTool } from "./tools.ts";

const server = new Server(
  { name: "miraneo", version: "0.1.0" },
  { capabilities: { tools: {} } },
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: TOOLS.map((t) => ({
    name: t.name,
    description: t.description,
    inputSchema: t.inputSchema,
  })),
}));

server.setRequestHandler(CallToolRequestSchema, async (req) =>
  runTool(req.params.name, (req.params.arguments as Record<string, unknown> | undefined) ?? {}),
);

await server.connect(new StdioServerTransport());
console.error("[miraneo-mcp] connected on stdio");

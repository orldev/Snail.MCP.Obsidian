# Snail.MCP.Vault

A reliable, high-level API between an AI agent and an Obsidian vault. The server lets the agent semantically analyze and programmatically update Excalidraw schemes without any risk of corrupting their file structure (Markdown + embedded JSON), and manage regular Markdown notes.

## Why

LLMs understand logic well but struggle with spatial math and strict file formats. Editing an Excalidraw file by hand, an agent guesses coordinates, breaks the plugin envelope, or produces invalid JSON. This server isolates that complexity:

- **File safety** — the server generates element ids, computes grid coordinates, and performs safe JSON-in-Markdown edits. The agent just says "add a Database node".
- **Code / diagram sync** — from any project folder the agent can ask "update the architecture scheme"; the server finds the right scheme in the vault and patches it.
- **Semantic reading** — a raw Excalidraw file is noise (colors, fonts, point arrays); the server projects it into a clean graph: `Node A (Frontend) -> Node B (API)`.

## Status

Under active development. Working today: the Excalidraw codec (byte-exact round-trips through the plugin's `json` / `compressed-json` envelope), the read side — `vault_status`, `scheme_list`, `scheme_read` — and two ways to draw.

## Drawing: two tools, one choice

Both write the same plugin-valid file and both take `overwrite=true` to redraw a scheme in place. They differ in who decides the arrangement.

| | `scheme_create` | `scheme_draw` |
| --- | --- | --- |
| Who arranges | the server | the agent |
| You provide | nodes, edges, optional `group` | boxes, zones, texts, arrows — each with coordinates |
| Good for | architecture from code, a quick map, a first draft, anything where the information matters more than the picture | a drawing meant to be presented: swimlanes, a legend, emphasis by size or colour, a boundary band, a timeline |
| Guarantees | nothing overlaps, connections stay readable | captions measured, ids and bindings correct, overlaps reported as warnings |
| Fails by | being a competent but ordinary graph | being exactly as good as the composition it was given |

`scheme_create` is the default. Reach for `scheme_draw` when the composition itself carries meaning — including to polish a scheme the automatic layout produced: read it back with `scheme_read`, keep what works, place the rest by hand.

**Automatic layout** stacks nodes that share a `group` on a labeled backdrop, packs the groups into a grid close to a landscape shape (a row runs left to right, the next one right to left, so consecutive stages stay neighbours), and routes connections along that grid.

## Install

A native program on macOS, Linux and Windows, a framework-dependent one elsewhere — `dotnet tool` picks the package of
the machine it runs on:

```bash
dotnet tool install -g Snail.MCP.Vault      # then the command is snail-mcp-vault
dnx Snail.MCP.Vault --yes                   # or run it without installing
```

On a machine that builds it from this repository, publish it beside the other Snail MCP servers and point the client
at `current`, which follows every later publication:

```bash
dotnet publish src -c Release -r osx-arm64 -p:Install=true
claude mcp add -s user mem1 -e SNAIL_MCP_VAULT_PATH="/absolute/path/to/Obsidian Vault" -- ~/.snail-mcp/Snail.MCP.Vault/current/Snail.MCP.Vault
```

Measured against `dotnet Snail.MCP.Vault.dll` on an Apple Silicon Mac: see *Performance* below.

## Configuration

| Variable | Required | Description |
| --- | --- | --- |
| `SNAIL_MCP_VAULT_PATH` | yes | Absolute path to the Obsidian vault root. |

## Developing locally

```json
{
  "servers": {
    "mem1": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<PATH TO src DIRECTORY>"],
      "env": {
        "SNAIL_MCP_VAULT_PATH": "/absolute/path/to/Obsidian Vault"
      }
    }
  }
}
```

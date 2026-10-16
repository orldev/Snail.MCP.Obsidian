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

**Limits.** A scheme takes at most 500 nodes (boxes) and 2000 edges (arrows); text sizes run from 8 to 120 px; without
groups, the columns all edges skip add up to at most 20000. Within them the slowest layout measured takes about 2 s; a
request beyond them is refused with a message saying what to change — the agent sees every refusal's text, not a
generic error. A new path is claimed atomically, so of two calls racing for it one is refused, and a drawing is
written whole or not at all.

## Install

A native program on macOS, Linux and Windows, a framework-dependent one elsewhere — `dotnet tool` picks the package of
the machine it runs on:

```bash
dotnet tool install -g Snail.MCP.Vault      # then the command is s3l-mcp-vault
dnx Snail.MCP.Vault --yes                   # or run it without installing
```

On a machine that builds it from this repository, publish it beside the other Snail MCP servers and point the client
at `current`, which follows every later publication:

```bash
dotnet publish src -c Release -r osx-arm64 -p:SnailMcpInstall=true
claude mcp add -s user mem1 -- ~/.s3l-mcp/Snail.MCP.Vault/current/Snail.MCP.Vault
```

The vault it serves is named in its settings file (*Configuration* below), not in the registration.

Measured against `dotnet Snail.MCP.Vault.dll` on an Apple Silicon Mac: see *Performance* below.

## Configuration

The server reads its settings file, then its `SNAIL_MCP_VAULT_` variables, which win over the file. The file is the
one `SNAIL_MCP_VAULT_CONFIG` names, the only one read when it is set, or else the first that exists of the chain every
Snail MCP server reads ([Snail.Sdk](https://orldev.github.io/Snail.Sdk/)): `.s3l-mcp-vault.json` or
`s3l-mcp-vault.json` in the working directory, `~/.config/s3l-mcp-vault/config.json`, `~/.s3l-mcp-vault.json`.

The vault's address is a setting of this machine: it belongs in `~/.config/s3l-mcp-vault/config.json`, readable by you
alone (`chmod 600`), never in a launchd job or a client's registration, which travel wherever they are copied:

```json
{ "Path": "/absolute/path/to/Obsidian Vault" }
```

| Variable | Field in the file | Required | Description |
| --- | --- | --- | --- |
| `SNAIL_MCP_VAULT_PATH` | `Path` | yes, in one of the two | Absolute path to the Obsidian vault root. |

## Developing locally

```json
{
  "servers": {
    "mem1": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<PATH TO src DIRECTORY>"]
    }
  }
}
```

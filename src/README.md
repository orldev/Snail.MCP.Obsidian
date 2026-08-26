# Snail.MCP.Vault

A reliable, high-level API between an AI agent and an Obsidian vault. The server lets the agent semantically analyze and programmatically update Excalidraw schemes without any risk of corrupting their file structure (Markdown + embedded JSON), and manage regular Markdown notes.

## Why

LLMs understand logic well but struggle with spatial math and strict file formats. Editing an Excalidraw file by hand, an agent guesses coordinates, breaks the plugin envelope, or produces invalid JSON. This server isolates that complexity:

- **File safety** — the server generates element ids, computes grid coordinates, and performs safe JSON-in-Markdown edits. The agent just says "add a Database node".
- **Code / diagram sync** — from any project folder the agent can ask "update the architecture scheme"; the server finds the right scheme in the vault and patches it.
- **Semantic reading** — a raw Excalidraw file is noise (colors, fonts, point arrays); the server projects it into a clean graph: `Node A (Frontend) -> Node B (API)`.

## Status

Under active development. Current focus: the Excalidraw codec — parsing and serializing the plugin's Markdown envelope with `json` / `compressed-json` (LZ-String) drawing blocks, guaranteeing byte-exact round-trips.

## Developing locally

```json
{
  "servers": {
    "snail-mcp-vault": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "<PATH TO src DIRECTORY>"]
    }
  }
}
```

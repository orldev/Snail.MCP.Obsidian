# Tools

5 tools in 1 family.
The names, descriptions and schemas are generated from the server itself, so they are exactly what the model receives in `tools/list`.

A reliable, high-level API between an AI agent and an Obsidian vault: the server lets the agent semantically analyze and programmatically update Excalidraw schemes without any risk of corrupting their file structure. `scheme_create` lays a scheme out from nodes and edges and is the default; `scheme_draw` renders one the agent composed itself, when the composition carries meaning.

| Tool | Family | What it does |
| --- | --- | --- |
| [`vault_status`](schemes.md#vault_status) | Excalidraw schemes: read and draw | Shows the vault root path and how many Excalidraw schemes it contains. |
| [`scheme_list`](schemes.md#scheme_list) | Excalidraw schemes: read and draw | Lists vault-relative paths of all Excalidraw scheme files. |
| [`scheme_read`](schemes.md#scheme_read) | Excalidraw schemes: read and draw | Reads an Excalidraw scheme as a logical graph: nodes are shapes with their captions, edges are arrows between them, labels are free-standing texts. |
| [`scheme_create`](schemes.md#scheme_create) | Excalidraw schemes: read and draw | Draws an Excalidraw scheme from a logical description: you list what exists and what connects to what, the server computes the arrangement, ids and bindings. |
| [`scheme_draw`](schemes.md#scheme_draw) | Excalidraw schemes: read and draw | Draws a scheme you compose yourself: you place every block, zone and label on the canvas, the server only renders it — measuring captions, generating ids, bi… |

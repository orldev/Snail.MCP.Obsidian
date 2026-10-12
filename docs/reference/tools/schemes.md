# Excalidraw schemes: read and draw

## <code>vault_<wbr>status</code>

Shows the vault root path and how many Excalidraw schemes it contains.

*read-only · every call adds · stays on this machine.*

Takes no parameters.

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {}
}
```

</details>

## <code>scheme_<wbr>list</code>

Lists vault-relative paths of all Excalidraw scheme files.

*read-only · every call adds · stays on this machine.*

Takes no parameters.

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {}
}
```

</details>

## <code>scheme_<wbr>read</code>

Reads an Excalidraw scheme as a logical graph: nodes are shapes with their captions, edges are arrows between them, labels are free-standing texts.

*read-only · every call adds · stays on this machine.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `path` | `string` | yes | — | Vault-relative path of the scheme file, e.g. 'Excalidraw/RAG.md'. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "path": {
      "description": "Vault-relative path of the scheme file, e.g. \u0027Excalidraw/RAG.md\u0027.",
      "type": "string"
    }
  },
  "required": [
    "path"
  ]
}
```

</details>

## <code>scheme_<wbr>create</code>

Draws an Excalidraw scheme from a logical description: you list what exists and what connects to what, the server computes the arrangement, ids and bindings. THE DEFAULT CHOICE — use it whenever the point is the information rather than the picture: an architecture drawn from the code, a quick map of a system, a first draft to refine later, or any request that does not say how the drawing should look. The layout is guaranteed readable: groups become labeled areas packed into a grid, and nothing overlaps. When the composition itself has to carry meaning — lanes, a legend, emphasis, a deliberate arrangement — use scheme_draw instead. To rework an existing scheme, call this with the SAME path, overwrite=true and the FULL new spec — never invent a neighbouring file name.

*may overwrite or delete · every call adds · stays on this machine.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `path` | `string` | yes | — | Vault-relative path of the scheme file, e.g. 'Excalidraw/Payments.md'. |
| `nodes` | `array` | yes | — | Nodes to draw; 'key' names the node for edges, 'text' is the caption, optional 'shape' is rectangle (default), diamond or ellipse. Optional 'group' stacks related nodes inside a labeled frame — prefer groups for schemes with more than ~8 nodes. |
| `edges` | `array?` | — | `null` | Directed connections between node keys, each with an optional label. |
| `overwrite` | `boolean` | — | `false` | Set true to redraw an existing file in place; the whole drawing is replaced by this spec. Without it an existing path is refused. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "path": {
      "description": "Vault-relative path of the scheme file, e.g. \u0027Excalidraw/Payments.md\u0027.",
      "type": "string"
    },
    "nodes": {
      "description": "Nodes to draw; \u0027key\u0027 names the node for edges, \u0027text\u0027 is the caption, optional \u0027shape\u0027 is rectangle (default), diamond or ellipse. Optional \u0027group\u0027 stacks related nodes inside a labeled frame \u2014 prefer groups for schemes with more than ~8 nodes.",
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "key": {
            "description": "Identifier used by edges to refer to this node.",
            "type": "string"
          },
          "text": {
            "description": "Caption drawn inside the shape; use \\n for multiple lines.",
            "type": "string"
          },
          "shape": {
            "description": "rectangle (default), diamond or ellipse.",
            "type": "string"
          },
          "group": {
            "description": "Optional group name, e.g. \u0027Clients\u0027 or \u0027Backend\u0027. Nodes sharing a group are stacked vertically inside a labeled frame; groups are laid out as columns by dependency flow. Grouping keeps big schemes compact and readable.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeColor": {
            "description": "Outline color as hex, e.g. #1971c2. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "backgroundColor": {
            "description": "Fill color as hex, e.g. #a5d8ff. Default transparent.",
            "type": [
              "string",
              "null"
            ]
          },
          "fillStyle": {
            "description": "How the fill is painted: solid (default), hachure, cross-hatch or zigzag.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeStyle": {
            "description": "Outline style: solid (default), dashed or dotted.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeWidth": {
            "description": "Outline width: 1 thin, 2 default, 4 extra bold.",
            "type": [
              "integer",
              "null"
            ]
          },
          "textColor": {
            "description": "Caption color as hex. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "fontSize": {
            "description": "Caption font size in px. Default 20.",
            "type": [
              "integer",
              "null"
            ]
          },
          "font": {
            "description": "Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.",
            "type": [
              "string",
              "null"
            ]
          }
        },
        "required": [
          "key",
          "text"
        ]
      }
    },
    "edges": {
      "description": "Directed connections between node keys, each with an optional label.",
      "type": [
        "array",
        "null"
      ],
      "items": {
        "type": [
          "object",
          "null"
        ],
        "properties": {
          "from": {
            "description": "Key of the source node.",
            "type": [
              "string",
              "null"
            ]
          },
          "to": {
            "description": "Key of the target node.",
            "type": [
              "string",
              "null"
            ]
          },
          "label": {
            "description": "Text drawn on the arrow. Empty for no label.",
            "type": [
              "string",
              "null"
            ]
          },
          "color": {
            "description": "Arrow color as hex, e.g. #e03131. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeStyle": {
            "description": "Line style: solid (default), dashed or dotted.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeWidth": {
            "description": "Line width: 1 thin, 2 default, 4 extra bold.",
            "type": [
              "integer",
              "null"
            ]
          },
          "startArrowhead": {
            "description": "Head at the source end: none (default), arrow, triangle, bar, dot or diamond.",
            "type": [
              "string",
              "null"
            ]
          },
          "endArrowhead": {
            "description": "Head at the target end: arrow (default), none, triangle, bar, dot or diamond.",
            "type": [
              "string",
              "null"
            ]
          }
        },
        "required": [
          "from",
          "to"
        ]
      },
      "default": null
    },
    "overwrite": {
      "description": "Set true to redraw an existing file in place; the whole drawing is replaced by this spec. Without it an existing path is refused.",
      "type": "boolean",
      "default": false
    }
  },
  "required": [
    "path",
    "nodes"
  ]
}
```

</details>

## <code>scheme_<wbr>draw</code>

Draws a scheme you compose yourself: you place every block, zone and label on the canvas, the server only renders it — measuring captions, generating ids, binding arrows and warning about overlaps. USE THIS when the composition itself carries meaning and automatic layout cannot express it: swimlanes per team or stage, nested or overlapping areas, a legend explaining what the colours mean, deliberate emphasis through size or colour, a boundary band, a radial or timeline arrangement — or when the drawing is meant to be presented (a status review, a report, a slide) rather than merely read. Also use it to polish a scheme that scheme_create laid out: read it back, keep what works, place the rest by hand. Canvas units are pixels: a block is roughly 200-450 wide and 70-150 tall, 60-120 of space between blocks reads well, and a zone needs about 60 above its first block for the title. Compose deliberately — this tool does nothing to save a careless arrangement. For an ordinary graph where any sensible layout will do, prefer scheme_create.

*may overwrite or delete · every call adds · stays on this machine.*

| Parameter | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `path` | `string` | yes | — | Vault-relative path of the scheme file, e.g. 'Excalidraw/Payments.md'. |
| `boxes` | `array` | yes | — | Blocks with captions; 'key' names the block for arrows. |
| `zones` | `array?` | — | `null` | Background areas drawn under everything else — lanes, sections, highlighted regions. |
| `texts` | `array?` | — | `null` | Free-standing texts: titles, legends, annotations. |
| `arrows` | `array?` | — | `null` | Connections between blocks, each optionally routed through explicit bends. |
| `overwrite` | `boolean` | — | `false` | Set true to redraw an existing file in place; the whole drawing is replaced. Without it an existing path is refused. |

<details><summary>JSON Schema</summary>

```json
{
  "type": "object",
  "properties": {
    "path": {
      "description": "Vault-relative path of the scheme file, e.g. \u0027Excalidraw/Payments.md\u0027.",
      "type": "string"
    },
    "boxes": {
      "description": "Blocks with captions; \u0027key\u0027 names the block for arrows.",
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "key": {
            "description": "Identifier used by arrows to refer to this block.",
            "type": "string"
          },
          "text": {
            "description": "Caption drawn inside; use \\n for multiple lines.",
            "type": "string"
          },
          "x": {
            "description": "Left edge in canvas units.",
            "type": "number"
          },
          "y": {
            "description": "Top edge in canvas units.",
            "type": "number"
          },
          "width": {
            "description": "Width in canvas units; computed from the text when omitted.",
            "type": [
              "number",
              "null"
            ]
          },
          "height": {
            "description": "Height in canvas units; computed from the text when omitted.",
            "type": [
              "number",
              "null"
            ]
          },
          "shape": {
            "description": "rectangle (default), diamond or ellipse.",
            "type": "string"
          },
          "strokeColor": {
            "description": "Outline color as hex, e.g. #1971c2. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "backgroundColor": {
            "description": "Fill color as hex, e.g. #a5d8ff. Default transparent.",
            "type": [
              "string",
              "null"
            ]
          },
          "fillStyle": {
            "description": "How the fill is painted: solid (default), hachure, cross-hatch or zigzag.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeStyle": {
            "description": "Outline style: solid (default), dashed or dotted.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeWidth": {
            "description": "Outline width: 1 thin, 2 default, 4 extra bold.",
            "type": [
              "integer",
              "null"
            ]
          },
          "textColor": {
            "description": "Caption color as hex. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "fontSize": {
            "description": "Caption size in px. Default 20.",
            "type": [
              "integer",
              "null"
            ]
          },
          "font": {
            "description": "Caption font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.",
            "type": [
              "string",
              "null"
            ]
          }
        },
        "required": [
          "key",
          "text",
          "x",
          "y"
        ]
      }
    },
    "zones": {
      "description": "Background areas drawn under everything else \u2014 lanes, sections, highlighted regions.",
      "type": [
        "array",
        "null"
      ],
      "items": {
        "type": [
          "object",
          "null"
        ],
        "properties": {
          "x": {
            "description": "Left edge in canvas units.",
            "type": "number"
          },
          "y": {
            "description": "Top edge in canvas units.",
            "type": "number"
          },
          "width": {
            "type": "number"
          },
          "height": {
            "type": "number"
          },
          "title": {
            "description": "Caption drawn inside the top-left corner. Empty for a bare area.",
            "type": [
              "string",
              "null"
            ]
          },
          "backgroundColor": {
            "description": "Fill color as hex, e.g. #f8f9fa, or \u0027transparent\u0027. Default #f8f9fa.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeColor": {
            "description": "Outline color as hex. Default #ced4da.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeStyle": {
            "description": "Outline style: solid (default), dashed or dotted.",
            "type": [
              "string",
              "null"
            ]
          },
          "titleColor": {
            "description": "Title color as hex. Default #868e96.",
            "type": [
              "string",
              "null"
            ]
          },
          "titleSize": {
            "description": "Title size in px. Default 20.",
            "type": [
              "integer",
              "null"
            ]
          }
        },
        "required": [
          "x",
          "y",
          "width",
          "height"
        ]
      },
      "default": null
    },
    "texts": {
      "description": "Free-standing texts: titles, legends, annotations.",
      "type": [
        "array",
        "null"
      ],
      "items": {
        "type": [
          "object",
          "null"
        ],
        "properties": {
          "text": {
            "type": [
              "string",
              "null"
            ]
          },
          "x": {
            "description": "Left edge in canvas units.",
            "type": "number"
          },
          "y": {
            "description": "Top edge in canvas units.",
            "type": "number"
          },
          "fontSize": {
            "description": "Size in px. Default 20.",
            "type": [
              "integer",
              "null"
            ]
          },
          "color": {
            "description": "Color as hex. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "font": {
            "description": "Font: excalifont (default, hand-drawn), nunito, lilita, comic, virgil, helvetica or cascadia.",
            "type": [
              "string",
              "null"
            ]
          }
        },
        "required": [
          "text",
          "x",
          "y"
        ]
      },
      "default": null
    },
    "arrows": {
      "description": "Connections between blocks, each optionally routed through explicit bends.",
      "type": [
        "array",
        "null"
      ],
      "items": {
        "type": [
          "object",
          "null"
        ],
        "properties": {
          "from": {
            "description": "Key of the source block.",
            "type": [
              "string",
              "null"
            ]
          },
          "to": {
            "description": "Key of the target block.",
            "type": [
              "string",
              "null"
            ]
          },
          "fromSide": {
            "description": "Face of the source block the arrow leaves from: auto (default), left, right, top or bottom.",
            "type": [
              "string",
              "null"
            ]
          },
          "toSide": {
            "description": "Face of the target block the arrow enters: auto (default), left, right, top or bottom.",
            "type": [
              "string",
              "null"
            ]
          },
          "bends": {
            "description": "Corner points the arrow passes through, as [x, y] pairs in canvas units. Empty for a straight line.",
            "type": [
              "array",
              "null"
            ],
            "items": {
              "type": [
                "array",
                "null"
              ],
              "items": {
                "type": "number"
              }
            }
          },
          "label": {
            "description": "Text drawn on the arrow. Empty for no label.",
            "type": [
              "string",
              "null"
            ]
          },
          "color": {
            "description": "Arrow color as hex, e.g. #e03131. Default #1e1e1e.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeStyle": {
            "description": "Line style: solid (default), dashed or dotted.",
            "type": [
              "string",
              "null"
            ]
          },
          "strokeWidth": {
            "description": "Line width: 1 thin, 2 default, 4 extra bold.",
            "type": [
              "integer",
              "null"
            ]
          },
          "startArrowhead": {
            "description": "Head at the source end: none (default), arrow, triangle, bar, dot or diamond.",
            "type": [
              "string",
              "null"
            ]
          },
          "endArrowhead": {
            "description": "Head at the target end: arrow (default), none, triangle, bar, dot or diamond.",
            "type": [
              "string",
              "null"
            ]
          }
        },
        "required": [
          "from",
          "to"
        ]
      },
      "default": null
    },
    "overwrite": {
      "description": "Set true to redraw an existing file in place; the whole drawing is replaced. Without it an existing path is refused.",
      "type": "boolean",
      "default": false
    }
  },
  "required": [
    "path",
    "boxes"
  ]
}
```

</details>


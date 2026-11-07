# ContextSketch: Spec

A small WPF desktop tool for quickly sketching **system context diagrams**: one system being designed, with primary actors on the left and secondary actors, external systems, and storage on the right. Fast to create, saved as human-readable JSON, reloadable, exportable to SVG.

**Workflow request:** Use plan mode. Propose the architecture and a phased implementation plan, resolve the "Decisions for the planner" section, and confirm the plan before writing code.

## 1. Goals and non-goals

**Goals**
- Create a basic context diagram in under a minute: add elements, connect them, name them.
- Auto-layout gives a clean, conventionally arranged result immediately (primary left, secondary right); the user can then drag elements to adjust.
- Save/load a diagram as readable, diff-friendly JSON (hand-editable, git-friendly).
- Export the diagram as SVG.

**Non-goals (v1)**
- No container/component-level diagrams, nesting, or boundaries/groups.
- No real-time collaboration, cloud, accounts, or telemetry.
- No PNG/PDF export, themes, or plugin system.
- No auto-layout library dependency; keep layout simple and custom.

## 2. Tech constraints

- **C# 12 on .NET 8** (`net8.0-windows` for the app, `net8.0` for the core library).
- **WPF**, Windows only.
- **MVVM using CommunityToolkit.Mvvm** (source generators: `[ObservableProperty]`, `[RelayCommand]`). Avoid other UI frameworks.
- JSON via `System.Text.Json` (no Newtonsoft).
- Minimal NuGet dependencies. Justify any addition in the plan.
- Tests: xUnit for the core library (model, serialization, layout, SVG export). No UI tests required.

## 3. Domain model

**Diagram**: `schemaVersion` (int), `title`, `elements[]`, `relationships[]`.

**Element types**
| `type` | Meaning |
|---|---|
| `system` | The system being designed. Exactly one per diagram, shown in the centre. |
| `primaryActor` | A person/role/system that can **initiate** an interaction with the system (the "user" side). Conventionally on the **left**. |
| `secondaryActor` | A person/role/system that the system **calls on first**; it only participates in a scenario because the system pokes it (e.g. a payment provider, an email service). Conventionally on the **right**. |
| `externalSystem` | Other software the system integrates with. Like secondary actors, these are normally poked by the system. Right side. |
| `storage` | Database, file store, queue, etc. Always poked by the system. Right side (lower area). |

The primary/secondary distinction is mainly **context information** for the reader. Its only direct effect on the app is the layout zone and a distinct look. The app does not need to enforce who initiates what, and relationship `direction` stays independent of it.

**Element fields**
| Field | Notes |
|---|---|
| `id` | Stable string/GUID, unique within the diagram |
| `type` | One of the types above |
| `name` | Required, short |
| `description` | Optional, free text |
| `technology` | Optional, shown as plain text under the name (e.g. "PostgreSQL"). For a primary actor it is how they reach the system (e.g. "WPF", "Web browser", "CLI") |
| `x`, `y` | Position of the element's centre (diagram units) |
| `pinned` | `true` once the user has dragged it; auto-layout leaves pinned elements alone |

**Relationship**
| Field | Notes |
|---|---|
| `id` | Stable id |
| `from`, `to` | Element ids |
| `label` | A plain sentence, e.g. "Submits orders". Relationships carry no technology; that belongs to the elements. |
| `direction` | `oneWay` (from → to) \| `twoWay` |

**Rules**
- Exactly one `system` element per diagram (created automatically in a new diagram; it cannot be deleted, only renamed).
- Deleting an element deletes its relationships.
- A relationship must reference existing elements; no self-relationships.
- Loading must validate and report clear errors (unknown schema version, dangling references, duplicate ids, missing or multiple system elements, unknown types) rather than crash.

### Example JSON
```json
{
  "schemaVersion": 1,
  "title": "Online Shop",
  "elements": [
    { "id": "sys", "type": "system", "name": "Shop Platform", "description": "Web shop and order handling", "x": 0, "y": 0, "pinned": false },
    { "id": "cust", "type": "primaryActor", "name": "Customer", "technology": "Web browser", "x": -350, "y": -80, "pinned": false },
    { "id": "admin", "type": "primaryActor", "name": "Shop Admin", "x": -350, "y": 120, "pinned": false },
    { "id": "pay", "type": "secondaryActor", "name": "Payment Provider", "technology": "Stripe", "x": 350, "y": -150, "pinned": false },
    { "id": "mail", "type": "externalSystem", "name": "Email Service", "technology": "SMTP", "x": 350, "y": 0, "pinned": false },
    { "id": "db", "type": "storage", "name": "Orders DB", "technology": "PostgreSQL", "x": 350, "y": 180, "pinned": false }
  ],
  "relationships": [
    { "id": "r1", "from": "cust", "to": "sys", "label": "Places orders", "direction": "oneWay" },
    { "id": "r2", "from": "admin", "to": "sys", "label": "Manages catalogue", "direction": "oneWay" },
    { "id": "r3", "from": "sys", "to": "pay", "label": "Takes payment", "direction": "twoWay" },
    { "id": "r4", "from": "sys", "to": "mail", "label": "Sends receipts", "direction": "oneWay" },
    { "id": "r5", "from": "sys", "to": "db", "label": "Reads/writes orders", "direction": "twoWay" }
  ]
}
```
Use camelCase property names and camelCase enum strings. Indented output with stable ordering so diffs are clean.

## 4. Behaviour

**Canvas and editing**
- Main window: toolbar (New, Open, Save, Save As, Export SVG, Auto-layout), a pan/zoomable canvas, and a properties panel for the selected item.
- Add element via toolbar buttons per type (Primary Actor, Secondary Actor, External System, Storage). Added elements are auto-placed in their layout zone and given an editable default name.
- Create a relationship by selecting a source element, then dragging or clicking to a target (planner to pick the simplest reliable interaction). Edit label and direction in the properties panel.
- Select, rename, delete (Delete key) elements and relationships. Inline rename on double-click is nice to have.
- Drag elements to move them; this sets `pinned = true`. Pinned elements may be dragged anywhere, including across the centre line. Relationship lines follow the elements and attach at element edges, with arrowheads per `direction` and the label rendered on the line, wrapped onto a few lines if long.
- Distinct visual shapes per type, readable at a glance. Suggestion: person-like shape or icon for primary actor, a visibly related but distinguishable style for secondary actor, emphasised rectangle for the system, plain rectangle for external system, cylinder for storage. Keep styling simple.
- Optional: a faint vertical divider or subtle zone tinting to reinforce the left/right split (planner's call; must not appear in the SVG export unless cheap and tasteful).

**Auto-layout (zones)**

The central system sits at the origin. A clear vertical split runs through it: **left = primary, right = secondary side.** Top and bottom space is used to give elements room, not to blur the split.

- **Left zone:** all `primaryActor` elements, stacked vertically and centred on the system's y. With many actors, spread them over a taller column (using the top-left and bottom-left areas) rather than widening it.
- **Right zone:** `secondaryActor`, `externalSystem`, and `storage` elements. Suggested arrangement, top to bottom: secondary actors in the **top-right**, external systems in the **middle-right**, storage in the **bottom-right**. Each group is stacked in a column, overflowing into a second column further right only if needed.
- Spacing is generous and automatic so that nothing overlaps, and relationship lines from the system to its neighbours stay reasonably short and uncluttered.
- Deterministic: same input gives the same positions. Order within a group follows creation order in the file.
- Pinned elements are never moved by auto-layout. Runs automatically when elements are added or a file without positions is loaded, and on demand via "Auto-layout". An "Auto-layout all" option clears all pins first.
- Layout logic lives in the core library as a pure function (model in, positions out) so it can be unit tested.

**File handling**
- Save/Open as `.json` (suggest `.contextsketch.json` as the default suffix) via standard dialogs.
- Track unsaved changes (title bar marker, prompt on close/new/open).
- Loading a malformed or invalid file shows a clear message and leaves the current diagram untouched.

**SVG export**
- Export the full diagram to a single standalone `.svg`: shapes, text, arrows, labels, cropped to content with a margin.
- Generated from the model by a dedicated exporter in the core library (not a screenshot of the WPF canvas), so it is testable and WPF-independent.

## 5. Suggested structure (planner may refine)

```
ContextSketch.sln
  ContextSketch.Core/        # net8.0, no WPF: model, validation, JSON, layout, SVG export
  ContextSketch.App/         # net8.0-windows WPF: views, viewmodels (CommunityToolkit.Mvvm), dialogs
  ContextSketch.Core.Tests/  # xUnit
```
Keep the core free of any UI dependency. ViewModels wrap/observe the core model; avoid logic in code-behind except canvas input plumbing.

## 6. Quality bar

- Round-trip: save then load yields an identical diagram (covered by tests).
- Core has unit tests for: validation rules, JSON round-trip, layout (no overlaps, primary elements left of the system and secondary-side elements right of it, secondary actors above external systems above storage, pinned respected, deterministic), SVG output (well-formed XML, contains expected elements).
- Builds clean with `dotnet build` and tests pass with `dotnet test`. Nullable reference types enabled; no warnings.
- Code should be small and readable; this is a "little app". Prefer simple solutions over frameworks.

## 7. Decisions for the planner

Please decide and justify briefly in the plan:
1. How to render the canvas in WPF (e.g. `ItemsControl` over a `Canvas` with templated element/relationship views vs. custom drawing) and how to do pan/zoom.
2. Interaction for creating relationships (drag from a handle vs. click source then target).
3. Line routing: straight lines to element edges is sufficient; curves or orthogonal routing only if cheap.
4. Undo/redo: out of scope for v1 unless the chosen architecture makes it nearly free. Say which.
5. Phasing: suggest an order that gives a working end-to-end slice early (e.g. core model + JSON + tests → static render → editing → layout → SVG export).
6. Whether to show a visual left/right divider or zone tint on the canvas.

## 8. Acceptance criteria

- [ ] New diagram starts with one central system; I can add primary actors, secondary actors, external systems, and storage.
- [ ] I can connect elements with labelled, one-way or two-way arrows.
- [ ] Auto-layout puts primary actors on the left and secondary actors (top-right), external systems (middle-right) and storage (bottom-right) on the right, without overlaps.
- [ ] Dragging an element keeps it where I put it, even through later auto-layouts.
- [ ] Save, close, reopen: the diagram is identical, and the JSON is readable and hand-editable.
- [ ] Invalid JSON gives a friendly error and does not crash or corrupt the current diagram.
- [ ] Export SVG opens correctly in a browser and looks like the on-screen diagram.

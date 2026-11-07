# Context Sketch

A small Windows desktop tool (WPF, .NET) for sketching system context diagrams: one system in the middle, with the people and systems around it. Layout is automatic, diagrams save as readable `.contextsketch.json`, and they export to SVG.

> **Superseded by [Motif Sketch](https://github.com/johnrpage/Motif.Sketch).** Motif Sketch combines this tool and [HexaSketch](https://github.com/johnrpage/HexaSketch) into one browser app. This repo is kept as reference material, and its diagram scheme and layout rules carry over to Motif Sketch.

## Running it

```
dotnet run --project src/ContextSketch.App
dotnet test
```

## Docs

- [SPEC.md](SPEC.md): the requirements
- [DESIGN.md](DESIGN.md): the design overview, diagram scheme, layout rules and file format

## License

MIT, see [LICENSE](LICENSE).

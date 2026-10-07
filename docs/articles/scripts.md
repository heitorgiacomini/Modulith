## Architecture diagrams

From the repository root, use Windows PowerShell with `System.Drawing` and Microsoft Edge:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/generate-readme-diagrams.ps1 -PreserveExisting
```

The generator publishes the approved illustrated PNG masters from `scripts/diagram-sources` and creates matching SVG companions that embed those illustrations. The runtime and module illustrations were edited from the original images using the built-in image tool, preserving their design. Their SVG companions are not independently editable vector drawings. The two-panel `architecture.svg` remains an editable vector source; Edge renders its matching PNG. All outputs are copied into `docs/images` for DocFX, and SVGs include accessible titles and descriptions. To change illustration content, edit its raster master using the original as the visual reference, then rerun this script. The visual-edit requirements are recorded in `scripts/diagram-sources/prompts.md`.

Use `-PreserveExisting` when replacing published diagrams: originals are renamed to `name.old.png` or `name.old.svg` and checked by SHA-256. Existing archives are retained, with collisions named `name.1.old.png`, then `name.2.old.png`, and likewise for SVG. During layout iteration, omit the switch to overwrite only the active outputs.

Build the documentation into a temporary output directory:

```powershell
docfx build docs/docfx.json --output "$env:TEMP/modulith-docs-preview"
```

Review image legibility, arrow directions, and relative links before publishing. Service names, ports, and flows must match application and Compose configuration.

# ef-core
> Add-Migration InitialCreate -OutputDir Data/Migrations -Project Basket -StartupProject Api -Context BasketDbContext
> Update-Database -Context BasketDbContext

# generate docs
  > docfx

# generate logs
  > cd "root" \
  >  git-cliff --config docs\cliff.toml -o docs\articles\changelog.md

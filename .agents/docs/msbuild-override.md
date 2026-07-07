## MSBuild property override precedence

When overriding a property like `RunAnalyzersDuringBuild` / `EnforceCodeStyleInBuild` / `TreatWarningsAsErrors` against `linq2db` source (consumed as a submodule by `linq2db.docs` for docfx metadata extraction, or as a project reference in any consumer), env vars **don't override** conditional `<PropertyGroup>` reassignments in the project's `Directory.Build.props`. Pattern:

```xml
<RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
<RunAnalyzersDuringBuild Condition="$(Configuration) == 'Release'">true</RunAnalyzersDuringBuild>
```

Env vars are loaded as global properties before evaluation, but the conditional reassignment overwrites them when its condition fires. Only **command-line `-p:` global properties** (or equivalent — docfx's `metadata[].properties` field, MSBuild Tools' `GlobalProperties`) win against conditional project-file assignments.

Practical: when an env-var override "didn't work", reach for `-p:`. When invoking docfx, edit `docfx.json` metadata entries' `properties` field. See [`linq2db.docs:build.ps1`](https://github.com/linq2db/docs/blob/master/build.ps1) + `source/docfx.json` for the canonical pattern.

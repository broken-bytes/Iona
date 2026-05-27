# Iona SDK

User-facing, shippable pieces of Iona.

| Project | What it is |
|---|---|
| `StandardLibrary/` | The Iona runtime/standard library, packaged as `Iona.StandardLibrary` (`Iona.Builtins`). |
| `Iona.Sdk/` | **MSBuild project SDK** — lets `dotnet build` compile `.iona` projects. |
| `Iona.Build.Tasks/` | The `IonaCompile` MSBuild task (the backend `Iona.Sdk` calls). |
| `Iona.Templates/` | `dotnet new` templates: `ionaconsole`, `ionalib`, `ionasln`. |
| `samples/HelloIona/` | Dev-form sample (manual SDK import, builds from repo without packaging). |
| `samples/HelloPackaged/` | Production-form sample (`Sdk="Iona.Sdk/0.1.0"`, restored from the local feed). |
| `pack-local.sh` | Builds + packs the SDK, stdlib, and templates into `../local-feed`. |

## How the MSBuild SDK works

`Iona.Sdk` plugs into `Microsoft.NET.Sdk` and replaces the C# compiler with Iona:

- `Sdk/Sdk.props` imports `Microsoft.NET.Sdk` props, sets `Language=Iona`,
  `DefaultLanguageSourceExtension=.iona`, and points `$(LanguageTargets)` at `Iona.targets`
  (the same hook C#/F#/VB use).
- `Sdk/Iona.targets` defines a `CoreCompile` that calls the `IonaCompile` task, stubs
  `CreateManifestResourceNames`, and imports `Microsoft.Common.targets` to get the rest of
  the build pipeline. `ProduceReferenceAssembly` is off (Iona emits a single assembly).
- `IonaCompile` (in `Iona.Build.Tasks`, **netstandard2.0**) **shells out** to the Iona CLI in
  its own process — deliberately, to avoid loading the compiler + Mono.Cecil into MSBuild.
  It passes `-o @(IntermediateAssembly)` so the compiler writes the assembly directly to the
  right path, and treats a non-zero exit code (or a missing output file) as a build failure.

## Building a `.iona` project

### Production form (`Sdk="Iona.Sdk/0.1.0"`)

```xml
<Project Sdk="Iona.Sdk/0.1.0">
  <PropertyGroup>
    <OutputType>exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

The inline `/0.1.0` version plus the `iona-local` feed in the repo's `NuGet.config` is all
that's needed — MSBuild's NuGet SDK resolver restores `Iona.Sdk` from the feed. The implicit
`Iona.StandardLibrary` reference restores from the same feed.

Local-feed workflow:

```bash
sh SDK/pack-local.sh                         # build + pack SDK/stdlib/templates into local-feed/
dotnet build path/to/MyApp.ionaproj          # restores Iona.Sdk + Iona.StandardLibrary from the feed
```

`pack-local.sh` also clears the `0.1.0` entries from the global NuGet cache so re-packed bits
are picked up without a version bump.

### Templates

```bash
dotnet new install local-feed/Iona.Templates.0.1.0.nupkg
dotnet new ionaconsole -n MyApp        # exe project + Program.iona
dotnet new ionalib     -n MyLib        # dll project + Library.iona
dotnet new ionasln     -n MySln        # empty solution
```

### Dev form (no packaging, fast inner loop)

`samples/HelloIona/HelloIona.ionaproj` uses the manual SDK-import form and overrides the tool
paths to the unpackaged `bin/` output, so it builds straight from the repo:

```bash
dotnet build SDK/Iona.Build.Tasks
dotnet build Toolchain/Iona
dotnet build SDK/samples/HelloIona/HelloIona.ionaproj
```

## CLI compile contract

The task invokes the CLI as:

```
iona <sources...> [-r <ref> <ref> ...] [-f <framework>] -t <dll|exe> -o <output-path>
```

- `-t` / `--target` selects the artifact kind (dll/exe).
- `-o` / `--output` is the full output path (with extension); the assembly name is derived
  from the file name (C#/F# `csc /out:` convention). Omitting it defaults to `App.dll` in
  the working directory.
- The CLI returns exit code 1 on compile errors.

## Solutions

`dotnet sln add` rejects `.ionaproj` ("unknown project type") — the dotnet CLI only knows a
hardcoded set of project extensions, and that's not something a NuGet SDK can extend. MSBuild
itself builds `.ionaproj` projects inside a solution fine; you just have to add the entries by
hand using the standard project-type GUID:

```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "MyApp", "MyApp\MyApp.ionaproj", "{...}"
EndProject
```

`dotnet build MySln.sln` then builds them. The `ionasln` template creates the (empty) solution;
populating it currently needs this manual step.

## Known limitations / next steps

- **`dotnet sln add` doesn't accept `.ionaproj`** — manual `.sln` entry required (see above).
  Proper fix needs a registered Iona project-type, which is an SDK/VS-level concern.
- **Implicit `Iona.StandardLibrary` reference is a runtime safety net**, not a compile input —
  the compiler registers builtins programmatically and maps them to `System.*`. Opt out with
  `<DisableImplicitIonaStandardLibraryReference>true</DisableImplicitIonaStandardLibraryReference>`.
- **No `.resx`/resource support** (`CreateManifestResourceNames` is a no-op stub).
- **TFM mapping is coarse** — `net*`→.NET 10 default, `netstandard2.0`, `net4*`→.NET FW 4.

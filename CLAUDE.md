# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

- **`RECmd`**: command line Registry access (`net472;net10.0`): searches (`--sa/--sk/--sv/--sd/--ss`), key dumps (`--kn`), and batch mode (`--bn` with a `.reb` YAML file from `BatchExamples`, to CSV or JSON). Plugins (`RegistryPlugin.*.dll` in the `Plugins` folder next to it, from the RegistryPlugins repo) parse the values they know; their rows have `(plugin)` as the value type, and `DumpPluginValues` writes each plugin's full output to a timestamped folder.
- **`rla`**: replays transaction logs and saves the hives (`--out`). `OutputNames` decides where: under the hive's own path (default), or flat with `--nop` (`--cn` names profile hives `drive_profile_name`; a name taken in the run gets `_2`).
- Both open hives with the Registry package and plugins build on the RegistryPluginBase package (RegistryPlugins repo); fixes to hive or log handling belong in Registry, followed by a package bump here. Input may be corrupt or hostile.
- `BatchExamples`: batch files maintained mostly by the community; `verify.yml` lints them with yamllint (`.yamllint`). `--sync` downloads them from this repo's master.

## Behavior to keep

- A hive is dirty when its sequence numbers differ, or when its header checksum is wrong and it has logs. A dirty hive without logs is an error unless `--nl` (RECmd); rla copies it as it is with `--ca`. Logs are `.LOG`, `.LOG1` and `.LOG2` next to the hive, in any case, and empty ones are skipped. A hive in use is read raw (Windows, administrator), with its logs.
- `--recover` (default true) is passed to `RegistryHive.RecoverDeleted`.
- In `-d` mode, a file with a hive's name that isn't a hive is skipped (debug message); with `-f` it is an error.
- Exit codes: 0 on success, 1 for bad arguments, a bad batch file, or a hive that could not be processed. No `Environment.Exit`.
- Value lookups use `FirstOrDefault` (a key can have the same value name twice).
- Batch CSV columns (`BatchCsvOut`) are what KAPE and users' scripts read: keep them and their order.

## Commands

The SDK is pinned in `global.json` (.NET 10). Tests target `net10.0` and use NUnit 5 + FluentAssertions 7 (pinned to `[7.2.2,8.0.0)`, since 8.x requires a commercial license).

```bash
dotnet build -c Release                         # RECmd.slnx: RECmd and rla for net472 and net10.0, and the tests
dotnet test -c Release                          # CI (test.yml)
dotnet test --filter "FullyQualifiedName~RlaTests"
```

## Tests

- `OutputNamesTests`: rla's output paths (issue #112: paths recreated, names that don't collide, profile names from Windows and Linux paths).
- `RlaTests`, `RECmdTests`: the built `rla.dll` and `RECmd.dll` run as processes on the DFIR Artifact Museum's dirty Windows 11 SYSTEM hive and its logs (`TestFiles/DFIRArtifactMuseum`, gzipped, MIT; see the README there): replay, `--nop`, batch CSV, bad batch files, `--kn`, `--nl`, exit codes.
- Validation of a change across the local corpus (`../Corpus/Registry`, never copied into the repo): run the old and new builds with `-d <corpus> --bn BatchExamples/DFIRBatch.reb --csv <dir>` and compare the CSVs as multisets of rows (ignoring `PluginDetailFile`). The old build needs `DOTNET_ROLL_FORWARD=Major` when only a newer runtime is installed, and each build its own `Plugins` folder.

## Versioning and release

- RECmd and rla have `<Version>` in their csproj, bumped by hand. Neither is packed; the user builds, signs and releases them.

# Install CLI

Pack and install the latest local build of PPDS CLI as a global tool.

## Usage

`/install-cli`

## What It Does

Run the existing installation script:

```powershell
.\scripts\Install-LocalCli.ps1
```

This script:
1. Packs PPDS.Cli to `nupkgs/`
2. Finds the latest package by timestamp
3. Uninstalls any existing version
4. Installs the new version globally

## After Installation

Verify with:
```bash
ppds --version
```

## When to Use

- After making changes to PPDS.Cli or its dependencies
- When testing CLI behavior locally
- Before running integration tests that use the CLI

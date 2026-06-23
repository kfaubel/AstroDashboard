# Astrophotography Dashboard

A C# WPF application for scanning and analyzing astrophotography data from FITS files organized in a hierarchical directory structure.

## Features

- **Hierarchical Project Structure**: Browse telescope → project → night organization
- **Exposure Summary**: View filter-specific exposure statistics with sorting
- **Toggle Date Visibility**: Show/hide dates in the summary by default
- **Filter Statistics**: L, R, G, B, S, H, O filters with automatic grouping
- **Exposure Time Calculations**: Automatically parse and sum exposure times from filenames
- **Subtotals & Grand Totals**: Per-filter totals and complete session totals

## Building the Application

### Prerequisites
- .NET 8.0 SDK or later

### Build Instructions

```bash
cd C:\Users\ken\Development\AstroDashboard
dotnet build
```

### Running the Application

**Option 1: From command line with path argument**
```bash
dotnet run -- "C:\path\to\data"
```

**Option 2: From command line without arguments (uses current directory)**
```bash
dotnet run
```

**Option 3: Run the compiled executable**
```bash
.\bin\Debug\net8.0-windows\AstroDashboard.exe "C:\path\to\data"
```

**Option 4: From the GUI**
1. Run the application
2. Enter the path in the text box at the top
3. Click "Refresh"

## Release Scripts

This repository includes two release helpers in the project root:

- `release.cmd` for quick usage from Command Prompt
- `release.ps1` for full control in PowerShell
- `release.sh` for Bash shells (Git Bash, WSL, etc.)

Both scripts update the version in `AstroDashboard.csproj`, commit the change, create a Git tag, and push the current branch and tag.

When the tag is pushed, GitHub Actions workflow `.github/workflows/release.yml` is triggered automatically and performs a Release publish build.

Release workflow outputs:

- A Release build for `win-x64`
- A zipped package: `AstroDashboard-vX.Y.Z-win-x64.zip`
- A GitHub Release entry with release notes and attached zip asset

### Quick Release (CMD)

```bat
release.cmd
```

Default bump type is `patch`.

To choose the bump type:

```bat
release.cmd major
release.cmd minor
release.cmd patch
```

### PowerShell Release

```powershell
.\release.ps1 -BumpType patch
```

### Bash Release

```bash
./release.sh patch
./release.sh minor
./release.sh major
```

The Bash wrapper accepts exactly one parameter and forwards it to `release.ps1`.

Supported bump types:

- `major`
- `minor`
- `patch`

Optional parameters:

```powershell
.\release.ps1 -BumpType minor -ProjectFile AstroDashboard.csproj -DefaultVersion 0.1.0
```

Notes:

- If `<Version>` is missing in the project file, the script adds it using `DefaultVersion` before bumping.
- The script also sets `<AssemblyVersion>` and `<FileVersion>` to `x.y.z.0`.
- The script runs a local `dotnet build -c Release` validation before commit/tag/push.
- You will be prompted to confirm before changes are applied.

## Directory Structure

The application expects data organized as follows:

```
RootDirectory/
├── Telescope1/
│   ├── ProjectA/
│   │   └── Data/
│   │       ├── NIGHT_2026-06-10/
│   │       │   └── LIGHT/
│   │       │       └── 2026-06-10_03-01-05_L_RMS_7.78_HFR_1.84_ECC_0.77_FWHM_5.78_Stars_479_100_120.00s_-10.00C_0202.fits
│   │       └── NIGHT_2026-06-11/
│   │           └── LIGHT/
│   │               └── 2026-06-11_02-30-00_R_RMS_6.45_HFR_1.56_ECC_0.82_FWHM_5.12_Stars_498_110_60.00s_-10.00C_0301.fits
│   └── ProjectB/
│       └── Data/
│           └── ...
└── Telescope2/
    └── ProjectC/
        └── Data/
            └── ...
```

## FITS Filename Format

The application parses FITS filenames using this pattern:
```
YYYY-MM-DD_HH-MM-SS_[FILTER]_..._[EXPOSURE]s_.._.fits
```

Example:
```
2026-06-10_03-01-05_L_RMS_7.78_HFR_1.84_ECC_0.77_FWHM_5.78_Stars_479_100_120.00s_-10.00C_0202.fits
```

- **Date**: `2026-06-10` (extracted from filename)
- **Filter**: `L` (single letter: L, R, G, B, S, H, or O)
- **Exposure**: `120.00s` (parsed as seconds, converted to minutes in display)

## UI Layout

### Single Summary Window
- One hierarchical tree that combines folder structure and summary data
- Hierarchy: Telescope → Project → Filter → Night
- Columns: **Name**, **Files**, **Minutes**
- Files and minutes are right-aligned for easy comparison
- Project rows are bold

### Toolbar
- **Browse Button**: Pick a top-level directory
- **Text Box**: View or edit the root directory path
- **Refresh Button**: Re-scan the entered directory
- Last selected top-level directory is restored on next app launch

## Test Data

A sample test directory structure has been created at:
```
C:\Users\ken\Development\AstroDashboard\TestData\
```

With test FITS files in:
```
TestData/Telescope1/ProjectA/Data/NIGHT_2026-06-10/LIGHT/
```

To test the application:
1. Run the application
2. Enter `C:\Users\ken\Development\AstroDashboard\TestData` in the text box
3. Click "Refresh"

## How It Works

1. **Directory Scanning**: Recursively searches for `Data` folders
2. **Path Extraction**: Extracts telescope and project names from directory paths above `Data`
3. **Night Parsing**: Finds `NIGHT_YYYY-MM-DD` folders within `Data`
4. **File Parsing**: Scans `LIGHT` subdirectories for `.fits` files
5. **Regex Matching**: Parses filename patterns to extract date, filter, and exposure time
6. **Aggregation**: Groups data by filter and date for summary display
7. **Sorting**: Displays results sorted by filter, then by date

## Data Ignored

Files that don't match the expected filename pattern are silently ignored. Only properly formatted FITS files are included in the summary.

## Troubleshooting

### No data appears
- Verify the directory path exists and contains the expected structure
- Check that FITS files follow the correct naming pattern
- Ensure `Data` and `NIGHT_YYYY-MM-DD` directories are properly named

### Exposure time seems wrong
- Verify that the exposure time in the filename is in the format `###.##s` (e.g., `120.00s`)
- Check that the exposure time appears after the filter letter

### Files not found
- Ensure `.fits` extension is lowercase
- Verify that FITS files are in `Data/NIGHT_YYYY-MM-DD/LIGHT/` structure

## Dependencies

- .NET 8.0 Windows Desktop
- System.Collections
- System.IO
- WPF Framework

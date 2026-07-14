# EVE-O Preview — Personal Fork

[![English](https://img.shields.io/badge/lang-English-red.svg)](README.md)
[![简体中文](https://img.shields.io/badge/lang-简体中文-yellow.svg)](README-cn.md)

This is a personal Windows-focused fork of [EVE-O Preview](https://github.com/Proopai/eve-o-preview). It keeps the original client-preview and switching workflow while adding crop presets, bulk character assignment, improved cycle groups, configurable overlays, local log-derived status, and expanded localization.

This project is independent, unofficial, and not endorsed by CCP Games or the upstream EVE-O Preview maintainers.

## Important policy warning

Upstream EVE-O Preview has historically been discussed and accepted as a view-only display of a **full, unchanged EVE client**. This fork can display only a selected region and can add information derived from local logs. Those features materially change that behavior and must not be described as covered by the earlier approval.

- Review CCP's current policies before using experimental features with EVE Online.
- Cropped EVE previews may fall outside the published allowance for full, unchanged previews.
- This fork does not broadcast input, forward clicks, automate gameplay, use image recognition, or control an EVE client.
- You are responsible for how you configure and use the software.

Relevant background: [CCP overlay guidance](https://www.eveonline.com/news/view/overlays-isk-buyer-amnesty-and-account-security) and the [upstream EVE-O discussion](https://forums.eveonline.com/t/eve-o-preview-v5-1-0-able-actor-multi-client-preview-switcher-2021-05-08-limited-linux-support/4202).

## Downloads and documentation

- [Public releases](https://github.com/Sussic/eve-o-preview-personal/releases)
- [Private build testing guide](README-TESTING.md)
- [Changelog](CHANGELOG.md)
- [Upstream repository](https://github.com/Proopai/eve-o-preview)

Release executables are currently unsigned. Windows may display an unknown-publisher warning. Download only from a repository you trust and compare the ZIP's SHA-256 checksum with the checksum attached to the release.

## Features

### Original EVE-O workflow

- Live, view-only thumbnails for running EVE clients.
- Click a thumbnail or use configurable hotkeys to focus its client.
- Per-client and global thumbnail layouts, sizing, opacity, zoom, and visibility.
- Saved cycle groups and per-character hotkeys.
- No input broadcasting or gameplay automation.

### Features added by this fork

- **Crop presets:** drag-select a normalized source region, name it, and reuse it across characters.
- **Bulk assignment:** filter large character lists, select visible/open characters, and apply one preset to many clients.
- **Cycle-group crops:** apply a saved crop preset to the members of a saved or temporary cycle group.
- **Temporary cycle groups:** maintain up to five session-only groups without changing saved JSON membership. Hold the corresponding number key and click a thumbnail to toggle membership.
- **Improved group editor:** visible forward/backward hotkey recording, ordering, filtering, and status.
- **Configurable overlays:** independently choose the font, colour, and global anchor for character and solar-system labels.
- **Solar-system labels:** read local EVE logs without ESI authentication or network access.
- **Recent-damage highlight:** optionally draw a configurable border after incoming damage is detected in game logs. This feature is still under active testing.
- **Localization:** runtime-selectable English, Simplified Chinese, and Spanish UI text. Translation completeness can vary by build.
- **Old-config migration:** older EVE-O JSON files are loaded and supplemented with defaults for new fields.

## Requirements

- Windows 10 or Windows 11, x64.
- EVE clients in Fixed Window or Windowed mode. Exclusive fullscreen is not supported.
- The self-contained release does not require a separate .NET installation.
- Building from source requires the .NET 8 SDK.

Linux/Wine behavior and the new Windows DWM crop workflow are not currently supported or tested by this fork's release builds.

## Install

1. Download the Windows x64 ZIP and its SHA-256 file from Releases.
2. Verify the checksum if possible.
3. Extract the entire ZIP to a writable folder, such as `Documents\EVE-O-Preview`.
4. Do not run it directly from the ZIP and do not install it under `Program Files`.
5. Close any other EVE-O Preview instance before starting this build; EVE-O allows only one running instance.
6. Run `EVE-O-Preview.exe`.

Keep `EVE-O-Preview.locale` next to the executable. The application stores `EVE-O-Preview.json` beside the executable and needs write access to that directory.

## Updating and old configurations

An old configuration should retain hotkeys, cycle membership, aliases, thumbnail positions, sizes, and other recognized settings. Missing properties are added with safe defaults when the file is saved.

Before the first run of any test build:

1. Close EVE-O Preview.
2. Copy `EVE-O-Preview.json` to a dated backup.
3. Extract the new build to a separate folder.
4. Copy the old JSON into that folder.
5. Start the new build and verify hotkeys, cycle order, thumbnail placement, and sizes before changing settings.

Never distribute your JSON with a release. It can contain character names, aliases, layouts, and hotkeys.

## Crop presets

1. Open the **Crops** tab.
2. Select **New** and name the preset.
3. Choose an open client under **Capture from**.
4. Select the source area and save it.
5. Filter or bulk-select characters and choose **Apply assignments**.

Crop rectangles are stored as normalized coordinates so they survive resolution changes better than fixed pixel coordinates. A crop can also be selected from the **Cycle Groups** tab and applied to that group's current members.

## Cycle groups

**Saved groups** are written to JSON and survive restarts. **Temporary groups** exist only for the current EVE-O session and overlay saved membership while active.

- Configure forward and backward hotkeys in **Cycle Groups**.
- Reorder saved members with the group editor.
- For temporary group 1–5, hold the matching number key and click a thumbnail to toggle that character.
- Temporary membership clears when EVE-O exits.
- Applying a crop to temporary members changes their crop assignment; the temporary membership itself remains session-only.

## Local log features

This fork does not require ESI and does not upload logs. It reads files under the current Windows Documents folder:

```text
Documents\EVE\logs\Chatlogs
Documents\EVE\logs\Gamelogs
```

EVE may redirect Documents through OneDrive; the application uses the path returned by Windows.

### Solar-system label

Enable **Log Chat to File** in EVE and enable the solar-system option in EVE-O's **Overlay** tab. The tracker associates a log with a character through its `Listener` header and follows Local-channel/system-change entries.

### Recent-damage border

Enable **Log Combat Messages to File** in EVE, then enable recent-damage highlighting in the **Overlay** tab. The current parser recognizes incoming English combat messages with positive damage and a `from` direction. It ignores outgoing attacks, repairs, zero-damage events, and non-combat notifications.

The border colour, thickness, and duration are configurable. This feature is experimental; report missed or incorrect triggers without posting private chat logs.

## Mouse controls

| Action | Control |
| --- | --- |
| Focus a client | Left-click its thumbnail |
| Minimize a client | Ctrl + left-click |
| Toggle normal cycle exclusion | Shift + left-click |
| Toggle temporary group 1–5 | Hold 1–5 + left-click |
| Return to the last non-EVE application | Ctrl + Shift + left-click |
| Move a thumbnail | Right-drag |
| Resize a thumbnail | Hold both mouse buttons and drag |

## Build from source

```powershell
dotnet restore src\Eve-O-Preview\Eve-O-Preview.csproj
dotnet build src\Eve-O-Preview\Eve-O-Preview.csproj --configuration Release -p:EVEOTARGET=Windows
```

Run a development build with:

```powershell
dotnet run --project src\Eve-O-Preview\Eve-O-Preview.csproj --configuration Debug -p:EVEOTARGET=Windows
```

Use the included EVE-O-Mock project when testing crop behavior. Do not commit generated JSON, real EVE logs, or personal test data.

## Reporting a bug

Include:

- build/tag name and Windows version;
- whether the clean upstream build shows the same issue;
- exact steps and expected/actual behavior;
- relevant screenshot;
- build output or a short, redacted log excerpt;
- whether an old or fresh configuration was used.

Do not upload full chat/game logs or an unredacted configuration. See [README-TESTING.md](README-TESTING.md) for the private-test checklist.

## Credits and license

This fork is derived from EVE-O Preview and retains its upstream history and copyright notices. Thanks to the upstream maintainers and contributors for the original application.

Licensed under the [MIT License](LICENSE). EVE Online and related marks are property of CCP Games.

# Private build testing guide

This guide is for invited testers of prerelease builds. Test builds may crash, rewrite configuration defaults, or contain incomplete UI and translations. Do not replace a known-good installation with a test build.

## Before testing

1. Download only from the private repository's Releases page.
2. Download both the ZIP and its `.sha256.txt` file.
3. Verify the checksum:

   ```powershell
   Get-FileHash .\EVE-O-Preview-*.zip -Algorithm SHA256
   ```

4. Extract the ZIP to a new writable directory.
5. Close every existing EVE-O Preview process.
6. Back up the old `EVE-O-Preview.json`.
7. Copy the backup into the test directory only if testing migration.

Never send another person's JSON or include your own JSON in a bug report without redacting character names, aliases, layouts, and hotkeys.

## Smoke test

Test these before changing configuration:

- EVE-O starts without a Windows error dialog.
- Existing clients and thumbnails appear.
- Clicking a thumbnail focuses the correct client.
- Existing per-character and cycle hotkeys work.
- Existing thumbnail positions and sizes are retained.
- The settings window opens every tab without crashing.
- Closing and reopening EVE-O preserves settings.

## Feature checklist

### Crops

- Create, rename, and delete a preset.
- Capture a region from more than one client.
- Assign a preset to one character and then to many filtered characters.
- Verify full-window reset.
- Restart and confirm assignments persist.
- Apply a preset from a saved cycle group.
- Apply a preset to a temporary group's current members.

### Cycle groups

- Record forward and backward hotkeys.
- Add, remove, and reorder saved members.
- Restart and verify saved membership and order.
- Hold 1–5 and click thumbnails to toggle temporary membership.
- Verify temporary membership clears after EVE-O exits.
- Verify a client returns to its saved group behavior when removed from a temporary group.

### Overlays and logs

- Change character-name font, colour, and position.
- Change solar-system font, colour, and position independently.
- Enable EVE's chat logging and verify the correct system follows each character.
- Enable EVE's combat-message logging and test the recent-damage border.
- Verify outgoing attacks do not trigger an incoming-damage border.
- Change border colour, thickness, and duration.

### Localization

- Switch between English, Simplified Chinese, and Spanish.
- Check all tabs, dropdown values, statuses, record/remove controls, and dialogs.
- Switch back to English and confirm no translated dropdown values remain cached.

## Reporting a failure

Please include:

```text
Build/tag:
Windows version and display scaling:
Fresh or migrated JSON:
Number of EVE clients:
Feature/tab:
Steps to reproduce:
Expected result:
Actual result:
Does it reproduce after restarting EVE-O?:
```

Attach a screenshot when layout matters. For log-reading failures, include only the smallest redacted lines needed to reproduce the parser issue. Do not upload private conversations or complete EVE logs.

If EVE-O exits immediately, first check the system tray and Task Manager: another EVE-O instance may own the application's single-instance lock.

## Rollback

1. Close the test build.
2. Return to the previous installation directory.
3. Restore the backed-up JSON if the test build saved it.
4. Do not copy new JSON fields back unless you understand them.

Test builds are not automatic updates. Keep the known-good executable and configuration backup until the prerelease is replaced by a stable build.

# Changelog

This changelog covers the personal fork. For upstream history, see the [upstream repository](https://github.com/Proopai/eve-o-preview).

## Unreleased / private testing

- Add configurable recent-incoming-damage thumbnail borders sourced from local EVE game logs.
- Detect UTF-8, UTF-16LE, and UTF-16BE EVE log encodings.
- Refresh the damage overlay immediately when its state changes.
- Continue parser and high-client-count testing before a stable release.

## 8.0.3.0

- Add named crop presets with graphical source-region selection.
- Store crop rectangles as normalized coordinates.
- Add bulk character filtering and assignment in the Crops tab.
- Apply crop presets to saved and temporary cycle-group members.
- Add a redesigned cycle-group editor with visible hotkey recording and member ordering.
- Add five session-only temporary cycle groups toggled by number-key thumbnail clicks.
- Add configurable character-name and solar-system overlay fonts, colours, and anchors.
- Read solar-system information from local EVE logs without ESI.
- Add English, Simplified Chinese, and Spanish runtime UI localization.
- Improve loading and migration of older configuration files.
- Preserve existing hotkeys, cycle groups, thumbnail positions, and per-client sizing where present.

## Upstream base

This fork was originally based on upstream EVE-O Preview `8.0.2.28`. Upstream fixes and behavior remain credited to their original authors.

# Apex Pro Live Heatmap 0.1.2-beta

This release fixes physical key counting stopping after the application
was minimized to or restored from the system tray.

## Fix

Windows Forms may recreate the main window's native handle when
`ShowInTaskbar` changes. Raw Input was registered only for the original
handle, so the UI and GameSense output could continue while no new key
events arrived.

The utility now registers Raw Input every time the native window handle
is created. This keeps capture attached across tray transitions and
other handle recreations.

## Licensing

This release is available under the PolyForm Noncommercial License
1.0.0. Commercial use requires a separate written license from the
copyright holder.

## Privacy

The app stores aggregate per-key counts only. It does not store
characters, key order, timestamps, active applications, clipboard data
or typed text.

## Supported setup

Designed and tested for the SteelSeries Apex Pro Full-size Gen 3 with
German ISO layout on Windows with SteelSeries GG.

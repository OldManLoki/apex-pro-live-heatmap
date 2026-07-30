# Apex Pro Live Heatmap 0.1.3-beta

This release removes a temporary non-interactive ghost window that
could appear while minimizing the application to the system tray.

## Fix

Version 0.1.2 protected Raw Input from native window-handle recreation,
but the tray transition still forced such a recreation by changing
`ShowInTaskbar`.

The tray behavior now simply hides and restores the existing form.
This removes the unnecessary transition while preserving the Raw Input
re-registration safeguard for any handle recreation caused elsewhere.

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

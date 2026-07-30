# Apex Pro Live Heatmap

An unofficial, portable Windows utility that counts physical key presses, calculates a live heatmap and displays it on a **SteelSeries Apex Pro** through the local **GameSense API**.

The current keyboard map is designed and tested for the **Apex Pro Full-size Gen 3 with German ISO (ISO-DE) layout**.

> **Beta software:** Version 0.1.1 works on the tested setup, but other Apex Pro generations and layouts may require mapping adjustments.

[Deutsche Anleitung](README.de.md)

## Features

- Live per-key RGB heatmap on the keyboard
- Matching on-screen keyboard preview
- Logarithmic or linear heat normalization
- Adjustable heat half-life
- Optional counting of key-repeat events
- Optional local long-term counters
- Start, stop and reset controls
- System tray support
- Optional per-user Windows autostart
- No administrator rights, Python or package manager required

## Requirements

- Windows 10 or Windows 11
- SteelSeries GG with the Engine service running
- A SteelSeries Apex Pro with per-key RGB
- .NET Framework 4.7.2 or newer (included with Windows 10/11)

## Download and start

1. Download the latest ZIP from [Releases](../../releases).
2. Extract the ZIP completely.
3. Start SteelSeries GG and make sure the keyboard appears in **Engine**.
4. Double-click `Start Apex Heatmap.cmd` or `ApexProHeatmap.exe`.
5. If necessary, enable **Apex Pro Live Heatmap** under **Engine → Apps** in GG.

Windows SmartScreen may warn about the unsigned executable. This project does not currently use a paid code-signing certificate. The full C# source and the one-command local build are included for inspection.

The **Configure** button shown for the app inside GG opens GG's general GameSense settings. The heatmap preview, counters and controls are in the separate **Apex Pro Live Heatmap** window.

### Installation folder

No special SteelSeries or GameSense folder is required. SteelSeries GG
does not discover the utility by scanning its installation directory.
When started, the utility registers itself with the local GameSense API.

Extract all files together into any normal folder where your Windows
account can write, for example:

```text
C:\Tools\ApexProLiveHeatmap
```

or:

```text
%LOCALAPPDATA%\Programs\ApexProLiveHeatmap
```

Do not run the utility directly from inside the ZIP. Avoid protected
locations such as `C:\Program Files`, because the utility creates
`config.json` and optionally `stats.json` beside the executable. Set up
Windows autostart only after choosing the final folder. If the folder is
moved later, remove and reinstall the autostart shortcut using the
included scripts.

## Controls

- **Start / Stop:** Enable or disable global input counting and RGB output.
- **Clear live heatmap:** Clear current heat while keeping long-term totals.
- **Clear all counters:** Delete current heat and saved statistics.
- **Half-life:** Choose how quickly old key presses fade. `0` disables fading.
- **Count held-key repeats:** Count Windows key-repeat events while a key is held.
- **Store long-term counters locally:** Save aggregate totals to `stats.json`.
- **Minimize to tray:** Hide the window while counting and RGB output continue.

Double-click the tray icon to restore the window. Its context menu offers **Open**, **Start/Stop capture** and **Exit**. Closing the normal window with **X** exits the application and releases the GameSense lighting.

## Configuration

On first use, the app creates `config.json` next to the executable. It can be edited while the app is closed. [`config.example.json`](config.example.json) documents the defaults.

| Setting | Meaning |
| --- | --- |
| `updateIntervalMs` | RGB update interval; 250 ms is the default. |
| `autosaveSeconds` | Save interval for `stats.json`. |
| `heatHalfLifeMinutes` | Visual half-life of the live heatmap. |
| `countAutoRepeat` | Count repeated key-down events while holding a key. |
| `persistStatistics` | Store aggregate long-term counters locally. |
| `startAutomatically` | Start capture when the app opens. |
| `minimizeToTray` | Hide the window in the tray when minimized. |
| `normalization` | `logarithmic` reveals less-used keys; `linear` emphasizes peaks. |

## Heat scale

The coldest keys start as dark navy and progress through cyan, green and yellow to red. With logarithmic normalization, rarely used keys become visible earlier while heavily used keys still form the hottest areas. Heat decays according to the selected half-life.

## Privacy

The utility uses Windows Raw Input and intentionally processes only:

- the physical key scan code,
- whether the key is extended,
- an aggregate counter.

It does **not** determine characters or save key order, timestamps, words, active window/application names, or clipboard contents. The recorded data cannot reconstruct typed text or passwords.

The only network traffic is local HTTP communication with the loopback address (`127.0.0.1`) published by SteelSeries GG. If persistence is enabled, aggregate totals are written only to `stats.json` beside the app. Disable **Store long-term counters locally** and delete `stats.json` to keep no statistics on disk.

## Supported layout and limitations

The ISO-DE map includes physical positions for `Z/Y`, `Ü/Ö/Ä`, `ß`, `#`, `< >`, `Alt Gr`, the ISO Enter key, navigation cluster and number pad. GameSense uses a device-independent 22 × 6 grid and maps it to the attached keyboard.

Keys handled only inside the keyboard—such as the volume wheel or some Fn functions—may not emit Windows keyboard input and therefore may not be counted.

Only the Apex Pro Full-size Gen 3 with German ISO layout has been tested so far. Reports and mapping contributions for other Apex Pro variants are welcome.

## Troubleshooting

- **“GG is not running”:** Open SteelSeries GG, then press Stop and Start in the utility.
- **The app is listed but RGB does not react:** Enable it under **Engine → Apps** and temporarily disable other GameSense apps.
- **Normal lighting does not return after Stop:** Wait a few seconds or toggle the app once in GG.
- **A special key lights in the wrong place:** The mapping is defined in `BuildLayout` in `ApexHeatmapApp.cs`.
- **Windows blocks the download:** Open the downloaded ZIP's properties, choose **Unblock** if shown, then extract it fully.

## Build from source

Run `Build.cmd`. It uses the .NET Framework C# compiler included with Windows and creates `ApexProHeatmap.exe`. No external dependencies are downloaded.

## Technical basis

SteelSeries GG publishes its local GameSense endpoint in `%PROGRAMDATA%\SteelSeries\SteelSeries Engine 3\coreProps.json`. The utility registers a game and bitmap handler, then sends 132 RGB values (22 × 6) to `/game_event`. On Stop or Exit it calls `/stop_game`.

- [SteelSeries GameSense SDK](https://github.com/SteelSeries/gamesense-sdk)
- [Sending events and discovering the local server](https://github.com/SteelSeries/gamesense-sdk/blob/master/doc/api/sending-game-events.md)
- [Full-keyboard bitmap handler](https://github.com/SteelSeries/gamesense-sdk/blob/master/doc/api/json-handlers-full-keyboard-lighting.md)

## License and status

Version 0.1.1-beta and later is available under the
[PolyForm Noncommercial License 1.0.0](LICENSE). Personal, hobby,
educational and other noncommercial use, modification and distribution
are welcome under its terms. Commercial use requires a
[separate written license](COMMERCIAL-LICENSING.md) from the copyright
holder.

The historical 0.1.0-beta release used the MIT License; see the
[license history](LICENSE-HISTORY.md).

Copyright 2026 OldManLoki. This is an independent community project and
is not affiliated with or endorsed by SteelSeries.

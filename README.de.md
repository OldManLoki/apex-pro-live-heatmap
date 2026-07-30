# Apex Pro Live Heatmap

Ein portables Windows-Tool für eine **SteelSeries Apex Pro Full-size mit deutschem ISO-Layout**. Es zählt physische Tastendrücke, berechnet daraus laufend eine Heatmap und überträgt die Farben über die lokale SteelSeries-GameSense-Schnittstelle an die Tastenbeleuchtung.

## Voraussetzungen

- Windows 10 oder Windows 11
- SteelSeries GG mit laufendem Engine-Dienst
- SteelSeries Apex Pro mit Einzeltasten-RGB
- .NET Framework 4.7.2 oder neuer (in Windows 10/11 enthalten)

Es sind weder Python noch PowerShell-Skripte, Paketmanager oder Administratorrechte erforderlich.

## Start

1. SteelSeries GG starten und prüfen, dass die Apex Pro in **Engine** erkannt wird.
2. `Start Apex Heatmap.cmd` oder direkt `ApexProHeatmap.exe` doppelklicken.
3. Es öffnet sich das eigene Fenster **Apex Pro Live Heatmap**. Das Tool startet standardmäßig sofort mit der Erfassung.
4. In GG erscheint unter **Engine → Apps** der Eintrag **Apex Pro Live Heatmap**. Falls GG die Beleuchtung nicht übernimmt, dort die App aktivieren.

Die Schaltfläche **Konfigurieren** in SteelSeries GG öffnet nur GGs allgemeine GameSense-Einstellungen. Zähler, Heatmap-Vorschau, Start/Stop und Halbwertszeit befinden sich im separaten Fenster des lokalen Tools.

Mit **Stop** endet die Erfassung, und GG stellt nach wenigen Sekunden die normale Beleuchtung wieder her. Beim Schließen des Fensters passiert dasselbe.

Optional richtet `Start mit Windows installieren.cmd` einen Autostart-Link nur für das aktuelle Windows-Konto ein. `Autostart entfernen.cmd` entfernt ihn wieder.

### Installationsordner

Es ist kein besonderer SteelSeries- oder GameSense-Ordner erforderlich.
SteelSeries GG sucht das Tool nicht anhand seines Speicherorts. Beim
Start meldet sich das Tool selbstständig über die lokale
GameSense-Schnittstelle bei GG an.

Entpacke alle Dateien gemeinsam in einen normalen Ordner, in den dein
Windows-Konto schreiben darf, beispielsweise:

```text
C:\Tools\ApexProLiveHeatmap
```

oder:

```text
%LOCALAPPDATA%\Programs\ApexProLiveHeatmap
```

Starte das Tool nicht direkt innerhalb der ZIP-Datei. Geschützte
Verzeichnisse wie `C:\Programme` beziehungsweise `C:\Program Files`
solltest du vermeiden, da das Tool `config.json` und optional
`stats.json` neben der Programmdatei anlegt. Richte den Autostart erst
ein, nachdem der endgültige Ordner feststeht. Wenn du den Ordner später
verschiebst, entferne den Autostart mit dem beiliegenden Skript und
richte ihn anschließend erneut ein.

## Bedienung

- **Start / Stop:** globale Tastenerfassung und RGB-Ausgabe ein- oder ausschalten.
- **Live-Heatmap leeren:** aktuelle Hitze löschen, Langzeit-Zähler behalten.
- **Alle Zähler löschen:** Heatmap und gespeicherte Statistik vollständig löschen.
- **Halbwertszeit:** bestimmt, wie schnell ältere Anschläge visuell verblassen. `0` bedeutet kein Verblassen.
- **Gedrückthalten mehrfach zählen:** zählt die Windows-Tastenwiederholung. Ausgeschaltet zählt ein langes Halten nur einmal.
- **Langzeit-Zähler lokal speichern:** schreibt Summen nach `stats.json`. Ausgeschaltet bleibt die aktuelle Sitzung nur im Arbeitsspeicher.
- **In Infobereich:** blendet das Fenster aus; Erfassung und RGB-Ausgabe laufen weiter.
- **Beim Minimieren ausblenden:** schickt das Fenster beim normalen Minimieren automatisch in den Infobereich.

Ein Doppelklick auf das Symbol im Infobereich öffnet das Fenster wieder. Das Rechtsklick-Menü bietet **Öffnen**, **Erfassung starten/stoppen** und **Beenden**. Das normale **X** beendet die App weiterhin vollständig.

## Konfiguration

Die Datei `config.json` kann bei geschlossenem Tool bearbeitet werden:

| Einstellung | Bedeutung |
| --- | --- |
| `updateIntervalMs` | RGB-Aktualisierung; 250 ms ist flüssig und schont GG. |
| `autosaveSeconds` | Speicherintervall für `stats.json`. |
| `heatHalfLifeMinutes` | Visuelle Halbwertszeit der Live-Heatmap. |
| `countAutoRepeat` | Wiederholte Keydown-Ereignisse beim Halten mitzählen. |
| `persistStatistics` | Langzeit-Zähler lokal speichern. |
| `startAutomatically` | Erfassung direkt beim Öffnen starten. |
| `minimizeToTray` | Beim normalen Minimieren in den Infobereich wechseln. |
| `normalization` | `logarithmic` hebt auch seltene Tasten hervor; `linear` betont die Spitzen stärker. |

## Datenschutz

Das Tool verwendet die robuste Windows-Raw-Input-Schnittstelle und verarbeitet absichtlich nur:

- physischen Scan-Code der Taste,
- erweitertes/nicht erweitertes Tastenmerkmal,
- Zählerstand.

Es ermittelt **keine Zeichen**, speichert **keine Reihenfolge**, Zeitstempel, Wörter, Fenster-/Programmnamen oder Zwischenablageinhalte. Damit kann es keine eingegebenen Texte oder Passwörter rekonstruieren. Die einzige Netzwerkkommunikation geht per HTTP an die von SteelSeries GG veröffentlichte Loopback-Adresse `127.0.0.1`. Langzeitdaten liegen ausschließlich als aggregierte Zahlen in `stats.json` neben dem Tool.

Wer keinerlei Daten auf dem Datenträger möchte, deaktiviert **Langzeit-Zähler lokal speichern** und löscht eine eventuell vorhandene `stats.json`.

Die Erfassung erfolgt auch dann, wenn das Toolfenster nicht im Vordergrund ist. Das Tool selbst benötigt keine Administratorrechte und sollte normal gestartet werden.

## Deutsches ISO-Layout

Das Layout ordnet unter anderem `Z/Y`, `Ü/Ö/Ä`, `ß`, `#`, `< >`, `Alt Gr`, die ISO-Eingabetaste, Navigationsblock und Nummernblock nach ihrer physischen Position zu. GameSense erwartet ein geräteunabhängiges Raster von 22 × 6 Zellen und überträgt dieses auf die tatsächlich angeschlossenen Tasten.

Einige Sonderfunktionen, die die Tastatur ausschließlich intern verarbeitet (beispielsweise das Lautstärkerad oder reine Fn-Funktionen), erzeugen eventuell kein Windows-Tastaturereignis und werden dann nicht gezählt.

## Fehlerbehebung

- **„GG läuft nicht“:** SteelSeries GG öffnen; danach im Tool Stop und wieder Start drücken.
- **App sichtbar, aber keine RGB-Reaktion:** In GG unter Engine → Apps **Apex Pro Live Heatmap** aktivieren. Andere GameSense-Apps testweise deaktivieren.
- **Normale Beleuchtung kommt nach Stop nicht zurück:** drei Sekunden warten oder in GG die App kurz aus-/einschalten.
- **Zählung stoppt nach Nutzung des Infobereichs:** auf Version 0.1.2-beta oder neuer aktualisieren. Frühere Versionen konnten ihre Raw-Input-Registrierung verlieren, wenn Windows das versteckte Fenster neu erzeugte.
- **Einzelne Sondertaste sitzt farblich daneben:** `ApexHeatmapApp.cs` enthält in `BuildLayout` die Zuordnung. Das GameSense-Raster ist geräteunabhängig; kleine Modellabweichungen können dort korrigiert werden.
- **Start wird von Windows blockiert:** Rechtsklick auf die heruntergeladene ZIP-Datei → Eigenschaften → gegebenenfalls „Zulassen“, danach vollständig entpacken.

## Lizenz

Version 0.1.1-beta und spätere Versionen stehen unter der
[PolyForm Noncommercial License 1.0.0](LICENSE). Private,
nichtkommerzielle, schulische und gemeinnützige Nutzung, Veränderung
und Weitergabe sind im Rahmen dieser Lizenz willkommen. Kommerzielle
Nutzung benötigt eine
[gesonderte schriftliche Lizenz](COMMERCIAL-LICENSING.md) des
Rechteinhabers.

Die historische Version 0.1.0-beta wurde unter der MIT-Lizenz
veröffentlicht; Einzelheiten stehen in der
[Lizenzhistorie](LICENSE-HISTORY.md).

Copyright 2026 OldManLoki. Dieses Projekt ist ein unabhängiges
Community-Projekt und weder mit SteelSeries verbunden noch von
SteelSeries unterstützt.

## Quellcode und eigener Build

Der vollständige C#-Quellcode liegt in `ApexHeatmapApp.cs`. `Build.cmd` kompiliert daraus mit dem in Windows enthaltenen .NET-Framework-Compiler eine neue `ApexProHeatmap.exe`.

## Technische Grundlage

SteelSeries GG veröffentlicht seine lokale GameSense-Adresse in `%PROGRAMDATA%\SteelSeries\SteelSeries Engine 3\coreProps.json`. Das Tool registriert die App und einen Bitmap-Handler und sendet ein Array aus 132 RGB-Werten (22 × 6) an `/game_event`. Beim Stop wird `/stop_game` aufgerufen.

Offizielle Referenzen:

- [SteelSeries GameSense SDK](https://github.com/SteelSeries/gamesense-sdk)
- [Events senden und Serveradresse ermitteln](https://github.com/SteelSeries/gamesense-sdk/blob/master/doc/api/sending-game-events.md)
- [Full-Keyboard-Bitmap](https://github.com/SteelSeries/gamesense-sdk/blob/master/doc/api/json-handlers-full-keyboard-lighting.md)

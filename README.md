# Flowstate

**Your time. Your focus. Your flow.**

Native Windows-11-Produktivitaets-App (C#, .NET 8 LTS, WinUI 3).

## Struktur

```text
src/Flowstate.Core          Logik (AutoPlan, Zeitfenster, Wiederholungen, Timer, Versionsvergleich)
src/Flowstate.App           WinUI-3-Oberflaeche (unpackaged, Mica, Focus Studio)
tests/Flowstate.Core.Tests  xUnit-Tests
```

## Bauen

```powershell
dotnet test tests/Flowstate.Core.Tests
dotnet build src/Flowstate.App/Flowstate.App.csproj -c Release -p:Platform=x64 -r win-x64
```

## Projektstatus

Legende: [x] erledigt und verifiziert, [~] vorhanden aber nicht verifiziert, [ ] offen

### Phase 1 - Foundation
- [x] Core-Bibliothek (TreatWarningsAsErrors)
- [x] Aufgabenmodell + Validierung
- [x] Freie-Zeitfenster-Berechnung
- [x] AutoPlan-Engine (Prioritaet, Faelligkeit, Aufteilung, Pausen, keine Ueberschneidung, nie in der Vergangenheit)
- [x] Wiederholungsregeln (taeglich, woechentlich, monatlich)
- [x] Pomodoro-Zyklus + driftfreier Timer
- [x] SemVer-Vergleich fuer den Updater (inkl. Pre-Release)
- [x] 31 xUnit-Tests mit .NET SDK 8.0.427 (Linux): 31/31 bestanden
- [~] WinUI-3-App mit Mica, NavigationView und Focus-Timer (Build unter Windows noch nicht geprueft)
- [ ] GitHub Actions CI (`.github/workflows/ci.yml` muss manuell angelegt werden, fehlende Token-Berechtigung `workflow`)
- [ ] Solution-Datei, Dependency Injection, Logging, SQLite/EF Core, Theme-System

### Phase 2-10
- [ ] Task Manager UI, Projekte, Kalender, AutoPlan UI, Sound Studio, Workspaces, Quick Capture, Analytics, Daily Review, Settings
- [ ] Inno-Setup-Installer (Per-User Standard ohne Admin, Per-Machine optional)
- [ ] GitHub-Updater, Release-Workflow
- [ ] Manuelle Windows-11-Tests (Tray, Hotkeys, UAC, DPI, Multi-Monitor)

Es wurde noch kein Release veroeffentlicht.

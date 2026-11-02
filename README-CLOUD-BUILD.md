# AirSketch AR: privater Cloud-Build

Dieses Verzeichnis ist eine separate Kopie des Unity-Quellprojekts. Es wurde noch
nicht online hochgeladen. Die Kopie enthaelt `Assets`, `Packages` und
`ProjectSettings`; Unity-Cache und andere Downloads wurden nicht mitkopiert.

## Einmalig auf dem Windows-PC

1. Installiere GitHub Desktop, falls es noch nicht installiert ist.
2. Starte GitHub Desktop und melde dich bei deinem GitHub-Konto an.
3. Waehle **File > Add Local Repository...** und oeffne `D:\AirSketchAR-CloudSource`.
   Wenn GitHub Desktop anbietet, ein Repository anzulegen, bestaetige dies.
4. Erstelle den ersten Commit und waehle **Publish repository**.
5. Stelle unbedingt **Keep this code private** ein. Veroeffentliche das Repository
   nicht. Pruefe vor dem Upload, dass es wirklich privat bleibt.
6. Git Large File Storage (Git LFS) wird fuer eine grosse Terrain-Demo-Datei
   verwendet. GitHub Desktop verwendet Git LFS beim Veroeffentlichen des Projekts.
   Falls ein Upload-Limit angezeigt wird, nicht die Datei ungeschuetzt hochladen;
   zuerst melden.

## Cloud-Build in Unity

1. Melde dich im Unity Dashboard unter `cloud.unity.com` mit deinem Unity-Konto an.
2. Oeffne dein Unity-Projekt und den Bereich **Build Automation**.
3. Verbinde als Source-Control-Anbieter das soeben erstellte **private**
   GitHub-Repository und waehle den Branch mit dem ersten Commit.
4. Erstelle ein Build-Ziel fuer **iOS**. Waehle die Unity-Version
   `6000.1.9f1`, wenn sie in der Auswahl vorhanden ist. Die AR-Startszene ist
   bereits als erste Build-Szene eingetragen.
5. Folge in Unity Build Automation den Einstellungen fuer iOS-Signierung.
   Fuer einen Upload zu TestFlight brauchst du das Apple-Distribution-Zertifikat,
   ein passendes App-Store-Profil sowie gegebenenfalls einen App-Store-Connect-
   API-Schluessel. Wenn du stattdessen Ad-Hoc installierst, muss das Profil
   dein iPhone enthalten.
6. Lade Zertifikate und geheime Schluessel ausschliesslich direkt in das sichere
   Unity-Dashboard hoch. Niemals GitHub-Repository-Secrets oeffentlich machen;
   Dateien mit Endung `.p12`, `.mobileprovision` oder `.p8` nicht committen.
7. Starte den iOS-Cloud-Build. Wenn er erfolgreich ist, verwende TestFlight
   oder die von Unity angebotene private Verteilung, um AirSketch AR auf dem
   iPhone zu installieren. Beim ersten Start den Kamerazugriff erlauben.

Unity Build Automation kann bestimmte Unity-Versionen als veraltet markieren.
Falls `6000.1.9f1` nicht angeboten wird, den Editor nicht selbst im GitHub-
Repository umstellen: Der Wechsel auf eine andere Unity-Version muss mit dem
Projekt getestet werden.

Unity Build Automation oder Git Large File Storage koennen je nach Tarif Kosten
verursachen. Pruefe die Preise in GitHub und im Unity Dashboard, bevor du den
ersten Commit hochlaedst oder einen Cloud-Build startest.

Der fertige iOS-Build ist eine echte iPhone-App mit AR Foundation und Apples
ARKit-Provider. Fuer das Bauen lokal auf dem Windows-PC werden weder das Xcode-
Projekt noch Unitys `Library`-Ordner in GitHub benoetigt.

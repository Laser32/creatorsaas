# CreatorDesktop

Vollautomatisches Tool für YouTube-Doku-Kanäle: sucht Creative-Commons-Videos, lädt sie herunter, lädt sie auf deinen Kanal hoch, postet Cross-Promotion in Reddit/Telegram/Discord/TikTok, beantwortet Kommentare per AI, trackt Konkurrenz und Trending-Themen — alles aus einer Windows-Desktop-App.

---

## 📋 Inhaltsverzeichnis

1. [Build & erste Schritte](#-build--erste-schritte)
2. [Tab-Übersicht](#-tab-übersicht)
3. [API-Keys & Zugangsdaten beschaffen](#-api-keys--zugangsdaten-beschaffen)
4. [Settings-Tab: alle Optionen](#-settings-tab-alle-optionen)
5. [Workflow: Erstes Video](#-workflow-erstes-video)
6. [Auto-Mode (24/7-Betrieb)](#-auto-mode-247-betrieb)
7. [Backup & Wiederherstellung](#-backup--wiederherstellung)
8. [Dateien & Datenordner](#-dateien--datenordner)
9. [Troubleshooting](#-troubleshooting)

---

## 🚀 Build & erste Schritte

### Voraussetzungen

- Windows 10/11 oder Windows Server
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installiert
- Internetverbindung

### Build

1. ZIP entpacken
2. Doppelklick auf `build.bat`
3. Nach erfolgreichem Build liegt `CreatorDesktop.exe` im Ordner `bin\Release\net8.0-windows\win-x64\publish\`

### Erster Start

Die EXE ist self-contained — du brauchst kein weiteres .NET zur Laufzeit. Beim ersten Start:

- Wird `yt-dlp.exe` automatisch heruntergeladen (~11 MB von GitHub)
- Wird `ffmpeg.exe` automatisch heruntergeladen (~80 MB)
- Settings-Ordner wird angelegt unter:
  ```
  C:\Users\<DEIN-USER>\AppData\Local\CreatorDesktop\
  ```

---

## 📑 Tab-Übersicht

| Tab | Zweck |
|---|---|
| **Create Video** | Einzelnes Video erstellen, CC-Videos suchen + downloaden + uploaden |
| **Auto-Modus** | Themen-Queue + Scheduler, Konkurrenz-Tracker, saisonale Vorschläge |
| **Wish** | User-Wünsche aus deinem Discord-Channel |
| **Backup** | Backup + Restore aller Konfigurations-Dateien |
| **Settings** | Alle API-Keys + Verhaltens-Einstellungen |

---

## 🔑 API-Keys & Zugangsdaten beschaffen

### 1. AI-Provider (Pflicht für Auto-Mode)

Wähle einen — der wird für Skript-Generierung + AI-Antworten + Übersetzungen genutzt.

**Anthropic Claude** *(Empfehlung)*
- Konto bei https://console.anthropic.com erstellen
- "API Keys" → "Create Key" → kopieren
- Default-Modell: `claude-sonnet-4-6`

**OpenAI**
- https://platform.openai.com/api-keys
- "Create new secret key" → kopieren
- Default-Modell: `gpt-4o`

**Google Gemini**
- https://aistudio.google.com/apikey
- "Create API key" → kopieren
- Default-Modell: `gemini-2.0-flash`

### 2. YouTube OAuth (Pflicht für Upload)

1. https://console.cloud.google.com/ → Neues Projekt erstellen
2. "APIs & Services" → "Bibliothek" → "YouTube Data API v3" aktivieren
3. "APIs & Services" → "Bibliothek" → "YouTube Analytics API" aktivieren *(für Performance-Check)*
4. "APIs & Services" → "OAuth-Zustimmungsbildschirm" einrichten (External, Test-Modus)
5. Bei Scopes diese Scopes hinzufügen:
   - `https://www.googleapis.com/auth/youtube`
   - `https://www.googleapis.com/auth/youtube.upload`
   - `https://www.googleapis.com/auth/youtube.force-ssl`
   - `https://www.googleapis.com/auth/yt-analytics.readonly`
6. Test-User hinzufügen → deine Gmail-Adresse
7. "Anmeldedaten" → "OAuth-Client-ID erstellen" → Typ: **Desktop-App**
8. Client-ID + Client-Secret kopieren

### 3. ElevenLabs *(optional, für AI-Voiceover)*

- https://elevenlabs.io → Profil → API Keys → kopieren
- Voice-ID findest du unter https://elevenlabs.io/voice-library

### 4. Pexels *(optional, für B-Roll bei Pipeline-Mode)*

- https://www.pexels.com/api → kostenlos registrieren → API-Key kopieren

### 5. Reddit *(optional, für Cross-Posting)*

1. https://www.reddit.com/prefs/apps → unten "create another app..."
2. Typ: **script**
3. Redirect URI: `http://localhost`
4. Nach "create app":
   - **Client-ID**: kurze Zeichenkette direkt unter "personal use script"
   - **Client-Secret**: Zeichenkette neben "secret"
5. Reddit-Username + Passwort: deine normalen Login-Daten
6. **Wichtig**: 2FA muss aus sein, oder du nutzt ein App-Passwort

### 6. Telegram *(optional, für Kanal-Cross-Posting)*

1. In Telegram an **@BotFather** schreiben
2. `/newbot` → Name eingeben → Token kopieren
3. Telegram-Kanal öffnen → Verwalten → Administratoren → Bot hinzufügen mit Recht "Nachrichten senden"
4. Beliebige Nachricht in den Kanal posten
5. Im Browser öffnen: `https://api.telegram.org/bot<DEIN-TOKEN>/getUpdates`
6. Chat-ID suchen: `"chat":{"id":-1001234567890,...}` (negative Zahl bei Kanälen!)

### 7. Discord-Bot *(für Wish-Channel)*

1. https://discord.com/developers/applications → "New Application" → Name
2. Reiter "Bot" → "Reset Token" → Token kopieren
3. Reiter "OAuth2 → URL Generator":
   - Scope: ☑ `bot`
   - Permissions: ☑ `Read Messages/View Channels`, ☑ `Read Message History`
4. URL kopieren → im Browser öffnen → Bot zu deinem Server hinzufügen
5. In Discord: Einstellungen → Erweitert → **Entwicklermodus an**
6. Rechtsklick auf deinen Wunsch-Channel → "Channel-ID kopieren"

### 8. Discord-Webhook *(für Upload-Posts)*

Viel einfacher als ein Bot:

1. Discord-Channel öffnen → Zahnrad → "Integrationen" → "Webhooks" → "Neuer Webhook"
2. Name + Avatar setzen → URL kopieren
3. Fertig — kein Token, keine Scopes

### 9. TikTok

Kein Setup nötig. Tool exportiert vertikale Videos in einen Ordner — du lädst sie manuell hoch (TikTok-API verlangt App-Approval, die für Privatpersonen nicht praktikabel ist).

---

## ⚙️ Settings-Tab: alle Optionen

### AI-Provider
- Provider wählen (Anthropic / OpenAI / Gemini)
- API-Key + Modell-Name eintragen

### YouTube
- Client-ID + Client-Secret eintragen
- **"Mit YouTube verbinden"** klicken → Browser öffnet sich → einloggen → erlauben
- Status-Label zeigt "verbunden" wenn alles passt

### Auto-Kategorie
- ☑ Kategorie automatisch erkennen → Tool wählt YouTube-Kategorie aus Titel/Topic-Keywords

### Auto-Kommentar
- ☑ Automatisch ersten Kommentar nach Upload posten
- Template: z.B. `Was hat dich am meisten überrascht? 👇`

### Auto-Optimierung
- ☑ Schwache Videos nach 24h erkennen → Tray-Tip-Warnung bei < 50 Views

### Auto-Reply
- ☑ Automatisch auf neue Kommentare antworten (Template)
- ☑ AI-generierte Antworten statt Template (braucht AI-Provider)

### Reddit Cross-Posting
- Client-ID + Secret + Username + Passwort
- Subreddits: Komma-getrennt, ohne `r/`
- ☑ Nach Upload automatisch posten

### TikTok-Export
- ☑ Vertikale Version (1080x1920) exportieren
- ☑ TikTok-Upload-Seite im Browser öffnen (nur beim ersten Video)
- Export-Ordner (leer = `OutputFolder/tiktok`)

### Telegram
- Bot-Token (von @BotFather)
- Chat-ID (z.B. `-1001234567890`)
- Nachrichten-Template (Platzhalter `{title}`, `{url}`)
- ☑ Nach Upload posten

### Discord
- Bot-Token (für Wish-Channel)
- Wish-Channel-ID (Rechtsklick → Channel-ID kopieren)
- **Invite-Link** (z.B. `https://discord.gg/abc123`) — kommt automatisch unter jede Video-Beschreibung
- ☑ Wünsche automatisch alle X Stunden abholen

### Mehrsprachige Titel
- ☑ Aktivieren
- Zielsprachen (Komma): `en, es, tr, fr` (ISO-Codes)
- Video bleibt deutsch, Titel wird pro Land lokalisiert

### Discord-Webhook
- ☑ Nach Upload Embed im Channel posten
- Webhook-URL eintragen

### AI-Antworten
- ☑ Kontextbezogene Antworten statt fixem Template

### Trending-Detector
- ☑ Aktivieren
- ☑ Themen automatisch in Auto-Queue einreihen
- Intervall: Standard 12h

### Veröffentlichung planen
- ☑ Geplante Veröffentlichung statt sofort
- Tage ab heute: 1
- Uhrzeit: 15:00

### Sichtbarkeit
- public / unlisted / private (Default: public)

### Output-Folder
- Wohin Videos lokal gespeichert werden (Default: `Videos/CreatorDesktop`)

---

## 🎬 Workflow: Erstes Video

### Variante A — CC-Videos direkt 1:1 hochladen

1. **Create-Tab** öffnen
2. **Topic** wählen (Dropdown oder eigenes Wort tippen, z.B. "Haie")
3. ☑ "Nur Creative-Commons-Videos verwenden"
4. **"🔍 CC-Videos suchen"** klicken
5. Liste durchgehen, Häkchen setzen bei interessanten Videos
6. **"⬇ Markierte herunterladen"** klicken
7. Videos werden heruntergeladen + Thumbnails erstellt
8. **Playlists auswählen** (Häkchen setzen bei "Filme", "Geschichte" etc.)
9. **"📤 Auf YouTube hochladen"** klicken
10. Tool macht in Folge:
    - Lizenz-Check (überspringt nicht-CC)
    - Upload mit allen Metadaten + Thumbnail
    - Eintragen in markierte Playlists
    - Auto-Kommentar posten
    - Reddit/Telegram/Discord-Cross-Post
    - TikTok-Version exportieren
    - Lokale Datei löschen

### Variante B — Komplettes Video mit AI-Skript

1. Topic eingeben (z.B. "Mondlandung")
2. ☐ "Nur CC" abwählen
3. Dauer einstellen (z.B. 30 Min)
4. **"▶ Video erzeugen"** klicken
5. Tool:
   - AI generiert Skript + Szenen
   - ElevenLabs erstellt Voiceover
   - Pexels lädt B-Roll
   - FFmpeg rendert finales Video
6. Danach manuell auf Upload klicken (oder ☑ Auto-Upload)

---

## 🤖 Auto-Mode (24/7-Betrieb)

Empfohlen für deinen Windows-Server.

### Setup

1. **Auto-Modus-Tab** öffnen
2. **Channel-Theme** eingeben (z.B. "interessante historische Ereignisse")
3. **Themen-Queue** befüllen (zeilenweise oder per "✨ Themen generieren")
4. ☑ "Scheduler aktiv"
5. Intervall einstellen (Standard: 24h = 1 Video/Tag)
6. **"Auto-Modus starten"** klicken

### Konkurrenz-Tracker

1. **"+ Kanal hinzufügen"** → URL oder Kanal-ID einfügen
2. ☑ "Tracker aktiv" + Intervall (Standard 6h)
3. Tool checkt RSS-Feeds aller Kanäle (kostenlos, kein API-Quota)
4. Neue Videos werden **fett** dargestellt
5. **"🔍 Lizenzen prüfen"** prüft jedes Video: 🟢 CC / 🟡 Standard / 🔴 gesperrt
6. **"⬇ Markierte herunterladen"** lädt grüne + gelbe Videos in deinen Output-Ordner

### Saisonale Themen

- Automatischer Kalender-Check (Hai-Sommer, Halloween, Mondlandung-Jahrestag etc.)
- ☑ "Treffer-Themen automatisch in Auto-Queue einreihen"
- **"Datei öffnen"** → bearbeite `seasonal.json` selbst für eigene Jahrestage

### Wish-Tab

- **"🔄 Wünsche von Discord abholen"** → Bot liest deinen Channel
- Jeder Wunsch wird auf YouTube gesucht → bei Treffern landen Videos hier
- Doppelklick → Detail-Dialog mit Treffern + "In Auto-Queue einreihen"-Button

---

## 💾 Backup & Wiederherstellung

### Manuell

1. **Backup-Tab** öffnen
2. Optional: Backup-Ordner setzen (Default: `AppData/CreatorDesktop/backups`)
3. **"💾 Jetzt Backup erstellen"** → ZIP mit allen Konfigs

### Automatisch *(empfohlen)*

- ☑ "Automatisches Backup aktivieren"
- Intervall: Standard 24h
- Alte Backups löschen nach 30 Tagen

### Wiederherstellen

- **"📂 Backup wiederherstellen"** → ZIP wählen → App neu starten
- Oder: Doppelklick auf Backup in der Liste

### Was wird gesichert

- `settings.json` (API-Keys + alle Einstellungen)
- `playlists.json` (deine Playlist-Liste)
- `competitors.json` (verfolgte Konkurrenz-Kanäle)
- `wishes.json` (Discord-Wünsche-Historie)

---

## 📁 Dateien & Datenordner

Alle Konfigurations-Dateien liegen unter:

```
C:\Users\<USER>\AppData\Local\CreatorDesktop\
```

| Datei | Inhalt | Editierbar |
|---|---|---|
| `settings.json` | API-Keys + alle Settings | Vorsichtig |
| `playlists.json` | Deine YouTube-Playlists (Name + ID) | ✅ Ja |
| `competitors.json` | Konkurrenz-Kanäle | ✅ Ja |
| `wishes.json` | Discord-Wünsche-Historie | ✅ Ja |
| `seasonal.json` | Saisonaler Themen-Kalender | ✅ Ja |
| `yt-dlp.exe` | YouTube-Downloader (auto-DL) | ❌ Nein |
| `ffmpeg\ffmpeg.exe` | Video-Tool (auto-DL) | ❌ Nein |
| `backups\*.zip` | Auto-Backups | — |

**Videos landen** standardmäßig in:
```
C:\Users\<USER>\Videos\CreatorDesktop\
```

---

## 🛠️ Troubleshooting

### Build-Fehler "PadY does not exist"
Veralteter Quellcode — neueste ZIP nutzen.

### "Insufficient Permission" beim Kommentar/Playlist
OAuth-Scopes nicht komplett. Im Settings-Tab **"Trennen"** klicken, dann **"Mit YouTube verbinden"** — beim Re-Auth alle Scopes erlauben.

### "Thumbnail-Upload fehlgeschlagen 429 (Too Many Requests)"
YouTube limitiert Thumbnail-Uploads auf ~30/Tag pro Kanal. Tool versucht 3x mit Backoff. Bei Erschöpfung: Video ist trotzdem online, nur ohne Custom-Thumbnail.

### Reddit "WRONG_PASSWORD" oder 401
2FA muss aus sein. Oder App-Passwort in Reddit-Settings generieren und das nutzen.

### Discord-Wish-Bot liest keine Nachrichten
- Bot muss als **Mitglied** im Server sein (OAuth-URL einmalig durchlaufen)
- Bot braucht Channel-Berechtigung **"Nachrichten lesen"**
- Channel-ID kopiert? (Entwicklermodus an?)

### Telegram "Bad Request: chat not found"
- Chat-ID ist negativ bei Kanälen (z.B. `-1001234567890`)
- Bot muss als Admin im Kanal sein

### Trending-Check findet nichts
- Konkurrenz-Tracker oder Themen-Queue muss Keywords enthalten
- yt-dlp ist auto-installiert, prüfe ob `AppData/CreatorDesktop/yt-dlp.exe` da ist

### Konkurrenz-Tracker findet Kanal nicht
- URL muss zu YouTube-Kanal-Seite zeigen (z.B. `https://youtube.com/@kurzgesagt`)
- Oder Kanal-ID direkt (`UCsXVk37bltHxD1rDPwtNM8Q`)

### App stürzt beim Start ab
- Settings-Datei korrupt? Lösche `settings.json` → App startet mit Defaults
- Oder Backup vom Backup-Tab wiederherstellen

### Wo logge ich was die App macht?
- **Create-Tab**: Log-Box rechts
- **Auto-Tab**: Auto-Modus-Log unten
- **Settings/Tray**: Status-Label am unteren Fensterrand

---

## ⚠️ Rechtliche Hinweise

### Creative Commons (CC)
- Nur **CC-BY**-Videos darfst du wiederverwenden + auf deinem Kanal hochladen
- Tool macht automatisch korrekte Namensnennung in der Beschreibung (Original-Kanal + Link)
- Tool prüft Lizenz per yt-dlp — bei "Standard YouTube-Lizenz" warnt es vor Upload

### Music Content-ID
- CC-Videos können trotzdem Content-ID-Ansprüche auf Musik haben
- Bei Anspruch: in YouTube Studio annehmen oder anfechten — kein Strike-Risiko

### Auto-Kommentare / AI-Antworten
- Legal, solange du als Kanal-Betreiber antwortest
- Tool spammt nicht — antwortet nur auf echte User-Kommentare
- Bei Sorgen: AI-Antworten ausschalten, nur Template nutzen

### Datenschutz
- Alle Daten + API-Keys liegen **lokal** in `AppData`
- Keine Telemetrie, keine Cloud, keine Tracker
- Backups gehen in deinen lokalen Ordner

---

## 🔗 Hilfreiche Links

- yt-dlp: https://github.com/yt-dlp/yt-dlp
- FFmpeg: https://ffmpeg.org
- YouTube Data API: https://developers.google.com/youtube/v3
- Reddit API: https://www.reddit.com/dev/api
- Telegram Bot API: https://core.telegram.org/bots/api
- Discord Developer: https://discord.com/developers/applications

---

**Viel Erfolg mit deinem Kanal! 🎬🚀**

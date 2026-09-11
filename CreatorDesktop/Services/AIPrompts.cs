using System.Text.Json;
using CreatorDesktop.Models;

namespace CreatorDesktop.Services;

/// <summary>
/// Shared prompts and response-parsing used by ClaudeService, OpenAIService, GeminiService.
/// </summary>
internal static class AIPrompts
{
    public static string ScriptPrompt(string topic, string style, string language, int targetSeconds, string? keywords = null)
    {
        var langName = language switch { "de" => "Deutsch", "en" => "English", _ => language };
        return $$"""
        Du bist ein professioneller YouTube-Video-Skriptautor.
        Erstelle ein {{style}}-Video auf {{langName}} zum Thema: "{{topic}}".
        Ziel-Laenge: ca. {{targetSeconds}} Sekunden.

        Antworte AUSSCHLIESSLICH mit gueltigem JSON im folgenden Format (KEIN Markdown, KEINE Erklaerung):

        {
          "title": "Catchy Video Title (max 60 Zeichen)",
          "description": "YouTube-Beschreibung 150-300 Zeichen",
          "tags": ["tag1", "tag2", "tag3", "tag4", "tag5"],
          "scenes": [
            {
              "order": 1,
              "title": "Kurzer Szenentitel",
              "narration": "Der gesprochene Text dieser Szene. 1-3 Saetze.",
              "brollQuery": "english search term for stock footage",
              "durationSeconds": 10
            }
          ]
        }

        Wichtig:
        - 4 bis 8 Szenen, Summe der Sekunden ~= {{targetSeconds}}.
        - brollQuery muss IMMER auf Englisch sein (Pexels-Suche).
        - narration ist der gesprochene Text, klar und natuerlich.
        - Beginne mit einem starken Hook in der ersten Szene.
        """;
    }

    public static string TopicsPrompt(string channelTheme, int count, string language)
    {
        var langName = language switch { "de" => "Deutsch", "en" => "English", _ => language };
        return $$"""
        Du bist YouTube-Content-Stratege.
        Schlage {{count}} konkrete Video-Themen fuer einen YouTube-Kanal vor.
        Kanal-Schwerpunkt: "{{channelTheme}}"
        Sprache: {{langName}}

        Anforderungen:
        - Jedes Thema in EINEM Satz formuliert (KEIN Titel-Stil, sondern Thema-Beschreibung)
        - Konkret genug fuer ein 60-Sekunden-Video
        - Interessant und klickwuerdig, aber nicht reisserischer Clickbait
        - Themenvielfalt: keine zwei Themen sollten zu aehnlich sein

        Antworte AUSSCHLIESSLICH mit gueltigem JSON, KEIN Markdown, KEIN Text drumherum:
        {"topics": ["thema 1", "thema 2", "..."]}
        """;
    }

    /// <summary>
    /// Stage 1 of long-form documentary generation: produces an outline with
    /// chapters but no narration yet (kept short to fit AI output tokens).
    /// </summary>
    public static string DocumentaryOutlinePrompt(string topic, string language, int targetSeconds, string research)
    {
        var langName = language switch { "de" => "Deutsch", "en" => "English", _ => language };
        var targetMinutes = Math.Max(1, targetSeconds / 60);
        var chapterCount = Math.Clamp(targetMinutes / 4, 5, 12);
        var wordsPerChapter = (targetSeconds * 150 / 60) / chapterCount;  // ~150 wpm

        var researchBlock = string.IsNullOrWhiteSpace(research)
            ? "(Keine Recherche-Materialien vorhanden — nutze dein eigenes Wissen.)"
            : "RECHERCHE-MATERIAL (aus YouTube-Transcripts):\n" + research;

        return $$"""
        Du bist Autor und Regisseur fuer langformatige YouTube-Dokumentationen.
        Erstelle die GLIEDERUNG fuer eine {{targetMinutes}}-minuetige Doku auf {{langName}}.
        Thema: "{{topic}}".

        {{researchBlock}}

        Antworte AUSSCHLIESSLICH mit gueltigem JSON (kein Markdown, kein Text drumherum):

        {
          "title": "Packender Titel max 70 Zeichen",
          "description": "YouTube-Beschreibung 250-500 Zeichen",
          "tags": ["tag1","tag2","tag3","tag4","tag5","tag6","tag7","tag8"],
          "chapters": [
            {
              "order": 1,
              "title": "Kapitel-Titel",
              "summary": "1-2 Saetze: was wird in diesem Kapitel inhaltlich erzaehlt",
              "brollQuery": "english search term for stock footage that fits this chapter"
            }
          ]
        }

        Anforderungen:
        - GENAU {{chapterCount}} Kapitel.
        - Pro Kapitel ~{{wordsPerChapter}} Woerter Erzaehltext (wird spaeter generiert; jetzt nur die Gliederung).
        - Kapitel 1 muss ein starker Hook sein. Letztes Kapitel: Fazit/Ausblick.
        - brollQuery IMMER auf Englisch, generisch genug dass Stock-Footage existiert (z.B. "ancient roman ruins", "neural network animation").
        - Inhaltlich auf den Recherche-Materialien aufbauen, NICHT erfinden.
        """;
    }

    /// <summary>
    /// Stage 2: produces the full narration text for ONE chapter, given the
    /// outline of the whole doc and the research material.
    /// </summary>
    public static string ChapterNarrationPrompt(string topic, string language, string chapterTitle,
        string chapterSummary, int chapterIndex, int totalChapters, int targetWords, string research)
    {
        var langName = language switch { "de" => "Deutsch", "en" => "English", _ => language };
        var researchBlock = string.IsNullOrWhiteSpace(research)
            ? ""
            : "\nRECHERCHE-MATERIAL:\n" + research + "\n";

        return $$"""
        Du schreibst ein Kapitel einer Doku auf {{langName}} zum Thema "{{topic}}".
        Kapitel {{chapterIndex}} von {{totalChapters}}: "{{chapterTitle}}"
        Inhalt des Kapitels: {{chapterSummary}}
        {{researchBlock}}
        Schreibe den GESPROCHENEN ERZAEHLTEXT fuer dieses Kapitel.

        Regeln:
        - Ca. {{targetWords}} Woerter.
        - Schreibstil: ruhig, sachlich, doku-typisch. Wie ein Off-Sprecher.
        - KEINE Kapitelnummern, KEINE Zwischenueberschriften, KEINE Regie-Anweisungen.
        - KEINE Markdown-Formatierung.
        - Liefere NUR den Fliesstext (kein JSON, kein "Hier ist..."). Direkt mit dem ersten gesprochenen Satz beginnen.
        - Bei Kapitel 1: starker Hook in den ersten 2 Saetzen.
        - Bei letztem Kapitel: Schlussreflexion / Ausblick.
        """;
    }

    public static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < 0 || end <= start)
            throw new InvalidOperationException("Keine JSON-Antwort vom KI-Modell:\n" + text);
        return text.Substring(start, end - start + 1);
    }

    public static VideoJob ParseScriptJob(string json, string topic, string style, string language, int targetSeconds)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var job = new VideoJob
        {
            Topic = topic,
            Style = style,
            Language = language,
            TargetDurationSeconds = targetSeconds,
            Title = root.GetProperty("title").GetString() ?? topic,
            Description = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : ""
        };
        if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            foreach (var t in tags.EnumerateArray())
                job.Tags.Add(t.GetString() ?? "");

        foreach (var s in root.GetProperty("scenes").EnumerateArray())
        {
            job.Scenes.Add(new Scene
            {
                Order = s.GetProperty("order").GetInt32(),
                Title = s.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Narration = s.GetProperty("narration").GetString() ?? "",
                BRollQuery = s.GetProperty("brollQuery").GetString() ?? topic,
                DurationSeconds = s.TryGetProperty("durationSeconds", out var ds) ? ds.GetInt32() : 10
            });
        }
        return job;
    }

    /// <summary>
    /// Parses the documentary outline JSON. Each chapter becomes a Scene with empty narration —
    /// the pipeline fills narration via a second AI call per chapter.
    /// </summary>
    public static VideoJob ParseOutline(string json, string topic, string language, int targetSeconds)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var job = new VideoJob
        {
            Topic = topic,
            Style = "documentary",
            Language = language,
            TargetDurationSeconds = targetSeconds,
            Title = root.GetProperty("title").GetString() ?? topic,
            Description = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : ""
        };
        if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            foreach (var t in tags.EnumerateArray())
                job.Tags.Add(t.GetString() ?? "");

        var chapters = root.GetProperty("chapters");
        var chapterCount = chapters.GetArrayLength();
        var perChapterSeconds = chapterCount > 0 ? targetSeconds / chapterCount : 60;

        foreach (var c in chapters.EnumerateArray())
        {
            job.Scenes.Add(new Scene
            {
                Order = c.GetProperty("order").GetInt32(),
                Title = c.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                // Use "summary" as a placeholder; real narration filled later.
                Narration = c.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                BRollQuery = c.TryGetProperty("brollQuery", out var b) ? b.GetString() ?? topic : topic,
                DurationSeconds = perChapterSeconds
            });
        }
        return job;
    }

    public static List<string> ParseTopics(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<string>();
        foreach (var item in doc.RootElement.GetProperty("topics").EnumerateArray())
        {
            var t = item.GetString();
            if (!string.IsNullOrWhiteSpace(t))
                result.Add(t.Trim());
        }
        return result;
    }
}

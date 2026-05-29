using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Consultation;

namespace PiedraAzul.Infrastructure.Services;

/// <summary>
/// Implementación de orientación médica con Google Gemini (Generative Language API).
/// Si no hay API key configurada o la IA falla, usa una recomendación de respaldo
/// basada en palabras clave para que la demo nunca se rompa.
/// </summary>
public class GeminiConsultationService : IConsultationAIService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public GeminiConsultationService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Especialidades disponibles ──────────────────────────────────────────
    private const string NaturalMedicine = "NATURAL_MEDICINE";
    private const string Chiropractic = "CHIROPRACTIC";
    private const string Optometry = "OPTOMETRY";
    private const string Physiotherapy = "PHYSIOTHERAPY";

    public async Task<ConsultationRecommendation> RecommendSpecialtyAsync(
        ConsultationRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var model = _config["Gemini:Model"] ?? "gemini-2.0-flash";

        // Sin API key → respaldo por palabras clave
        if (string.IsNullOrWhiteSpace(apiKey))
            return FallbackRecommendation(request);

        try
        {
            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = BuildPrompt(request) } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            var client = _httpFactory.CreateClient("Gemini");

            // Reintento con backoff para manejar rate limiting (429)
            HttpResponseMessage response = null!;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("X-goog-api-key", apiKey);
                httpRequest.Content = JsonContent.Create(payload);
                response = await client.SendAsync(httpRequest, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                // Si es 429 (TooManyRequests), espera y reintenta
                if ((int)response.StatusCode == 429 && attempt < 2)
                {
                    await Task.Delay(1000 * (attempt + 1), cancellationToken);
                    continue;
                }

                return FallbackRecommendation(request);
            }

            if (!response.IsSuccessStatusCode)
                return FallbackRecommendation(request);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
                return FallbackRecommendation(request);

            var ai = JsonSerializer.Deserialize<GeminiResult>(text, JsonOpts);
            if (ai is null || string.IsNullOrWhiteSpace(ai.Specialty))
                return FallbackRecommendation(request);

            var code = NormalizeSpecialty(ai.Specialty);
            var reasoning = string.IsNullOrWhiteSpace(ai.Reasoning)
                ? "Según lo que nos cuentas, este especialista es el más adecuado para ayudarte."
                : ai.Reasoning.Trim();

            return new ConsultationRecommendation(
                code,
                LabelFor(code),
                reasoning,
                Math.Clamp(ai.Confidence, 50, 99));
        }
        catch
        {
            return FallbackRecommendation(request);
        }
    }

    // ── Prompt ────────────────────────────────────────────────────────────────
    private static string BuildPrompt(ConsultationRequest r) => $$"""
        Eres un asistente de orientación de la clínica Piedra Azul, que ofrece estas especialidades:
        - NATURAL_MEDICINE (Terapia Natural): cansancio, estrés, dolores de cabeza, malestar general,
          problemas digestivos, defensas bajas y bienestar integral.
        - CHIROPRACTIC (Quiropraxia): dolor de espalda, cuello, columna, articulaciones y postura.
        - OPTOMETRY (Óptica): problemas de visión, molestias en los ojos, dolor de cabeza por la vista o lentes.
        - PHYSIOTHERAPY (Fisioterapia): rehabilitación, recuperación de lesiones, movilidad reducida y recuperación muscular.

        Un paciente respondió:
        - Zona o tipo de molestia: {{r.SymptomArea}}
        - Tiempo con la molestia: {{r.Duration}}
        - Intensidad: {{r.Intensity}}
        - En sus palabras: "{{r.Description}}"

        Recomienda UNA sola especialidad de la lista. Da una razón breve, cálida y en español sencillo
        (máximo 2 frases), tuteando al paciente. No des diagnóstico médico ni recetes medicamentos.

        Responde SOLO en JSON con este formato exacto:
        {"specialty":"<CÓDIGO>","reasoning":"<razón breve>","confidence":<número entre 0 y 100>}
        """;

    // ── Normalización de la especialidad devuelta por la IA ────────────────────
    private static string NormalizeSpecialty(string raw)
    {
        var s = raw.Trim().ToUpperInvariant();

        if (s.Contains("OPTOMETR") || s.Contains("OPTIC") || s.Contains("VIST") || s.Contains("OJO"))
            return Optometry;
        if (s.Contains("CHIRO") || s.Contains("QUIRO") || s.Contains("COLUMNA") || s.Contains("ESPALDA"))
            return Chiropractic;
        if (s.Contains("PHYSIO") || s.Contains("FISIO") || s.Contains("REHAB"))
            return Physiotherapy;
        if (s.Contains("NATURAL"))
            return NaturalMedicine;

        return NaturalMedicine;
    }

    private static string LabelFor(string code) => code switch
    {
        Optometry => "Óptica",
        Chiropractic => "Quiropraxia",
        Physiotherapy => "Fisioterapia",
        _ => "Terapia Natural"
    };

    // ── Respaldo sin IA: heurística por palabras clave ─────────────────────────
    private static ConsultationRecommendation FallbackRecommendation(ConsultationRequest r)
    {
        var text = $"{r.SymptomArea} {r.Description}".ToLowerInvariant();

        string code;
        int confidence = 58; // Muy bajo por defecto

        // Detectar especialidad y ajustar confianza según coincidencias claras
        if (ContainsAny(text, "ojo", "ojos", "vista", "ver", "visión", "vision", "lentes", "gafas", "borroso"))
        {
            code = Optometry;
            confidence = CountMatches(text, "ojo", "ojos", "vista", "borroso", "lentes") >= 2 ? 82 : 70;
        }
        else if (ContainsAny(text, "espalda", "cuello", "columna", "articulacion", "articulación", "postura", "lumbar", "cervical", "dolor"))
        {
            code = Chiropractic;
            confidence = CountMatches(text, "espalda", "cuello", "columna", "dolor") >= 2 ? 85 : 74;
        }
        else if (ContainsAny(text, "lesion", "lesión", "rehabilitar", "rehabilitación", "movilidad", "músculo", "musculo", "esguince", "fractura", "recuperar"))
        {
            code = Physiotherapy;
            confidence = CountMatches(text, "lesion", "lesión", "fractura", "esguince", "rehabilit") >= 2 ? 88 : 78;
        }
        else
        {
            code = NaturalMedicine;
            confidence = CountMatches(text, "cansancio", "estrés", "estres", "malestar", "débil", "debil") >= 2 ? 76 : 62;
        }

        return new ConsultationRecommendation(
            code,
            LabelFor(code),
            "Según lo que nos cuentas, te recomendamos comenzar con este especialista. Estamos aquí para acompañarte.",
            confidence);
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var t in terms)
            if (text.Contains(t))
                return true;
        return false;
    }

    private static int CountMatches(string text, params string[] terms)
    {
        int count = 0;
        foreach (var t in terms)
            if (text.Contains(t))
                count++;
        return count;
    }

    private record GeminiResult
    {
        public string? Specialty { get; init; }
        public string? Reasoning { get; init; }
        public int Confidence { get; init; }
    }
}

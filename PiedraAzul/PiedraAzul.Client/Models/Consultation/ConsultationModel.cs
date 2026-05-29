using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.Consultation;

/// <summary>
/// Respuestas del paciente en la consulta inteligente. Las tres primeras son
/// opciones tipo ABCD (opcionales); <see cref="Description"/> es el texto libre obligatorio.
/// </summary>
public class ConsultationModel
{
    public string? SymptomArea { get; set; }
    public string? Duration { get; set; }
    public string? Intensity { get; set; }

    [Required(ErrorMessage = "Cuéntanos un poco sobre lo que te ocurre.")]
    [MinLength(5, ErrorMessage = "Escribe al menos unas palabras para poder ayudarte.")]
    public string Description { get; set; } = "";
}

/// <summary>Recomendación devuelta por la IA del servidor.</summary>
public class ConsultationRecommendation
{
    public string RecommendedSpecialty { get; set; } = "";
    public string SpecialtyLabel { get; set; } = "";
    public string Reasoning { get; set; } = "";
    public int ConfidencePercent { get; set; }
}

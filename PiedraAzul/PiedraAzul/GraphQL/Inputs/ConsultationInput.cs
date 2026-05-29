namespace PiedraAzul.GraphQL.Inputs;

/// <summary>
/// Entrada de la consulta inteligente (anónima). Las tres primeras son respuestas
/// tipo ABCD; <see cref="Description"/> es el texto libre del paciente.
/// </summary>
public record ConsultationInput(
    string SymptomArea,
    string Duration,
    string Intensity,
    string Description
);

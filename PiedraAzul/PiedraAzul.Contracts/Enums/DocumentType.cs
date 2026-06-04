namespace PiedraAzul.Contracts.Enums
{
    /// <summary>
    /// Tipo de documento de identidad colombiano.
    /// </summary>
    public enum DocumentType
    {
        /// <summary>Cédula de ciudadanía (adultos colombianos).</summary>
        CC = 0,

        /// <summary>Tarjeta de identidad (menores de edad).</summary>
        TI = 1,

        /// <summary>Cédula de extranjería (extranjeros residentes).</summary>
        CE = 2,

        /// <summary>Pasaporte (documento internacional).</summary>
        PA = 3
    }
}

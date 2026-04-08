using Microsoft.Extensions.DependencyInjection;

namespace PiedraAzul.Infrastructure;

public static class DebugInfra
{
    public static void Test()
    {
        Console.WriteLine("🔥 DebugInfra.Test() fue llamado!");
        throw new InvalidOperationException("Test de debug");
    }
}
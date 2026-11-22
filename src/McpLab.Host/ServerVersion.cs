using System.Reflection;

namespace McpLab.Host;

internal static class ServerVersion
{
    public static string Current { get; } =
        typeof(ServerVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";
}

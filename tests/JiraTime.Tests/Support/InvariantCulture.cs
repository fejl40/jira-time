using System.Globalization;
using System.Runtime.CompilerServices;

namespace JiraTime.Tests.Support;

/// <summary>Runs the tests with the invariant culture, as Program.cs runs the app, whatever the machine's language settings are.</summary>
internal static class InvariantCulture
{
    [ModuleInitializer]
    internal static void Use()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}

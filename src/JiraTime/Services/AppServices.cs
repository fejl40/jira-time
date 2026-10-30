using JiraTime.Storage;
using Microsoft.AspNetCore.DataProtection;
using JiraTime.Services.Jira;
using JiraTime.Services.JiraLog;
using JiraTime.Services.SignIn;
using JiraTime.Services.Worklogs;

namespace JiraTime.Services;

/// <summary>
/// The app's services, registered once for Program.cs and the tests (which swap in their own clock, Jira and store).
/// The UI's own services (modals, motion, the shown period) are registered in Program.cs.
/// </summary>
public static class AppServices
{
    /// <summary>Registers storage, the token encryption, the signed-in user, the Jira client and the data services.</summary>
    /// <param name="dataDir">The data folder: documents, encryption keys and the Jira request log.</param>
    public static IServiceCollection AddJiraTime(this IServiceCollection services, string dataDir)
    {
        services.AddSingleton(TimeProvider.System);

        // Storage: JSON files on disk for now. Swap this line for a document database later.
        services.AddSingleton<IDocumentStore>(_ => new FileDocumentStore(Path.Combine(dataDir, "documents")));

        // Encrypts the Jira connection (incl. token) before it is put in the browser's localStorage.
        // The keys stay on the server, next to the data, so they survive restarts.
        var dataProtection = services.AddDataProtection()
            .SetApplicationName("JiraTime")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
        // On Windows the key file itself is encrypted to your Windows account.
        if (OperatingSystem.IsWindows())
            dataProtection.ProtectKeysWithDpapi();

        // Per browser tab: who is signed in (verified with Jira), and that user's data.
        services.AddScoped<CurrentUser>();
        services.AddScoped<JiraIdentity>();
        services.AddScoped<SignInSession>();
        services.AddSingleton<LegacyDataMigration>();
        services.AddScoped<RetryQueue>();
        services.AddScoped<WorklogSync>();
        services.AddScoped<TimeLog>();
        services.AddScoped<PeriodLoader>();
        services.AddTransient<IssueStatuses>();
        services.AddSingleton<RecentWorklogIssues>();
        services.AddScoped<WorkCalendarService>();
        services.AddScoped<EpicExportStore>();
        services.AddScoped<MonthExportLoader>();
        services.AddScoped<ProfilePictures>();
        services.AddSingleton<JiraIssueLookup>();

        // Every Jira request is logged per user and day (data/logs/jira) and told live to the user's open tabs.
        services.AddSingleton(sp => new JiraRequestLog(Path.Combine(dataDir, "logs", "jira"), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<JiraRequestFeed>();
        services.AddSingleton<JiraRequestRecorder>();
        services.AddScoped<MyJiraLog>();

        // One typed client for every Jira request: timeout, rate-limit retries, queue and logging (JiraResilience).
        services.AddJiraHttp();
        services.AddTransient<JiraClient>();
        services.AddTransient<JiraWorklogClient>();
        return services;
    }
}

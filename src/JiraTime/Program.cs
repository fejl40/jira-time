using JiraTime.Components;
using JiraTime.Services;
using JiraTime.Storage;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.GetFullPath(builder.Configuration["DataDirectory"] ?? "data", builder.Environment.ContentRootPath);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Storage: JSON files on disk for now. Swap this line for a document database later.
builder.Services.AddSingleton<IDocumentStore>(_ => new FileDocumentStore(Path.Combine(dataDir, "documents")));

// Encrypts the Jira connection (incl. token) before it is put in the browser's localStorage.
// The keys stay on the server, next to the data, so they survive restarts.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("JiraTime")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
// On Windows the key file itself is encrypted to your Windows account.
if (OperatingSystem.IsWindows())
    dataProtection.ProtectKeysWithDpapi();

// Per browser tab: who is signed in (verified with Jira), and that user's data.
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<JiraIdentity>();
builder.Services.AddScoped<SignInSession>();
builder.Services.AddScoped<JiraTime.Components.MotionScript>();
builder.Services.AddScoped<JiraTime.Components.Modals>();
builder.Services.AddSingleton<LegacyDataMigration>();
builder.Services.AddScoped<RetryQueue>();
builder.Services.AddScoped<WorklogSync>();
builder.Services.AddScoped<TimeLog>();
builder.Services.AddScoped<PeriodLoader>();
builder.Services.AddTransient<IssueStatuses>();
builder.Services.AddSingleton<RecentWorklogIssues>();
builder.Services.AddScoped<WorkCalendarService>();
builder.Services.AddScoped<EpicExportStore>();
builder.Services.AddScoped<MonthExportLoader>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<JiraIssueLookup>();
// Every Jira request is logged per user and day (data/logs/jira) and told live to the user's open tabs.
builder.Services.AddSingleton(sp => new JiraRequestLog(Path.Combine(dataDir, "logs", "jira"), sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<JiraRequestFeed>();
builder.Services.AddSingleton<JiraRequestRecorder>();
builder.Services.AddScoped<MyJiraLog>();
builder.Services.AddHttpClient<JiraClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient<JiraWorklogClient>(c => c.Timeout = TimeSpan.FromSeconds(30));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

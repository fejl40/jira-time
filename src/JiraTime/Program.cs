using JiraTime.Components;
using JiraTime.Services;
using JiraTime.Storage;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.GetFullPath(builder.Configuration["DataDirectory"] ?? "data", builder.Environment.ContentRootPath);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Storage: JSON files on disk for now. Swap this line for a document database later.
builder.Services.AddFileDocumentStore(Path.Combine(dataDir, "documents"));

// Encrypts the Jira connection (incl. token) before it is put in the browser's localStorage.
// The keys stay on the server, next to the data, so they survive restarts.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("JiraTime")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
// On Windows the key file itself is encrypted to your Windows account.
if (OperatingSystem.IsWindows())
    dataProtection.ProtectKeysWithDpapi();

builder.Services.AddScoped<ConnectionState>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<TimeEntryService>();
builder.Services.AddScoped<WorklogSync>();
builder.Services.AddSingleton<WorkCalendarService>();
builder.Services.AddHttpClient<JiraClient>(c => c.Timeout = TimeSpan.FromSeconds(30));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

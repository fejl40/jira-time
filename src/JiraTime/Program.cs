using System.Globalization;
using JiraTime.Components;
using JiraTime.Components.Modals;
using JiraTime.Components.Sheet;
using JiraTime.Services;

// Numbers are written with a decimal point whatever the machine's language settings are (7.5, never 7,5).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.GetFullPath(builder.Configuration["DataDirectory"] ?? "data", builder.Environment.ContentRootPath);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddJiraTime(dataDir);

// The UI's own services, per browser tab.
builder.Services.AddScoped<MotionScript>();
builder.Services.AddScoped<ModalService>();
builder.Services.AddTransient<ShownPeriod>(); // one per timesheet page

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

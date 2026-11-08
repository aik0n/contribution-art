using ContributionArt.Components;
using ContributionArt.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(PresetOptions.PredefinedFileName, optional: false, reloadOnChange: false);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddGitHubClient(builder.Configuration);
builder.Services.AddPresets(builder.Configuration);

builder.Services.AddSingleton<GitPusher>();

var app = builder.Build();

app.Services.GetRequiredService<IPresetStore>().ReloadUserPresets();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using Handog_AdminWeb.Components;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// ---- STEP 1: DEFINE ALL STATIC CONTENT MIRRORS FIRST ----
app.UseStaticFiles(); // Standard web assets folder (wwwroot)

// Map your direct path to look back into the Mobile Project's Image directory
var mobileImagesPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Handog_MobileApp", "Resources", "Images");

if (Directory.Exists(mobileImagesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mobileImagesPath),
        RequestPath = "/mobile-images"
    });
}

// ---- STEP 2: MAP THE WEB COMPONENT ROUTERS AFTER ASSETS ----
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
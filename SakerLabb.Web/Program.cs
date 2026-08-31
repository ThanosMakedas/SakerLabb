using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using SakerLabb.Web.Components;
using SakerLabb.Web.Data;
using SakerLabb.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ImportService>();

builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<TicketRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<FileService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.Services.GetRequiredService<Db>().Initialize();

// Sakerhetsheaders. Satts i OnStarting sa att de kommer med aven pa
// felsidor, dar svaret annars nollstalls och headers forsvinner.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        return Task.CompletedTask;
    });

    await next();
});

app.UseDeveloperExceptionPage();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseCors();

app.UseStaticFiles();
app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "files")),
    RequestPath = "/files"
});

app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>();

app.Run();

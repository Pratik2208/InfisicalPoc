var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Basic liveness check
app.MapGet("/", () => Results.Ok(new { status = "Infisical POC API is running" }));

// Verifies that a secret injected by Infisical (via env vars in CI, or your
// local shell / launchSettings when running manually) is reachable through
// .NET's standard IConfiguration pipeline. No Infisical SDK code needed here
// because environment variables are a built-in configuration source.
app.MapGet("/api/secret-check", (IConfiguration config) =>
{
    // The name of the secret key to look for can itself be overridden via
    // the SECRET_KEY_NAME env var/config value; defaults to DATABASE_PASSWORD.
    var secretKeyName = config["SECRET_KEY_NAME"] ?? "DATABASE_PASSWORD";
    var secretValue = config[secretKeyName];

    if (string.IsNullOrEmpty(secretValue))
    {
        return Results.NotFound(new
        {
            found = false,
            key = secretKeyName,
            message = "Secret not found in configuration/environment."
        });
    }

    // Never echo the actual secret value back — just prove it exists and
    // report its length so you can sanity-check it matches what you set in Infisical.
    return Results.Ok(new
    {
        found = true,
        key = secretKeyName,
        length = secretValue.Length
    });
});

app.Run();

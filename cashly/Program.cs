using System.Text;
using System.Text.Json.Serialization;
using cashly.src;
using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.Middleware;
using cashly.src.Services.Implementations;
using cashly.src.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ===================================
// CORS
// ===================================
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: MyAllowSpecificOrigins,
        policy =>
        {
            policy
                .WithOrigins(
                    "https://roberto-ingenito.ddns.net", // produzione
                    "http://localhost:3000" // sviluppo
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Importante per JWT cookies/auth
        }
    );
});

builder.Services.AddEndpointsApiExplorer();

// ===================================
// SWAGGER
// ===================================
builder.Services.AddSwaggerGen(c =>
{
    // Definisci lo schema di sicurezza (Security Definition)
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "Inserisci il token JWT in questo formato: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
        }
    );

    c.OperationFilter<SecurityRequirementsOperationFilter>();
});

// Aggiungi il DbContext ai servizi dell'applicazione (Dependency Injection).
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.MapEnum<TransactionType>("transaction_type"));
});

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();


// Aggiungi i controller
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Questa riga dice al serializzatore di trattare gli enum come stringhe
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Abilita l'autorizzazione per i controller
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Qui dici al middleware come deve validare il token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Valida la chiave di firma (il segreto)
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))
            ),

            // Valida l'issuer (chi ha emesso il token)
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            // Valida l'audience (per chi è il token)
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            // Valida la scadenza del token
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ===================================
// GESTIONE ECCEZIONI GLOBALE
// i middleware devono essere registrati il prima possibile nella pipeline
// ===================================
app.UseMiddleware<GlobalExceptionMiddleware>();

// ===================================
// PATH BASE: /cashly-api/
// Questo fa sì che tutte le route siano prefissate con /cashly-api/
// ===================================
app.UsePathBase("/cashly-api");

// ===================================
// FORWARDED HEADERS
// Necessario per gestire correttamente HTTPS dietro Nginx
// ===================================
app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
        // Accetta headers da qualsiasi proxy (dato che siamo in Docker network)
        KnownProxies = { },
        KnownIPNetworks = { },
    }
);

// ===================================
// SWAGGER - Configurato per funzionare con /cashly-api/
// ===================================
app.UseSwagger(c =>
{
    // Personalizza il path e il server URL per Swagger
    c.PreSerializeFilters.Add(
        (swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers = [new() { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}/cashly-api", Description = "API Server" }];
        }
    );
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/cashly-api/swagger/v1/swagger.json", "Cashly API V1");
    c.RoutePrefix = "swagger"; // Swagger sarà disponibile a /cashly-api/swagger
});

// ===================================
// CORS - Deve essere prima di Authentication/Authorization
// ===================================
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===================================
// MIGRAZIONI DATABASE
// Applica le migrazioni in sospeso all'avvio
// ===================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Applica le migrazioni in sospeso
        context.Database.Migrate();

        Console.WriteLine("Migrazioni applicate con successo.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Un errore è avvenuto durante l'applicazione delle migrazioni.");
    }
}

app.Run();

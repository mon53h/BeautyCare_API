using System.Text;
using BeautyCare_API.Data;
using BeautyCare_API.Repositorios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;

// ===========================================================
// 1️ BASE DE DATOS - SQL LOCAL / SQLITE EN PRODUCCIÓN
// ===========================================================
var sqlServerConnection = builder.Configuration.GetConnectionString("ConexionSql");
var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection")
                       ?? "Data Source=beautycare.db";

builder.Services.AddDbContext<AplicationsDbContext>(opt =>
{
    if (env.IsDevelopment())
    {
        //  DESARROLLO → SQL Server (tu máquina)
        opt.UseSqlServer(sqlServerConnection);
    }
    else
    {
        //  PRODUCCIÓN → SQLite (host)
        opt.UseSqlite(sqliteConnection);
    }
});

// ===========================================================
// 2️ CORS – Permitir Angular
// ===========================================================
const string CorsPolicy = "AllowAngularApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ===========================================================
// 3️ JWT – Autenticación y validación de tokens
// ===========================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key");
var issuer = jwtSection.GetValue<string>("Issuer") ?? "BeautyCare.API";
var audience = jwtSection.GetValue<string>("Audience") ?? "BeautyCare.Client";

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Falta Jwt:Key en appsettings.json.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();

// ===========================================================
// 4️ Swagger con Autenticación Bearer
// ===========================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BeautyCare_API",
        Version = "v1",
        Description = "API de BeautyCare (JWT + EF Core + Swagger)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===========================================================
// 5️ Repositorios
// ===========================================================
builder.Services.AddScoped<LoginRepository>();
builder.Services.AddScoped<UsuariosRepository>();
builder.Services.AddScoped<ClientesRepository>();
builder.Services.AddScoped<PersonalRepository>();
builder.Services.AddScoped<ServiciosRepository>();
builder.Services.AddScoped<CitasRepository>();
builder.Services.AddScoped<CitasServiciosRepository>();

// ===========================================================
// 6️ Construcción y ejecución
// ===========================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Migración automática: aplica a la BD del entorno actual
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AplicationsDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Test endpoints
app.MapGet("/", () => "✅ BeautyCare API está funcionando");
app.MapGet("/ping", () => Results.Ok("pong 🩷"));

app.Run();

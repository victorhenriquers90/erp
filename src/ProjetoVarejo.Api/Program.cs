using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjetoVarejo.Api.Auth;
using ProjetoVarejo.Api.Configuration;
using ProjetoVarejo.Api.Endpoints;
using ProjetoVarejo.Api.Middleware;
using ProjetoVarejo.Api.Services;
using ProjetoVarejo.Application.Auditing;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Repositories;
using ProjetoVarejo.Infrastructure.Services;
using FluentValidation;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Application.Validators;
using Serilog;
using StackExchange.Redis;
using Microsoft.Data.SqlClient;

// ── Plano B: suporte a Windows Service ──────────────────────────────────────
// Quando instalado como serviço, o processo não tem janela nem console.
// UseWindowsService() é no-op fora do Windows; não afeta desenvolvimento.
// O instalador passa --environment Development --urls http://0.0.0.0:5094 no binPath,
// o que faz o ASP.NET Core escutar em todas as interfaces e ignorar a checagem de
// chaves placeholder (adequado para deploy em rede local / intranet).
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(opts =>
    opts.ServiceName = "ProjetoVarejo Web");

// PHASE 11: Configure Serilog structured logging
LoggingConfiguration.ConfigureLogging(builder);

// Bloquear inicialização se chaves placeholder não foram trocadas.
// Em modo Development (incluindo o serviço Windows instalado pelo Plano B),
// esta checagem é ignorada — adequado para rede local / intranet.
if (!builder.Environment.IsDevelopment())
{
    var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "";
    var apiKeys = builder.Configuration.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();
    var insecureJwt = jwtKey.Contains("sua-chave-secreta") || jwtKey.Contains("${") || jwtKey.Length < 32;
    var insecureApi = apiKeys.Any(k => k.Contains("TROQUE-ESTA") || k.Contains("${"));
    if (insecureJwt || insecureApi)
    {
        Log.Fatal("SEGURANÇA: Chaves placeholder detectadas em ambiente {Env}. " +
                  "Configure JWT_SECRET_KEY e API_KEY antes de usar em produção.", builder.Environment.EnvironmentName);
        Environment.Exit(1);
    }
}

builder.Services.AddSingleton<SessaoApp>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sqlOpt => sqlOpt.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null))
       .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

// PHASE 10: Redis Configuration for Caching
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<CachingSettings>(builder.Configuration.GetSection("Caching"));

var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();
if (redisSettings?.Enabled == true && !string.IsNullOrEmpty(redisSettings.Connection))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisSettings.Connection);
        builder.Services.AddSingleton(redis);
        Log.Information("Redis connection established: {Connection}", redisSettings.Connection);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to connect to Redis - caching disabled");
        builder.Services.AddSingleton<IConnectionMultiplexer>(x => null!);
    }
}
else
{
    Log.Information("Redis caching is disabled in configuration");
}

// 🏗️ FASE 2: Dependency Inversion - Repository Pattern + Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// PHASE 3: Service Interfaces for Abstraction
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<IAutenticacaoService, AutenticacaoService>();
builder.Services.AddScoped<IVendaService, VendaService>();
builder.Services.AddScoped<ICaixaService, CaixaService>();
builder.Services.AddScoped<IFinanceiroService, FinanceiroService>();
builder.Services.AddScoped<INfceService, NfceService>();

// PHASE 2.5-3: NFCE Infrastructure Components (dependencies of NfceService)
builder.Services.AddSingleton<ProjetoVarejo.Infrastructure.Nfce.NfceXmlGenerator>();
builder.Services.AddSingleton<ProjetoVarejo.Infrastructure.Nfce.NfceAssinador>();
builder.Services.AddSingleton<ProjetoVarejo.Infrastructure.Nfce.SefazSpClient>();
builder.Services.AddSingleton<ProjetoVarejo.Infrastructure.Nfce.NfceCancelamentoBuilder>();
builder.Services.AddSingleton<ProjetoVarejo.Infrastructure.Nfce.NfceInutilizacaoBuilder>();

// PHASE 5: FluentValidation - Centralized Validation
builder.Services.AddScoped<IValidator<Usuario>, UsuarioValidator>();
builder.Services.AddScoped<IValidator<Produto>, ProdutoValidator>();
builder.Services.AddScoped<IValidator<Venda>, VendaValidator>();
builder.Services.AddScoped<IValidator<ItemVenda>, ItemVendaValidator>();
builder.Services.AddScoped<IValidator<PagamentoVenda>, PagamentoVendaValidator>();
builder.Services.AddScoped<IValidator<CaixaSessao>, CaixaSessionValidator>();

// Services without interfaces (can be added in future phases)
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<ProducaoGuardService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddSingleton<ApiKeyValidator>();

// ── Plano B: beacon UDP para descoberta automática na rede local ─────────────
builder.Services.AddHostedService<ProjetoVarejo.Api.Services.LanBeaconService>();

// PHASE 8: JWT Authentication & Authorization
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<ITokenService, TokenService>();

// PHASE 10: Token Service with caching support
if (redisSettings?.Enabled == true)
{
    builder.Services.AddScoped<ICachedTokenService, CachedTokenService>();
}

// PHASE 11: Audit Logging & Health Checks
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
});
HealthCheckEndpoints.AddHealthCheckServices(builder);

// Configure JWT Bearer Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = jwtSettings.ValidateIssuer,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = jwtSettings.ValidateAudience,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = jwtSettings.ValidateLifetime,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Serilog.Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                Serilog.Log.Debug("JWT token validado para usuário {Usuario}", userName);
                return Task.CompletedTask;
            }
        };
    });

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        // Role-based policies
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Administrador"));

        options.AddPolicy("AdminOrGerente", policy =>
            policy.RequireRole("Administrador", "Gerente"));

        // Permission-based policies
        options.AddPolicy("CanOpenPdv", policy =>
            policy.RequireClaim("Permissao", "AbrirPdv"));

        options.AddPolicy("CanCancelSale", policy =>
            policy.RequireClaim("Permissao", "CancelarVenda"));

        options.AddPolicy("CanAdjustInventory", policy =>
            policy.RequireClaim("Permissao", "AjustarEstoque"));

        options.AddPolicy("CanViewFinancials", policy =>
            policy.RequireClaim("Permissao", "VisualizarFinanceiro"));

        options.AddPolicy("CanApplyDiscount", policy =>
            policy.RequireClaim("Permissao", "AplicarDesconto"));

        options.AddPolicy("CanManageUsers", policy =>
            policy.RequireClaim("Permissao", "GerenciarUsuarios"));
    });
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PHASE 10: Response Compression for performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = new[]
    {
        "text/html", "text/css", "text/javascript", "text/plain",
        "application/json", "application/javascript", "application/xml"
    };
});

// 🔒 CORS Seguro: Apenas domínios específicos permitidos
// Lê lista de origens permitidas do appsettings.json
var corsPolicy = "ProjetoVarejoCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Dev: aceita qualquer origem local
            policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.IsLoopback;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count", "X-Page-Count");
        }
        else
        {
            // Produção: origens explícitas da config
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("X-Total-Count", "X-Page-Count");
        }
    });
});

var app = builder.Build();

// PHASE 11: Ensure database is created - Connect to master first to create ProjetoVarejo database
async Task EnsureDatabaseCreatedAsync()
{
    var connectionString = builder.Configuration.GetConnectionString("Default");
    var masterConnectionString = connectionString?.Replace("Database=ProjetoVarejo", "Database=master") ?? "";

    Log.Information("Creating database if not exists...");

    // Wait for SQL Server to be ready before attempting connection
    Log.Information("Waiting 15 seconds for SQL Server to initialize...");
    await Task.Delay(15000);

    // Retry logic: try up to 10 times with exponential backoff
    for (int attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            // Connect to master and create database if needed
            var connectionStringBuilder = new SqlConnectionStringBuilder(masterConnectionString);
            connectionStringBuilder.ConnectTimeout = 30;
            var masterConnectionStringWithTimeout = connectionStringBuilder.ConnectionString;

            using (var masterConnection = new SqlConnection(masterConnectionStringWithTimeout))
            {
                await masterConnection.OpenAsync();

                using (var command = masterConnection.CreateCommand())
                {
                    command.CommandTimeout = 30;
                    command.CommandText = @"
                        IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ProjetoVarejo')
                        BEGIN
                            CREATE DATABASE ProjetoVarejo;
                        END";
                    await command.ExecuteNonQueryAsync();
                }

                Log.Information("Database 'ProjetoVarejo' ensured to exist");
            }

            // Now connect to the actual database and create tables
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
                Log.Information("Database schema ensured created successfully");
            }

            return; // Success, exit function
        }
        catch (Exception ex)
        {
            int delayMs = (int)Math.Pow(2, attempt - 1) * 1000; // 1s, 2s, 4s, 8s, 16s, 32s, etc.

            if (attempt < 10)
            {
                Log.Warning(ex, "Failed to ensure database creation (attempt {Attempt}/10). Retrying in {DelayMs}ms...", attempt, delayMs);
                await Task.Delay(delayMs);
            }
            else
            {
                Log.Warning(ex, "Failed to ensure database creation after 10 attempts - continuing with startup");
            }
        }
    }
}

await EnsureDatabaseCreatedAsync();

// PHASE 10: Response compression middleware
app.UseResponseCompression();

// ── Plano B: servir Blazor WASM como interface web ──────────────────────────
// UseBlazorFrameworkFiles serve os arquivos do framework WASM (_framework/*).
// UseDefaultFiles mapeia "/" para "/index.html".
// UseStaticFiles serve wwwroot do projeto ProjetoVarejo.Web.
// MapFallbackToFile captura rotas SPA como /produtos, /estoque, etc.
app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(corsPolicy);

// ASP.NET Core Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Status da API (movido de "/" para não colidir com o index.html do WASM)
app.MapGet("/api/status", () => Results.Ok(new { app = "ProjetoVarejo.Api", versao = "1.0", utc = DateTime.UtcNow }))
   .AllowAnonymous()
   .WithName("Status")
   .WithOpenApi();

// PHASE 11: Health check endpoints
HealthCheckEndpoints.MapHealthCheckEndpoints(app);

// Existing endpoints
ProdutoEndpoints.Map(app);
EstoqueEndpoints.Map(app);
ClienteEndpoints.Map(app);
RelatorioEndpoints.Map(app);

// Endpoint families
app.MapVendasEndpoints();
app.MapCaixaEndpoints();
app.MapFinanceiroEndpoints();
app.MapFornecedoresEndpoints();
app.MapAuthEndpoints();
app.MapUsuariosEndpoints();

// ── Plano B: fallback SPA — rotas como /produtos resolvem para index.html ───
// Deve vir DEPOIS de todos os endpoints de API, pois captura qualquer rota restante.
app.MapFallbackToFile("index.html");

// Log application startup
Log.Information("ProjetoVarejo.Api starting - Environment: {Environment}", builder.Environment.EnvironmentName);

app.Run();

// Log graceful shutdown
Log.Information("ProjetoVarejo.Api shutting down");

public partial class Program { }

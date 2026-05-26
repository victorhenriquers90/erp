using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Api.Auth;
using ProjetoVarejo.Api.Endpoints;
using ProjetoVarejo.Application.Auditing;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SessaoApp>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
       .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<RelatorioService>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddSingleton<ApiKeyValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔒 CORS Seguro: Apenas domínios específicos permitidos
// Lê lista de origens permitidas do appsettings.json
var corsPolicy = "ProjetoVarejoCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:3000", "https://localhost:5173" }; // Padrão para dev

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-Total-Count", "X-Page-Count"); // Para paginação
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(corsPolicy);
app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/", () => Results.Ok(new { app = "ProjetoVarejo.Api", versao = "1.0", utc = DateTime.UtcNow }))
   .AllowAnonymous();

ProdutoEndpoints.Map(app);
EstoqueEndpoints.Map(app);
ClienteEndpoints.Map(app);
RelatorioEndpoints.Map(app);

app.Run();

public partial class Program { }

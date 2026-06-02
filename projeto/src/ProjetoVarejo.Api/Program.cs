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
builder.Services.AddScoped<VendaService>();
builder.Services.AddScoped<FinanceiroService>();
builder.Services.AddScoped<CaixaService>();
builder.Services.AddSingleton<ApiKeyValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/", () => Results.Ok(new { app = "ProjetoVarejo.Api", versao = "1.0", utc = DateTime.UtcNow }))
   .AllowAnonymous();

ProdutoEndpoints.Map(app);
EstoqueEndpoints.Map(app);
ClienteEndpoints.Map(app);
RelatorioEndpoints.Map(app);
VendaEndpoints.Map(app);
FinanceiroEndpoints.Map(app);
CaixaEndpoints.Map(app);
UsuarioEndpoints.Map(app);

app.Run();

public partial class Program { }

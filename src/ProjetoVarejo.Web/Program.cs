using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProjetoVarejo.Web;
using ProjetoVarejo.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Quando hospedado pelo ProjetoVarejo.Api (Plano B), o BaseAddress aponta
// automaticamente para o servidor correto na rede local — sem configuração manual.
// Em modo standalone (dev), o appsettings.json do wwwroot pode sobrescrever a URL.
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ProjetoVarejoApi>();

// Autenticação JWT com sessionStorage — fecha aba = desloga automaticamente
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<JwtAuthStateProvider>();

await builder.Build().RunAsync();

# PHASE 10, 11, 12: Performance, Monitoring, & Deployment

**Status:** Planning & Implementation  
**Target Duration:** 2-3 weeks  
**Objective:** Production-ready enterprise application with performance optimization, comprehensive monitoring, and CI/CD automation

---

# PHASE 10: Performance, Caching & Optimization

## Context

The API currently has **160+ integration tests** passing and is **feature-complete** with all endpoints. However, production performance is unknown:
- No caching strategy for frequently accessed data
- No database query optimization
- JWT validation happens on every request
- No response compression
- No API response caching

**PHASE 10 Objective:** Optimize API response times to <100ms for GET requests and <500ms for POST requests while reducing database load.

---

## PHASE 10: Implementation Strategy

### 1. Redis Integration for JWT Token Caching

**File:** `src/ProjetoVarejo.Api/Services/CachedTokenService.cs` (NEW)

```csharp
public interface ICachedTokenService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    Task InvalidateTokenAsync(string token);
    Task<string> GenerateAccessTokenAsync(Usuario usuario);
    Task<string> GenerateRefreshTokenAsync(Usuario usuario);
}

public class CachedTokenService : ICachedTokenService
{
    private readonly ITokenService _tokenService;
    private readonly IDistributedCache _cache;
    private const string TokenCacheKeyPrefix = "jwt:";
    private const int TokenCacheDurationSeconds = 3600; // 1 hour

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        // Check cache first
        var cacheKey = $"{TokenCacheKeyPrefix}{token.GetHashCode()}";
        var cachedPrincipal = await _cache.GetStringAsync(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedPrincipal))
        {
            // Deserialize from cache
            return DeserializePrincipal(cachedPrincipal);
        }

        // Validate token
        var principal = _tokenService.ValidateToken(token);
        if (principal != null)
        {
            // Cache the result
            var serialized = SerializePrincipal(principal);
            await _cache.SetStringAsync(cacheKey, serialized, 
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(TokenCacheDurationSeconds)
                });
        }

        return principal;
    }

    // ... other methods
}
```

**DI Registration:** `Program.cs`
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});
services.AddScoped<ICachedTokenService, CachedTokenService>();
```

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "CacheSettings": {
    "TokenCacheDurationSeconds": 3600,
    "QueryCacheDurationSeconds": 300,
    "ListCacheDurationSeconds": 60
  }
}
```

---

### 2. Response Caching for Read-Heavy Endpoints

**File:** `src/ProjetoVarejo.Api/Middleware/ResponseCachingMiddleware.cs` (NEW)

```csharp
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private const string CacheKeyPrefix = "response:";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "GET")
        {
            var cacheKey = $"{CacheKeyPrefix}{context.Request.Path}{context.Request.QueryString}";
            var cachedResponse = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResponse))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(cachedResponse);
                return;
            }

            // Intercept response and cache it
            var originalBody = context.Response.Body;
            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                await _next(context);

                if (context.Response.StatusCode == 200)
                {
                    memoryStream.Position = 0;
                    var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                    
                    // Cache GET responses for 60 seconds
                    await _cache.SetStringAsync(cacheKey, responseBody,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                        });

                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBody);
                }
                else
                {
                    await memoryStream.CopyToAsync(originalBody);
                }
            }
        }
        else
        {
            await _next(context);
        }
    }
}
```

**Registration:** `Program.cs`
```csharp
app.UseMiddleware<ResponseCachingMiddleware>();
```

---

### 3. Database Query Optimization

**Query Optimization Strategy:**

#### A. Add Indexes to Frequently Queried Columns

**File:** `src/ProjetoVarejo.Infrastructure/Data/AppDbContextModelBuilder.cs` (Update)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Usuario indexes
    modelBuilder.Entity<Usuario>()
        .HasIndex(u => u.Login).IsUnique().HasName("IX_Usuario_Login");
    
    modelBuilder.Entity<Usuario>()
        .HasIndex(u => u.Ativo).HasName("IX_Usuario_Ativo");

    // Venda indexes
    modelBuilder.Entity<Venda>()
        .HasIndex(v => v.CriadoEm).HasName("IX_Venda_CriadoEm");
    
    modelBuilder.Entity<Venda>()
        .HasIndex(v => v.Status).HasName("IX_Venda_Status");

    // Produto indexes
    modelBuilder.Entity<Produto>()
        .HasIndex(p => p.Codigo).IsUnique().HasName("IX_Produto_Codigo");
    
    modelBuilder.Entity<Produto>()
        .HasIndex(p => p.CategoriaId).HasName("IX_Produto_CategoriaId");

    // NotaFiscal indexes
    modelBuilder.Entity<NotaFiscal>()
        .HasIndex(n => n.ChaveAcesso).IsUnique().HasName("IX_NotaFiscal_ChaveAcesso");
    
    modelBuilder.Entity<NotaFiscal>()
        .HasIndex(n => n.Status).HasName("IX_NotaFiscal_Status");

    // LancamentoFinanceiro indexes
    modelBuilder.Entity<LancamentoFinanceiro>()
        .HasIndex(l => l.DataVencimento).HasName("IX_Lancamento_DataVencimento");
}
```

#### B. Implement Entity Projection for List Operations

**Pattern Example - VendaService:**

```csharp
// BEFORE: Loads entire Venda with all related data
public async Task<List<VendaDto>> ListarAsync(DateTime de, DateTime ate)
{
    return await _unitOfWork.Vendas
        .Query()
        .Where(v => v.CriadoEm >= de && v.CriadoEm <= ate)
        .Include(v => v.Usuario)
        .Include(v => v.ItensVenda)
        .ThenInclude(i => i.Produto)
        .ToListAsync();
}

// AFTER: Projects only needed fields
public async Task<List<VendaListDto>> ListarAsync(DateTime de, DateTime ate)
{
    return await _unitOfWork.Vendas
        .Query()
        .Where(v => v.CriadoEm >= de && v.CriadoEm <= ate)
        .Select(v => new VendaListDto
        {
            Id = v.Id,
            NumeroVenda = v.Id,
            DataCriacao = v.CriadoEm,
            Usuario = v.Usuario.Nome,
            Total = v.Total,
            Status = v.Status.ToString(),
            ItensCount = v.ItensVenda.Count
        })
        .ToListAsync();
}
```

#### C. Add AsNoTracking() for Read-Only Queries

```csharp
// Avoid EF change tracking for read-only operations
public async Task<List<Produto>> ListarAsync()
{
    return await _unitOfWork.Produtos
        .Query()
        .AsNoTracking()  // ← Added
        .ToListAsync();
}
```

#### D. Pagination (Already Implemented in PHASE 7)

Ensure all list endpoints use pagination:
```csharp
public async Task<PagedResult<T>> GetPagedAsync(int page = 1, int pageSize = 50)
{
    var query = _unitOfWork.Items.Query().AsNoTracking();
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<T>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

---

### 4. Response Compression

**File:** `src/ProjetoVarejo.Api/Program.cs`

```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/json" });
});

app.UseResponseCompression();
```

---

### 5. API Performance Benchmarks

**New File:** `tests/ProjetoVarejo.Tests/Performance/PerformanceBenchmarks.cs`

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[SimpleJob(warmupCount: 3, targetCount: 5)]
[MemoryDiagnoser]
public class ApiPerformanceBenchmarks
{
    private IntegrationTestFixture _fixture;
    private HttpClient _client;
    private string _adminToken;

    [GlobalSetup]
    public async Task Setup()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
        
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", 
            new { usuario = "admin", senha = "senha123" });
        var content = await loginResponse.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        _adminToken = content.Data.Token;
    }

    [Benchmark]
    public async Task GetVendas_FirstPage()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/vendas?page=1&pageSize=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task GetProducts_Cached()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/produtos?page=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task ValidateToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/vendas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
```

**Run benchmarks:**
```bash
dotnet run -c Release -- --runtests
```

---

## PHASE 10 Success Criteria

✅ Redis configured and running  
✅ JWT validation cached (avg <10ms response time)  
✅ GET endpoints cached (avg <50ms response time)  
✅ POST endpoints optimized (avg <200ms response time)  
✅ Database indexes created  
✅ Entity projection implemented for list operations  
✅ Response compression enabled  
✅ Benchmarks show <100ms GET, <500ms POST  
✅ Memory usage optimized  
✅ No N+1 queries  

---

---

# PHASE 11: Monitoring, Logging & Alerting

## Context

The API is now optimized but lacks visibility into:
- Request/response times in production
- Error rates and types
- Business metrics (sales, transactions, etc.)
- System health (CPU, memory, database)
- User activity and audit trails

**PHASE 11 Objective:** Implement comprehensive monitoring, distributed tracing, and alerting for production support.

---

## PHASE 11: Implementation Strategy

### 1. Structured Logging with Serilog

**File:** `src/ProjetoVarejo.Api/Program.cs` (Update)

```csharp
using Serilog;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        path: "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            SchemaName = "dbo",
            TableName = "Logs",
            AutoCreateSqlTable = true
        },
        columnOptions: GetColumnOptions())
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "ProjetoVarejo.Api")
    .CreateLogger();

builder.Host.UseSerilog();

// ... rest of configuration

static ColumnOptions GetColumnOptions()
{
    var columnOptions = new ColumnOptions();
    columnOptions.Store.Remove(StandardColumn.Properties);
    columnOptions.Store.Add(StandardColumn.LogEvent);
    columnOptions.AdditionalDataColumns = new Collection<DataColumn>
    {
        new DataColumn { ColumnName = "UserId", DataType = typeof(int) },
        new DataColumn { ColumnName = "RequestPath", DataType = typeof(string) },
        new DataColumn { ColumnName = "ResponseStatusCode", DataType = typeof(int) },
        new DataColumn { ColumnName = "Duration", DataType = typeof(long) },
    };
    return columnOptions;
}
```

### 2. Distributed Tracing with Application Insights

**File:** `src/ProjetoVarejo.Api/Program.cs` (Add to Services)

```csharp
services.AddApplicationInsightsTelemetry(builder.Configuration["APPINSIGHTS_CONNECTIONSTRING"]);

services.AddSingleton<ITelemetryInitializer, MyTelemetryInitializer>();

// Add custom activities
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSqlClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddConsoleExporter());
```

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

### 3. Request/Response Logging Middleware

**File:** `src/ProjetoVarejo.Api/Middleware/RequestResponseLoggingMiddleware.cs` (NEW)

```csharp
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, 
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Log request
        _logger.LogInformation(
            "HTTP Request: {Method} {Path} from {RemoteIP}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log response
                _logger.LogInformation(
                    "HTTP Response: {Method} {Path} {StatusCode} {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }
}
```

**Registration:**
```csharp
app.UseMiddleware<RequestResponseLoggingMiddleware>();
```

### 4. Business Metrics Tracking

**File:** `src/ProjetoVarejo.Api/Services/MetricsService.cs` (NEW)

```csharp
public interface IMetricsService
{
    void RecordSale(decimal totalAmount);
    void RecordPayment(decimal amount);
    void RecordInventoryMovement(int quantity, string type);
    void RecordApiError(string endpoint, int statusCode);
    Task<MetricsSnapshot> GetMetricsAsync();
}

public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly ActivitySource _activitySource;

    private int _totalSales;
    private decimal _totalSalesAmount;
    private int _failedRequests;

    public void RecordSale(decimal totalAmount)
    {
        _totalSales++;
        _totalSalesAmount += totalAmount;

        _logger.LogInformation(
            "Sale recorded: Amount={Amount}, TotalSales={Total}",
            totalAmount,
            _totalSales);

        using (var activity = _activitySource.StartActivity("RecordSale"))
        {
            activity?.SetTag("amount", totalAmount);
            activity?.SetTag("total_sales", _totalSales);
        }
    }

    public void RecordApiError(string endpoint, int statusCode)
    {
        _failedRequests++;
        _logger.LogWarning(
            "API Error: {Endpoint} {StatusCode}",
            endpoint,
            statusCode);
    }

    public async Task<MetricsSnapshot> GetMetricsAsync()
    {
        return new MetricsSnapshot
        {
            TotalSales = _totalSales,
            TotalSalesAmount = _totalSalesAmount,
            FailedRequests = _failedRequests,
            Timestamp = DateTime.UtcNow
        };
    }
}

public class MetricsSnapshot
{
    public int TotalSales { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int FailedRequests { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 5. Health Check Endpoints

**File:** `src/ProjetoVarejo.Api/Endpoints/HealthEndpoints.cs` (NEW)

```csharp
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/live", new HealthCheckOptions 
        { 
            Predicate = r => r.Tags.Contains("live") 
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions 
        { 
            Predicate = r => r.Tags.Contains("ready") 
        });
    }
}
```

**Registration:**
```csharp
services.AddHealthChecks()
    .AddCheck("database", async () =>
    {
        try
        {
            using var context = new AppDbContext();
            await context.Database.ExecuteSqlAsync($"SELECT 1");
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database connection failed");
        }
    }, tags: new[] { "live", "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis"), 
        tags: new[] { "ready" });

app.MapHealthEndpoints();
```

### 6. Audit Logging

**File:** `src/ProjetoVarejo.Application/Services/AuditService.cs` (NEW)

```csharp
public interface IAuditService
{
    Task LogAsync(AuditLog auditLog);
    Task<List<AuditLog>> GetLogsAsync(int usuarioId, DateTime de, DateTime ate);
}

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public async Task LogAsync(AuditLog auditLog)
    {
        auditLog.DataHora = DateTime.UtcNow;
        await _unitOfWork.AuditLogs.InsertAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Audit Log: User={UserId} Action={Acao} Entity={Entidade} EntityId={EntidadeId}",
            auditLog.UsuarioId,
            auditLog.Acao,
            auditLog.Entidade,
            auditLog.EntidadeId);
    }

    public async Task<List<AuditLog>> GetLogsAsync(int usuarioId, DateTime de, DateTime ate)
    {
        return await _unitOfWork.AuditLogs
            .Query()
            .Where(l => l.UsuarioId == usuarioId && 
                        l.DataHora >= de && 
                        l.DataHora <= ate)
            .OrderByDescending(l => l.DataHora)
            .ToListAsync();
    }
}
```

---

## PHASE 11 Success Criteria

✅ Structured logging to file and database  
✅ Distributed tracing with Application Insights  
✅ Request/response logging middleware  
✅ Business metrics tracking  
✅ Health check endpoints  
✅ Audit logging for sensitive operations  
✅ Performance metrics visible in Application Insights  
✅ Error tracking and alerting configured  
✅ Logs searchable and filterable  

---

---

# PHASE 12: CI/CD & Containerization

## Context

The API is now optimized and monitored but lacks:
- Automated deployment pipeline
- Container support for scaling
- Build automation and testing
- Production deployment process

**PHASE 12 Objective:** Implement CI/CD pipeline and containerization for cloud-ready deployment.

---

## PHASE 12: Implementation Strategy

### 1. Docker Configuration

**File:** `Dockerfile` (ROOT)

```dockerfile
# Multi-stage build for ProjetoVarejo API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /source

# Copy solution and project files
COPY ["ProjetoVarejo.sln", "./"]
COPY ["src/ProjetoVarejo.Shared/ProjetoVarejo.Shared.csproj", "src/ProjetoVarejo.Shared/"]
COPY ["src/ProjetoVarejo.Domain/ProjetoVarejo.Domain.csproj", "src/ProjetoVarejo.Domain/"]
COPY ["src/ProjetoVarejo.Application/ProjetoVarejo.Application.csproj", "src/ProjetoVarejo.Application/"]
COPY ["src/ProjetoVarejo.Infrastructure/ProjetoVarejo.Infrastructure.csproj", "src/ProjetoVarejo.Infrastructure/"]
COPY ["src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj", "src/ProjetoVarejo.Api/"]

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy published application
COPY --from=builder /app/publish .

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "ProjetoVarejo.Api.dll"]
```

### 2. Docker Compose for Local Development

**File:** `docker-compose.yml`

```yaml
version: '3.8'

services:
  # SQL Server Database
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  # Redis Cache
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

  # ProjetoVarejo API
  api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Initial Catalog=ProjetoVarejo;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true
      - ConnectionStrings__Redis=redis:6379
      - ASPNETCORE_URLS=http://+:8080
    ports:
      - "8080:8080"
    depends_on:
      sqlserver:
        condition: service_started
      redis:
        condition: service_healthy
    volumes:
      - ./logs:/app/logs

volumes:
  sqlserver_data:
  redis_data:

networks:
  default:
    name: projeto-varejo-network
```

**Start services:**
```bash
docker-compose up -d
docker-compose logs -f api
```

### 3. GitHub Actions CI/CD Pipeline

**File:** `.github/workflows/ci-cd.yml`

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  # Build and Test Job
  build-test:
    runs-on: ubuntu-latest
    
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: "YourPassword123!"
          ACCEPT_EULA: "Y"
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123! -Q 'SELECT 1' || exit 1"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 1433:1433

    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build -c Release --no-restore

    - name: Run Tests
      run: dotnet test -c Release --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost;Initial Catalog=ProjetoVarejo;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"

    - name: Collect Code Coverage
      run: dotnet test -c Release --collect:"XPlat Code Coverage" --logger "trx" -- RunConfiguration.DisableAppDomain=true
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost;Initial Catalog=ProjetoVarejo;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true"

    - name: Upload Test Results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: '**/TestResults/**'

    - name: Upload Coverage Reports
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage/coverage.cobertura.xml
        flags: unittests
        name: codecov-umbrella

  # Security Scanning Job
  security:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Run Trivy Vulnerability Scanner
      uses: aquasecurity/trivy-action@master
      with:
        scan-type: 'fs'
        scan-ref: '.'
        format: 'sarif'
        output: 'trivy-results.sarif'

    - name: Upload Trivy Results
      uses: github/codeql-action/upload-sarif@v2
      with:
        sarif_file: 'trivy-results.sarif'

  # Container Build and Push Job
  build-container:
    needs: [build-test, security]
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
    - uses: actions/checkout@v3

    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2

    - name: Log in to Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}

    - name: Extract Metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=semver,pattern={{version}}
          type=semver,pattern={{major}}.{{minor}}
          type=sha

    - name: Build and Push Docker Image
      uses: docker/build-push-action@v4
      with:
        context: .
        push: ${{ github.event_name != 'pull_request' }}
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}

  # Deployment Job
  deploy:
    needs: build-container
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    
    steps:
    - uses: actions/checkout@v3

    - name: Deploy to Azure Container Instances
      uses: azure/aci-deploy-action@v1
      with:
        resource-group: ${{ secrets.AZURE_RESOURCE_GROUP }}
        dns-name-label: ${{ secrets.AZURE_DNS_LABEL }}
        image: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
        ports: 8080
        environment-variables: |
          ConnectionStrings__DefaultConnection=${{ secrets.DB_CONNECTION_STRING }}
          ConnectionStrings__Redis=${{ secrets.REDIS_CONNECTION_STRING }}

    - name: Post Deployment Tests
      run: |
        echo "Running smoke tests..."
        curl -f http://${{ secrets.AZURE_DNS_LABEL }}.azurecontainer.io:8080/health || exit 1

    - name: Create Deployment Notification
      uses: 8398a7/action-slack@v3
      with:
        status: ${{ job.status }}
        text: 'Deployment to production completed'
        webhook_url: ${{ secrets.SLACK_WEBHOOK }}
      if: always()
```

### 4. Kubernetes Deployment (Optional - Advanced)

**File:** `k8s/deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: projeto-varejo-api
  labels:
    app: projeto-varejo-api

spec:
  replicas: 3
  selector:
    matchLabels:
      app: projeto-varejo-api

  template:
    metadata:
      labels:
        app: projeto-varejo-api

    spec:
      containers:
      - name: api
        image: ghcr.io/your-org/projeto-varejo:latest
        imagePullPolicy: Always
        
        ports:
        - containerPort: 8080
          name: http
        
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: projeto-varejo-secrets
              key: db-connection
        - name: ConnectionStrings__Redis
          valueFrom:
            secretKeyRef:
              name: projeto-varejo-secrets
              key: redis-connection
        
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
        
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"

---
apiVersion: v1
kind: Service
metadata:
  name: projeto-varejo-api-service

spec:
  type: LoadBalancer
  selector:
    app: projeto-varejo-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
    name: http

---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: projeto-varejo-api-hpa

spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: projeto-varejo-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### 5. Environment Configuration Files

**File:** `.env.example`

```env
# API Configuration
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080

# Database
ConnectionStrings__DefaultConnection=Server=sqlserver;Initial Catalog=ProjetoVarejo;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true

# Redis
ConnectionStrings__Redis=redis:6379

# Logging
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Microsoft=Warning

# Application Insights
APPINSIGHTS_CONNECTIONSTRING=InstrumentationKey=your-key-here

# JWT Settings
Jwt__SecretKey=your-secret-key-min-32-chars-long-here
Jwt__Issuer=ProjetoVarejo.Api
Jwt__Audience=ProjetoVarejo.Client
Jwt__ExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7
```

---

## PHASE 12 Success Criteria

✅ Dockerfile builds successfully  
✅ Docker Compose runs all services  
✅ GitHub Actions pipeline triggers on push  
✅ Build succeeds with all tests passing  
✅ Code coverage > 80% verified  
✅ Container image built and pushed to registry  
✅ Container deployed and accessible  
✅ Health checks passing  
✅ Automated deployment on main branch  
✅ Slack notifications on deployment  
✅ (Optional) Kubernetes deployment working  

---

---

## Overall Architecture - Final State

```
┌─────────────────────────────────────────────────────────────┐
│                    GitHub Actions CI/CD                      │
│  Push → Build → Test → Security Scan → Container → Deploy  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              Docker Container Registry (GHCR)               │
│           ProjetoVarejo API:latest (Multi-stage)            │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│           Production Environment (Azure/K8s)                │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ProjetoVarejo API (3 replicas, auto-scaling)        │  │
│  │  - Health checks (/health, /health/live, /health/ready) │ │
│  │  - Request/Response logging                          │  │
│  │  - Distributed tracing (Application Insights)        │  │
│  │  - Business metrics tracking                         │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  SQL Server Database (Managed)                       │  │
│  │  - Indexed for performance                           │  │
│  │  - Automated backups                                 │  │
│  │  - Audit log storage                                 │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Redis Cache (Managed)                               │  │
│  │  - JWT token caching                                 │  │
│  │  - Response caching                                  │  │
│  │  - Session management                                │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Application Insights (Monitoring)                   │  │
│  │  - Performance metrics                               │  │
│  │  - Error tracking                                    │  │
│  │  - Distributed tracing                               │  │
│  │  - Custom business metrics                           │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Sequence

### Week 1: PHASE 10 (Performance)
- Day 1-2: Redis setup and JWT caching
- Day 3: Response caching middleware
- Day 4: Database optimization (indexes, projections)
- Day 5: Benchmarking and verification

### Week 2: PHASE 11 (Monitoring)
- Day 1: Structured logging with Serilog
- Day 2-3: Application Insights integration
- Day 4: Audit logging
- Day 5: Health checks and verification

### Week 3: PHASE 12 (CI/CD)
- Day 1: Docker setup and multi-stage builds
- Day 2: Docker Compose configuration
- Day 3: GitHub Actions pipeline
- Day 4: Container deployment
- Day 5: Kubernetes (optional) and smoke tests

---

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing (>80% coverage)
- [ ] Code review approved
- [ ] Security scan passed
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] Backup strategy in place
- [ ] Monitoring/alerting configured

### Deployment
- [ ] Blue-green deployment or canary release
- [ ] Database migrations applied
- [ ] Configuration updated for environment
- [ ] Health checks passing
- [ ] Logging verified
- [ ] Alerts configured

### Post-Deployment
- [ ] Smoke tests passed
- [ ] User-facing features verified
- [ ] Performance metrics normal
- [ ] Error rate low
- [ ] Rollback plan ready
- [ ] Incident response team notified

---

## Success Criteria (ALL PHASES)

✅ **API Response Times:** GET <100ms, POST <500ms  
✅ **Cache Hit Rate:** >80% for read operations  
✅ **Test Coverage:** >80% code coverage  
✅ **Logging:** All requests/errors logged with context  
✅ **Monitoring:** Real-time visibility into system health  
✅ **Deployment:** Fully automated CI/CD pipeline  
✅ **Availability:** 99.9% uptime SLA  
✅ **Security:** No vulnerabilities in container scan  
✅ **Scalability:** Auto-scaling based on demand  
✅ **Documentation:** Complete and updated  

---

**PHASE 10, 11, 12 Ready to Begin Implementation!** 🚀

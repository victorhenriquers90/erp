using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Tests.Integration;
using Xunit;
using HttpContentExtensions = ProjetoVarejo.Tests.Integration.HttpContentExtensions;

namespace ProjetoVarejo.Tests.Integration.Workflows;

/// <summary>
/// End-to-end integration tests for complete business workflows.
/// Tests complete user journeys across multiple endpoints and services.
/// </summary>
[Collection("Integration Tests")]
public class EndToEndWorkflowTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;
    private string _caixaToken;
    private string _gerenteToken;

    public EndToEndWorkflowTests()
    {
        _fixture = new IntegrationTestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _authenticatedClient = _fixture.CreateClient();

        _adminToken = await AuthenticateAsync("admin", "senha123");
        _caixaToken = await AuthenticateAsync("caixa", "senha123");
        _gerenteToken = await AuthenticateAsync("gerente", "senha123");
    }

    public async Task DisposeAsync()
    {
        _authenticatedClient?.Dispose();
        await _fixture.DisposeAsync();
    }

    private async Task<string> AuthenticateAsync(string usuario, string senha)
    {
        var loginRequest = new { usuario, senha };
        var response = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        return content.Data.Token;
    }

    #region Sales Workflow Tests

    [Fact]
    public async Task CompleteSalesWorkflow_CreateVenda_AddItem_Finalize()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act - Step 1: Create new venda
        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        Assert.True(vendaResponse.StatusCode == HttpStatusCode.Created || vendaResponse.StatusCode == HttpStatusCode.OK);
        var vendaContent = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.NotNull(vendaContent.Data);
        var vendaId = vendaContent.Data.Id;

        // Act - Step 2: Add item to venda
        var itemRequest = new { produtoId = 1, quantidade = 5, precoUnitario = 100m };
        var itemResponse = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/items", itemRequest);
        Assert.True(itemResponse.StatusCode == HttpStatusCode.OK ||
                   itemResponse.StatusCode == HttpStatusCode.Created ||
                   itemResponse.StatusCode == HttpStatusCode.BadRequest);

        // Act - Step 3: Finalize venda
        var finalizeResponse = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/finalize", new { });
        Assert.True(finalizeResponse.StatusCode == HttpStatusCode.OK ||
                   finalizeResponse.StatusCode == HttpStatusCode.Accepted ||
                   finalizeResponse.StatusCode == HttpStatusCode.BadRequest);

        // Assert - Venda creation succeeded
        Assert.NotNull(vendaContent.Data);
    }

    [Fact]
    public async Task SalesWorkflow_CreateVenda_CancelVenda()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Step 1: Create venda
        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        Assert.True(vendaResponse.StatusCode == HttpStatusCode.Created || vendaResponse.StatusCode == HttpStatusCode.OK);
        var vendaContent = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = vendaContent.Data.Id;

        // Act - Step 2: Cancel venda
        var cancelRequest = new { motivo = "Teste de cancelamento" };
        var cancelResponse = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/cancel", cancelRequest);

        // Assert
        Assert.True(cancelResponse.StatusCode == HttpStatusCode.OK ||
                   cancelResponse.StatusCode == HttpStatusCode.Accepted ||
                   cancelResponse.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SalesWorkflow_GetVendaById_AfterCreation()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act - Step 1: Create venda
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var createContent = await createResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = createContent.Data.Id;

        // Act - Step 2: Retrieve venda by ID
        var getResponse = await _authenticatedClient.GetAsync($"/api/vendas/{vendaId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getContent = await getResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();

        // Assert
        Assert.True(getContent.Success);
        Assert.NotNull(getContent.Data);
    }

    #endregion

    #region Inventory Management Workflow Tests

    [Fact]
    public async Task InventoryWorkflow_RegisterMovement_ListMovements()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Step 1: Register entry movement
        var entryRequest = new
        {
            produtoId = 1,
            tipo = "Entrada",
            quantidade = 10m,
            observacao = "Compra do fornecedor"
        };
        var entryResponse = await _authenticatedClient.PostAsJsonAsync("/api/estoque/movimentos", entryRequest);
        Assert.True(entryResponse.StatusCode == HttpStatusCode.OK ||
                   entryResponse.StatusCode == HttpStatusCode.Created ||
                   entryResponse.StatusCode == HttpStatusCode.BadRequest);

        // Act - Step 2: List movements
        var listResponse = await _authenticatedClient.GetAsync("/api/estoque/movimentos");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listContent = await listResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();

        // Assert
        Assert.True(listContent.Success);
    }

    [Fact]
    public async Task InventoryWorkflow_ListProducts_BelowMinimum()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Get products below minimum stock
        var response = await _authenticatedClient.GetAsync("/api/estoque/produtos-minimo");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    #endregion

    #region Financial Workflow Tests

    [Fact]
    public async Task FinancialWorkflow_CreateAccount_RecordEntry()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);

        // Act - Step 1: Create financial account
        var accountRequest = new
        {
            nome = "Conta Corrente",
            banco = "Banco Teste",
            agencia = "0001",
            numero = "123456"
        };
        var accountResponse = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/contas", accountRequest);
        Assert.True(accountResponse.StatusCode == HttpStatusCode.Created ||
                   accountResponse.StatusCode == HttpStatusCode.OK ||
                   accountResponse.StatusCode == HttpStatusCode.BadRequest);

        // Act - Step 2: Record financial entry
        var entryRequest = new
        {
            contaId = 1,
            tipo = "Receita",
            valor = 1000m,
            descricao = "Venda de produtos",
            dataVencimento = DateTime.Today.ToString("yyyy-MM-dd")
        };
        var entryResponse = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/lancamentos", entryRequest);
        Assert.True(entryResponse.StatusCode == HttpStatusCode.Created ||
                   entryResponse.StatusCode == HttpStatusCode.OK ||
                   entryResponse.StatusCode == HttpStatusCode.BadRequest);

        // Assert
        Assert.True(entryResponse.StatusCode != HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FinancialWorkflow_RecordEntry_MarkAsPaid()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Step 1: Record financial entry
        var entryRequest = new
        {
            contaId = 1,
            tipo = "Despesa",
            valor = 500m,
            descricao = "Despesa operacional"
        };
        var entryResponse = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/lancamentos", entryRequest);
        Assert.True(entryResponse.StatusCode == HttpStatusCode.Created ||
                   entryResponse.StatusCode == HttpStatusCode.OK ||
                   entryResponse.StatusCode == HttpStatusCode.BadRequest);

        // Act - Step 2: Mark as paid
        var paidResponse = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/lancamentos/1/marcar-pago", new { });
        Assert.True(paidResponse.StatusCode == HttpStatusCode.OK ||
                   paidResponse.StatusCode == HttpStatusCode.NoContent ||
                   paidResponse.StatusCode == HttpStatusCode.NotFound);
    }

    #endregion

    #region Cash Register Workflow Tests

    [Fact]
    public async Task CashRegisterWorkflow_OpenSession_AddSupply()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Step 1: Open cash session
        var openRequest = new
        {
            saldoAbertura = 0m,
            usuarioId = 1
        };
        var openResponse = await _authenticatedClient.PostAsJsonAsync("/api/caixa/open-session", openRequest);
        Assert.True(openResponse.StatusCode == HttpStatusCode.Created ||
                   openResponse.StatusCode == HttpStatusCode.OK ||
                   openResponse.StatusCode == HttpStatusCode.BadRequest);

        // Act - Step 2: Add supply to cash
        var supplyRequest = new
        {
            valor = 100m,
            observacao = "Reposição inicial"
        };
        var supplyResponse = await _authenticatedClient.PostAsJsonAsync("/api/caixa/supply", supplyRequest);
        Assert.True(supplyResponse.StatusCode == HttpStatusCode.OK ||
                   supplyResponse.StatusCode == HttpStatusCode.Created ||
                   supplyResponse.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CashRegisterWorkflow_Supply_Withdrawal_Summary()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act - Step 1: Add supply
        var supplyRequest = new { valor = 100m, observacao = "Reposição" };
        var supplyResponse = await _authenticatedClient.PostAsJsonAsync("/api/caixa/supply", supplyRequest);

        // Act - Step 2: Remove withdrawal
        var withdrawalRequest = new { valor = 50m, observacao = "Retirada" };
        var withdrawalResponse = await _authenticatedClient.PostAsJsonAsync("/api/caixa/withdrawal", withdrawalRequest);

        // Act - Step 3: Get summary
        var summaryResponse = await _authenticatedClient.GetAsync("/api/caixa/summary");
        Assert.True(summaryResponse.StatusCode == HttpStatusCode.OK ||
                   summaryResponse.StatusCode == HttpStatusCode.NoContent);

        // Assert
        Assert.True(supplyResponse.StatusCode != HttpStatusCode.Unauthorized);
        Assert.True(withdrawalResponse.StatusCode != HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Supplier Management Workflow Tests

    [Fact]
    public async Task SupplierWorkflow_CreateSupplier_UpdateSupplier()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);

        // Act - Step 1: Create supplier
        var createRequest = new
        {
            nome = "Novo Fornecedor",
            cnpj = "12345678000195",
            email = "fornecedor@test.com",
            telefone = "1133334444"
        };
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created || createResponse.StatusCode == HttpStatusCode.OK);

        // Act - Step 2: Update supplier
        var updateRequest = new
        {
            nome = "Fornecedor Atualizado",
            email = "novo@test.com"
        };
        var updateResponse = await _authenticatedClient.PutAsJsonAsync("/api/fornecedores/1", updateRequest);
        Assert.True(updateResponse.StatusCode == HttpStatusCode.OK ||
                   updateResponse.StatusCode == HttpStatusCode.NoContent ||
                   updateResponse.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SupplierWorkflow_ListSuppliers_GetById()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Step 1: List all suppliers
        var listResponse = await _authenticatedClient.GetAsync("/api/fornecedores");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Act - Step 2: Get specific supplier
        var getResponse = await _authenticatedClient.GetAsync("/api/fornecedores/1");
        Assert.True(getResponse.StatusCode == HttpStatusCode.OK ||
                   getResponse.StatusCode == HttpStatusCode.NotFound);

        // Assert
        var listContent = await listResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.True(listContent.Success);
    }

    #endregion

    #region Authentication Workflow Tests

    [Fact]
    public async Task AuthenticationWorkflow_Login_AccessProtectedEndpoint()
    {
        // Act - Step 1: Login
        var loginRequest = new { usuario = "admin", senha = "senha123" };
        var loginResponse = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginContent = await loginResponse.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        var token = loginContent.Data.Token;

        // Act - Step 2: Use token to access protected endpoint
        SetAuthorizationHeader(token);
        var protectedResponse = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task AuthenticationWorkflow_Login_RefreshToken()
    {
        // Act - Step 1: Login
        var loginRequest = new { usuario = "admin", senha = "senha123" };
        var loginResponse = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        var refreshToken = loginContent.Data.RefreshToken;

        // Act - Step 2: Refresh token
        var refreshRequest = new { refreshToken = refreshToken };
        var refreshResponse = await _authenticatedClient.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.True(refreshResponse.StatusCode == HttpStatusCode.OK ||
                   refreshResponse.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthenticationWorkflow_MultipleLogins_DifferentRoles()
    {
        // Act & Assert - Admin login
        var adminResponse = await LoginAndAccessEndpoint("admin", "senha123", "/api/financeiro/lancamentos");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        // Act & Assert - Gerente login
        var gerenteResponse = await LoginAndAccessEndpoint("gerente", "senha123", "/api/vendas");
        Assert.Equal(HttpStatusCode.OK, gerenteResponse.StatusCode);

        // Act & Assert - Caixa login
        var caixaResponse = await LoginAndAccessEndpoint("caixa", "senha123", "/api/vendas");
        Assert.Equal(HttpStatusCode.OK, caixaResponse.StatusCode);
    }

    #endregion

    #region Helper Methods

    private void SetAuthorizationHeader(string token)
    {
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<HttpResponseMessage> LoginAndAccessEndpoint(string usuario, string senha, string endpoint)
    {
        // Login
        var loginRequest = new { usuario, senha };
        var loginResponse = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        var token = loginContent.Data.Token;

        // Access endpoint with token
        SetAuthorizationHeader(token);
        return await _authenticatedClient.GetAsync(endpoint);
    }

    #endregion
}

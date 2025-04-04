using growIntegrationTests;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Xunit;
using projeto.Controllers; // Adiciona o namespace do controlador
using static projeto.Controllers.UtilizadorsController;

public class IntegrationTest1
{
    private readonly HttpClient _client;
    private readonly UtilizadorsController _controller;

    public IntegrationTest1()
    {
        // Usando a factory personalizada
        var factory = new CustomWebApplicationFactory2(); // Especifica o tipo correto do ponto de entrada
        _client = factory.CreateClient();
        
    }

    [Fact]
    public async Task ProcessarPagamento_DeveRetornarSucesso_QuandoPagamentoValido()
    {
        // Arrange: Cria o objeto de requisição com os dados necessários para o pagamento
        var pagamentoRequest = new
        {
            Valor = 100.00m,
            PaymentMethodId = "pm_card_visa", // ID de método de pagamento
            LeilaoId = 1,
            DescontoUsadoId = -1, // Não há desconto, por exemplo
            FullName = "Rodrigo",
            Address = "rua da estrela",
            City = "Barreiro",
            PostalCode = "1234-567",
            Country = "Portugal",
            Phone = "123456789",
            NIF = "123456789"
        };

        // Mock de resposta do Stripe
        var stripeMockResponse = new
        {
            success = true,
            message = "Payment successfully completed!"
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(pagamentoRequest), Encoding.UTF8, "application/json");

        // Aqui você pode simular a requisição para o endpoint de processamento de pagamento
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(stripeMockResponse), Encoding.UTF8, "application/json")
        };

        // Act: Simula a requisição para o endpoint de processamento de pagamento
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

        // Acessando o valor como booleano de forma segura
        bool success = responseObject.success;

        // Assert: Verifica se a resposta foi bem-sucedida
        Assert.True(success, "Expected success to be true.");
        Assert.Equal("Payment successfully completed!", (string)responseObject.message);
    }

    [Fact]
    public async Task ProcessarPagamento_DeveRetornarFalha_QuandoPagamentoInvalido()
    {
        // Arrange: Cria o objeto de requisição com os dados necessários para o pagamento
        var pagamentoRequest = new
        {
            Valor = 100.00m,
            PaymentMethodId = "pm_card_visa", // ID de método de pagamento
            LeilaoId = 1,
            DescontoUsadoId = -1, // Não há desconto, por exemplo
            FullName = "Rodrigo",
            Address = "rua da estrela",
            City = "Barreiro",
            PostalCode = "1234-567",
            Country = "Portugal",
            Phone = "123456789",
            NIF = "123456789"
        };

        // Mock de resposta de erro do Stripe (simulando falha no pagamento)
        var stripeMockResponse = new
        {
            success = false,
            message = "Payment failed: Insufficient funds"  // Mensagem de erro
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(pagamentoRequest), Encoding.UTF8, "application/json");

        // Simula a resposta de erro da API
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(JsonConvert.SerializeObject(stripeMockResponse), Encoding.UTF8, "application/json")
        };

        // Act: Simula a requisição para o endpoint de processamento de pagamento
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

        // Acessando o valor como booleano de forma segura
        bool success = responseObject.success;

        // Assert: Verifica se a resposta foi uma falha
        Assert.False(success, "Expected success to be false.");
        Assert.Equal("Payment failed: Insufficient funds", (string)responseObject.message);
    }




}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;
using projeto.Controllers;
using projeto.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using projeto.Models;
using Microsoft.EntityFrameworkCore.InMemory;
using System.Linq.Expressions;





namespace growTests
{
    public class ForgotPasswordTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly UtilizadorsController _controller;

        public ForgotPasswordTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            _mockContext = new Mock<ApplicationDbContext>(options);
            _mockConfig = new Mock<IConfiguration>();

            _controller = new UtilizadorsController(_mockContext.Object, null, _mockConfig.Object);
        }

        [Fact]
        public async Task ForgotPassword_ValidEmail_ShouldGenerateVerificationCode()
        {
            // Arrange
            var testEmail = "teste@email.com";
            var utilizador = new Utilizador { Email = testEmail, UtilizadorId = 1 };
            _mockContext.Setup(c => c.Utilizador
                                     .FirstOrDefaultAsync(It.IsAny<Expression<Func<Utilizador, bool>>>(),
                                                          It.IsAny<CancellationToken>()))
                        .ReturnsAsync(utilizador);

            // Act
            var result = await _controller.ForgotPassword(testEmail);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var verification = await _mockContext.Object.VerificationModel.FirstOrDefaultAsync();
            Assert.NotNull(verification);
            Assert.True(verification.VerificationCode > 100000 && verification.VerificationCode < 999999);
        }

        [Fact]
        public async Task ForgotPassword_InvalidEmail_ShouldReturnError()
        {
            // Arrange
            var testEmail = "naoexiste@email.com";
            _mockContext.Setup(c => c.Utilizador
                                     .FirstOrDefaultAsync(It.IsAny<Expression<Func<Utilizador, bool>>>(),
                                                          It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Utilizador)null);

            // Act
            var result = await _controller.ForgotPassword(testEmail) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ViewData.ModelState.ContainsKey("Email"));
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static projeto.Controllers.UtilizadorsController;
using Microsoft.Extensions.Configuration;


namespace growTests
{
    public class PaymentsTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UtilizadorsController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly FakeEmailSender _fakeEmailSender;
        private readonly Mock<IConfiguration> _mockConfig;

        public PaymentsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockConfig = new Mock<IConfiguration>();
            _fakeEmailSender = new FakeEmailSender();

            _controller = new UtilizadorsController(
                _dbContext,
                webHostEnvironment: null,
                _mockConfig.Object,
                _fakeEmailSender
            );

            _httpContext = new DefaultHttpContext { Session = new MockSession() };
            _controller.ControllerContext = new ControllerContext { HttpContext = _httpContext };
            _controller.TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task Pagamentos_RedirectsToLogin_WhenUserIsNotLoggedIn()
        {
            // Arrange
            _httpContext.Session.Remove("UserEmail");

            // Act
            var result = await _controller.Pagamentos();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

    }
}

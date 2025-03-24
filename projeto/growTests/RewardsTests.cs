﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using projeto.Controllers;
using projeto.Data;
using projeto.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using growTests;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace growTests
{
    public class RewardsTests
    {

        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private DescontosController GetControllerWithSession(ApplicationDbContext context, string userEmail)
        {
            var controller = new DescontosController(context);

            var httpContext = new DefaultHttpContext();

            var session = new MockSession();
            if (userEmail != null)
            {
                session.SetString("UserEmail", userEmail);
            }
            httpContext.Session = session;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task RedeemDesconto_UserNotLoggedIn_ShouldRedirectWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = GetControllerWithSession(context, null);

            // Act
            var result = await controller.RedeemDesconto(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("You need to be logged to reddem a discount.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task RedeemDesconto_NotEnoughPoints_ShouldRedirectWithError()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            var user = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "test",
                Email = "test@example.com",
                Password = "Pass$ord",
                Pontos = 5
            };

            var desconto = new Desconto
            {
                DescontoId = 1,
                Descricao = "10% Off",
                Valor = 10,
                PontosNecessarios = 10,
                IsLoja = true
            };

            context.Utilizador.Add(user);
            context.Desconto.Add(desconto);
            await context.SaveChangesAsync();

            var controller = GetControllerWithSession(context, user.Email);

            // Act
            var result = await controller.RedeemDesconto(desconto.DescontoId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("You don't have enough points to redeem the discount.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task RedeemDesconto_Success_ShouldReducePointsAndAddRedeemedDiscount()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            var user = new Utilizador
            {
                UtilizadorId = 1,
                Nome = "test",
                Email = "test@example.com",
                Password = "Pass$ord",
                Pontos = 50
            };

            var desconto = new Desconto
            {
                DescontoId = 1,
                Descricao = "10% Off",
                Valor = 10,
                PontosNecessarios = 20,
                IsLoja = true
            };

            context.Utilizador.Add(user);
            context.Desconto.Add(desconto);
            await context.SaveChangesAsync();

            var controller = GetControllerWithSession(context, user.Email);

            // Act
            var result = await controller.RedeemDesconto(desconto.DescontoId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Discount redeemed!", controller.TempData["SuccessMessage"]);

            var updatedUser = await context.Utilizador.FindAsync(user.UtilizadorId);
            Assert.Equal(30, updatedUser.Pontos);

            var redeemed = await context.DescontoResgatado.FirstOrDefaultAsync();
            Assert.NotNull(redeemed);
            Assert.Equal(user.UtilizadorId, redeemed.UtilizadorId);
            Assert.Equal(desconto.DescontoId, redeemed.DescontoId);
        }
    }
}
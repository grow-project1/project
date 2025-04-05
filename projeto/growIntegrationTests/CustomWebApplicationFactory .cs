using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System;

namespace growIntegrationTests
{
    public class CustomWebApplicationFactory2 : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Define o caminho correto para o ContentRoot
            var contentRootPath = AppContext.BaseDirectory;

            // Verificar se o caminho existe
            if (!System.IO.Directory.Exists(contentRootPath))
            {
                throw new InvalidOperationException($"O caminho especificado para o ContentRoot não foi encontrado: {contentRootPath}");
            }

            builder.UseContentRoot(contentRootPath);

            builder.ConfigureServices(services =>
            {
            });
        }
    }
}

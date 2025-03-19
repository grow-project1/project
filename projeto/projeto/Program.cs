using growTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Adicionar o DbContext � cole��o de servi�os
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext")
    ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")));

builder.Services.AddDistributedMemoryCache(); // Necess�rio para armazenar sess�es na mem�ria
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Defina o tempo de inatividade conforme necess�rio
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Necess�rio para GDPR em alguns cen�rios
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Adicionar os outros servi�os
builder.Services.AddControllersWithViews();

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Configurar o pipeline de requisi��es HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // O valor padr�o de HSTS � 30 dias. Voc� pode querer mudar isso para cen�rios de produ��o.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Para ambientes de desenvolvimento
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Adiciona o middleware de sess�o

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Leilaos}/{action=Index}/{id?}");

app.Run();

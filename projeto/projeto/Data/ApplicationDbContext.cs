using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using projeto.Models;

namespace projeto.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<projeto.Models.LoginModel> LoginModel { get; set; } = default!;
        public DbSet<projeto.Models.Desconto> Desconto { get; set; } = default!;
        public DbSet<projeto.Models.Utilizador> Utilizador { get; set; } = default!;
        public DbSet<projeto.Models.LogUtilizador> LogUtilizadores { get; set; } = default!;
        public DbSet<projeto.Models.VerificationModel> VerificationModel { get; set; } = default!;
        public DbSet<projeto.Models.DescontoResgatado> DescontoResgatado { get; set; } = default!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Desconto>().HasData(
                new Desconto
                {
                    DescontoId = 1,
                    Descricao = "10% desconto",
                    Valor = 10.0, // 10% de desconto
                    PontosNecessarios = 10,
                    IsLoja = true // Indica que pertence à loja
                },
                new Desconto
                {
                    DescontoId = 2,
                    Descricao = "20% desconto",
                    Valor = 20.0, // 20% de desconto
                    PontosNecessarios = 20,
                    IsLoja = true // Indica que pertence à loja
                }
            );
        }



    }
}

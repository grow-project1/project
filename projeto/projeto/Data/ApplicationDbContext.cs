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

        public DbSet<Utilizador> Utilizador { get; set; } = default!;
        public DbSet<LoginModel> LoginModel { get; set; } = default!;
        public DbSet<Desconto> Desconto { get; set; } = default!;
        public DbSet<LogUtilizador> LogUtilizadores { get; set; } = default!;
        public DbSet<VerificationModel> VerificationModel { get; set; } = default!;
        public DbSet<DescontoResgatado> DescontoResgatado { get; set; } = default!;
        public DbSet<Item> Itens { get; set; } = default!;
        public DbSet<Leilao> Leiloes { get; set; } = default!;
        public DbSet<Licitacao> Licitacoes { get; set; } = default!;
       



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Leilao>()
                .HasOne(l => l.Item)
                .WithOne() 
                .HasForeignKey<Leilao>(l => l.ItemId) 
                .OnDelete(DeleteBehavior.Cascade); 


            modelBuilder.Entity<Desconto>().HasData(
                new Desconto
                {
                    DescontoId = 1,
                    Descricao = "10% discount",
                    Valor = 10.0,
                    PontosNecessarios = 10,
                    UtilizadorId = null, 
                    IsLoja = true
                },
                new Desconto
                {
                    DescontoId = 2,
                    Descricao = "25% discount",
                    Valor = 25.0,
                    PontosNecessarios = 20,
                    UtilizadorId = null,
                    IsLoja = true
                }
            ); 
    }
        }


    }



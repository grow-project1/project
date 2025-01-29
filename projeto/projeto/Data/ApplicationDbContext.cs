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

        public DbSet<LoginModel> LoginModel { get; set; } = default!;
        public DbSet<Desconto> Desconto { get; set; } = default!;
        public DbSet<Utilizador> Utilizador { get; set; } = default!;
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
                .WithOne() // Relacionamento 1:1
                .HasForeignKey<Leilao>(l => l.ItemId) // A chave estrangeira está em Leilao
                .OnDelete(DeleteBehavior.Cascade); // Exclui o item quando o leilão é excluído


            modelBuilder.Entity<Desconto>().HasData(
                new Desconto
                {
                    DescontoId = 1,
                    Descricao = "Desconto de 10% na Loja",
                    Valor = 10.0,
                    PontosNecessarios = 10,
                    UtilizadorId = null, // Pode ser associado depois
                    IsLoja = true
                },
                new Desconto
                {
                    DescontoId = 2,
                    Descricao = "Desconto de 25% desconto",
                    Valor = 25.0,
                    PontosNecessarios = 20,
                    UtilizadorId = null,
                    IsLoja = true
                }
            ); 
    }
        }


    }



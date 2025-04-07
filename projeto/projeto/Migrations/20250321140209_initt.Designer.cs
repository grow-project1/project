﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using projeto.Data;

#nullable disable

namespace projeto.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250321140209_initt")]
    partial class initt
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("projeto.Models.Desconto", b =>
                {
                    b.Property<int>("DescontoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("DescontoId"));

                    b.Property<DateTime?>("DataFim")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DataObtencao")
                        .HasColumnType("datetime2");

                    b.Property<string>("Descricao")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsLoja")
                        .HasColumnType("bit");

                    b.Property<int>("PontosNecessarios")
                        .HasColumnType("int");

                    b.Property<int?>("UtilizadorId")
                        .HasColumnType("int");

                    b.Property<double>("Valor")
                        .HasColumnType("float");

                    b.HasKey("DescontoId");

                    b.HasIndex("UtilizadorId");

                    b.ToTable("Desconto");

                    b.HasData(
                        new
                        {
                            DescontoId = 1,
                            Descricao = "10% discount",
                            IsLoja = true,
                            PontosNecessarios = 10,
                            Valor = 10.0
                        },
                        new
                        {
                            DescontoId = 2,
                            Descricao = "25% discount",
                            IsLoja = true,
                            PontosNecessarios = 20,
                            Valor = 25.0
                        });
                });

            modelBuilder.Entity("projeto.Models.DescontoResgatado", b =>
                {
                    b.Property<int>("DescontoResgatadoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("DescontoResgatadoId"));

                    b.Property<DateTime>("DataResgate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DataValidade")
                        .HasColumnType("datetime2");

                    b.Property<int>("DescontoId")
                        .HasColumnType("int");

                    b.Property<int>("UtilizadorId")
                        .HasColumnType("int");

                    b.HasKey("DescontoResgatadoId");

                    b.HasIndex("DescontoId");

                    b.HasIndex("UtilizadorId");

                    b.ToTable("DescontoResgatado");
                });

            modelBuilder.Entity("projeto.Models.Item", b =>
                {
                    b.Property<int>("ItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ItemId"));

                    b.Property<int>("Categoria")
                        .HasColumnType("int");

                    b.Property<string>("Descricao")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FotoUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("PrecoInicial")
                        .HasColumnType("float");

                    b.Property<bool>("Sustentavel")
                        .HasColumnType("bit");

                    b.Property<string>("Titulo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ItemId");

                    b.ToTable("Itens");
                });

            modelBuilder.Entity("projeto.Models.Leilao", b =>
                {
                    b.Property<int>("LeilaoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LeilaoId"));

                    b.Property<DateTime>("DataFim")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DataInicio")
                        .HasColumnType("datetime2");

                    b.Property<int>("EstadoLeilao")
                        .HasColumnType("int");

                    b.Property<int>("ItemId")
                        .HasColumnType("int");

                    b.Property<bool>("Pago")
                        .HasColumnType("bit");

                    b.Property<int>("UtilizadorId")
                        .HasColumnType("int");

                    b.Property<double>("ValorAtualLance")
                        .HasColumnType("float");

                    b.Property<double>("ValorIncrementoMinimo")
                        .HasColumnType("float");

                    b.Property<int?>("VencedorId")
                        .HasColumnType("int");

                    b.HasKey("LeilaoId");

                    b.HasIndex("ItemId")
                        .IsUnique();

                    b.ToTable("Leiloes");
                });

            modelBuilder.Entity("projeto.Models.Licitacao", b =>
                {
                    b.Property<int>("LicitacaoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LicitacaoId"));

                    b.Property<DateTime>("DataLicitacao")
                        .HasColumnType("datetime2");

                    b.Property<int>("LeilaoId")
                        .HasColumnType("int");

                    b.Property<int>("UtilizadorId")
                        .HasColumnType("int");

                    b.Property<double>("ValorLicitacao")
                        .HasColumnType("float");

                    b.HasKey("LicitacaoId");

                    b.HasIndex("LeilaoId");

                    b.HasIndex("UtilizadorId");

                    b.ToTable("Licitacoes");
                });

            modelBuilder.Entity("projeto.Models.LogUtilizador", b =>
                {
                    b.Property<int>("LogUtilizadorId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LogUtilizadorId"));

                    b.Property<bool>("IsLoginSuccess")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LogDataLogin")
                        .HasColumnType("datetime2");

                    b.Property<string>("LogMessage")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LogUtilizadorEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UtilizadorId")
                        .HasColumnType("int");

                    b.HasKey("LogUtilizadorId");

                    b.HasIndex("UtilizadorId");

                    b.ToTable("LogUtilizadores");
                });

            modelBuilder.Entity("projeto.Models.LoginModel", b =>
                {
                    b.Property<int>("LoginModelId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("LoginModelId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("LoginModelId");

                    b.ToTable("LoginModel");
                });

            modelBuilder.Entity("projeto.Models.Utilizador", b =>
                {
                    b.Property<int>("UtilizadorId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UtilizadorId"));

                    b.Property<string>("CodigoPostal")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EstadoConta")
                        .HasColumnType("int");

                    b.Property<string>("ImagePath")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Morada")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Nome")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Pais")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("Pontos")
                        .HasColumnType("int");

                    b.Property<string>("Telemovel")
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.HasKey("UtilizadorId");

                    b.ToTable("Utilizador");
                });

            modelBuilder.Entity("projeto.Models.VerificationModel", b =>
                {
                    b.Property<int>("VerificationModelId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("VerificationModelId"));

                    b.Property<DateTime>("RequestTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("VerificationCode")
                        .HasColumnType("int");

                    b.HasKey("VerificationModelId");

                    b.ToTable("VerificationModel");
                });

            modelBuilder.Entity("projeto.Models.Desconto", b =>
                {
                    b.HasOne("projeto.Models.Utilizador", "Utilizador")
                        .WithMany()
                        .HasForeignKey("UtilizadorId");

                    b.Navigation("Utilizador");
                });

            modelBuilder.Entity("projeto.Models.DescontoResgatado", b =>
                {
                    b.HasOne("projeto.Models.Desconto", "Desconto")
                        .WithMany()
                        .HasForeignKey("DescontoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("projeto.Models.Utilizador", "Utilizador")
                        .WithMany()
                        .HasForeignKey("UtilizadorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Desconto");

                    b.Navigation("Utilizador");
                });

            modelBuilder.Entity("projeto.Models.Leilao", b =>
                {
                    b.HasOne("projeto.Models.Item", "Item")
                        .WithOne()
                        .HasForeignKey("projeto.Models.Leilao", "ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");
                });

            modelBuilder.Entity("projeto.Models.Licitacao", b =>
                {
                    b.HasOne("projeto.Models.Leilao", "Leilao")
                        .WithMany("Licitacoes")
                        .HasForeignKey("LeilaoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("projeto.Models.Utilizador", "Utilizador")
                        .WithMany()
                        .HasForeignKey("UtilizadorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Leilao");

                    b.Navigation("Utilizador");
                });

            modelBuilder.Entity("projeto.Models.LogUtilizador", b =>
                {
                    b.HasOne("projeto.Models.Utilizador", "Utilizador")
                        .WithMany()
                        .HasForeignKey("UtilizadorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Utilizador");
                });

            modelBuilder.Entity("projeto.Models.Leilao", b =>
                {
                    b.Navigation("Licitacoes");
                });
#pragma warning restore 612, 618
        }
    }
}

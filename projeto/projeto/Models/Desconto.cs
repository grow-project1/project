using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace growTests.Models

{
    public class Desconto
    {
        public int DescontoId { get; set; }
        public string Descricao { get; set; }
        public double Valor { get; set; }
        public int PontosNecessarios { get; set; }
        public DateTime? DataObtencao { get; set; }
        public DateTime? DataFim { get; set; }
        public int? UtilizadorId { get; set; }
        public Utilizador? Utilizador { get; set; }
        public bool IsLoja { get; set; }
    }


}

using System;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace projeto.Models

{
    public class Desconto
    {
        public int DescontoId { get; set; }
        public string Descricao { get; set; } // Adicionado para descrição dos descontos
        public double Valor { get; set; }
        public int PontosNecessarios { get; set; }
        public DateTime? DataObtencao { get; set; }
        public DateTime? DataFim { get; set; }

        public int? UtilizadorId { get; set; } // Associação com utilizador
        public Utilizador? Utilizador { get; set; }

        public bool IsLoja { get; set; } // True para descontos da loja
    }


}

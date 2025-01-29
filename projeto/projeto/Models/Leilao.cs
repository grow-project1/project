namespace projeto.Models
{
    public class Leilao
    {
        public int LeilaoId { get; set; }
        public int ItemId { get; set; } // Chave estrangeira
        public Item Item { get; set; } = default!; // Associação com o item
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public double ValorIncrementoMinimo { get; set; }
        public List<Licitacao>? Licitacoes { get; set; }
        public string? Vencedor { get; set; }
        public int UtilizadorId { get; set; } 
    }

}

namespace projeto.Models
{
    public class Leilao
    {
        public int LeilaoId { get; set; }
        public int ItemId { get; set; }
        public Item Item { get; set; } = default!;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public double ValorIncrementoMinimo { get; set; }
        public List<Licitacao>? Licitacoes { get; set; }
        public string? Vencedor { get; set; }
        public int UtilizadorId { get; set; }
        public double ValorAtualLance { get; set; }
        public EstadoLeilao EstadoLeilao { get; set; } 
    }
}

namespace projeto.Models
{
    public class Licitacao
    {
        public int LicitacaoId { get; set; }
        public DateTime DataLicitacao { get; set; } = DateTime.Now;
        public double ValorLicitacao { get; set; }
        public int LeilaoId { get; set; }
        public Leilao Leilao { get; set; } = default!;

        public int UtilizadorId { get; set; }
        public Utilizador Utilizador { get; set; } = default!;
    }
}

namespace projeto.Models
{
    public class PagamentoDetalhesViewModel
    {
        public Leilao Leilao { get; set; }
        public Utilizador Utilizador { get; set; }
        public List<DescontoResgatado> DescontosDisponiveis { get; set; } = new List<DescontoResgatado>();
        public int? DescontoSelecionado { get; set; }
    }

}

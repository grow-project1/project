namespace growTests.Models
{
    public class DescontoResgatado
    {
        public int DescontoResgatadoId { get; set; }
        public int DescontoId { get; set; }
        public int UtilizadorId { get; set; }
        public DateTime DataResgate { get; set; }
        public DateTime DataValidade { get; set; }
        public Desconto Desconto { get; set; }
        public Utilizador Utilizador { get; set; }
    }

}

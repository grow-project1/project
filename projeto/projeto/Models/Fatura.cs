namespace projeto.Models
{
    public class Fatura
    {
        public int Id { get; set; }
        public string Numero { get; set; }
        public DateTime Data { get; set; }
        public string NomeComprador { get; set; }
        public string NIF { get; set; }
        public string ItemLeiloado { get; set; }
        public decimal ValorBase { get; set; }
        public decimal IVA { get; set; }
        public decimal TotalComIVA { get; set; }
        public decimal Desconto { get; set; }
    }
}
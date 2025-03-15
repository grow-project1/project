namespace growTests.Models
{
    public class LogUtilizador
    {
        public int LogUtilizadorId { get; set; }
        public DateTime LogDataLogin { get; set; } = DateTime.Now;
        public Utilizador Utilizador { get; set; }
        public string LogUtilizadorEmail { get; set; }
        public string LogMessage { get; set; }
        public bool IsLoginSuccess { get; set; }
    }
}

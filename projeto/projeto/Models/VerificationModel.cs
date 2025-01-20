namespace projeto.Models
{
    public class VerificationModel
    {
        public int VerificationModelId { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public int VerificationCode { get; set; }
    }
}

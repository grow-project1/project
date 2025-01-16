using System.ComponentModel.DataAnnotations;

namespace projeto.Models
{
    public class Utilizador
    {
        public int UtilizadorId { get; set; }

        [Required(ErrorMessage = "The Email field is required")]
        [EmailAddress(ErrorMessage = "Invalid Format")]
        public string Email { get; set; }

        public string Nome { get; set; }
        public string Password { get; set; }
    }
}

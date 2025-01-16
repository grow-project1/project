using System;
using System.ComponentModel.DataAnnotations;

namespace projeto.Models
{
    public class Utilizador
    {
        public int UtilizadorId { get; set; }

        [Required(ErrorMessage = "The Email field is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The Name field is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "The Password field is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } // Hash da senha para segurança

        public DateTime CreatedAt { get; set; } = DateTime.Now; // Define a data de criação automaticamente
    }
}

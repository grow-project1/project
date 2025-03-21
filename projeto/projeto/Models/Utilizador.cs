using System;
using System.ComponentModel.DataAnnotations;

namespace projeto.Models
{
    public class Utilizador
    {
        public int UtilizadorId { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Nome { get; set; }

        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must have at least 6 characters, one uppercase letter, one number, and one special character.")]
        public string Password { get; set; }

        [StringLength(255)]
        public string? Morada { get; set; } = "Não definido";

        [StringLength(20)]
        [RegularExpression(@"^\d{4}-\d{3}$", ErrorMessage = "Invalid postal code format (e.g., 1234-567)")]
        public string? CodigoPostal { get; set; } = "0000-000";

        [StringLength(15)]
        [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "Invalid phone number format")]
        public string? Telemovel { get; set; } = "000000000";

        [StringLength(100)]
        public string? Pais { get; set; } = "Não definido";

        [StringLength(255)]
        public string ImagePath { get; set; } = "~/images/avatar_default.png";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public EstadoConta EstadoConta { get; set; } = EstadoConta.Ativa;

        public int Pontos { get; set; } = 20;
    }

    public enum EstadoConta
    {
        Ativa,
        Bloqueada,
        Cancelada
    }
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace growTests.Models
{
    public class Item
    { 
        public int ItemId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "O preço inicial deve ser maior ou igual a 0.")]
        public double PrecoInicial { get; set; }
        public Categoria Categoria { get; set; }
        public bool Sustentavel { get; set; }
        public string? FotoUrl { get; set; }
        [NotMapped]
        public IFormFile fotoo { get; set; }
    }
}

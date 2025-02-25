using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace projeto.Models
{
    public class Item
    { 
        public int ItemId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public double PrecoInicial { get; set; }
        public Categoria Categoria { get; set; }
        public bool Sustentavel { get; set; }



        public string? FotoUrl { get; set; }

        [NotMapped]
        public IFormFile fotoo { get; set; }
    }
}

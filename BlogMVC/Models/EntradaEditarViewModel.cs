using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Models
{
    public class EntradaEditarViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El {0} es requerido")]
        public required string Titulo { get; set; }
        [Required(ErrorMessage = "El {0} es requerido")]
        public required string Cuerpo { get; set; }
        [DisplayName("Imagen Portada")]
        public IFormFile? ImagenPortada { get; set; }
        public string? ImagenPortadaActual { get; set; }
        public bool ImagenRemovida { get; set; }
    }
}

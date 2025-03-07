using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductosApi.Models
{
    [Table("Productos")]
    public class Products
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int Cantidad { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Precio { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey("Categoria")]
        public int CategoriaId { get; set; }

        public virtual Categoria? Categoria { get; set; }

        // Para almacenar la ruta de la imagen
        public string? ImagenUrl { get; set; }
    }
}

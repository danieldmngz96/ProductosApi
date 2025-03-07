using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using ProductosApi.Models; // Importar los modelos

namespace ProductosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly string _connectionString;

        public ProductosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult GetProductos()
        {
            try
            {
                List<Products> productos = new List<Products>();

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Productos", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productos.Add(new Products
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
                                    Precio = reader.GetDecimal(reader.GetOrdinal("Precio")),
                                    FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                                    CategoriaId = reader.GetInt32(reader.GetOrdinal("CategoriaId"))
                                });
                            }
                        }
                    }
                }

                return Ok(productos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        [HttpPost]
        public IActionResult CrearProducto([FromBody] Products producto)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Productos (Nombre, Cantidad, Precio, FechaCreacion, CategoriaId) VALUES (@Nombre, @Cantidad, @Precio, @FechaCreacion, @CategoriaId)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Cantidad", producto.Cantidad);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                        cmd.Parameters.AddWithValue("@CategoriaId", producto.CategoriaId);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Producto creado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public IActionResult ActualizarProducto(int id, [FromBody] Products producto)
        {
            if (id != producto.Id)
            {
                return BadRequest(new { message = "El ID del producto en la URL y en el cuerpo no coinciden." });
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Productos SET Nombre = @Nombre, Cantidad = @Cantidad, Precio = @Precio, CategoriaId = @CategoriaId, ImagenUrl = @ImagenUrl WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", producto.Id);
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Cantidad", producto.Cantidad);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@CategoriaId", producto.CategoriaId);
                        cmd.Parameters.AddWithValue("@ImagenUrl", (object?)producto.ImagenUrl ?? DBNull.Value);

                        int filasAfectadas = cmd.ExecuteNonQuery();

                        if (filasAfectadas == 0)
                        {
                            return NotFound(new { message = "Producto no encontrado." });
                        }
                    }
                }

                return Ok(new { message = "Producto actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("SubirImagen/{id}")]
        public async Task<IActionResult> SubirImagen(int id, IFormFile imagen)
        {
            if (imagen == null || imagen.Length == 0)
            {
                return BadRequest("No se ha proporcionado ninguna imagen.");
            }

            try
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"{Guid.NewGuid()}_{imagen.FileName}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }

                string imageUrl = $"{Request.Scheme}://{Request.Host}/imagenes/{fileName}";

                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE Productos SET ImagenUrl = @ImagenUrl WHERE Id = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@ImagenUrl", imageUrl);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "Imagen subida exitosamente", imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }


        [HttpGet("ListarPorCategorias")]
        public IActionResult GetProductosPorCategorias()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT c.Id AS CategoriaId, c.Nombre AS Categoria, p.* " +
                        "FROM Categorias c LEFT JOIN Productos p ON c.Id = p.CategoriaId", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var categorias = new Dictionary<int, object>();

                            while (reader.Read())
                            {
                                int categoriaId = reader.GetInt32(reader.GetOrdinal("CategoriaId"));
                                string categoriaNombre = reader.GetString(reader.GetOrdinal("Categoria"));

                                if (!categorias.ContainsKey(categoriaId))
                                {
                                    categorias[categoriaId] = new
                                    {
                                        Id = categoriaId,
                                        Nombre = categoriaNombre,
                                        Productos = new List<object>()
                                    };
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("Id")))
                                {
                                    ((List<object>)((dynamic)categorias[categoriaId]).Productos).Add(new
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                        Cantidad = reader.GetInt32(reader.GetOrdinal("Cantidad")),
                                        Precio = reader.GetDecimal(reader.GetOrdinal("Precio")),
                                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                                        ImagenUrl = reader.IsDBNull(reader.GetOrdinal("ImagenUrl")) ? null : reader.GetString(reader.GetOrdinal("ImagenUrl"))
                                    });
                                }
                            }

                            return Ok(categorias.Values);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Login request)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT Id, Nombre FROM Usuarios WHERE Email = @Email AND PasswordHash = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Password", request.Password); // Hashear en producción

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var response = new
                            {
                                id = reader["Id"],
                                nombre = reader["Nombre"],
                                token = Guid.NewGuid().ToString() // Simulación de token
                            };
                            return Ok(response);
                        }
                        else
                        {
                            return Unauthorized(new { message = "Correo o contraseña incorrectos" });
                        }
                    }
                }
            }


        }
    }
}

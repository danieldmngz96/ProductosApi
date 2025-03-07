var builder = WebApplication.CreateBuilder(args);

// 🔹 Agregar servicios de controladores
builder.Services.AddControllers();

// 🔹 Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// 🔹 Agregar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 Servidor de imágenes
app.UseStaticFiles();

// 🔹 Configurar el pipeline de la aplicación
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 🔹 Habilitar CORS antes de los controladores
app.UseCors("AllowAllOrigins");

// 🔹 Habilitar los controladores en la API
app.MapControllers();

app.Run();

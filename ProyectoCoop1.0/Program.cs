using Microsoft.EntityFrameworkCore;
using ProyectoCoop1._0.Models;

var builder = WebApplication.CreateBuilder(args);

// Configurar DbContext con cadena de conexión
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Agregar servicios MVC
builder.Services.AddControllersWithViews();

// Agregar servicios de sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Permitir CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost7138", policy =>
    {
        policy.WithOrigins("http://localhost:7138")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configuración de autenticación con cookies
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Usuarios/Login";
        options.LogoutPath = "/Usuarios/Login";
        options.AccessDeniedPath = "/Usuarios/AccessDenied";

        options.Cookie.IsEssential = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // ✅ La cookie no es persistente (se elimina al cerrar el navegador)
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Opcional: caduca por inactividad

        options.SlidingExpiration = false;

        options.Events.OnSigningIn = context =>
        {
            context.Properties.IsPersistent = false; // 🔥 No se guarda la cookie en disco
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowLocalhost7138");

app.UseSession();            // Habilitar sesiones
app.UseAuthentication();     // Habilitar autenticación
app.UseAuthorization();      // Habilitar autorización

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuarios}/{action=Login}/{id?}");

// Crear un usuario administrador si no existe
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    context.Database.EnsureCreated();

    var admin = context.Usuarios.FirstOrDefault(u => u.usuario == "admin");
    if (admin == null)
    {
        context.Usuarios.Add(new Usuario
        {
            usuario = "admin",
            contrasenia = "1234",
            esadmin = true
        });
        context.SaveChanges();
    }
}

app.Run();

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCoop1._0.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoCoop1._0.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usuario, string contraseña)
        {
            Console.WriteLine($"Login intentado con usuario: '{usuario}' y contraseña: '{contraseña}'");

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                ViewBag.Error = "Debe ingresar usuario y contraseña.";
                return View();
            }

            // Login admin hardcodeado
            if (usuario.ToLower().Trim() == "admin" && contraseña == "1234")
            {
                var claimsAdmin = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("EsAdmin", "true")
        };

                var identityAdmin = new ClaimsIdentity(claimsAdmin, "MyCookieAuth");
                var principalAdmin = new ClaimsPrincipal(identityAdmin);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false
                };

                await HttpContext.SignInAsync("MyCookieAuth", principalAdmin, authProperties);
                return RedirectToAction("Index", "Socios");
            }

            // Normalizamos credenciales ingresadas
            string usuarioNormalizado = usuario.Replace(" ", "").ToLowerInvariant().Trim();
            string contraseñaNormalizada = contraseña.Trim();

            Usuario user = null;

            // Buscar solo por Socios.usuarioLogin
            var socios = (await _context.Socios
                .Include(s => s.Usuario)
                .ToListAsync())
                .AsEnumerable();

            var socio = socios.FirstOrDefault(s =>
                (s.usuarioLogin ?? "").Replace(" ", "").ToLowerInvariant().Trim() == usuarioNormalizado &&
                s.Usuario != null &&
                (s.Usuario.contrasenia ?? "").Trim() == contraseñaNormalizada);

            user = socio?.Usuario;

            if (user != null)
            {
                Console.WriteLine($"Usuario encontrado por Socios.usuarioLogin: {user.usuario}");

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.usuario),
            new Claim(ClaimTypes.Role, user.esadmin ? "Admin" : "Socio")
        };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync("MyCookieAuth", principal, authProperties);

                return user.esadmin
                    ? RedirectToAction("Index", "Socios")
                    : RedirectToAction("CreateTurnoSocio", "Turnoes");
            }

            // Falló el login
            ViewBag.Error = "Credenciales inválidas";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login");
        }

        // Crear un Socio y su Usuario automáticamente
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        
        public async Task<IActionResult> Create([Bind("id,nombre,apellido,email,telefono,direccion")] Socio socio)
        {
            if (ModelState.IsValid)
            {
                // Generar el usuarioLogin automáticamente antes de guardar el Socio
                if (string.IsNullOrEmpty(socio.nombre) || string.IsNullOrEmpty(socio.apellido))
                {
                    ViewBag.ErrorMessages = "El nombre y apellido no pueden estar vacíos.";
                    return View(socio);
                }

                // Generamos el usuarioLogin automáticamente
                socio.usuarioLogin = socio.nombre.ToLower() + socio.apellido.ToLower();

                // Verificamos que usuarioLogin no esté vacío
                if (string.IsNullOrEmpty(socio.usuarioLogin))
                {
                    ViewBag.ErrorMessages = "El usuarioLogin no se generó correctamente.";
                    return View(socio);
                }

                // Crear el Usuario con una contraseña por defecto
                var usuario = new Usuario
                {
                    contrasenia = "default_password", // Contraseña por defecto
                    esadmin = false // Asumimos que no es admin para los socios
                };

                // Guardamos el Usuario primero
                _context.Add(usuario);
                await _context.SaveChangesAsync();

                // Ahora vinculamos el UsuarioId con el Socio
                socio.UsuarioId = usuario.id;

                // Guardamos el Socio con el UsuarioId
                _context.Add(socio);
                await _context.SaveChangesAsync(); // Guardamos el Socio

                return RedirectToAction(nameof(Index));
            }
            return View(socio);
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Usuarios.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }
    }
}


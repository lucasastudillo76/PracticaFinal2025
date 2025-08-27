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
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usuario, string contraseña)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                ViewBag.Error = "Debe ingresar usuario y contraseña.";
                return View();
            }

            var userNameTrimmed = usuario.Trim().ToLowerInvariant();
            var passTrimmed = contraseña.Trim();

            // ADMIN HARDCODEADO
            if (userNameTrimmed == "admin" && passTrimmed == "1234")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);
                return RedirectToAction("Index", "Socios");
            }

            // Buscar socio con usuarioLogin
            var socio = await _context.Socios
                .Include(s => s.Usuario)
                .FirstOrDefaultAsync(s =>
                    s.usuarioLogin.ToLower().Trim() == userNameTrimmed &&
                    s.Usuario != null &&
                    s.Usuario.contrasenia.Trim() == passTrimmed);

            if (socio?.Usuario != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, socio.Usuario.usuario ?? socio.usuarioLogin),
                    new Claim(ClaimTypes.Role, "Socio")
                };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);
                return RedirectToAction("CreateTurnoSocio", "Turnoes");
            }

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

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("id,nombre,apellido,email,telefono,direccion")] Socio socio)
        {
            if (!ModelState.IsValid)
                return View(socio);

            if (string.IsNullOrEmpty(socio.nombre) || string.IsNullOrEmpty(socio.apellido))
            {
                ViewBag.ErrorMessages = "El nombre y apellido no pueden estar vacíos.";
                return View(socio);
            }

            socio.usuarioLogin = socio.nombre.ToLower() + socio.apellido.ToLower();

            var usuario = new Usuario
            {
                contrasenia = "default_password",
                esadmin = false
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            socio.UsuarioId = usuario.id;
            _context.Socios.Add(socio);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

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


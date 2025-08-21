using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCoop1._0.Models;

namespace ProyectoCoop1._0.Controllers
{
    [Authorize]
    public class TurnoesController : Controller
    {
        private readonly AppDbContext _dbContext;

        public TurnoesController(AppDbContext context)
        {
            _dbContext = context;
        }

        // ADMIN - INDEX (solo turnos no finalizados)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var turnos = await _dbContext.Turnos
                .Include(t => t.Socio)
                .Where(t => t.estado == "Pendiente")
                .OrderBy(t => t.FechaHora)
                .ToListAsync();

            return View(turnos);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var turno = await _dbContext.Turnos
                .Include(t => t.Socio)
                .FirstOrDefaultAsync(m => m.id == id);

            if (turno == null) return NotFound();

            return View(turno);
        }

        // SOCIO - FORMULARIO CREAR TURNO
        [Authorize(Roles = "Socio")]
        public IActionResult CreateTurnoSocio()
        {
            return View();
        }

        // SOCIO - POST CREAR TURNO
        [HttpPost]
        [Authorize(Roles = "Socio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTurnoSocio(Turno turno)
        {
            var usuarioLogin = User.Identity?.Name?.Trim().ToLower();

            if (string.IsNullOrEmpty(usuarioLogin))
                return Unauthorized();

            var socio = await _dbContext.Socios
                .FirstOrDefaultAsync(s => s.usuarioLogin.ToLower() == usuarioLogin);

            if (socio == null)
            {
                ViewBag.Error = "No se encontró el socio correspondiente al usuario actual.";
                return View(turno);
            }

            turno.socioId = socio.id;
            turno.estado = "Pendiente";

            // Validación: turno ya existente
            bool existeTurno = await _dbContext.Turnos
                .AnyAsync(t => t.FechaHora == turno.FechaHora && t.socioId == turno.socioId);

            if (existeTurno)
                ModelState.AddModelError("FechaHora", "Ya existe un turno asignado en esa fecha y hora.");

            // Validación: no fechas pasadas
            if (turno.FechaHora < DateTime.Now)
                ModelState.AddModelError("FechaHora", "No se pueden seleccionar fechas pasadas.");

            // Validación: días hábiles
            if (turno.FechaHora.DayOfWeek == DayOfWeek.Saturday || turno.FechaHora.DayOfWeek == DayOfWeek.Sunday)
                ModelState.AddModelError("FechaHora", "Solo se pueden seleccionar días hábiles (lunes a viernes).");

            // Validación: horario permitido
            if (turno.FechaHora.Hour < 7 || turno.FechaHora.Hour > 12)
                ModelState.AddModelError("FechaHora", "La hora debe estar entre las 07:00 y las 12:00.");

            // Validación: minutos exactos (en punto)
            if (turno.FechaHora.Minute != 0)
                ModelState.AddModelError("FechaHora", "Solo se permiten turnos en horas exactas (por ejemplo: 07:00, 08:00, etc.).");

            if (ModelState.IsValid)
            {
                _dbContext.Add(turno);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(CreateTurnoSocio));
            }

            return View(turno);
        }

        // ADMIN - CREAR TURNO
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.socioId = _dbContext.Socios
                .Select(s => new SelectListItem
                {
                    Value = s.id.ToString(),
                    Text = s.nombre + " " + s.apellido
                }).ToList();

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,FechaHora,servicio,tarea,estado,socioId")] Turno turno)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Add(turno);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.socioId = _dbContext.Socios
                .Select(s => new SelectListItem
                {
                    Value = s.id.ToString(),
                    Text = s.nombre + " " + s.apellido
                }).ToList();

            return View(turno);
        }

        // ADMIN - EDITAR SOLO EL ESTADO
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var turno = await _dbContext.Turnos
                .Include(t => t.Socio)
                .FirstOrDefaultAsync(t => t.id == id);

            if (turno == null) return NotFound();

            ViewBag.NombreCompletoSocio = turno.Socio.nombre + " " + turno.Socio.apellido;

            return View(turno);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,estado")] Turno turnoInput)
        {
            if (id != turnoInput.id) return NotFound();

            var turnoEnDb = await _dbContext.Turnos.FindAsync(id);
            if (turnoEnDb == null) return NotFound();

            if (turnoEnDb.estado == "Pendiente" && turnoInput.estado == "Finalizado" || turnoInput.estado == "Suspendido")
            {
                turnoEnDb.estado = turnoInput.estado;

                try
                {
                    _dbContext.Update(turnoEnDb);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TurnoExists(turnoEnDb.id)) return NotFound();
                    throw;
                }
            }

            ModelState.AddModelError("estado", "Solo se puede cambiar el estado de Pendiente a Finalizado o Suspendido.");
            return View(turnoEnDb);
        }

        // ADMIN - ELIMINAR TURNO
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var turno = await _dbContext.Turnos
                .Include(t => t.Socio)
                .FirstOrDefaultAsync(m => m.id == id);

            if (turno == null) return NotFound();

            return View(turno);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var turno = await _dbContext.Turnos.FindAsync(id);
            if (turno != null)
            {
                _dbContext.Turnos.Remove(turno);
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ADMIN - LISTA DE FINALIZADOS CON FILTRO POR APELLIDO
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Finalizados(string filtroApellido)
        {
            var turnosFinalizados = _dbContext.Turnos
                .Include(t => t.Socio)
                .Where(t => t.estado == "Finalizado" || t.estado == "Suspendido");

            if (!string.IsNullOrEmpty(filtroApellido))
            {
                filtroApellido = filtroApellido.ToLower().Trim();
                turnosFinalizados = turnosFinalizados
                    .Where(t => t.Socio.apellido.ToLower().Contains(filtroApellido));
            }

            var lista = await turnosFinalizados
                .OrderByDescending(t => t.FechaHora)
                .ToListAsync();

            ViewBag.FiltroApellido = filtroApellido;

            return View(lista);
        }
        

        [Authorize(Roles = "Socio")]
        public async Task<IActionResult> TurnosSuspendidos()
        {
            string nombreUsuario = User.Identity.Name;

            var usuario = await _dbContext.Usuarios
                .FirstOrDefaultAsync(u => u.usuario == nombreUsuario);

            if (usuario == null)
                return Unauthorized();

            var socio = await _dbContext.Socios
                .FirstOrDefaultAsync(s => s.UsuarioId == usuario.id);

            if (socio == null)
                return NotFound("Socio no encontrado");

            var turnosSuspendidos = await _dbContext.Turnos
                .Include(t => t.Socio)
                .Where(t => t.estado == "Suspendido" && t.socioId == socio.id)                
                //.Include(t => t.tarea)
                .ToListAsync();

            return View(turnosSuspendidos);
        }


        [Authorize(Roles = "Socio")]
        [HttpGet]
        public async Task<IActionResult> Reprogramar(int id)
        {
            var turno = await _dbContext.Turnos.FindAsync(id);
            if (turno == null || turno.estado != "Suspendido") return NotFound();

            return View(turno);
        }
        
            [HttpPost]
            [Authorize(Roles = "Socio")]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Reprogramar(Turno turnoInput)
            {
                var turnoEnDb = await _dbContext.Turnos.FindAsync(turnoInput.id);
                if (turnoEnDb == null || turnoEnDb.estado != "Suspendido")
                    return NotFound();

                // Validaciones sobre la nueva fecha
                DateTime nuevaFecha = turnoInput.FechaHora;

                if (nuevaFecha == DateTime.MinValue)
                    ModelState.AddModelError("FechaHora", "La fecha no puede ser vacía.");

                if (nuevaFecha <= DateTime.Now)
                    ModelState.AddModelError("FechaHora", "La fecha debe ser posterior a la actual.");

                if (nuevaFecha.DayOfWeek == DayOfWeek.Saturday || nuevaFecha.DayOfWeek == DayOfWeek.Sunday)
                    ModelState.AddModelError("FechaHora", "Debe elegir un día hábil.");

                if (nuevaFecha.Hour < 7 || nuevaFecha.Hour > 11)
                    ModelState.AddModelError("FechaHora", "Hora fuera del horario permitido.");

                bool yaExisteTurno = await _dbContext.Turnos.AnyAsync(t =>
                    t.id != turnoEnDb.id &&
                    t.FechaHora == nuevaFecha &&
                    t.estado == "Pendiente");

                if (yaExisteTurno)
                    ModelState.AddModelError("FechaHora", "Ya existe un turno en ese horario.");

                if (!ModelState.IsValid)
                {
                    // Mantenemos los valores originales en la vista
                    turnoEnDb.FechaHora = turnoInput.FechaHora;
                    return View(turnoEnDb);
                }

                // ✔️ Solo modificamos lo que queremos
                turnoEnDb.FechaHora = nuevaFecha;
                turnoEnDb.estado = "Pendiente";

                // No tocamos servicio, tarea, socioId, etc.
                await _dbContext.SaveChangesAsync();

                return RedirectToAction(nameof(TurnosSuspendidos));
            }
            //ModelState.Remove("servicio");
            //ModelState.Remove("tarea");
            //ModelState.Remove("estado");                    

        private bool TurnoExists(int id)
        {
            return _dbContext.Turnos.Any(e => e.id == id);
        }
    }
}

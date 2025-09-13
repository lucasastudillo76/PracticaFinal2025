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
                .OrderByDescending(t => t.FechaHora) // CAMBIO
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

        [Authorize(Roles = "Socio")]
        public IActionResult CreateTurnoSocio()
        {
            var turno = new Turno();
            return View(turno);
        }

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

            bool existeTurno = await _dbContext.Turnos
                .AnyAsync(t => t.FechaHora == turno.FechaHora && t.socioId == turno.socioId);

            if (existeTurno)
                ModelState.AddModelError("FechaHora", "Ya existe un turno asignado en esa fecha y hora.");

            if (turno.FechaHora < DateTime.Now)
                ModelState.AddModelError("FechaHora", "No se pueden seleccionar fechas pasadas.");

            if (turno.FechaHora.DayOfWeek == DayOfWeek.Saturday || turno.FechaHora.DayOfWeek == DayOfWeek.Sunday)
                ModelState.AddModelError("FechaHora", "Solo se pueden seleccionar días hábiles (lunes a viernes).");

            if (turno.FechaHora.Hour < 7 || turno.FechaHora.Hour > 12)
                ModelState.AddModelError("FechaHora", "La hora debe estar entre las 07:00 y las 12:00.");

            if (turno.FechaHora.Minute != 0)
                ModelState.AddModelError("FechaHora", "Solo se permiten turnos en horas exactas (ej: 07:00, 08:00, etc.).");

            var fechaLimite = turno.FechaHora.AddDays(-7);
            bool mismaTareaEnLaSemana = await _dbContext.Turnos
                .AnyAsync(t =>
                    t.socioId == turno.socioId &&
                    t.servicio == turno.servicio &&
                    t.tarea == turno.tarea &&
                    t.FechaHora >= fechaLimite &&
                    t.FechaHora <= turno.FechaHora);

            if (mismaTareaEnLaSemana)
                ModelState.AddModelError(string.Empty, "Ya tenés un turno con la misma tarea y servicio esta semana.");

            if (ModelState.IsValid)
            {
                _dbContext.Add(turno);
                await _dbContext.SaveChangesAsync();
                TempData["TurnoReservado"] = $"Turno reservado para el {turno.FechaHora:dd/MM/yyyy HH:mm} con éxito.";
                return RedirectToAction(nameof(CreateTurnoSocio));
            }

            return View(turno);
        }

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

            if (turnoEnDb.estado == "Pendiente" && (turnoInput.estado == "Finalizado" || turnoInput.estado == "Suspendido"))
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Finalizados(string filtroApellido)
        {
            var turnosFinalizados = _dbContext.Turnos
                .Include(t => t.Socio)
                .Where(t => t.estado == "Finalizado" || t.estado == "Suspendido" || t.estado == "Cancelado");

            if (!string.IsNullOrEmpty(filtroApellido))
            {
                filtroApellido = filtroApellido.ToLower().Trim();
                turnosFinalizados = turnosFinalizados
                    .Where(t => t.Socio.apellido.ToLower().Contains(filtroApellido));
            }

            var lista = await turnosFinalizados
                .OrderByDescending(t => t.FechaHora) // CAMBIO
                .ToListAsync();

            ViewBag.FiltroApellido = filtroApellido;

            return View(lista);
        }

        [Authorize(Roles = "Socio")]
        public async Task<IActionResult> TurnosSuspendidos()
        {
            var usuarioLogin = User.Identity?.Name?.Trim().ToLower();

            if (string.IsNullOrEmpty(usuarioLogin))
                return Unauthorized();

            var socio = await _dbContext.Socios
                .FirstOrDefaultAsync(s => s.usuarioLogin.ToLower() == usuarioLogin);

            if (socio == null)
                return NotFound("Socio no encontrado");

            var turnosSuspendidos = await _dbContext.Turnos
                .Where(t => t.estado == "Suspendido" && t.socioId == socio.id)
                .OrderByDescending(t => t.FechaHora) // CAMBIO
                .ToListAsync();

            return View(turnosSuspendidos);
        }

        [Authorize(Roles = "Socio")]
        public async Task<IActionResult> MisTurnos()
        {
            var usuarioLogin = User.Identity?.Name?.Trim().ToLower();

            if (string.IsNullOrEmpty(usuarioLogin))
                return Unauthorized();

            var socio = await _dbContext.Socios
                .FirstOrDefaultAsync(s => s.usuarioLogin.ToLower() == usuarioLogin);

            if (socio == null)
                return NotFound();

            var turnos = await _dbContext.Turnos
                .Include(t => t.Socio)
                .Where(t => t.socioId == socio.id && (t.estado == "Finalizado" || t.estado == "Pendiente"))
                .OrderByDescending(t => t.FechaHora) // YA ESTABA CORRECTO
                .ToListAsync();

            return View(turnos);
        }

        [HttpGet]
        public IActionResult Cancelar(int id)
        {
            var turno = _dbContext.Turnos.FirstOrDefault(t => t.id == id);
            if (turno == null)
            {
                return NotFound();
            }

            turno.estado = "Cancelado";
            _dbContext.SaveChanges();

            return RedirectToAction("MisTurnos");
        }

        [Authorize(Roles = "Socio")]
        [HttpGet]
        public async Task<IActionResult> Reprogramar(int id)
        {
            var turno = _dbContext.Turnos.Find(id);
            if (turno == null)
            {
                return NotFound();
            }

            return View(turno);
        }

        [HttpPost]
        [Authorize(Roles = "Socio,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reprogramar(Turno turnoInput)
        {
            var turnoEnDb = await _dbContext.Turnos.FindAsync(turnoInput.id);
            if (turnoEnDb == null)
                return NotFound();

            var usuarioLogin = User.Identity?.Name?.Trim().ToLower();
            var socio = await _dbContext.Socios.FirstOrDefaultAsync(s => s.usuarioLogin.ToLower() == usuarioLogin);

            if (User.IsInRole("Socio") && turnoEnDb.socioId != socio?.id)
                return Forbid();

            if (turnoEnDb.estado != "Suspendido" && !User.IsInRole("Admin"))
                return BadRequest("No puede reprogramar este turno.");

            DateTime nuevaFecha = turnoInput.FechaHora;

            if (nuevaFecha == DateTime.MinValue)
                ModelState.AddModelError("FechaHora", "La fecha no puede ser vacía.");
            if (nuevaFecha <= DateTime.Now)
                ModelState.AddModelError("FechaHora", "La fecha debe ser posterior a la actual.");
            if (nuevaFecha.DayOfWeek == DayOfWeek.Saturday || nuevaFecha.DayOfWeek == DayOfWeek.Sunday)
                ModelState.AddModelError("FechaHora", "Debe elegir un día hábil.");
            if (nuevaFecha.Hour < 7 || nuevaFecha.Hour > 12)
                ModelState.AddModelError("FechaHora", "La hora debe estar entre las 07:00 y las 12:00.");

            bool yaExisteTurno = await _dbContext.Turnos.AnyAsync(t =>
                t.id != turnoEnDb.id &&
                t.FechaHora == nuevaFecha &&
                t.estado == "Pendiente");

            if (yaExisteTurno)
                ModelState.AddModelError("FechaHora", "Ya existe un turno en ese horario.");

            if (!ModelState.IsValid)
            {
                turnoEnDb.FechaHora = turnoInput.FechaHora;
                return View(turnoEnDb);
            }

            turnoEnDb.FechaHora = nuevaFecha;
            turnoEnDb.estado = "Pendiente";

            await _dbContext.SaveChangesAsync();

            TempData["MensajeReprogramacion"] = $"Su turno ha sido reprogramado para {turnoEnDb.FechaHora:dd/MM/yyyy HH:mm} con éxito.";

            return RedirectToAction(User.IsInRole("Admin") ? "Index" : "MisTurnos");
        }

        private bool TurnoExists(int id)
        {
            return _dbContext.Turnos.Any(e => e.id == id);
        }
    }
}


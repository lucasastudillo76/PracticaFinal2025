using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProyectoCoop1._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoCoop1._0.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SociosController : Controller
    {
        private readonly AppDbContext _context;

        public SociosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Socios
        public IActionResult Index(int? id, string apellido)
        {
            var socios = _context.Socios.AsQueryable();

            if (id.HasValue)
                socios = socios.Where(s => s.id == id.Value);

            if (!string.IsNullOrEmpty(apellido))
                socios = socios.Where(s => s.apellido.Contains(apellido));

            return View(socios.ToList());
        }

        // GET: Socios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var socio = await _context.Socios.FirstOrDefaultAsync(m => m.id == id);
            if (socio == null)
                return NotFound();

            return View(socio);
        }

        // GET: Socios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Socios/Create        
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create([Bind("id,nombre,apellido,email,telefono,direccion,usuarioLogin")] Socio socio)
            {
                if (ModelState.IsValid)
                {
                    socio.usuarioLogin = socio.usuarioLogin?.Trim().ToLower();

                    if (_context.Socios.Any(s => s.usuarioLogin == socio.usuarioLogin))
                    {
                        ViewBag.ErrorMessages = "El nombre de usuario ya existe. Por favor, elija otro.";
                        return View(socio);
                    }

                    var usuario = new Usuario
                    {
                        usuario = socio.usuarioLogin,
                        contrasenia = "1234",
                        esadmin = false
                    };

                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    socio.UsuarioId = usuario.id;
                    _context.Socios.Add(socio);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }

                var errores = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                ViewBag.ErrorMessages = errores;

                return View(socio);
            }


        // GET: Socios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var socio = await _context.Socios.FindAsync(id);
            if (socio == null)
                return NotFound();

            return View(socio);
        }

        // POST: Socios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,nombre,apellido,email,telefono,direccion")] Socio socio)
        {
            if (id != socio.id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(socio);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SocioExists(socio.id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(socio);
        }

        // GET: Socios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var socio = await _context.Socios.FirstOrDefaultAsync(m => m.id == id);
            if (socio == null)
                return NotFound();

            return View(socio);
        }

        // POST: Socios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var socio = await _context.Socios.FindAsync(id);
            if (socio != null)
            {
                _context.Socios.Remove(socio);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Socios/Importar
        public IActionResult Importar()
        {
            return View();
        }

        // POST: Socios/Importar
        [HttpPost]
        public async Task<IActionResult> Importar(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError("", "Por favor seleccione un archivo válido.");
                return View();
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    var socios = new List<Socio>();

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var socio = new Socio
                        {
                            nombre = worksheet.Cells[row, 1].Text,
                            apellido = worksheet.Cells[row, 2].Text,
                            email = worksheet.Cells[row, 3].Text,
                            telefono = worksheet.Cells[row, 4].Text,
                            direccion = worksheet.Cells[row, 5].Text
                        };

                        socios.Add(socio);
                    }

                    _context.Socios.AddRange(socios);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Socios/Exportar
        [HttpGet]
        public IActionResult Exportar()
        {
            return View();
        }

        // POST: Socios/DescargarExcel
        [HttpPost]
        public IActionResult DescargarExcel()
        {
            var socios = _context.Socios.ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Socios");

                worksheet.Cells[1, 1].Value = "Nombre";
                worksheet.Cells[1, 2].Value = "Apellido";
                worksheet.Cells[1, 3].Value = "Email";
                worksheet.Cells[1, 4].Value = "Teléfono";
                worksheet.Cells[1, 5].Value = "Dirección";

                for (int i = 0; i < socios.Count; i++)
                {
                    var socio = socios[i];
                    worksheet.Cells[i + 2, 1].Value = socio.nombre;
                    worksheet.Cells[i + 2, 2].Value = socio.apellido;
                    worksheet.Cells[i + 2, 3].Value = socio.email;
                    worksheet.Cells[i + 2, 4].Value = socio.telefono;
                    worksheet.Cells[i + 2, 5].Value = socio.direccion;
                }

                var excelBytes = package.GetAsByteArray();
                string fileName = $"Socios_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private bool SocioExists(int id)
        {
            return _context.Socios.Any(e => e.id == id);
        }
    }
}

using GestorTarea.Models;
using System;
using System.Data.Entity.Core;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace GestorTarea.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly GestorTareasEntities db = new GestorTareasEntities();

        public static class HashHelper
        {
            public static string Hash(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;

                using (var sha = SHA256.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(input);
                    var hash = sha.ComputeHash(bytes);
                    return Convert.ToBase64String(hash);
                }
            }
        }

        // LOGIN para vistas (formulario tradicional)
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Usuarios model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string hashedPassword = HashHelper.Hash(model.Password);

                    var usuario = db.Usuarios.FirstOrDefault(u =>
                        u.Email == model.Email &&
                        u.Password == hashedPassword &&
                        u.Activo == true);

                    if (usuario != null)
                    {
                        usuario.FechaUltimoAcceso = DateTime.Now;
                        db.SaveChanges();

                        // NO limpiar sesión antes de asignar datos
                        // Session.Clear();
                        // Session.Abandon();
                        // Response.Cookies.Clear();

                        Session["UsuarioId"] = usuario.IdUsuario;
                        Session["Usuario"] = usuario.Name;
                        Session["Rol"] = usuario.Rol;

                        return RedirectToAction("Crear", "Proyecto");
                    }

                    ModelState.AddModelError("", "Credenciales inválidas.");
                }
                catch (EntityCommandCompilationException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error EF: {ex.Message}");
                    ModelState.AddModelError("", "Error del sistema");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error general: {ex.Message}");
                    ModelState.AddModelError("", "Error inesperado");
                }
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Usuario");
        }

        // Registro (puedes mantener igual)
        public ActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registro(User model)
        {
            if (ModelState.IsValid)
            {
                if (db.Usuarios.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Este correo ya está registrado.");
                    return View(model);
                }
                try
                {
                    var usuario = new Usuarios
                    {
                        Name = model.Name,
                        Email = model.Email,
                        Password = HashHelper.Hash(model.Password),
                        Rol = "Usuario",
                        Activo = true,
                        Fecha = DateTime.Now
                    };

                    db.Usuarios.Add(usuario);
                    db.SaveChanges();

                    TempData["mensaje"] = "Usuario registrado correctamente";
                    return RedirectToAction("Registro");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                }
            }

            return View(model);
        }

        // Otros métodos: Index, Editar, AdminUser, etc. los puedes mantener igual
 



    public ActionResult Index()
            {// Verificar si el usuario está en sesión
                if (Session["Usuario"] == null)
                {
                    return RedirectToAction("Login", "Usuario");
                }
                // Verificar si el usuario es administrador
                var usuarios = db.Usuarios.ToList();
                return View(usuarios);
            }




            [HttpPost]
            public ActionResult Editar(int id, string rol, bool activo)
            {
                // Verificar si el usuario está en sesión y es administrador
                // y si el usuario está en sesión
                // si no, retornar un mensaje de error
                // si el usuario no está en sesión o no es administrador, retornar un mensaje de error
                // si el usuario no existe, retornar un mensaje de error    
                // si el usuario existe, actualizar su rol y estado y retornar un mensaje de éxito
                if (Session["Usuario"] == null || Session["Rol"] == null || Session["Rol"].ToString() != "Administrador")
                {
                    return Json(new { success = false, message = "No autorizado" });
                }

                // Buscar el usuario por ID
                var usuario = db.Usuarios.FirstOrDefault(u => u.IdUsuario == id);
                // Si el usuario existe, actualizar su rol y estado
                if (usuario != null)
                {
                    usuario.Rol = rol;
                    usuario.Activo = activo;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                // Si el usuario no existe, retornar un mensaje de error
                return Json(new { success = false, message = "Usuario no encontrado" });
            }



            public ActionResult AdminUser()
            {
                // Validar si el usuario está en sesión y tiene rol de Administrador
                if (!(Session["Rol"]?.ToString() == "Administrador") || Session["Usuario"] == null)
                {
                    return View("AccesoDenegado");
                }

                //var usuarios = db.Usuarios.ToList(); // Muestra todos (activos e inactivos)

                var usuarios = db.Usuarios.Where(u => u.Activo).ToList(); // muestra solo activos si lo deseas
                return View(usuarios);
            }
        }





}


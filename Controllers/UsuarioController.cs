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
    {// Esta clase maneja la autenticación de usuarios, registro y administración de usuarios
        private readonly GestorTareasEntities db = new GestorTareasEntities();

        public static class HashHelper
        {
            // Método para hashear contraseñas usando SHA256
            public static string Hash(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;
                // Asegúrate de usar un algoritmo seguro para hashear contraseñas
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
        // POST para procesar el login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Usuarios model)
        {
            if (ModelState.IsValid)
            {
                try
                {// Validar si el usuario ya está en sesión
                    string hashedPassword = HashHelper.Hash(model.Password);

                    var usuario = db.Usuarios.FirstOrDefault(u =>
                        u.Email == model.Email &&
                        u.Password == hashedPassword &&
                        u.Activo == true);

                    if (usuario != null)
                    {// Usuario encontrado y activo
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
                    // Usuario no encontrado o inactivo
                    ModelState.AddModelError("", "Credenciales inválidas.");
                }
                catch (EntityCommandCompilationException ex)
                {// Manejo de excepciones específicas de Entity Framework
                    System.Diagnostics.Debug.WriteLine($"Error EF: {ex.Message}");
                    ModelState.AddModelError("", "Error del sistema");
                }
                catch (Exception ex)
                {// Manejo de excepciones generales
                    System.Diagnostics.Debug.WriteLine($"Error general: {ex.Message}");
                    ModelState.AddModelError("", "Error inesperado");
                }
            }

            return View(model);
        }
        // Método para cerrar sesión
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
            {// Validar si el correo ya está registrado
                if (db.Usuarios.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Este correo ya está registrado.");
                    return View(model);
                }
                try
                {// Crear un nuevo usuario
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
                    // Limpiar la sesión antes de redirigir
                    TempData["mensaje"] = "Usuario registrado correctamente";
                    return RedirectToAction("Registro");
                }
                catch (Exception ex)
                {// Manejo de excepciones
                    ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                }
            }

            return View(model);
        }

        // Otros métodos: Index, Editar, AdminUser, etc. los puedes mantener igual



        // Método para mostrar la lista de usuarios (solo si el usuario es administrador)
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


        // Método para editar un usuario (solo si el usuario es administrador)

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


        // Método para mostrar la lista de usuarios (solo si el usuario es administrador)
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


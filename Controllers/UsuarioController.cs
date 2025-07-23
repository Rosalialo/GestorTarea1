using GestorTarea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;


namespace GestorTarea.Controllers
{
    //[RequireHttps]
    public class UsuarioController : Controller
    {
        private readonly GestorTareasEntities db = new GestorTareasEntities();

        //Contraseña cifrada con SHA256
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

        // GET: Usuarios/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Usuarios/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Usuarios model)
        {
            if (ModelState.IsValid)
            {
                // Validar con base de datos, incluye el hash
                string hashedPassword = HashHelper.Hash(model.Password);
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == model.Email && u.Password == hashedPassword && u.Activo);

                if (usuario != null)
                {
                    // Registrar fecha de último acceso
                    usuario.FechaUltimoAcceso = DateTime.Now;
                    db.SaveChanges();

                    // Guardar datos de sesión
                    Session["Usuario"] = usuario.Name;
                    Session["Rol"] = usuario.Rol;

                    return RedirectToAction("Crear", "Proyecto"); // Redirigir a la vista principal
                }

                // Este mensaje es genérico para no dar información sensible
                ModelState.AddModelError("", "Credenciales inválidas.");
            }

            return View(model);
        }





        public ActionResult Logout()
        {
            // Limpiar la sesión
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Usuario");
        }
        // GET: Usuario/Registro
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
                {// Verificar si el correo ya está registrado
                    ModelState.AddModelError("Email", "Este correo ya está registrado.");
                    return View(model);
                }
                try
                {
                    var usuario = new Usuarios
                    {// Crear un nuevo usuario
                        Name = model.Name,
                        Email = model.Email,
                        Password = HashHelper.Hash(model.Password),//asignacion de contraseña 
                        Rol = "Usuario", // Asignación por defecto
                        Activo = true,   // Usuario siempre activo al registrarse
                        Fecha = DateTime.Now
                    };

                    db.Usuarios.Add(usuario);
                   

                    db.SaveChanges();
                    // Mensaje de éxito
                    TempData["mensaje"] = "Usuario registrado correctamente";
                    return RedirectToAction("Registro");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error inesperado: " + ex.Message);
                }
            }

            return View(model);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Registro(Usuarios usuario)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            usuario.Fecha = DateTime.Now; // asignar fecha aquí
        //            db.Usuarios.Add(usuario);
        //            db.SaveChanges();
        //            return RedirectToAction("Registro", "Usuario");
        //        }
        //        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
        //        {
        //            foreach (var validationErrors in ex.EntityValidationErrors)
        //            {
        //                foreach (var validationError in validationErrors.ValidationErrors)
        //                {
        //                    ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
        //                }
        //            }
        //        }
        //    }
        //    // Si hay errores, vuelve a mostrar el formulario con mensajes
        //    return View(usuario);
        //}





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


    }





}


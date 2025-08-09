using GestorTarea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace GestorTarea.Controllers
{
    public class ProyectoController : Controller
    {
        private readonly GestorTareasEntities db = new GestorTareasEntities();

        // GET: Proyecto - Vista principal con grid de proyectos
        public ActionResult Index()
        {// Verificar si el usuario está autenticado
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0) return RedirectToAction("Login", "Usuario");
            return View();
        }
        // GET: Proyecto/Crear - Vista para crear un nuevo proyecto
        public ActionResult Crear()
        {
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0)
                return RedirectToAction("Login", "Usuario");

            var proyectos = db.Proyectos
                .Where(p => p.IdUsuarioResponsable == usuarioId || p.UsuarioProyecto.Any(up => up.IdUsuario == usuarioId))
                .ToList();

            return View(proyectos);
        }
        // GET: Proyecto/IndexP/{id} - Vista para ver un proyecto específico
        public ActionResult IndexP(int id)
        {// Verificar si el usuario está autenticado
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0) return RedirectToAction("Login", "Usuario");

            try
            {// Verificar si el usuario tiene permisos para ver el proyecto
                string rolUsuario = Session["Rol"]?.ToString();

                var proyecto = db.Proyectos.FirstOrDefault(p => p.IdProyecto == id);
                if (proyecto == null)
                {
                    TempData["Error"] = "Proyecto no encontrado";
                    return RedirectToAction("Crear");
                }
                // Verificar si el usuario es responsable, colaborador o administrador
                bool esResponsable = proyecto.IdUsuarioResponsable == usuarioId;
                bool esColaborador = db.UsuarioProyecto.Any(pu => pu.IdProyecto == id && pu.IdUsuario == usuarioId);
                bool esAdmin = rolUsuario == "Administrador";

                if (!esResponsable && !esColaborador && !esAdmin)
                {
                    TempData["Error"] = "No tienes permisos para acceder a este proyecto";
                    return RedirectToAction("Index");
                }
                // Cargar el proyecto con sus tareas y usuarios
                proyecto = db.Proyectos
                    .Include(p => p.Tareas)
                    .Include(p => p.Usuarios)
                    .FirstOrDefault(p => p.IdProyecto == id);
                // Verificar si el proyecto tiene tareas y usuarios
                ViewBag.TotalTareas = proyecto.Tareas?.Count() ?? 0;
                ViewBag.TareasCompletadas = proyecto.Tareas?.Count(t => t.Estado == "Completada") ?? 0;
                ViewBag.NombreCreador = proyecto.Usuarios?.Name ?? "Usuario desconocido";
                ViewBag.PuedeEliminar = esResponsable || esAdmin;

                return View(proyecto);
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al cargar el proyecto";
                return RedirectToAction("Index");
            }
        }
        // GET: Proyecto/ObtenerProyectos - Método para obtener los proyectos del usuario actual
        public JsonResult ObtenerProyectos()
        {
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "No autorizado" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var proyectos = db.Proyectos
                    .Where(p => p.IdUsuarioResponsable == usuarioId || p.UsuarioProyecto.Any(up => up.IdUsuario == usuarioId))
                    .Include(p => p.UsuarioProyecto)
                    .Include(p => p.Tareas)
                    .OrderByDescending(p => p.FechaInicio)
                    .ToList()
                    .Select(p => new
                    {
                        id = p.IdProyecto,
                        titulo = p.Nombre,
                        descripcion = p.Descripcion,
                        fechaInicio = p.FechaInicio,
                        estado = p.Estado,
                        memberCount = p.UsuarioProyecto.Count,
                        tareas = p.Tareas
                            .Where(t => t.IdUsuarioAsignado == usuarioId)
                            .Select(t => new
                            {
                                idTarea = t.IdTarea,
                                titulo = t.Titulo,
                                descripcion = t.Descripcion,
                                estado = t.Estado,
                                prioridad = t.Prioridad,
                                fechaLimite = t.FechaLimite
                            })
                            .ToList()
                    });

                return Json(new { success = true, proyectos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "Error al obtener proyectos: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        // POST: Proyecto/CrearProyecto - Método para crear un nuevo proyecto
        public class ProyectoCrearModel
        {
            public string title { get; set; }
            public string description { get; set; }
            public string members { get; set; }
            public List<string> emails { get; set; }
        }
        // POST: Proyecto/CrearProyecto - Método para crear un nuevo proyecto
        [HttpPost]
        public JsonResult CrearProyecto(ProyectoCrearModel model)
        {
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {// Validar los datos del modelo
                if (string.IsNullOrWhiteSpace(model.title))
                    return Json(new { success = false, message = "El título es obligatorio" });

                if (model.title.Length > 100)
                    return Json(new { success = false, message = "El título no puede exceder 100 caracteres" });
                // Validar que el título no exista ya
                var proyecto = new Proyectos
                {
                    Nombre = model.title.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(model.description) ? null : model.description.Trim(),
                    FechaInicio = DateTime.Now,
                    Estado = "Activo",
                    IdUsuarioResponsable = usuarioId
                };

                db.Proyectos.Add(proyecto);
                db.SaveChanges();

                db.UsuarioProyecto.Add(new UsuarioProyecto
                {
                    IdProyecto = proyecto.IdProyecto,
                    IdUsuario = usuarioId
                });
                // Si el proyecto tiene miembros adicionales, agregarlos
                if (model.members == "conMiembros" && model.emails != null && model.emails.Count > 0)
                {
                    foreach (var email in model.emails)
                    {
                        var usuario = db.Usuarios.FirstOrDefault(u => u.Email == email);
                        if (usuario != null)
                        {// Verificar si el usuario ya es miembro del proyecto
                            bool yaEsMiembro = db.UsuarioProyecto.Any(pu => pu.IdProyecto == proyecto.IdProyecto && pu.IdUsuario == usuario.IdUsuario);
                            if (!yaEsMiembro)
                            {
                                db.UsuarioProyecto.Add(new UsuarioProyecto
                                {
                                    IdProyecto = proyecto.IdProyecto,
                                    IdUsuario = usuario.IdUsuario
                                });
                            }
                        }
                    }
                }

                db.SaveChanges();
                // Obtener el nombre del creador del proyecto
                var nombreCreador = db.Usuarios.FirstOrDefault(u => u.IdUsuario == usuarioId)?.Name ?? "Usuario";

                return Json(new
                {// Retornar el proyecto creado
                    success = true,
                    message = "Proyecto creado exitosamente",
                    proyecto = new
                    {
                        id = proyecto.IdProyecto,
                        nombre = proyecto.Nombre,
                        descripcion = proyecto.Descripcion,
                        fechaInicio = proyecto.FechaInicio,
                        estado = proyecto.Estado,
                        nombreCreador = nombreCreador,
                        memberCount = 1 + (model.emails?.Count ?? 0)
                    }
                });
            }
            catch (Exception)
            {// Manejo de excepciones
                Response.StatusCode = 500;
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }
        // POST: Proyecto/EliminarProyecto - Método para eliminar un proyecto
        [HttpPost]
        public JsonResult EliminarProyecto(int id)
        {// Verificar si el usuario está autenticado
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {// Verificar si el usuario tiene permisos para eliminar el proyecto
                string rolUsuario = Session["Rol"]?.ToString();
                // Verificar si el proyecto existe y si el usuario es responsable o administrador
                var proyecto = db.Proyectos.Find(id);
                if (proyecto == null)
                    return Json(new { success = false, message = "Proyecto no encontrado" });
                // Verificar si el usuario es responsable del proyecto o tiene rol de administrador
                if (proyecto.IdUsuarioResponsable != usuarioId && rolUsuario != "Administrador")
                    return Json(new { success = false, message = "No tienes permisos para eliminar este proyecto" });
                // Eliminar las tareas asociadas al proyecto
                var tareasDelProyecto = db.Tareas.Where(t => t.IdProyecto == id).ToList();
                foreach (var tarea in tareasDelProyecto)
                {
                    db.Tareas.Remove(tarea);
                }

                db.Proyectos.Remove(proyecto);
                db.SaveChanges();
                // Eliminar las relaciones de usuario con el proyecto
                return Json(new { success = true, message = "Proyecto eliminado correctamente" });
            }
            catch (Exception ex)
            {// Manejo de excepciones
                Response.StatusCode = 500;
                return Json(new { success = false, message = "Error al eliminar el proyecto: " + ex.Message });
            }
        }

        // Método auxiliar para obtener el ID del usuario actual
        private int ObtenerUsuarioActualId()
        {// Verificar si el usuario ya está en sesión
            if (Session["UsuarioId"] != null)
                return (int)Session["UsuarioId"];

            if (Session["Usuario"] != null)
            {// Buscar el usuario en la base de datos
                var usuario = db.Usuarios.FirstOrDefault(u => u.Name == Session["Usuario"].ToString() && u.Activo);
                if (usuario != null)
                {
                    Session["UsuarioId"] = usuario.IdUsuario;
                    return usuario.IdUsuario;
                }
            }
            return 0;
        }

        // Otros métodos (Crear, Editar, EliminarTarea, etc) deberían incluir esta misma verificación de sesión al inicio.
        public JsonResult ObtenerTareasPorProyecto(int idProyecto)
        {// Verificar si el usuario está autenticado
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "No autorizado" }, JsonRequestBehavior.AllowGet);
            }

            // Verificar que el usuario es responsable o miembro del proyecto
            bool esMiembro = db.Proyectos.Any(p =>
                p.IdProyecto == idProyecto &&
                (p.IdUsuarioResponsable == usuarioId || p.UsuarioProyecto.Any(up => up.IdUsuario == usuarioId))
            );

            if (!esMiembro)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "No tienes acceso a este proyecto" }, JsonRequestBehavior.AllowGet);
            }
            // Obtener las tareas del proyecto
            var tareas = db.Tareas
                .Where(t => t.IdProyecto == idProyecto)
                .Select(t => new
                {
                    t.IdTarea,
                    t.Titulo,
                    t.Descripcion,
                    t.Estado,
                    t.Prioridad,
                    t.FechaLimite,
                    NombreUsuario = t.Usuarios != null ? t.Usuarios.Name : null
                })
                .ToList();

            return Json(new { success = true, tareas }, JsonRequestBehavior.AllowGet);
        }
        // Método para crear una nueva tarea en un proyecto
        [HttpPost]
        public JsonResult CrearTarea(int idProyecto, string titulo, string descripcion, string estado)
        {
            int usuarioId = ObtenerUsuarioActualId();
            if (usuarioId == 0)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "No autorizado" });
            }

            // Verificar que usuario pertenece al proyecto
            bool esMiembro = db.Proyectos.Any(p =>
                p.IdProyecto == idProyecto &&
                (p.IdUsuarioResponsable == usuarioId || p.UsuarioProyecto.Any(up => up.IdUsuario == usuarioId))
            );
            // Verificar si el usuario es responsable o miembro del proyecto
            if (!esMiembro)
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "No tienes acceso a este proyecto" });
            }
            // Validar los datos de la tarea
            if (string.IsNullOrWhiteSpace(titulo))
                return Json(new { success = false, message = "El título es obligatorio" });

            var tarea = new Tareas
            {
                IdProyecto = idProyecto,
                Titulo = titulo.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                Estado = estado ?? "pendiente",
                IdUsuarioAsignado = usuarioId, // Opcional: asignar a quien crea la tarea
                FechaLimite = null,
                Prioridad = "media"
            };

            db.Tareas.Add(tarea);
            db.SaveChanges();

            return Json(new { success = true, message = "Tarea creada correctamente", tareaId = tarea.IdTarea });
        }

        // Método para editar una tarea existente
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}

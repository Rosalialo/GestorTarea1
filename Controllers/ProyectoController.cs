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
        {
            return View();
        }



        public ActionResult IndexP(int id)
        {
            try
            {
                int usuarioId = ObtenerUsuarioActualId();

                var proyecto = db.Proyectos
                    .Include(p => p.Tareas)
                    .FirstOrDefault(p => p.IdProyecto == id);

                if (proyecto == null)
                {
                    TempData["Error"] = "Proyecto no encontrado";
                    return RedirectToAction("Index");
                }

                if (proyecto.IdUsuarioResponsable != usuarioId)
                {
                    TempData["Error"] = "No tienes permisos para acceder a este proyecto";
                    return RedirectToAction("Index");
                }

                ViewBag.TotalTareas = proyecto.Tareas?.Count() ?? 0;
                ViewBag.TareasCompletadas = proyecto.Tareas?.Count(t => t.Estado == "Completada") ?? 0;

                return View(proyecto); // ✅ Aquí estaba el error
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al cargar el proyecto";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public JsonResult ObtenerProyectos()
        {
            try
            {
                // Obtener ID del usuario actual (ajusta según tu sistema de autenticación)
                int usuarioId = ObtenerUsuarioActualId();

                var proyectos = db.Proyectos
                    .Where(p => p.IdUsuarioResponsable == usuarioId)
                    .OrderByDescending(p => p.FechaInicio)
                    .Select(p => new {
                        id = p.IdProyecto,
                        titulo = p.Nombre,
                        descripcion = p.Descripcion,
                        fechaInicio = p.FechaInicio,
                        estado = p.Estado,
                        members = "solo" // Por defecto, puedes ajustar esta lógica
                    })
                    .ToList();

                return Json(new { success = true, proyectos }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log del error si tienes sistema de logging
                return Json(new { success = false, message = "Error al obtener proyectos" }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Crear proyecto desde el modal (nuevo método para tu frontend)
        [HttpPost]
        public JsonResult CrearProyecto()
        {
            try
            {
                // Leer el JSON del request
                var json = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

                string title = data.title;
                string description = data.description;
                string members = data.members;

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(title))
                {
                    return Json(new
                    {
                        success = false,
                        message = "El título es obligatorio"
                    });
                }

                if (title.Length > 100)
                {
                    return Json(new
                    {
                        success = false,
                        message = "El título no puede exceder 100 caracteres"
                    });
                }

                // Obtener ID del usuario actual
                int usuarioId = ObtenerUsuarioActualId();

                // Crear nuevo proyecto
                var proyecto = new Proyectos
                {
                    Nombre = title.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    FechaInicio = DateTime.Now,
                    Estado = "Activo",
                    IdUsuarioResponsable = usuarioId
                };

                db.Proyectos.Add(proyecto);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Proyecto creado exitosamente",
                    proyecto = new
                    {
                        id = proyecto.IdProyecto,
                        nombre = proyecto.Nombre,
                        descripcion = proyecto.Descripcion,
                        fechaInicio = proyecto.FechaInicio,
                        estado = proyecto.Estado
                    }
                });
            }
            catch (Exception ex)
            {
                // Log del error si tienes sistema de logging
                return Json(new
                {
                    success = false,
                    message = "Error interno del servidor"
                });
            }
        }

        // Método auxiliar para obtener el ID del usuario actual
        private int ObtenerUsuarioActualId()
        {
            // Implementa según tu sistema de autenticación
            // Ejemplos:

            // Si usas Session:
            if (Session["UsuarioId"] != null)
            {
                return (int)Session["UsuarioId"];
            }

            // Si usas algún claim o cookie personalizada:
            // return int.Parse(User.Identity.Name); // si guardas el ID en Name

            // Si tienes una tabla de usuarios y usas el nombre:
            // var usuario = db.Usuarios.FirstOrDefault(u => u.NombreUsuario == User.Identity.Name);
            // return usuario?.IdUsuario ?? 0;

            // Temporal para testing (reemplaza con tu lógica):
            return 1; // ID de usuario hardcodeado - CAMBIAR POR TU LÓGICA
        }

        // GET: Proyecto/Crear (mantener para compatibilidad)
        public ActionResult Crear()
        {
            ViewBag.Usuarios = db.Usuarios.Where(u => u.Activo).ToList();
            return View();
        }

        // POST: Crear proyecto (método original - mantener para compatibilidad)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(Models.Proyectos proyecto)
        {
            ViewBag.Usuarios = db.Usuarios.Where(u => u.Activo).ToList();
            if (ModelState.IsValid)
            {
                proyecto.FechaInicio = DateTime.Now;
                proyecto.Estado = "Activo";
                db.Proyectos.Add(proyecto);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(proyecto);
        }

        // POST: Crear proyecto AJAX (método original - mantener para compatibilidad)
        [HttpPost]
        public JsonResult CrearAjax(Proyectos proyecto)
        {
            if (ModelState.IsValid)
            {
                proyecto.FechaInicio = DateTime.Now;
                proyecto.Estado = "Activo";
                db.Proyectos.Add(proyecto);
                db.SaveChanges();
                return Json(new { success = true, id = proyecto.IdProyecto });
            }
            return Json(new { success = false });
        }

        // GET: Detalles del proyecto
        public ActionResult Detalles(int id)
        {
            var proyecto = db.Proyectos.Find(id);
            if (proyecto == null)
                return HttpNotFound();
            return View(proyecto);
        }

        // GET: Editar proyecto
        public ActionResult Editar(int id)
        {
            var proyecto = db.Proyectos.Find(id);
            if (proyecto == null)
                return HttpNotFound();

            ViewBag.Usuarios = db.Usuarios.Where(u => u.Activo).ToList();
            return View(proyecto);
        }

        // POST: Editar proyecto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(Proyectos proyecto)
        {
            if (ModelState.IsValid)
            {
                db.Entry(proyecto).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Usuarios = db.Usuarios.Where(u => u.Activo).ToList();
            return View(proyecto);
        }

        // POST: Eliminar proyecto
        [HttpPost]
        public JsonResult EliminarTarea(int id)
        {
            try
            {
                int usuarioId = ObtenerUsuarioActualId();

                var tarea = db.Tareas.Find(id);
                if (tarea == null)
                    return Json(new { success = false, message = "Tarea no encontrada." });

                if (tarea.IdUsuarioAsignado != usuarioId)
                    return Json(new { success = false, message = "No tienes permiso para eliminar esta tarea." });

                db.Tareas.Remove(tarea);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // GET: Listar proyectos (vista alternativa)
        public ActionResult Indexes()
        {
            int usuarioId = ObtenerUsuarioActualId();
            var proyectos = db.Proyectos
                .Where(p => p.IdUsuarioResponsable == usuarioId)
                .OrderByDescending(p => p.FechaInicio)
                .ToList();

            return View(proyectos);
        }

        // GET: Listar tareas de un proyecto
        public ActionResult ListTareas(int proyectoId)
        {
            var proyecto = db.Proyectos.Include("Tareas").FirstOrDefault(p => p.IdProyecto == proyectoId);
            if (proyecto == null)
                return HttpNotFound();

            // Verificar que el usuario actual puede ver este proyecto
            int usuarioId = ObtenerUsuarioActualId();
            if (proyecto.IdUsuarioResponsable != usuarioId)
                return new HttpUnauthorizedResult();

            var tareas = proyecto.Tareas.ToList();
            return View(tareas);
        }
        [HttpPost]
        public JsonResult GuardarTarea(int idProyecto, string titulo, string descripcion, string prioridad, string estado, DateTime? fechaVencimiento, int progreso)
        {
            try
            {
                var nuevaTarea = new Tareas
                {
                    IdProyecto = idProyecto,
                    Titulo = titulo,
                    Descripcion = descripcion,
                    Prioridad = prioridad,
                    Estado = estado,
                    FechaCreacion = DateTime.Now,
                    FechaLimite = fechaVencimiento,
                    Progreso = progreso,
                    IdUsuarioAsignado = ObtenerUsuarioActualId() // Asigna al usuario actual
                };

                db.Tareas.Add(nuevaTarea);
                db.SaveChanges();

                return Json(new { success = true, id = nuevaTarea.IdTarea });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // Dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }



        //[HttpPost]
        //public JsonResult EliminarTarea(int id)
        //{
        //    try
        //    {
        //        using (var db = new GestorTareasEntities())
        //        {
        //            var tarea = db.Tareas.Find(id);
        //            if (tarea != null)
        //            {
        //                db.Tareas.Remove(tarea);
        //                db.SaveChanges();
        //                return Json(new { success = true });
        //            }
        //            return Json(new { success = false, message = "Tarea no encontrada." });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}



        public JsonResult ObtenerTarea(int id)
        {
            using (var db = new GestorTareasEntities())
            {
                var tarea = db.Tareas.Find(id);
                return Json(tarea, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult EditarTarea(Tareas tareaEditada)
        {
            try
            {
                using (var db = new GestorTareasEntities())
                {
                    var tarea = db.Tareas.Find(tareaEditada.IdTarea);
                    if (tarea != null)
                    {
                        tarea.Titulo = tareaEditada.Titulo;
                        tarea.Descripcion = tareaEditada.Descripcion;
                        tarea.Prioridad = tareaEditada.Prioridad;
                        tarea.Estado = tareaEditada.Estado;
                        tarea.FechaLimite = tareaEditada.FechaLimite;
                        tarea.Progreso = tareaEditada.Progreso;

                        db.SaveChanges();
                        return Json(new { success = true });
                    }
                    return Json(new { success = false, message = "Tarea no encontrada." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerTareasPorProyecto(int idProyecto)
        {
            try
            {
                int usuarioId = ObtenerUsuarioActualId();

                var proyecto = db.Proyectos.Include("Tareas").FirstOrDefault(p => p.IdProyecto == idProyecto);
                if (proyecto == null || proyecto.IdUsuarioResponsable != usuarioId)
                {
                    return Json(new { success = false, message = "No autorizado o proyecto no encontrado" }, JsonRequestBehavior.AllowGet);
                }

                var tareas = proyecto.Tareas.Select(t => new
                {
                    id = t.IdTarea,
                    titulo = t.Titulo,
                    descripcion = t.Descripcion,
                    prioridad = t.Prioridad,
                    estado = t.Estado,
                    fechaLimite = t.FechaLimite?.ToString("yyyy-MM-dd"),
                    progreso = t.Progreso,
                    usuarioId = t.IdUsuarioAsignado
                }).ToList();

                return Json(new { success = true, tareas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}
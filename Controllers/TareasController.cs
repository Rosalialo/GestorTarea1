using Antlr.Runtime.Misc;
using GestorTarea.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using System.Web.Mvc;


public class TareasController : ApiController
{
    //private static string SecretKey = "123456";

//    //private ClaimsPrincipal ValidateToken(string token)
//    //{
//    //    var tokenHandler = new JwtSecurityTokenHandler();
//    //    var key = Encoding.ASCII.GetBytes(SecretKey);
//    //    try
//    //    {
//    //        var parameters = new TokenValidationParameters
//    //        {
//    //            ValidateIssuerSigningKey = true,
//    //            IssuerSigningKey = new SymmetricSecurityKey(key),
//    //            ValidateIssuer = false,
//    //            ValidateAudience = false,
//    //            ClockSkew = TimeSpan.Zero
//    //        };
//    //        SecurityToken validatedToken;
//    //        return tokenHandler.ValidateToken(token, parameters, out validatedToken);
//    //    }
//    //    catch
//    //    {
//    //        return null;
//    //    }
//    //}

//    // ✅ Obtener tareas de un proyecto (sin cambios, está perfecto)
//    [HttpGet]
//    [Route("lista/{idProyecto}")]
//    public IHttpActionResult GetTareasPorProyecto(int idProyecto)
//    {
//        var authHeader = Request.Headers.Authorization;
//        if (authHeader == null || authHeader.Scheme != "Bearer")
//            return Unauthorized();

//        var principal = ValidateToken(authHeader.Parameter);
//        if (principal == null)
//            return Unauthorized();

//        using (var db = new GestorTareasEntities())
//        {
//            // ✅ VERIFICAR: Solo usuarios que pertenecen al proyecto pueden ver sus tareas
//            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "UsuarioId");
//            if (userIdClaim == null)
//                return Unauthorized();

//            int usuarioId = int.Parse(userIdClaim.Value);

//            // Verificar que el usuario pertenece al proyecto
//            var perteneceAlProyecto = db.UsuarioProyecto
//                .Any(pu => pu.IdProyecto == idProyecto && pu.IdUsuario == usuarioId);

//            if (!perteneceAlProyecto)
//            {
//                return BadRequest("No tienes acceso a este proyecto");
//            }

//            var tareas = db.Tareas
//                .Where(t => t.IdProyecto == idProyecto) // ✅ Todas las tareas del proyecto
//                .Select(t => new
//                {
//                    t.IdTarea,
//                    t.Titulo,
//                    t.Estado,
//                    t.Descripcion,
//                    t.IdUsuarioAsignado,
//                    NombreUsuario = db.Usuarios
//                                      .Where(u => u.IdUsuario == t.IdUsuarioAsignado)
//                                      .Select(u => u.Name)
//                                      .FirstOrDefault()
//                })
//                .ToList();

//            return Ok(tareas);
//        }
//    }

//    // ✅ Crear tarea en un proyecto (mejorado)
//    [HttpPost]
//    [Route("crear")]
//    public IHttpActionResult CrearTarea([FromBody] Tareas tarea)
//    {
//        if (tarea == null || string.IsNullOrEmpty(tarea.Titulo) || tarea.IdProyecto == 0)
//            return BadRequest("Datos inválidos: falta título o proyecto");

//        var authHeader = Request.Headers.Authorization;
//        if (authHeader == null || authHeader.Scheme != "Bearer")
//            return Unauthorized();

//        var principal = ValidateToken(authHeader.Parameter);
//        if (principal == null)
//            return Unauthorized();

//        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "UsuarioId");
//        if (userIdClaim == null)
//            return Unauthorized();

//        int usuarioId = int.Parse(userIdClaim.Value);

//        using (var db = new GestorTareasEntities())
//        {
//            // ✅ VERIFICAR: El usuario debe pertenecer al proyecto
//            var perteneceAlProyecto = db.UsuarioProyecto
//                .Any(pu => pu.IdProyecto == tarea.IdProyecto && pu.IdUsuario == usuarioId);

//            if (!perteneceAlProyecto)
//            {
//                return BadRequest("No tienes acceso a este proyecto");
//            }

//            // ✅ MEJORADO: Valores por defecto más consistentes
//            tarea.FechaCreacion = DateTime.Now;
//            tarea.IdUsuarioAsignado = tarea.IdUsuarioAsignado ?? usuarioId;

//            // Asegurar estado válido
//            if (string.IsNullOrEmpty(tarea.Estado))
//                tarea.Estado = "pendiente";

//            try
//            {
//                db.Tareas.Add(tarea);
//                db.SaveChanges();

//                return Ok(new
//                {
//                    Mensaje = "Tarea creada correctamente",
//                    IdTarea = tarea.IdTarea,
//                    Titulo = tarea.Titulo,
//                    Estado = tarea.Estado
//                });
//            }
//            catch (Exception ex)
//            {
//                return InternalServerError(new Exception("Error al guardar la tarea: " + ex.Message));
//            }
//        }
//    }

//    // ✅ NUEVO: Actualizar tarea existente
//    [HttpPut]
//    [Route("actualizar/{id}")]
//    public IHttpActionResult ActualizarTarea(int id, [FromBody] Tareas tareaActualizada)
//    {
//        if (tareaActualizada == null || string.IsNullOrEmpty(tareaActualizada.Titulo))
//            return BadRequest("Datos inválidos");

//        var authHeader = Request.Headers.Authorization;
//        if (authHeader == null || authHeader.Scheme != "Bearer")
//            return Unauthorized();

//        var principal = ValidateToken(authHeader.Parameter);
//        if (principal == null)
//            return Unauthorized();

//        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "UsuarioId");
//        if (userIdClaim == null)
//            return Unauthorized();

//        int usuarioId = int.Parse(userIdClaim.Value);

//        using (var db = new GestorTareasEntities())
//        {
//            var tareaExistente = db.Tareas.FirstOrDefault(t => t.IdTarea == id);
//            if (tareaExistente == null)
//                return NotFound();

//            // Verificar acceso al proyecto
//            var perteneceAlProyecto = db.UsuarioProyecto
//                .Any(pu => pu.IdProyecto == tareaExistente.IdProyecto && pu.IdUsuario == usuarioId);

//            if (!perteneceAlProyecto)
//                return BadRequest("No tienes acceso a esta tarea");

//            // Actualizar campos
//            tareaExistente.Titulo = tareaActualizada.Titulo;
//            tareaExistente.Descripcion = tareaActualizada.Descripcion;
//            tareaExistente.Estado = tareaActualizada.Estado;

//            try
//            {
//                db.SaveChanges();
//                return Ok(new { Mensaje = "Tarea actualizada correctamente" });
//            }
//            catch (Exception ex)
//            {
//                return InternalServerError(new Exception("Error al actualizar: " + ex.Message));
//            }
//        }
//    }

//    // ✅ NUEVO: Eliminar tarea
//    [HttpDelete]
//    [Route("eliminar/{id}")]
//    public IHttpActionResult EliminarTarea(int id)
//    {
//        var authHeader = Request.Headers.Authorization;
//        if (authHeader == null || authHeader.Scheme != "Bearer")
//            return Unauthorized();

//        var principal = ValidateToken(authHeader.Parameter);
//        if (principal == null)
//            return Unauthorized();

//        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "UsuarioId");
//        if (userIdClaim == null)
//            return Unauthorized();

//        int usuarioId = int.Parse(userIdClaim.Value);

//        using (var db = new GestorTareasEntities())
//        {
//            var tarea = db.Tareas.FirstOrDefault(t => t.IdTarea == id);
//            if (tarea == null)
//                return NotFound();

//            // Verificar acceso al proyecto
//            var perteneceAlProyecto = db.UsuarioProyecto
//                .Any(pu => pu.IdProyecto == tarea.IdProyecto && pu.IdUsuario == usuarioId);

//            if (!perteneceAlProyecto)
//                return BadRequest("No tienes acceso a esta tarea");

//            try
//            {
//                db.Tareas.Remove(tarea);
//                db.SaveChanges();
//                return Ok(new { Mensaje = "Tarea eliminada correctamente" });
//            }
//            catch (Exception ex)
//            {
//                return InternalServerError(new Exception("Error al eliminar: " + ex.Message));
//            }
//        }
//    }

//    // Acción para obtener tareas asignadas al usuario en sesión
//        public ActionResult ObtenerTareas()
//    {
//        if (Session["UsuarioId"] == null)
//        {
//            // Si no hay sesión activa, devolver error 401
//            return new HttpStatusCodeResult(401, "No autorizado");
//        }

//        int usuarioId = (int)Session["UsuarioId"];
//        var tareas = db.Tareas
//            .Where(t => t.IdUsuarioResponsable == usuarioId)
//            .Select(t => new
//            {
//                t.IdTarea,
//                t.Titulo,
//                t.Estado,
//                t.Descripcion,
//                t.FechaCreacion
//            })
//            .ToList();

//        return Json(tareas, JsonRequestBehavior.AllowGet);
//    }

   


//}
}


    

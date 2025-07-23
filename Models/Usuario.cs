using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GestorTarea.Models
{
    public class User
    {
        // GET: User
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contraseña requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Debe tener al menos 6 caracteres")]
        public string Password { get; set; }

        // No se muestran en la vista, pero se asignan por defecto en el controlador
        public string Rol { get; set; } = "Usuario";
        public bool Activo { get; set; } = true;

        public DateTime Fecha
        {
            get; set;

        }
    }
}
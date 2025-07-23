using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GestorTarea.Controllers
{
    public class TareaController : Controller
    {
        // GET: Tarea
        public ActionResult Index()
        {
            return View();
        }
    }
}
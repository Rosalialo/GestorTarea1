using GestorTarea.App_Start;
using GestorTarea.Models;
using System;
using System.Web.Http;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using static GestorTarea.Controllers.UsuarioController;

namespace GestorTarea
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {

       
            AreaRegistration.RegisterAllAreas();

            // Registrar rutas Web API
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Registrar rutas MVC
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Registrar bundles si los tienes
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

    }

}





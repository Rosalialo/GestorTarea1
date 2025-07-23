using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace GestorTarea.Models
{
    public class conexiondb
    {
        string cadena = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=GestorTareas;Integrated Security=True";
        public SqlConnection Conectar()
        {
            SqlConnection conexion = new SqlConnection(cadena);
            try
            {
                Console.WriteLine("Intentando conectar a la base de datos...");
                conexion.Open();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al conectar a la base de datos: " + ex.Message);
            }
            return conexion;
        }
    }
}
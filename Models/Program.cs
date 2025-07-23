using GestorTarea.Models;
using System;

class Program
{
    static void Main(string[] args)
    {
        conexiondb conexion = new conexiondb();
        try
        {
            using (var conn = conexion.Conectar())
            {
                Console.WriteLine("✅ Conexión exitosa.");
                conn.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error en la conexión: " + ex.Message);
        }
    }
}
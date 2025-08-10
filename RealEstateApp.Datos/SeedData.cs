using RealEstateApp.Entidades;
using System;

namespace RealEstateApp.Datos
{
    public static class SeedData
    {
        public static void Inicializar(AppDbContext context)
        {
       
            if (context.Propiedades.Any())
                return;

       
            context.SaveChanges();
        }
    }
}

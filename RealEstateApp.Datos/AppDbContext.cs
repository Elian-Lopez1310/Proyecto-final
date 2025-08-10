using Microsoft.EntityFrameworkCore;
using RealEstateApp.Entidades;

namespace RealEstateApp.Datos
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<Propiedad> Propiedades { get; set; }
        public DbSet<Agente> Agentes { get; set; }
        public DbSet<ImagenPropiedad> ImagenesPropiedad { get; set; }
        public DbSet<Mejora> Mejoras { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<MensajeChat> MensajesChat { get; set; }   
        public DbSet<Oferta> Ofertas { get; set; }
        public DbSet<ChatMensaje> ChatMensajes { get; set; }   

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

    
            modelBuilder.Entity<Propiedad>()
                .HasMany(p => p.Mejoras)
                .WithMany(m => m.Propiedades)
                .UsingEntity(j => j.ToTable("PropiedadMejora"));

    
            modelBuilder.Entity<ChatMensaje>(e =>
            {
                e.ToTable("ChatMensajes");
                e.HasKey(x => x.Id);

               
                e.Property(x => x.Texto)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();

                e.Property(x => x.Fecha)
                    .HasColumnType("datetime2")
                    .IsRequired();

         
                e.HasIndex(x => x.PropiedadId);
                e.HasIndex(x => x.EmisorId);
                e.HasIndex(x => x.ReceptorId);

              
                e.HasOne<Propiedad>()
                    .WithMany()
                    .HasForeignKey(x => x.PropiedadId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<Usuario>()     
                    .WithMany()
                    .HasForeignKey(x => x.EmisorId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne<Usuario>()     
                    .WithMany()
                    .HasForeignKey(x => x.ReceptorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

         
        }
    }
}

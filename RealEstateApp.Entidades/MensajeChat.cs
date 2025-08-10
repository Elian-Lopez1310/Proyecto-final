namespace RealEstateApp.Entidades
{
    public class MensajeChat
    {
        public int Id { get; set; }

        public int AgenteId { get; set; }
        public int ClienteId { get; set; }
        public int? PropiedadId { get; set; }

        public string Texto { get; set; }        
        public string Remitente { get; set; }     
        public string Contenido { get; set; }  
        public DateTime Fecha { get; set; } = DateTime.Now;
        public bool EsCliente { get; set; }      


        public Agente Agente { get; set; }
        public Cliente Cliente { get; set; }
        public Propiedad Propiedad { get; set; }
    }
}

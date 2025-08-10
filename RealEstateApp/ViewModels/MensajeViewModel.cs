namespace RealEstateApp.Web.ViewModels
{
    public class MensajeViewModel
    {
        public string Texto { get; set; }
        public bool EsEnviadoPorCliente { get; set; }
        public DateTime Fecha { get; set; }

        public string Remitente { get; set; } // "cliente" o "agente"

    }
}

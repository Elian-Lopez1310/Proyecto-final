namespace RealEstateApp.Web.ViewModels
{
    public class ChatViewModel
    {
        public int ChatId { get; set; }
        public int ClienteId { get; set; }
        public int AgenteId { get; set; }

        public string NombreAgente { get; set; }
        public string FotoAgente { get; set; }

        public string NuevoMensaje { get; set; }  // ✅ Propiedad para escribir nuevo mensaje

        public AgenteViewModel Agente { get; set; }  // ✅ Info completa del agente

        public List<MensajeViewModel> Mensajes { get; set; } = new();

    }

}
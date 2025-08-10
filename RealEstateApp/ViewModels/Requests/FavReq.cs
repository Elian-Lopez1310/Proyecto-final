using System.Text.Json.Serialization;

namespace RealEstateApp.Web.ViewModels.Requests
{
    public class FavReq
    {
        public string Codigo { get; set; }

        // Si el front envía "codigo" en minúscula, también lo mapeamos
        [JsonPropertyName("codigo")]
        public string CodigoLower
        {
            set => Codigo = value;
        }
    }
}

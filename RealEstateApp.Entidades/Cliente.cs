namespace RealEstateApp.Entidades
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }

     
        public ICollection<MensajeChat> MensajesChat { get; set; }
    }
}

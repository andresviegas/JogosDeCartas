using Microsoft.Extensions.Options;

namespace guerraServer.Models
{
    public class Carta
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Valor { get; set; }
        public int Posicao { get; set; }
    }
}
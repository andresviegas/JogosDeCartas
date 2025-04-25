namespace guerraServer.Models
{
    public class Mao
    {
        public int IdJogador { get; set; }
        public List<int> Cartas { get; set; }

        public Mao(int idJogador, List<int> cartas)
        { 
            IdJogador = idJogador;
            Cartas = cartas; 
        }
    }
}

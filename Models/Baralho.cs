namespace guerraServer.Models
{
    public class Baralho
    {
        public int Id { get; set; } 
        //public List<Carta> Cartas {  get; set; }   

        public string tipo { get; set; }
        public List<int> Cartas { get; set; }

        public Baralho(int id, string tipo)
        {
            if (tipo == "sueca")
            {
                Id = id;
                Cartas = Enumerable.Range(1, 40).ToList();  // Cria uma lista de 1 a 40
            }
            else
            {
                Id = id;
                Cartas = Enumerable.Range(1, 52).ToList();
            }
        }

    }
}



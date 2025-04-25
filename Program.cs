using guerraServer.Models;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Adicionando a configuração do CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", builder =>
    {
        // Permite requisições de localhost:5173 (onde o React está rodando)
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()  // Permite qualquer método (GET, POST, etc.)
               .AllowAnyHeader(); // Permite qualquer cabeçalho
    });
});


builder.Services.AddSwaggerGen();

var app = builder.Build();

List<Mao> maos = new List<Mao>();
List<Jogador> jogadores = new List<Jogador>();
List<Carta> baralhoSueca = new List<Carta>();
Baralho baralhoGeral = new Baralho(0, "sueca");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

// Habilita o CORS para a política "AllowLocalhost"
app.UseCors("AllowLocalhost");

app.UseHttpsRedirection();

// Este endpoint é chamado quando o utilizador clica no botão "Criar Jogadores"
app.MapPost("/CriarJogadores", (List<string> nomes) =>
{
    Console.WriteLine($"Endpoint para criar jogadores chamado");

    foreach (var nome in nomes)
    {
        var jogador = new Jogador
        {
            Id = jogadores.Count + 1,
            Nome = nome,
            temMao = false
        };

        Console.WriteLine($"Criado jogador {jogador.Nome} com o id {jogador.Id}");
        jogadores.Add(jogador);
    }

    return Results.Ok(jogadores);
})
.WithName("PostJogadores");


app.MapGet("/GetJogadores", () =>
{
    Console.WriteLine("Chamado endpoint /GetJogadores");
    return Results.Ok(jogadores);
});

app.MapPost("/Baralhar", (int id) =>
{
    Console.WriteLine($"Chamado endpoint para baralhar o baralho com o id {id}");
    if (baralhoGeral == null)
    {
        return Results.NotFound($"Baralho geral não encontrado.");
    }
    var random = new Random();
    baralhoGeral.Cartas = baralhoGeral.Cartas.OrderBy(c => random.Next()).ToList();

    return Results.Ok(new { mensagem = "Baralho geral baralhado.", baralho = baralhoGeral });
});

app.MapPost("/DistribuirMaosGuerra", () =>
{
    Console.WriteLine("Chamado o endpoint para distribuir cartas (mãos) pelos jogadores");
    var primeiraParte = baralhoGeral.Cartas.Take(20).ToList();
    Console.WriteLine($"A primeira parte é {primeiraParte}");
    var segundaParte = baralhoGeral.Cartas.Skip(20).Take(20).ToList();
    Console.WriteLine($"A segunda parte é {segundaParte}");

    bool primeiraMetadeDistribuida = false;

    foreach (var jogador in jogadores)
    {
        if (jogador.temMao == false)
        {
            Console.WriteLine($"O jogador {jogador.Nome} não tinha nenhuma mão por isso vai-lhe ser atribuída uma");
            jogador.temMao = true;

            if (!primeiraMetadeDistribuida)
            {
                Console.WriteLine($"O jogador {jogador.Nome} vai receber a primeira parte do baralho");
                var mao = new Mao(jogador.Id, primeiraParte);
                maos.Add(mao);
                primeiraMetadeDistribuida = true;
            }
            else
            {
                Console.WriteLine($"O jogador {jogador.Nome} vai receber a segunda parte do baralho");
                var mao = new Mao(jogador.Id, segundaParte);
                maos.Add(mao);
            }
        }
    }

    return Results.Ok(new { jogadores, maos });
});


app.MapPost("/BaralharCartasJogadores", (int idJogador) =>
{
    var maoJogador = maos.FirstOrDefault(b => b.IdJogador == idJogador);
    if (maoJogador == null)
    {
        return Results.NotFound($"Baralho do jogador com ID {idJogador} não encontrado.");
    }

    var random = new Random();
    maoJogador.Cartas = maoJogador.Cartas.OrderBy(c => random.Next()).ToList();

    // Retornar resposta com sucesso e os baralhos atualizados
    var resultado = new
    {
        Jogador = new
        {
            Id = maoJogador.IdJogador,
            CartasBaralhadas = maoJogador.Cartas
        }
    };

    return Results.Ok(resultado);
});

app.MapGet("/IniciarBatalha", (int id1, int id2) =>
{
    Console.WriteLine($"Chamado o endpoint para iniciar batalha entre o jogador {id1} e {id2}");
    // Buscar jogador 1 com base no id1
    var jogador1 = jogadores.FirstOrDefault(j => j.Id == id1);
    if (jogador1 == null)
    {
        return Results.NotFound($"Jogador com ID {id1} não encontrado.");
    }

    // Buscar jogador 2 com base no id2
    var jogador2 = jogadores.FirstOrDefault(j => j.Id == id2);
    if (jogador2 == null)
    {
        return Results.NotFound($"Jogador com ID {id2} não encontrado.");
    }

    // Buscar a mão do jogador 1
    var maoJogador1 = maos.FirstOrDefault(b => b.IdJogador == jogador1.Id);
    if (maoJogador1 == null || maoJogador1.Cartas.Count == 0)
    {
        return Results.NotFound($"Baralho do jogador {jogador1.Nome} não encontrado ou está vazio.");
    }

    // Buscar a mão do jogador 2
    var maoJogador2 = maos.FirstOrDefault(b => b.IdJogador == jogador2.Id);
    if (maoJogador2 == null || maoJogador2.Cartas.Count == 0)
    {
        return Results.NotFound($"Baralho do jogador {jogador2.Nome} não encontrado ou está vazio.");
    }

    // Retornar o nome dos jogadores e a primeira carta de cada um
    var resultado = new
    {
        Jogador1 = new
        {
            Nome = jogador1.Nome,
            id = jogador1.Id,
            PrimeiraCarta = maoJogador1.Cartas.First() // Primeira carta do baralho do jogador 1
        },
        Jogador2 = new
        {
            Nome = jogador2.Nome,
            id = jogador2.Id,
            PrimeiraCarta = maoJogador1.Cartas.First() // Primeira carta do baralho do jogador 2
        }
    };

    EstadoBatalha.ResultadoBatalha = resultado;

    return Results.Ok(resultado);
});


app.MapGet("/TerminarBatalha", () =>
{
    // Verificar se há um resultado armazenado na variável global de batalha
    var batalha = EstadoBatalha.ResultadoBatalha as dynamic;
    if (batalha == null)
    {
        return Results.BadRequest("Nenhuma batalha foi realizada antes.");
    }

    // Obter os resultados dos jogadores
    string nomeJogador1 = batalha.Jogador1.Nome;
    var cartaJogador1 = batalha.Jogador1.PrimeiraCarta;
    int idJogador1 = batalha.Jogador1.id;
    var maoJogador1 = maos.FirstOrDefault(b => b.IdJogador == idJogador1);
    string nomeJogador2 = batalha.Jogador2.Nome;
    var cartaJogador2 = batalha.Jogador2.PrimeiraCarta;
    int idJogador2 = batalha.Jogador2.id;
    var maoJogador2 = maos.FirstOrDefault(b => b.IdJogador == idJogador2);
    // Procurar pela carta no baralho usando o Id
    var cartaEncontradaJogador1 = baralhoSueca.FirstOrDefault(c => c.Id == cartaJogador1);
    var cartaEncontradaJogador2 = baralhoSueca.FirstOrDefault(c => c.Id == cartaJogador2);

    // Verificar se as cartas foram encontradas
    if (cartaEncontradaJogador1 == null || cartaEncontradaJogador2 == null)
    {
        return Results.BadRequest("Uma das cartas não foi encontrada no baralho.");
    }

    // Obter os Valores das cartas
    var ValorCartaJogador1 = cartaEncontradaJogador1.Posicao;
    var ValorCartaJogador2 = cartaEncontradaJogador2.Posicao;

    // Lógica da batalha (exemplo de comparação simples)
    string resultado = "";

    if (ValorCartaJogador1 > ValorCartaJogador2)
    {
        resultado = $"{nomeJogador1} vence!";

        maoJogador1.Cartas.Add(cartaEncontradaJogador2.Id);
        maoJogador2.Cartas.Remove(cartaEncontradaJogador2.Id); 
    }
    else if (ValorCartaJogador1 < ValorCartaJogador2)
    {
        resultado = $"{nomeJogador2} vence!";
        maoJogador2.Cartas.Add(cartaEncontradaJogador1.Id);
        maoJogador1.Cartas.Remove(cartaEncontradaJogador1.Id);
    }
    else
    {
        resultado = "Empate... Vamos à guerra AGHHHH";

        if (maoJogador1.Cartas.Count < 4 || maoJogador2.Cartas.Count < 4)
        {
            // Determinar quem ganha por falta de cartas do oponente, ou é empate final.
            if (maoJogador1.Cartas.Count < 4 && maoJogador2.Cartas.Count < 4)
            {
                resultado = "Empate final - ambos sem cartas suficientes para a guerra!";
                // Poderia devolver as cartas iniciais? Ou terminar o jogo?
            }
            else if (maoJogador1.Cartas.Count < 4)
            {
                resultado = $"{nomeJogador2} vence por falta de cartas de {nomeJogador1} para a guerra!";
                maoJogador2.Cartas.Add(cartaEncontradaJogador1.Id); // J2 fica com a carta inicial de J1
                maoJogador1.Cartas.Remove(cartaEncontradaJogador1.Id);
                // J2 também fica com as poucas cartas que J1 tinha para a guerra (se houver)
                maoJogador2.Cartas.AddRange(maoJogador1.Cartas);
                maoJogador1.Cartas.Clear(); // Limpa o baralho de J1
            }
            else
            { // baralhoJogador2.Cartas.Count < 4
                resultado = $"{nomeJogador1} vence por falta de cartas de {nomeJogador2} para a guerra!";
                maoJogador1.Cartas.Add(cartaEncontradaJogador2.Id); // J1 fica com a carta inicial de J2
                maoJogador2.Cartas.Remove(cartaEncontradaJogador2.Id);
                // J1 também fica com as poucas cartas que J2 tinha para a guerra (se houver)
                maoJogador1.Cartas.AddRange(maoJogador2.Cartas);
                maoJogador2.Cartas.Clear(); // Limpa o baralho de J2
            }
            return Results.Ok(new { Resultado = resultado }); // Termina aqui se não houver cartas suficientes
        }

        //Continua se ambos têm cartas suficientes
        // Cartas Iniciais (já as temos): cartaEncontradaJogador1, cartaEncontradaJogador2

        // 1. Extrair os IDs das 3 cartas seguintes de cada baralho para a "guerra"
        var idsGuerraJogador1 = maoJogador1.Cartas.Take(3).ToList();
        var idsGuerraJogador2 = maoJogador2.Cartas.Take(3).ToList();

        // 2. Obter os objectos Carta correspondentes a esses IDs
        var cartasGuerraJogador1 = baralhoSueca.Where(c => idsGuerraJogador1.Contains(c.Id)).ToList();
        var cartasGuerraJogador2 = baralhoSueca.Where(c => idsGuerraJogador2.Contains(c.Id)).ToList();

        // Reordenar as cartas obtidas para garantir que a ordem corresponde à ordem retirada (Take(3))
        cartasGuerraJogador1 = cartasGuerraJogador1.OrderBy(c => idsGuerraJogador1.IndexOf(c.Id)).ToList();
        cartasGuerraJogador2 = cartasGuerraJogador2.OrderBy(c => idsGuerraJogador2.IndexOf(c.Id)).ToList();

        // 3. Identificar a 3ª carta de cada conjunto para comparação
        var terceiraCartaJogador1 = cartasGuerraJogador1[2];
        var terceiraCartaJogador2 = cartasGuerraJogador2[2];

        // 4. Agrupar TODOS os IDs envolvidos na guerra (inicial + 3 de cada)
        var todasIdsEnvolvidas = new List<int> { cartaEncontradaJogador1.Id, cartaEncontradaJogador2.Id };
        todasIdsEnvolvidas.AddRange(idsGuerraJogador1);
        todasIdsEnvolvidas.AddRange(idsGuerraJogador2);

        // 5. Remover TODAS essas cartas dos baralhos de AMBOS os jogadores
        //    É importante remover ANTES de adicionar ao vencedor para evitar duplicados ou erros.
        maoJogador1.Cartas.RemoveAll(id => todasIdsEnvolvidas.Contains(id));
        maoJogador2.Cartas.RemoveAll(id => todasIdsEnvolvidas.Contains(id));

        // 6. Comparar a 3ª carta e atribuir TODAS as cartas ao vencedor
        if (terceiraCartaJogador1.Posicao > terceiraCartaJogador2.Posicao)
        {
            resultado = $"{nomeJogador1} vence a guerra!";
            maoJogador1.Cartas.AddRange(todasIdsEnvolvidas); // Adiciona todas as cartas ao vencedor
        }
        else if (terceiraCartaJogador2.Posicao > terceiraCartaJogador1.Posicao)
        {
            resultado = $"{nomeJogador2} vence a guerra!";
            maoJogador2.Cartas.AddRange(todasIdsEnvolvidas); // Adiciona todas as cartas ao vencedor
        }
        else
        {
            // Empate duplo! Precisamos repetir o processo de guerra.
            // Isto pode levar a recursão ou um loop. Por agora, vamos indicar isso.
            resultado = "Guerra -> Empate novamente! (Lógica de repetição necessária)";

            // O que fazer com as cartas neste caso? Devolver aos donos originais? Manter separadas para próxima guerra?
            // Por simplicidade, podemos devolvê-las temporariamente (a ordem pode ficar baralhada!)
            maoJogador1.Cartas.Add(cartaEncontradaJogador1.Id);
            maoJogador1.Cartas.AddRange(idsGuerraJogador1);
            maoJogador2.Cartas.Add(cartaEncontradaJogador2.Id);
            maoJogador2.Cartas.AddRange(idsGuerraJogador2);
            // NOTA: Esta devolução simples pode não ser ideal para a lógica do jogo.
            //       Uma abordagem melhor seria chamar recursivamente a lógica de guerra ou usar um loop.
            // Depois desenvolver código para isto
        }


    }

    // Retornar o resultado final da rodada
    return Results.Ok(new
    {
        Resultado = resultado,
        CartaInicialJogador1 = cartaEncontradaJogador1, // Retorna a carta inicial que empatou
        CartaInicialJogador2 = cartaEncontradaJogador2, // Retorna a carta inicial que empatou
                                                        // Opcional: Poderia retornar as cartas da guerra também, se útil para o cliente
    });

});

app.MapGet("/MostrarMaos", () =>
{
    return Results.Ok(maos);

});

app.MapGet("/MostrarJogadores", () =>
{
    return Results.Ok(jogadores);
});

app.MapPost("/CriarCartas", () =>
{
    Console.WriteLine($"Chamado endpoint para criar cartas");
    var cartasBaralhoSueca = new List<Carta>
    {
        // Copas
        new Carta { Id = 1, Name = "Ás de Copas", Description = "Ás de Copas", Valor = 11, Posicao = 10 },
        new Carta { Id = 2, Name = "7 de Copas", Description = "7 de Copas", Valor = 10, Posicao = 9 },
        new Carta { Id = 3, Name = "Rei de Copas", Description = "Rei de Copas", Valor = 4, Posicao = 8 },
        new Carta { Id = 4, Name = "Valete de Copas", Description = "Valete de Copas", Valor = 3, Posicao = 7 },
        new Carta { Id = 5, Name = "Dama de Copas", Description = "Dama de Copas", Valor = 2, Posicao = 6 },
        new Carta { Id = 6, Name = "6 de Copas", Description = "6 de Copas", Valor = 0, Posicao = 5 },
        new Carta { Id = 7, Name = "5 de Copas", Description = "5 de Copas", Valor = 0, Posicao = 4 },
        new Carta { Id = 8, Name = "4 de Copas", Description = "4 de Copas", Valor = 0, Posicao = 3 },
        new Carta { Id = 9, Name = "3 de Copas", Description = "3 de Copas", Valor = 0, Posicao = 2 },
        new Carta { Id = 10, Name = "2 de Copas", Description = "2 de Copas", Valor = 0, Posicao = 1 },

        // Ouros
        new Carta { Id = 11, Name = "Ás de Ouros", Description = "Ás de Ouros", Valor = 11, Posicao = 10 },
        new Carta { Id = 12, Name = "7 de Ouros", Description = "7 de Ouros", Valor = 10, Posicao = 9 },
        new Carta { Id = 13, Name = "Rei de Ouros", Description = "Rei de Ouros", Valor = 4, Posicao = 8 },
        new Carta { Id = 14, Name = "Valete de Ouros", Description = "Valete de Ouros", Valor = 3, Posicao = 7 },
        new Carta { Id = 15, Name = "Dama de Ouros", Description = "Dama de Ouros", Valor = 2, Posicao = 6 },
        new Carta { Id = 16, Name = "6 de Ouros", Description = "6 de Ouros", Valor = 0, Posicao = 5 },
        new Carta { Id = 17, Name = "5 de Ouros", Description = "5 de Ouros", Valor = 0, Posicao = 4 },
        new Carta { Id = 18, Name = "4 de Ouros", Description = "4 de Ouros", Valor = 0, Posicao = 3 },
        new Carta { Id = 19, Name = "3 de Ouros", Description = "3 de Ouros", Valor = 0, Posicao = 2 },
        new Carta { Id = 20, Name = "2 de Ouros", Description = "2 de Ouros", Valor = 0, Posicao = 1 },

        // Espadas
        new Carta { Id = 21, Name = "Ás de Espadas", Description = "Ás de Espadas", Valor = 11, Posicao = 10 },
        new Carta { Id = 22, Name = "7 de Espadas", Description = "7 de Espadas", Valor = 10, Posicao = 9 },
        new Carta { Id = 23, Name = "Rei de Espadas", Description = "Rei de Espadas", Valor = 4, Posicao = 8 },
        new Carta { Id = 24, Name = "Valete de Espadas", Description = "Valete de Espadas", Valor = 3, Posicao = 7 },
        new Carta { Id = 25, Name = "Dama de Espadas", Description = "Dama de Espadas", Valor = 2, Posicao = 6 },
        new Carta { Id = 26, Name = "6 de Espadas", Description = "6 de Espadas", Valor = 0, Posicao = 5 },
        new Carta { Id = 27, Name = "5 de Espadas", Description = "5 de Espadas", Valor = 0, Posicao = 4 },
        new Carta { Id = 28, Name = "4 de Espadas", Description = "4 de Espadas", Valor = 0, Posicao = 3 },
        new Carta { Id = 29, Name = "3 de Espadas", Description = "3 de Espadas", Valor = 0, Posicao = 2 },
        new Carta { Id = 30, Name = "2 de Espadas", Description = "2 de Espadas", Valor = 0, Posicao = 1 },

        // Paus
        new Carta { Id = 31, Name = "Ás de Paus", Description = "Ás de Paus", Valor = 11, Posicao = 10 },
        new Carta { Id = 32, Name = "7 de Paus", Description = "7 de Paus", Valor = 10, Posicao = 9 },
        new Carta { Id = 33, Name = "Rei de Paus", Description = "Rei de Paus", Valor = 4, Posicao = 8 },
        new Carta { Id = 34, Name = "Valete de Paus", Description = "Valete de Paus", Valor = 3, Posicao = 7 },
        new Carta { Id = 35, Name = "Dama de Paus", Description = "Dama de Paus", Valor = 2, Posicao = 6 },
        new Carta { Id = 36, Name = "6 de Paus", Description = "6 de Paus", Valor = 0, Posicao = 5 },
        new Carta { Id = 37, Name = "5 de Paus", Description = "5 de Paus", Valor = 0, Posicao = 4 },
        new Carta { Id = 38, Name = "4 de Paus", Description = "4 de Paus", Valor = 0, Posicao = 3 },
        new Carta { Id = 39, Name = "3 de Paus", Description = "3 de Paus", Valor = 0, Posicao = 2 },
        new Carta { Id = 40, Name = "2 de Paus", Description = "2 de Paus", Valor = 0, Posicao = 1 }
    };

    baralhoSueca = cartasBaralhoSueca;


    return Results.Ok(cartasBaralhoSueca);
});



app.Run();

using Conde.Compressao;

namespace Conde.Compressao.Medidor;

/// <summary>
/// As medidas. Tudo contado em BITS, com a descompressao conferida: nenhuma
/// medida depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "entropia": Limite(); break;
            case "huffman": Huffmans(); break;
            case "aritmetica": Aritmeticas(); break;
            case "tabela": Tabela(); break;
            case "memoria": Memoria(); break;
            case "contagem": Contagem(); break;
            case "tudo": Limite(); Huffmans(); Aritmeticas(); Tabela(); Memoria(); Contagem(); break;
            default:
                Console.Error.WriteLine("medidas: entropia, huffman, aritmetica, tabela, memoria, contagem, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>A entropia de cada fonte, que e o limite.</summary>
    private static void Limite()
    {
        Console.WriteLine("== o limite: a entropia de cada fonte ==");
        Console.WriteLine();
        Console.WriteLine($"{"fonte",-20}{"simbolos",10}{"ordem zero",14}{"primeira ordem",17}{"minimo em bytes",18}");

        foreach (var fonte in Fonte.Todas())
            Console.WriteLine($"{fonte.Nome,-20}{fonte.Simbolos().Count(),10}{Entropia.OrdemZero(fonte),14:N3}" +
                              $"{Entropia.PrimeiraOrdem(fonte),17:N3}{Entropia.MinimoEmBits(fonte) / 8,18:N0}");

        Console.WriteLine();
        Console.WriteLine("a entropia de ORDEM ZERO trata os simbolos como independentes, e a de");
        Console.WriteLine("primeira ordem olha o simbolo anterior. Nas fontes sorteadas as duas sao");
        Console.WriteLine("quase iguais, porque la os simbolos sao mesmo independentes");
        Console.WriteLine();
        Console.WriteLine("na fonte REPETIDA elas se separam: a de ordem zero diz um numero alto e a");
        Console.WriteLine("de primeira ordem cai perto de zero, porque saber o simbolo anterior ja");
        Console.WriteLine("quase determina o seguinte. E essa diferenca que o LZ77 come");
        Console.WriteLine();
    }

    /// <summary>O Huffman contra o limite.</summary>
    private static void Huffmans()
    {
        Console.WriteLine("== Huffman: otimo entre os codigos de bit inteiro, e so entre eles ==");
        Console.WriteLine();
        Console.WriteLine($"{"fonte",-20}{"entropia",12}{"Huffman",12}{"sobra",10}{"desperdicio",14}{"Kraft",8}");

        foreach (var fonte in Fonte.Todas())
        {
            var codigos = Huffman.Codigos(fonte);
            var medio = Entropia.ComprimentoMedio(fonte, codigos);
            var entropia = Entropia.OrdemZero(fonte);

            Console.WriteLine($"{fonte.Nome,-20}{entropia,12:N3}{medio,12:N3}{medio - entropia,10:N3}" +
                              $"{(entropia > 0 ? (medio - entropia) / entropia : 0),14:P1}{Huffman.Kraft(codigos),8:N2}");
        }

        Console.WriteLine();
        Console.WriteLine("o teorema diz que o Huffman fica entre a entropia e a entropia mais UM bit,");
        Console.WriteLine("e a coluna da sobra confirma: nenhuma linha passa de um, e nenhuma e");
        Console.WriteLine("negativa");
        Console.WriteLine();
        Console.WriteLine("mas olhe a coluna do desperdicio na fonte enviesada de 0,99: a sobra de");
        Console.WriteLine("0,92 bit e quase nada em valor absoluto e e ONZE VEZES a entropia. O");
        Console.WriteLine("teorema do mais um bit nao e consolo quando a entropia e menor que um bit");
        Console.WriteLine();
        Console.WriteLine("e a razao e simples: o Huffman nao sabe gastar menos de um bit inteiro em");
        Console.WriteLine("simbolo nenhum. Nao e defeito de implementacao, e a forma do metodo");
        Console.WriteLine();

        Console.WriteLine($"  o que cada simbolo DEVERIA custar, na enviesada de 0,9:");
        var enviesada = Fonte.Enviesada(0.9);
        var tabela = Huffman.Codigos(enviesada);

        foreach (var simbolo in enviesada.Simbolos())
            Console.WriteLine($"    '{(char)simbolo}'  aparece {(double)enviesada.Contagem[simbolo] / enviesada.Tamanho,6:P1}" +
                              $"   custo teorico {Aritmetica.CustoDe(enviesada, simbolo),6:N3} bits" +
                              $"   Huffman da {tabela[simbolo].Length} bit");

        Console.WriteLine();
    }

    /// <summary>A aritmetica, que gasta bit fracionario.</summary>
    private static void Aritmeticas()
    {
        Console.WriteLine("== a aritmetica: bit fracionario por simbolo ==");
        Console.WriteLine();
        Console.WriteLine($"{"fonte",-20}{"entropia",12}{"Huffman",12}{"aritmetica",14}{"desperdicio",14}");

        foreach (var fonte in Fonte.Todas())
        {
            var entropia = Entropia.OrdemZero(fonte);
            var huffman = Comparacao.ComHuffman(fonte);
            var aritmetica = Comparacao.ComAritmetica(fonte);

            Console.WriteLine($"{fonte.Nome,-20}{entropia,12:N3}{huffman.BitsPorSimbolo,12:N3}" +
                              $"{aritmetica.BitsPorSimbolo,14:N3}{Entropia.Desperdicio(fonte, aritmetica.Bits),14:P2}");
        }

        Console.WriteLine();
        Console.WriteLine("a coluna do desperdicio da aritmetica fica abaixo de um decimo por cento em");
        Console.WriteLine("todas as fontes: ela gasta o minimo teorico, e o que sobra sao os poucos");
        Console.WriteLine("bits de fechamento do fluxo");
        Console.WriteLine();
        Console.WriteLine("na enviesada de 0,99 ela gasta um doze avos do que o Huffman gasta, com o");
        Console.WriteLine("mesmo modelo e a mesma informacao. A diferenca inteira e poder gastar");
        Console.WriteLine("0,0144 bit num simbolo em vez de um bit");
        Console.WriteLine();
    }

    /// <summary>A tabela completa, com a volta conferida.</summary>
    private static void Tabela()
    {
        Console.WriteLine("== todos os metodos, com a descompressao conferida ==");
        Console.WriteLine();
        Console.WriteLine($"{"fonte",-20}{"entropia",10}{"Huffman",10}{"aritmetica",12}{"LZ77",10}{"RLE",10}{"voltou",10}");

        foreach (var fonte in Fonte.Todas())
        {
            var todos = Comparacao.Todos(fonte).ToList();
            var voltou = todos.All(r => r.Voltou);

            Console.Write($"{fonte.Nome,-20}{Entropia.OrdemZero(fonte),10:N2}");
            foreach (var r in todos) Console.Write($"{r.BitsPorSimbolo,10:N2}".PadLeft(r.Metodo == "aritmetica" ? 12 : 10));
            Console.WriteLine($"{(voltou ? "sim" : "NAO"),10}");
        }

        Console.WriteLine();
        Console.WriteLine("a ultima coluna e o que faz o resto valer alguma coisa: todo numero desta");
        Console.WriteLine("tabela veio de um fluxo que foi descomprimido e comparado byte a byte com a");
        Console.WriteLine("entrada. Razao de compressao sem a volta nao mede nada");
        Console.WriteLine();
        Console.WriteLine("o RLE em texto AUMENTA o arquivo: ele gasta dezesseis bits para dizer 'um");
        Console.WriteLine("byte, uma vez', e em texto quase toda corrida tem tamanho um");
        Console.WriteLine();
        Console.WriteLine("e nas corridas ele ganha de todo mundo com dez linhas de codigo. Nao existe");
        Console.WriteLine("metodo bom: existe metodo que casa com a estrutura dos dados");
        Console.WriteLine();
    }

    /// <summary>O limite que nao e limite: fonte com memoria.</summary>
    private static void Memoria()
    {
        Console.WriteLine("== o limite que nao e limite ==");
        Console.WriteLine();

        var repetida = Fonte.Repetida();
        var entropia = Entropia.OrdemZero(repetida);
        var lz = Comparacao.ComLz77(repetida);
        var huffman = Comparacao.ComHuffman(repetida);

        Console.WriteLine($"  fonte repetida, {repetida.Tamanho:N0} bytes, {repetida.Simbolos().Count()} simbolos");
        Console.WriteLine($"    entropia de ordem zero:    {entropia,8:N3} bits/simbolo");
        Console.WriteLine($"    entropia de primeira ordem:{Entropia.PrimeiraOrdem(repetida),8:N3} bits/simbolo");
        Console.WriteLine($"    Huffman gastou:            {huffman.BitsPorSimbolo,8:N3} bits/simbolo");
        Console.WriteLine($"    LZ77 gastou:               {lz.BitsPorSimbolo,8:N3} bits/simbolo");
        Console.WriteLine($"    e a volta confere:         {lz.Voltou}");
        Console.WriteLine();
        Console.WriteLine("o LZ77 gasta MENOS que a entropia de ordem zero, e isso parece violar o");
        Console.WriteLine("teorema de Shannon. Nao viola nada");
        Console.WriteLine();
        Console.WriteLine("o teorema diz que nenhum codigo gasta menos que a entropia da FONTE. A");
        Console.WriteLine("entropia de ordem zero nao e a entropia de uma fonte com memoria: ela e o");
        Console.WriteLine("que daria se os simbolos fossem independentes, e nesta fonte eles nao sao");
        Console.WriteLine();
        Console.WriteLine("citar a entropia de ordem zero como 'o limite' e o erro mais comum sobre");
        Console.WriteLine("compressao, e ele e facil de cometer porque o numero existe e e facil de");
        Console.WriteLine("calcular. A primeira ordem ja mostra o buraco, e ainda nao e o fundo dele");
        Console.WriteLine();
    }

    /// <summary>A contagem que prova que nenhum metodo comprime tudo.</summary>
    private static void Contagem()
    {
        Console.WriteLine("== nenhum metodo comprime tudo, e isso e contagem ==");
        Console.WriteLine();
        Console.WriteLine($"{"bytes",8}{"entradas",12}{"encolheram",14}{"iguais",10}{"cresceram",12}");

        for (var bytes = 2; bytes <= 12; bytes += 2)
        {
            var (encolheram, iguais, cresceram) = Comparacao.TodasAsEntradas(bytes);
            Console.WriteLine($"{bytes,8}{(int)Math.Pow(2, bytes),12:N0}{encolheram,14:N0}{iguais,10:N0}{cresceram,12:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("todas as entradas de cada tamanho passaram pelo RLE, sem amostragem: sao");
        Console.WriteLine("dois simbolos, entao sao 2^n entradas e todas foram rodadas");
        Console.WriteLine();
        Console.WriteLine("o argumento e de pombos e nao de engenharia: ha 2^n mensagens de n bits e");
        Console.WriteLine("menos de 2^n mensagens mais curtas, entao nao existe funcao injetora de");
        Console.WriteLine("todas as mensagens em mensagens mais curtas");
        Console.WriteLine();
        Console.WriteLine("qualquer metodo que encolha alguma entrada PRECISA aumentar outra. A");
        Console.WriteLine("compressao funciona porque os arquivos que existem no mundo nao sao");
        Console.WriteLine("sorteados entre todos os possiveis");
        Console.WriteLine();
    }
}

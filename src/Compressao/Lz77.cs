namespace Conde.Compressao;

/// <summary>
/// LZ77, de 1977: em vez de codificar simbolos, apontar para o que ja passou.
///
/// Ele nao olha frequencia nenhuma. O que ele faz e procurar, numa janela do que
/// ja foi escrito, o trecho mais longo que casa com o que vem agora, e trocar
/// esse trecho por um par de numeros: a que distancia esta e quantos bytes
/// copiar.
///
/// E e por isso que ele passa POR BAIXO da entropia de ordem zero, o que parece
/// violar o teorema de Shannon e nao viola nada. O teorema diz que nenhum codigo
/// gasta menos que a entropia da FONTE; a entropia de ordem zero nao e a
/// entropia de uma fonte com memoria, e apenas o que se obteria tratando os
/// simbolos como independentes. Numa fonte que se repete, essa conta e um numero
/// alto e completamente furado.
///
/// A medida mostra isso com todas as letras: na fonte repetida, a entropia de
/// ordem zero diz 4,6 bits por simbolo e o LZ77 gasta uma fracao disso.
/// </summary>
public static class Lz77
{
    public const int Janela = 4096;
    public const int MaiorCasamento = 18;
    public const int MenorCasamento = 3;

    /// <summary>
    /// Comprime. Cada item e um bit de marca mais o conteudo: literal com oito
    /// bits, ou casamento com doze bits de distancia e quatro de comprimento.
    /// </summary>
    public static (byte[] Saida, long Bits, int Literais, int Casamentos) Comprimir(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var dados = fonte.Dados;
        var escritor = new EscritorDeBits();
        int literais = 0, casamentos = 0;
        var i = 0;

        while (i < dados.Length)
        {
            var (distancia, comprimento) = MelhorCasamento(dados, i);

            if (comprimento >= MenorCasamento)
            {
                escritor.Escrever(true);

                // A distancia vai gravada menos um. Ela vale de 1 a 4096 e doze
                // bits guardam de 0 a 4095: sem o menos um, a distancia 4096
                // vira zero na gravacao e a descompressao le de uma posicao que
                // ainda nao existe. O erro so aparece em fonte grande o
                // bastante para encher a janela.
                escritor.Escrever(distancia - 1, 12);
                escritor.Escrever(comprimento - MenorCasamento, 4);
                i += comprimento;
                casamentos++;
            }
            else
            {
                escritor.Escrever(false);
                escritor.Escrever(dados[i], 8);
                i++;
                literais++;
            }
        }

        var bits = escritor.Bits;
        return (escritor.Fechar(), bits, literais, casamentos);
    }

    /// <summary>
    /// O casamento mais longo dentro da janela, procurado por forca bruta.
    ///
    /// Forca bruta aqui e escolha: a estrutura de indice que os compressores de
    /// verdade usam muda o TEMPO e nao muda um bit da saida, e este repositorio
    /// mede bits. Deixar a busca obvia deixa o codigo legivel e a medida igual.
    /// </summary>
    public static (int Distancia, int Comprimento) MelhorCasamento(byte[] dados, int posicao)
    {
        ArgumentNullException.ThrowIfNull(dados);

        var inicio = Math.Max(0, posicao - Janela);
        int melhorDistancia = 0, melhorComprimento = 0;

        for (var atras = inicio; atras < posicao; atras++)
        {
            var comprimento = 0;

            // O casamento pode passar da posicao atual: copiar da saida que
            // ainda esta sendo escrita e o que faz uma corrida de mil bytes
            // iguais caber num par de numeros.
            while (comprimento < MaiorCasamento &&
                   posicao + comprimento < dados.Length &&
                   dados[atras + comprimento] == dados[posicao + comprimento])
                comprimento++;

            if (comprimento > melhorComprimento)
            {
                melhorComprimento = comprimento;
                melhorDistancia = posicao - atras;
            }
        }

        return (melhorDistancia, melhorComprimento);
    }

    public static byte[] Descomprimir(byte[] comprimido, int quantos)
    {
        ArgumentNullException.ThrowIfNull(comprimido);

        var leitor = new LeitorDeBits(comprimido);
        var saida = new List<byte>(quantos);

        while (saida.Count < quantos)
        {
            if (leitor.Ler())
            {
                var distancia = leitor.Ler(12) + 1;
                var comprimento = leitor.Ler(4) + MenorCasamento;

                var origem = saida.Count - distancia;
                for (var k = 0; k < comprimento && saida.Count < quantos; k++) saida.Add(saida[origem + k]);
            }
            else
            {
                saida.Add((byte)leitor.Ler(8));
            }
        }

        return saida.ToArray();
    }
}

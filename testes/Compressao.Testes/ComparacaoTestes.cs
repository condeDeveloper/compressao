using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

/// <summary>
/// A comparacao entre os metodos, com a VOLTA conferida.
///
/// Razao de compressao sem descompressao nao vale nada: qualquer metodo
/// "comprime" para zero byte se nao precisar devolver o original.
/// </summary>
public class ComparacaoTestes
{
    /// <summary>
    /// TODOS os metodos devolvem o original em TODAS as fontes. Esta e a linha
    /// que faz o resto do repositorio valer alguma coisa.
    /// </summary>
    [Fact]
    public void TodosOsMetodosDevolvemOOriginalEmTodasAsFontes()
    {
        foreach (var fonte in Fonte.Todas())
            foreach (var resultado in Comparacao.Todos(fonte))
                Assert.True(resultado.Voltou, $"{resultado.Metodo} nao devolveu {fonte.Nome}");
    }

    /// <summary>
    /// Nenhum metodo sem perda fica abaixo da entropia da fonte quando a fonte
    /// e mesmo sem memoria. Nas sorteadas, nem o LZ77 passa por baixo.
    /// </summary>
    [Fact]
    public void NaFonteSemMemoriaNinguemPassaPorBaixoDaEntropia()
    {
        foreach (var fonte in new[] { Fonte.Enviesada(0.9), Fonte.Uniforme() })
        {
            var entropia = Entropia.OrdemZero(fonte);

            foreach (var resultado in Comparacao.Todos(fonte))
                Assert.True(resultado.BitsPorSimbolo > entropia - 0.01,
                            $"{resultado.Metodo} gastou {resultado.BitsPorSimbolo:N3} e a entropia e {entropia:N3}");
        }
    }

    /// <summary>
    /// Cada fonte tem um vencedor DIFERENTE, e esse e o resumo do repositorio:
    /// nao existe metodo bom, existe metodo que casa com a estrutura dos dados.
    /// </summary>
    [Fact]
    public void CadaFonteTemUmVencedorDiferente()
    {
        var vencedores = new HashSet<string>();

        foreach (var fonte in Fonte.Todas())
            vencedores.Add(Comparacao.Todos(fonte).OrderBy(r => r.Bits).First().Metodo);

        Assert.True(vencedores.Count >= 3, $"so {vencedores.Count} metodos diferentes venceram alguma fonte");
    }

    [Fact]
    public void AComparacaoDeBytesEhPorConteudoENaoPorReferencia()
    {
        Assert.True(Comparacao.Igual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }));
        Assert.False(Comparacao.Igual(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 4 }));
        Assert.False(Comparacao.Igual(new byte[] { 1, 2 }, new byte[] { 1, 2, 3 }));
        Assert.True(Comparacao.Igual(Array.Empty<byte>(), Array.Empty<byte>()));
    }

    /// <summary>
    /// O RLE na fonte de corridas ganha da aritmetica, que gasta o minimo
    /// teorico de ordem zero. Isso nao e contradicao: a fonte de corridas tem
    /// memoria, e o minimo de ordem zero nao e o minimo dela.
    /// </summary>
    [Fact]
    public void ORleGanhaDaAritmeticaNasCorridas()
    {
        var fonte = Fonte.Corridas();

        Assert.True(Comparacao.ComRle(fonte).Bits < Comparacao.ComAritmetica(fonte).Bits);
        Assert.True(Entropia.PrimeiraOrdem(fonte) < Entropia.OrdemZero(fonte) / 2);
    }

    [Fact]
    public void OsBitsPorSimboloBatemComOsBitsTotais()
    {
        foreach (var fonte in Fonte.Todas())
            foreach (var resultado in Comparacao.Todos(fonte))
                Assert.Equal((double)resultado.Bits / fonte.Tamanho, resultado.BitsPorSimbolo, 9);
    }

    [Fact]
    public void OResumoDeCadaResultadoEhLegivel()
    {
        var resultado = Comparacao.ComHuffman(Fonte.Texto(1000));

        Assert.Contains("Huffman", resultado.ToString(), StringComparison.Ordinal);
        Assert.Contains("bits/simbolo", resultado.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ACompressaoDeUmaFonteCurtaFunciona()
    {
        var fonte = new Fonte("curta", "abracadabra"u8.ToArray());

        foreach (var resultado in Comparacao.Todos(fonte))
            Assert.True(resultado.Voltou, $"{resultado.Metodo} nao devolveu a fonte curta");
    }
}

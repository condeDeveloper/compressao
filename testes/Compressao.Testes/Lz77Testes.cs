using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class Lz77Testes
{
    private static Fonte De(string texto) => new("teste", texto.Select(c => (byte)c).ToArray());

    [Fact]
    public void ADescompressaoDevolveOOriginal()
    {
        foreach (var fonte in Fonte.Todas())
            Assert.True(Comparacao.ComLz77(fonte).Voltou, $"{fonte.Nome} nao voltou igual");
    }

    /// <summary>
    /// O ACHADO: o LZ77 passa POR BAIXO da entropia de ordem zero, e isso nao
    /// viola o teorema de Shannon.
    ///
    /// O teorema diz que nenhum codigo gasta menos que a entropia da FONTE. A
    /// entropia de ordem zero nao e a entropia de uma fonte com memoria: ela e o
    /// que daria se os simbolos fossem independentes. Citar ela como "o limite"
    /// e o erro mais comum sobre compressao.
    /// </summary>
    [Fact]
    public void OLz77PassaPorBaixoDaEntropiaDeOrdemZero()
    {
        foreach (var fonte in new[] { Fonte.Texto(), Fonte.Repetida(), Fonte.Corridas() })
        {
            var lz = Comparacao.ComLz77(fonte);
            Assert.True(lz.BitsPorSimbolo < Entropia.OrdemZero(fonte),
                        $"{fonte.Nome}: LZ77 {lz.BitsPorSimbolo:N2}, entropia {Entropia.OrdemZero(fonte):N2}");
            Assert.True(lz.Voltou);
        }
    }

    /// <summary>
    /// E a entropia de PRIMEIRA ordem ja mostra o buraco: ela cai muito abaixo
    /// da de ordem zero exatamente nas fontes em que o LZ77 ganha.
    /// </summary>
    [Fact]
    public void APrimeiraOrdemJaMostraOBuraco()
    {
        var repetida = Fonte.Repetida();

        Assert.True(Entropia.PrimeiraOrdem(repetida) < Entropia.OrdemZero(repetida) / 2);
        Assert.True(Comparacao.ComLz77(repetida).BitsPorSimbolo < Entropia.PrimeiraOrdem(repetida));
    }

    /// <summary>
    /// Na fonte UNIFORME ele AUMENTA o arquivo: nove bits por simbolo contra
    /// oito. O bit de marca de cada literal nao tem como se pagar quando nao ha
    /// repeticao nenhuma para achar.
    /// </summary>
    [Fact]
    public void NaFonteUniformeOLz77Aumenta()
    {
        var fonte = Fonte.Uniforme();
        var lz = Comparacao.ComLz77(fonte);

        Assert.True(lz.BitsPorSimbolo > 8, $"deu {lz.BitsPorSimbolo:N2}");
        Assert.InRange(lz.BitsPorSimbolo, 8.9, 9.1);
        Assert.True(lz.Voltou);
    }

    /// <summary>
    /// O casamento pode passar da posicao atual: copiar da saida que ainda esta
    /// sendo escrita e o que faz uma corrida longa caber num par de numeros.
    /// </summary>
    [Fact]
    public void OCasamentoPodePassarDaPosicaoAtual()
    {
        var dados = De("aaaaaaaaaaaaaaaaaaaa").Dados;
        var (distancia, comprimento) = Lz77.MelhorCasamento(dados, 1);

        Assert.Equal(1, distancia);
        Assert.Equal(Lz77.MaiorCasamento, comprimento);
    }

    [Fact]
    public void SemNadaAtrasNaoHaCasamento()
    {
        var dados = De("abc").Dados;
        var (_, comprimento) = Lz77.MelhorCasamento(dados, 0);

        Assert.Equal(0, comprimento);
    }

    /// <summary>
    /// A distancia vai gravada MENOS UM, e isso nao e enfeite: ela vale de 1 a
    /// 4096 e doze bits guardam de 0 a 4095. Sem o menos um, a distancia 4096
    /// vira zero na gravacao e a descompressao le de uma posicao que ainda nao
    /// existe, o que so aparece em fonte grande o bastante para encher a janela.
    /// </summary>
    [Fact]
    public void AJanelaCheiaNaoQuebraADescompressao()
    {
        var sorteio = new Fonte.Sorteio(7);
        var dados = new byte[20_000];

        // os primeiros 4096 bytes sorteados, e depois o arquivo inteiro
        // repetindo eles: isso forca casamentos na distancia maxima
        for (var i = 0; i < 4096; i++) dados[i] = (byte)(97 + sorteio.Proximo() % 26);
        for (var i = 4096; i < dados.Length; i++) dados[i] = dados[i - 4096];

        var fonte = new Fonte("janela cheia", dados);
        var (saida, _, _, _) = Lz77.Comprimir(fonte);

        Assert.Equal(fonte.Dados, Lz77.Descomprimir(saida, fonte.Tamanho));
    }

    [Fact]
    public void OsLiteraisEOsCasamentosSaoContados()
    {
        var (_, _, literais, casamentos) = Lz77.Comprimir(Fonte.Repetida());

        Assert.True(casamentos > literais, $"{casamentos} casamentos contra {literais} literais");
        Assert.True(literais > 0);
    }

    [Fact]
    public void NaFonteUniformeQuaseTudoEhLiteral()
    {
        var (_, _, literais, casamentos) = Lz77.Comprimir(Fonte.Uniforme());

        Assert.True(literais > casamentos * 100, $"{literais} literais contra {casamentos} casamentos");
    }

    [Fact]
    public void EntradaDeUmByteVoltaIgual()
    {
        var fonte = De("a");
        var (saida, _, _, _) = Lz77.Comprimir(fonte);

        Assert.Equal(fonte.Dados, Lz77.Descomprimir(saida, 1));
    }
}

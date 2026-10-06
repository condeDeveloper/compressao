using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class EntropiaTestes
{
    private static Fonte De(string texto) =>
        new("teste", texto.Select(c => (byte)c).ToArray());

    /// <summary>
    /// Um simbolo so tem entropia ZERO: nao ha informacao nenhuma em dizer o que
    /// ja se sabe.
    /// </summary>
    [Fact]
    public void UmSimboloSoTemEntropiaZero()
    {
        Assert.Equal(0, Entropia.OrdemZero(De("aaaaaaaa")), 12);
        Assert.Equal(0, Entropia.MinimoEmBits(De("aaaaaaaa")), 12);
    }

    /// <summary>
    /// Dois simbolos igualmente provaveis dao exatamente UM bit, e quatro dao
    /// exatamente dois. Esses sao os casos em que a conta fecha redonda.
    /// </summary>
    [Fact]
    public void OsCasosRedondosFecham()
    {
        Assert.Equal(1, Entropia.OrdemZero(De("abababab")), 12);
        Assert.Equal(2, Entropia.OrdemZero(De("abcdabcd")), 12);
        Assert.Equal(3, Entropia.OrdemZero(De("abcdefgh")), 12);
    }

    /// <summary>
    /// A fonte enviesada de 0,9 tem entropia perto de 0,469 bit, que e o numero
    /// que o repositorio inteiro gira em volta.
    /// </summary>
    [Fact]
    public void AEnviesadaTemQuaseMeioBit()
    {
        Assert.InRange(Entropia.OrdemZero(Fonte.Enviesada(0.9)), 0.44, 0.49);
        Assert.InRange(Entropia.OrdemZero(Fonte.Enviesada(0.99)), 0.07, 0.10);
    }

    /// <summary>
    /// A entropia nunca passa do logaritmo do numero de simbolos, e so chega la
    /// quando todos sao igualmente provaveis.
    /// </summary>
    [Fact]
    public void AEntropiaNuncaPassaDoLogaritmoDoAlfabeto()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var quantos = fonte.Simbolos().Count();
            Assert.True(Entropia.OrdemZero(fonte) <= Math.Log2(quantos) + 1e-9);
        }

        Assert.Equal(8, Entropia.OrdemZero(Fonte.Uniforme()), 1);
    }

    /// <summary>
    /// Saber o simbolo anterior nunca atrapalha, e a folga aqui tem motivo.
    ///
    /// A desigualdade H(X|Y) menor ou igual a H(X) vale para a DISTRIBUICAO. As
    /// duas contas daqui sao estimativas sobre conjuntos ligeiramente
    /// diferentes: a de primeira ordem olha os n-1 pares e a marginal das
    /// posicoes 1 a n-1, e a de ordem zero olha as n posicoes. Na fonte
    /// enviesada de 0,9 isso fez a primeira ordem dar 0,4667823 contra
    /// 0,4667727, cruzando por um centesimo de milesimo.
    ///
    /// Nao e erro de conta: e a diferenca entre a desigualdade e a estimativa
    /// dela. A tolerancia aqui e maior que esse ruido e muito menor que a queda
    /// de verdade, que na fonte repetida e de tres vezes.
    /// </summary>
    [Fact]
    public void SaberOAnteriorNuncaAtrapalha()
    {
        foreach (var fonte in Fonte.Todas())
            Assert.True(Entropia.PrimeiraOrdem(fonte) <= Entropia.OrdemZero(fonte) + 1e-4,
                        $"{fonte.Nome}: primeira {Entropia.PrimeiraOrdem(fonte)}, zero {Entropia.OrdemZero(fonte)}");

        var repetida = Fonte.Repetida();
        Assert.True(Entropia.PrimeiraOrdem(repetida) < Entropia.OrdemZero(repetida) / 2);
    }

    /// <summary>
    /// Na fonte SORTEADA as duas ficam quase iguais, porque la os simbolos sao
    /// mesmo independentes: nao ha memoria para a primeira ordem enxergar.
    /// </summary>
    [Fact]
    public void NaFonteSorteadaAsDuasOrdensEmpatam()
    {
        foreach (var fonte in new[] { Fonte.Enviesada(0.9), Fonte.Enviesada(0.99) })
            Assert.Equal(Entropia.OrdemZero(fonte), Entropia.PrimeiraOrdem(fonte), 2);
    }

    [Fact]
    public void OComprimentoMedioEhAMediaPesada()
    {
        var fonte = De("aaab");
        var codigos = new Dictionary<int, string> { ['a'] = "0", ['b'] = "111" };

        // 3/4 de um bit mais 1/4 de tres bits
        Assert.Equal(0.75 * 1 + 0.25 * 3, Entropia.ComprimentoMedio(fonte, codigos), 12);
    }

    [Fact]
    public void ODesperdicioEhRelativoAoMinimo()
    {
        var fonte = De("abababab");

        Assert.Equal(0, Entropia.Desperdicio(fonte, 8), 12);
        Assert.Equal(1, Entropia.Desperdicio(fonte, 16), 12);
        Assert.Equal(-0.5, Entropia.Desperdicio(fonte, 4), 12);
    }

    [Fact]
    public void FonteVaziaOuDeUmSimboloNaoQuebraAConta()
    {
        Assert.Equal(0, Entropia.OrdemZero(new Fonte("vazia", Array.Empty<byte>())), 12);
        Assert.Equal(0, Entropia.PrimeiraOrdem(De("a")), 12);
        Assert.Equal(0, Entropia.Desperdicio(De("aaaa"), 10), 12);
    }
}

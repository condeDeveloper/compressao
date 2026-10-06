using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class HuffmanTestes
{
    private static Fonte De(string texto) => new("teste", texto.Select(c => (byte)c).ToArray());

    /// <summary>
    /// O TEOREMA: o comprimento medio do Huffman fica entre a entropia e a
    /// entropia mais um bit. As duas pontas conferidas, em todas as fontes.
    /// </summary>
    [Fact]
    public void OHuffmanFicaEntreAEntropiaEAEntropiaMaisUmBit()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var entropia = Entropia.OrdemZero(fonte);
            var medio = Entropia.ComprimentoMedio(fonte, Huffman.Codigos(fonte));

            Assert.True(medio >= entropia - 1e-9, $"{fonte.Nome} ficou ABAIXO da entropia: {medio} < {entropia}");
            Assert.True(medio < entropia + 1, $"{fonte.Nome} passou de um bit de sobra: {medio} contra {entropia}");
        }
    }

    /// <summary>
    /// O ACHADO: na fonte enviesada de 0,99 o Huffman gasta ONZE VEZES a
    /// entropia, mesmo respeitando o teorema do mais um bit.
    ///
    /// A sobra de 0,92 bit e quase nada em valor absoluto, e e enorme quando a
    /// entropia e 0,083. O teorema do mais um bit nao e consolo nenhum quando a
    /// entropia e menor que um bit.
    /// </summary>
    [Fact]
    public void NaFonteMuitoEnviesadaOHuffmanGastaOnzeVezesOMinimo()
    {
        var fonte = Fonte.Enviesada(0.99);
        var medio = Entropia.ComprimentoMedio(fonte, Huffman.Codigos(fonte));

        Assert.Equal(1, medio, 9);
        Assert.True(medio / Entropia.OrdemZero(fonte) > 10);
    }

    /// <summary>
    /// E a razao e que ele nao sabe gastar menos de UM bit em simbolo nenhum,
    /// por mais provavel que o simbolo seja.
    /// </summary>
    [Fact]
    public void NenhumCodigoTemMenosDeUmBit()
    {
        foreach (var fonte in Fonte.Todas())
            foreach (var codigo in Huffman.Codigos(fonte).Values)
                Assert.True(codigo.Length >= 1);
    }

    /// <summary>
    /// A desigualdade de KRAFT da exatamente UM no Huffman: nenhum bit de
    /// endereco e desperdicado. Um codigo com soma menor que um deixa espaco na
    /// mesa.
    /// </summary>
    [Fact]
    public void AKraftDaExatamenteUm()
    {
        foreach (var fonte in Fonte.Todas())
            Assert.Equal(1, Huffman.Kraft(Huffman.Codigos(fonte)), 9);
    }

    [Fact]
    public void OsCodigosSaoPrefixoLivre()
    {
        foreach (var fonte in Fonte.Todas())
            Assert.True(Huffman.PrefixoLivre(Huffman.Codigos(fonte)), fonte.Nome);
    }

    /// <summary>
    /// O simbolo mais frequente nunca tem codigo mais longo que o menos
    /// frequente. E a propriedade que faz o metodo funcionar, e ela sai da
    /// construcao sem ser pedida.
    /// </summary>
    [Fact]
    public void SimboloMaisFrequenteNuncaTemCodigoMaisLongo()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var codigos = Huffman.Codigos(fonte);

            foreach (var a in fonte.Simbolos())
                foreach (var b in fonte.Simbolos())
                    if (fonte.Contagem[a] > fonte.Contagem[b])
                        Assert.True(codigos[a].Length <= codigos[b].Length,
                                    $"{(char)a} e mais frequente que {(char)b} e tem codigo maior");
        }
    }

    /// <summary>
    /// A volta e a unica prova de que a compressao esta certa: descomprimir tem
    /// que devolver a entrada byte a byte.
    /// </summary>
    [Fact]
    public void ADescompressaoDevolveOOriginal()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var r = Comparacao.ComHuffman(fonte);
            Assert.True(r.Voltou, $"{fonte.Nome} nao voltou igual");
        }
    }

    [Fact]
    public void UmSimboloSoAindaPrecisaDeUmBit()
    {
        var fonte = De("aaaaaaaa");
        var codigos = Huffman.Codigos(fonte);

        Assert.Single(codigos);
        Assert.Equal(1, codigos['a'].Length);
        Assert.True(Comparacao.ComHuffman(fonte).Voltou);
    }

    [Fact]
    public void FonteVaziaNaoTemCodigos()
    {
        Assert.Empty(Huffman.Codigos(new Fonte("vazia", Array.Empty<byte>())));
    }

    /// <summary>
    /// Os codigos sao os MESMOS a cada chamada. Com dois nos de mesmo peso,
    /// escolher um ou outro muda os codigos e nao muda o comprimento medio; o
    /// desempate por simbolo deixa a saida igual nos tres sistemas da
    /// integracao continua.
    /// </summary>
    [Fact]
    public void OsCodigosSaoOsMesmosACadaChamada()
    {
        var fonte = Fonte.Texto(2000);
        var primeira = Huffman.Codigos(fonte);
        var segunda = Huffman.Codigos(fonte);

        foreach (var (simbolo, codigo) in primeira) Assert.Equal(codigo, segunda[simbolo]);
    }

    [Fact]
    public void ATabelaCustaBitsQueASaidaNaoConta()
    {
        var codigos = Huffman.Codigos(Fonte.Texto(2000));
        Assert.Equal(codigos.Count * 13, Huffman.BitsDaTabela(codigos));
    }
}

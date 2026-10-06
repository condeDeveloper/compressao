using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class AritmeticaTestes
{
    private static Fonte De(string texto) => new("teste", texto.Select(c => (byte)c).ToArray());

    /// <summary>
    /// O ACHADO: a aritmetica gasta o MINIMO TEORICO, com menos de um quinto por
    /// cento de folga em todas as fontes.
    ///
    /// O que sobra sao os poucos bits de fechamento do fluxo, e eles somem num
    /// arquivo grande.
    /// </summary>
    [Fact]
    public void AAritmeticaGastaOMinimoTeorico()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var desperdicio = Entropia.Desperdicio(fonte, Comparacao.ComAritmetica(fonte).Bits);
            Assert.InRange(desperdicio, 0, 0.002);
        }
    }

    /// <summary>
    /// E ela ganha do Huffman exatamente onde o Huffman perde: na fonte muito
    /// enviesada, por um fator de doze.
    /// </summary>
    [Fact]
    public void NaFonteEnviesadaElaGanhaDoHuffmanPorUmFatorDeDoze()
    {
        var fonte = Fonte.Enviesada(0.99);

        var huffman = Comparacao.ComHuffman(fonte).BitsPorSimbolo;
        var aritmetica = Comparacao.ComAritmetica(fonte).BitsPorSimbolo;

        Assert.True(huffman / aritmetica > 10, $"a razao deu {huffman / aritmetica:N1}");
    }

    /// <summary>
    /// E ela NAO ganha quando nao ha o que ganhar: na fonte uniforme, onde cada
    /// simbolo custa oito bits cheios, as duas empatam.
    /// </summary>
    [Fact]
    public void NaFonteUniformeAsDuasEmpatam()
    {
        var fonte = Fonte.Uniforme();

        var huffman = Comparacao.ComHuffman(fonte).BitsPorSimbolo;
        var aritmetica = Comparacao.ComAritmetica(fonte).BitsPorSimbolo;

        Assert.InRange(huffman / aritmetica, 0.99, 1.02);
    }

    [Fact]
    public void ADescompressaoDevolveOOriginal()
    {
        foreach (var fonte in Fonte.Todas())
            Assert.True(Comparacao.ComAritmetica(fonte).Voltou, $"{fonte.Nome} nao voltou igual");
    }

    /// <summary>
    /// E volta tambem em entradas curtas e esquisitas, que e onde a
    /// renormalizacao costuma quebrar.
    /// </summary>
    [Fact]
    public void AVoltaConfereTambemEmEntradasCurtas()
    {
        foreach (var texto in new[] { "a", "ab", "aab", "abcabcabc", "aaaaaaaaaaaaaaaaaaab", "ba" })
        {
            var fonte = De(texto);
            var modelo = new Aritmetica.Modelo(fonte);
            var (saida, _) = Aritmetica.Comprimir(fonte);

            Assert.Equal(fonte.Dados, Aritmetica.Descomprimir(saida, modelo, fonte.Tamanho));
        }
    }

    /// <summary>
    /// O caso de SUBFLUXO, em que o intervalo fica preso em volta do meio sem
    /// decidir o primeiro bit, so aparece em mensagem longa. Uma fonte de vinte
    /// mil simbolos muito enviesada passa por ele milhares de vezes.
    /// </summary>
    [Fact]
    public void OSubfluxoAparecEmMensagemLonga()
    {
        var fonte = Fonte.Enviesada(0.999, 50_000);
        var modelo = new Aritmetica.Modelo(fonte);
        var (saida, bits) = Aritmetica.Comprimir(fonte);

        Assert.Equal(fonte.Dados, Aritmetica.Descomprimir(saida, modelo, fonte.Tamanho));
        Assert.True(bits < fonte.Tamanho, "nem comprimiu");
    }

    [Fact]
    public void OModeloCobreTodosOsSimbolos()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var modelo = new Aritmetica.Modelo(fonte);

            Assert.Equal(fonte.Simbolos().Count(), modelo.Simbolos.Length);
            Assert.Equal((uint)fonte.Tamanho, modelo.Total);
            Assert.Equal(0u, modelo.Acumulado[0]);

            for (var i = 0; i < modelo.Simbolos.Length; i++)
                Assert.True(modelo.Acumulado[i + 1] > modelo.Acumulado[i]);
        }
    }

    /// <summary>
    /// O custo teorico de um simbolo e o logaritmo de um sobre a probabilidade:
    /// 0,152 bit para quem aparece 90% das vezes, e e esse numero que o Huffman
    /// nao consegue gastar.
    /// </summary>
    [Fact]
    public void OCustoTeoricoDeUmSimboloProvavelEhUmaFracaoDeBit()
    {
        var fonte = Fonte.Enviesada(0.9);

        Assert.InRange(Aritmetica.CustoDe(fonte, 'a'), 0.13, 0.18);
        Assert.InRange(Aritmetica.CustoDe(fonte, 'b'), 3.0, 3.6);
        Assert.True(double.IsInfinity(Aritmetica.CustoDe(fonte, 'z')));
    }

    /// <summary>
    /// A media dos custos teoricos, pesada pelas frequencias, E a entropia. Essa
    /// e a definicao, conferida de volta.
    /// </summary>
    [Fact]
    public void AMediaDosCustosTeoricosEhAEntropia()
    {
        foreach (var fonte in Fonte.Todas())
        {
            var soma = 0.0;
            foreach (var simbolo in fonte.Simbolos())
                soma += (double)fonte.Contagem[simbolo] / fonte.Tamanho * Aritmetica.CustoDe(fonte, simbolo);

            Assert.Equal(Entropia.OrdemZero(fonte), soma, 9);
        }
    }
}

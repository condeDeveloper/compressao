using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class RleTestes
{
    private static Fonte De(string texto) => new("teste", texto.Select(c => (byte)c).ToArray());

    [Fact]
    public void ADescompressaoDevolveOOriginal()
    {
        foreach (var fonte in Fonte.Todas())
            Assert.True(Comparacao.ComRle(fonte).Voltou, $"{fonte.Nome} nao voltou igual");
    }

    [Fact]
    public void AsCorridasSaoContadasDireito()
    {
        var (saida, bits, corridas) = Rle.Comprimir(De("aaabbc"));

        Assert.Equal(3, corridas);
        Assert.Equal(new byte[] { 3, (byte)'a', 2, (byte)'b', 1, (byte)'c' }, saida);
        Assert.Equal(48, bits);
    }

    /// <summary>
    /// Nas CORRIDAS ele ganha de todo mundo, com dez linhas de codigo. Nao
    /// existe metodo bom: existe metodo que casa com a estrutura dos dados.
    /// </summary>
    [Fact]
    public void NasCorridasORleGanhaDeTodoMundo()
    {
        var fonte = Fonte.Corridas();
        var rle = Comparacao.ComRle(fonte);

        foreach (var outro in new[] { Comparacao.ComHuffman(fonte), Comparacao.ComAritmetica(fonte) })
            Assert.True(rle.BitsPorSimbolo < outro.BitsPorSimbolo,
                        $"o {outro.Metodo} gastou {outro.BitsPorSimbolo:N2} e o RLE {rle.BitsPorSimbolo:N2}");
    }

    /// <summary>
    /// E em TEXTO ele dobra o arquivo: dezesseis bits para dizer "um byte, uma
    /// vez", e em texto quase toda corrida tem tamanho um.
    /// </summary>
    [Fact]
    public void EmTextoORleDobraOArquivo()
    {
        var rle = Comparacao.ComRle(Fonte.Texto());

        Assert.True(rle.BitsPorSimbolo > 15, $"deu {rle.BitsPorSimbolo:N2}");
        Assert.True(rle.Voltou);
    }

    /// <summary>
    /// A corrida nao passa de 255, porque o contador e um byte. Uma corrida
    /// maior vira duas, e a volta continua certa.
    /// </summary>
    [Fact]
    public void CorridaMaiorQue255ViraDuas()
    {
        var fonte = new Fonte("longa", Enumerable.Repeat((byte)'a', 600).ToArray());
        var (saida, _, corridas) = Rle.Comprimir(fonte);

        Assert.Equal(3, corridas);
        Assert.Equal(255, saida[0]);
        Assert.Equal(fonte.Dados, Rle.Descomprimir(saida));
    }

    /// <summary>
    /// A CONTAGEM que prova que nenhum metodo comprime tudo: entre todas as
    /// entradas de n bytes, exatamente METADE cresce com o RLE.
    ///
    /// Esse e um argumento de pombos e nao de engenharia: ha 2^n mensagens de n
    /// bits e menos de 2^n mensagens mais curtas, entao nao existe funcao
    /// injetora de todas as mensagens em mensagens mais curtas.
    /// </summary>
    [Fact]
    public void MetadeDasEntradasCresceComORle()
    {
        for (var bytes = 2; bytes <= 12; bytes += 2)
        {
            var (encolheram, iguais, cresceram) = Comparacao.TodasAsEntradas(bytes);
            var total = (int)Math.Pow(2, bytes);

            Assert.Equal(total, encolheram + iguais + cresceram);
            Assert.Equal(total / 2, cresceram);
        }
    }

    [Fact]
    public void EOQueEncolheNuncaChegaAMetade()
    {
        for (var bytes = 4; bytes <= 12; bytes += 2)
        {
            var (encolheram, _, _) = Comparacao.TodasAsEntradas(bytes);
            Assert.True(encolheram < Math.Pow(2, bytes) / 2);
        }
    }

    [Fact]
    public void EntradaVaziaVoltaVazia()
    {
        var (saida, bits, corridas) = Rle.Comprimir(new Fonte("vazia", Array.Empty<byte>()));

        Assert.Empty(saida);
        Assert.Equal(0, bits);
        Assert.Equal(0, corridas);
        Assert.Empty(Rle.Descomprimir(saida));
    }

    [Fact]
    public void UmByteSoViraUmPar()
    {
        var (saida, bits, _) = Rle.Comprimir(De("a"));

        Assert.Equal(16, bits);
        Assert.Equal(new byte[] { 1, (byte)'a' }, saida);
    }
}

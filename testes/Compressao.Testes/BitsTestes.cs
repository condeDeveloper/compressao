using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class BitsTestes
{
    [Fact]
    public void OQueFoiEscritoEhOQueSaiLido()
    {
        var escritor = new EscritorDeBits();
        var bits = new[] { true, false, true, true, false, false, false, true, true, false, true };

        foreach (var bit in bits) escritor.Escrever(bit);
        var leitor = new LeitorDeBits(escritor.Fechar());

        foreach (var bit in bits) Assert.Equal(bit, leitor.Ler());
    }

    [Fact]
    public void OsBitsSaoContadosSemArredondarParaByte()
    {
        var escritor = new EscritorDeBits();
        for (var i = 0; i < 11; i++) escritor.Escrever(true);

        Assert.Equal(11, escritor.Bits);
        Assert.Equal(2, escritor.Fechar().Length);
    }

    /// <summary>
    /// Contar bit e nao byte e o que permite dizer que o Huffman gastou 4,006 e
    /// nao 4,0: num fluxo de vinte mil simbolos, arredondar para byte esconde a
    /// terceira casa.
    /// </summary>
    [Fact]
    public void ADiferencaEntreContarBitEContarByte()
    {
        var fonte = Fonte.Texto(1000);
        var (saida, bits) = Huffman.Comprimir(fonte);

        Assert.True(saida.Length * 8 >= bits);
        Assert.True(saida.Length * 8 - bits < 8);
    }

    [Fact]
    public void NumeroEmLarguraFixaVoltaIgual()
    {
        var escritor = new EscritorDeBits();
        escritor.Escrever(0, 12);
        escritor.Escrever(4095, 12);
        escritor.Escrever(2026, 12);
        escritor.Escrever(5, 4);

        var leitor = new LeitorDeBits(escritor.Fechar());

        Assert.Equal(0, leitor.Ler(12));
        Assert.Equal(4095, leitor.Ler(12));
        Assert.Equal(2026, leitor.Ler(12));
        Assert.Equal(5, leitor.Ler(4));
    }

    [Fact]
    public void EscreverCadeiaDeUnsEZerosFunciona()
    {
        var escritor = new EscritorDeBits();
        escritor.Escrever("10110001");

        var bytes = escritor.Fechar();
        Assert.Single(bytes);
        Assert.Equal(0b10110001, bytes[0]);
    }

    /// <summary>
    /// O ultimo byte e completado com zeros, e esses bits de enchimento existem
    /// no arquivo e nao existem na medida: eles sao artefato do byte, nao do
    /// metodo.
    /// </summary>
    [Fact]
    public void OUltimoByteEhCompletadoComZeros()
    {
        var escritor = new EscritorDeBits();
        escritor.Escrever("101");

        var bytes = escritor.Fechar();
        Assert.Equal(0b10100000, bytes[0]);
        Assert.Equal(3, escritor.Bits);
    }

    [Fact]
    public void LerDepoisDoFimDevolveFalso()
    {
        var leitor = new LeitorDeBits(new byte[] { 0xFF });

        for (var i = 0; i < 8; i++) Assert.True(leitor.Ler());

        Assert.True(leitor.Acabou);
        Assert.False(leitor.Ler());
    }

    [Fact]
    public void FluxoVazioJaComecaAcabado()
    {
        Assert.True(new LeitorDeBits(Array.Empty<byte>()).Acabou);
        Assert.Empty(new EscritorDeBits().Fechar());
    }
}

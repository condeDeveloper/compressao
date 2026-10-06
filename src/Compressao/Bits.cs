namespace Conde.Compressao;

/// <summary>
/// Escrever e ler BIT A BIT, que e o que falta em toda linguagem que so sabe
/// trabalhar com bytes.
///
/// Compressao mede em bits, e um metodo que acerta a conta e grava em byte
/// inteiro joga fora ate sete bits por fluxo. Isso nao muda nada num arquivo
/// grande e muda tudo num teste: sem contar bit, nao da para dizer se o Huffman
/// gastou 1,00 ou 1,02 bit por simbolo.
/// </summary>
public sealed class EscritorDeBits
{
    private readonly List<byte> bytes = [];
    private int atual;
    private int ocupados;

    /// <summary>Quantos bits ja foram escritos, sem arredondar para byte.</summary>
    public long Bits { get; private set; }

    public void Escrever(bool bit)
    {
        atual = (atual << 1) | (bit ? 1 : 0);
        ocupados++;
        Bits++;

        if (ocupados == 8)
        {
            bytes.Add((byte)atual);
            atual = 0;
            ocupados = 0;
        }
    }

    public void Escrever(string codigo)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        foreach (var c in codigo) Escrever(c == '1');
    }

    /// <summary>Escreve um numero em largura fixa, do bit mais alto para o mais baixo.</summary>
    public void Escrever(int valor, int largura)
    {
        for (var i = largura - 1; i >= 0; i--) Escrever(((valor >> i) & 1) == 1);
    }

    /// <summary>
    /// Fecha o fluxo completando o ultimo byte com zeros.
    ///
    /// Os bits de enchimento sao contados a parte: eles existem no arquivo e nao
    /// existem na medida, porque sao artefato do byte e nao do metodo.
    /// </summary>
    public byte[] Fechar()
    {
        if (ocupados > 0)
        {
            bytes.Add((byte)(atual << (8 - ocupados)));
            atual = 0;
            ocupados = 0;
        }

        return bytes.ToArray();
    }
}

public sealed class LeitorDeBits
{
    private readonly byte[] bytes;
    private int posicao;

    public LeitorDeBits(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        this.bytes = bytes;
    }

    public bool Acabou => posicao >= bytes.Length * 8;

    public bool Ler()
    {
        if (Acabou) return false;

        var bit = (bytes[posicao / 8] >> (7 - posicao % 8)) & 1;
        posicao++;
        return bit == 1;
    }

    public int Ler(int largura)
    {
        var valor = 0;
        for (var i = 0; i < largura; i++) valor = (valor << 1) | (Ler() ? 1 : 0);
        return valor;
    }
}

namespace Conde.Compressao;

/// <summary>
/// A comparacao entre os metodos, com a VOLTA conferida.
///
/// Razao de compressao sem descompressao nao vale nada: qualquer metodo
/// "comprime" para zero byte se nao precisar devolver o original. Entao toda
/// medida daqui passa por descomprimir e comparar byte a byte com a entrada, e
/// um metodo que nao devolve o original exato nao entra na tabela.
/// </summary>
public static class Comparacao
{
    public sealed record Resultado(string Metodo, string Fonte, long Bits, bool Voltou, double BitsPorSimbolo)
    {
        public override string ToString() => $"{Metodo} em {Fonte}: {BitsPorSimbolo:N3} bits/simbolo";
    }

    public static bool Igual(byte[] a, byte[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;

        return true;
    }

    public static Resultado ComHuffman(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var codigos = Huffman.Codigos(fonte);
        var (saida, bits) = Huffman.Comprimir(fonte);
        var volta = Huffman.Descomprimir(saida, codigos, fonte.Tamanho);

        return new Resultado("Huffman", fonte.Nome, bits, Igual(volta, fonte.Dados), (double)bits / fonte.Tamanho);
    }

    public static Resultado ComAritmetica(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var modelo = new Aritmetica.Modelo(fonte);
        var (saida, bits) = Aritmetica.Comprimir(fonte);
        var volta = Aritmetica.Descomprimir(saida, modelo, fonte.Tamanho);

        return new Resultado("aritmetica", fonte.Nome, bits, Igual(volta, fonte.Dados), (double)bits / fonte.Tamanho);
    }

    public static Resultado ComLz77(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var (saida, bits, _, _) = Lz77.Comprimir(fonte);
        var volta = Lz77.Descomprimir(saida, fonte.Tamanho);

        return new Resultado("LZ77", fonte.Nome, bits, Igual(volta, fonte.Dados), (double)bits / fonte.Tamanho);
    }

    public static Resultado ComRle(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var (saida, bits, _) = Rle.Comprimir(fonte);
        var volta = Rle.Descomprimir(saida);

        return new Resultado("RLE", fonte.Nome, bits, Igual(volta, fonte.Dados), (double)bits / fonte.Tamanho);
    }

    public static IEnumerable<Resultado> Todos(Fonte fonte)
    {
        yield return ComHuffman(fonte);
        yield return ComAritmetica(fonte);
        yield return ComLz77(fonte);
        yield return ComRle(fonte);
    }

    /// <summary>
    /// A CONTAGEM que prova que nenhum metodo comprime tudo.
    ///
    /// Esse e um argumento de pombos e nao de engenharia: ha 2^n mensagens de n
    /// bits e menos de 2^n mensagens mais curtas que n bits, entao nao existe
    /// funcao injetora de todas as mensagens em mensagens mais curtas. Qualquer
    /// metodo que encolha alguma entrada precisa aumentar outra.
    ///
    /// A medida roda todas as entradas de um tamanho pequeno pelo RLE e conta
    /// quantas encolheram e quantas cresceram.
    /// </summary>
    public static (int Encolheram, int Iguais, int Cresceram) TodasAsEntradas(int bytes, int simbolos = 2)
    {
        int encolheram = 0, iguais = 0, cresceram = 0;
        var total = (int)Math.Pow(simbolos, bytes);

        for (var n = 0; n < total; n++)
        {
            var dados = new byte[bytes];
            var resto = n;

            for (var i = 0; i < bytes; i++) { dados[i] = (byte)(97 + resto % simbolos); resto /= simbolos; }

            var (_, bits, _) = Rle.Comprimir(new Fonte("teste", dados));

            if (bits < bytes * 8) encolheram++;
            else if (bits == bytes * 8) iguais++;
            else cresceram++;
        }

        return (encolheram, iguais, cresceram);
    }
}

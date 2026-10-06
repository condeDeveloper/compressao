namespace Conde.Compressao;

/// <summary>
/// A CODIFICACAO ARITMETICA, que resolve o problema que o Huffman nao resolve.
///
/// A ideia e trocar "um codigo por simbolo" por "um numero para a mensagem
/// inteira". A mensagem vira um intervalo dentro de [0, 1): cada simbolo corta o
/// intervalo atual na proporcao da probabilidade dele, e no fim basta escrever
/// um numero qualquer de dentro do intervalo final.
///
/// Isso faz o custo de cada simbolo ser FRACIONARIO. Um simbolo de
/// probabilidade 0,9 encolhe o intervalo por 0,9 e custa 0,152 bit, e nao um bit
/// inteiro. Na fonte enviesada o Huffman gasta mais que o dobro do minimo, e
/// esta aqui gasta o minimo com tres casas de folga.
///
/// A implementacao e inteira, com renormalizacao, porque com ponto flutuante o
/// intervalo some depois de algumas dezenas de simbolos. E o caso de SUBFLUXO,
/// em que o intervalo fica preso em volta do meio sem decidir o primeiro bit, e
/// o que separa uma implementacao que funciona de uma que quase funciona.
/// </summary>
public static class Aritmetica
{
    private const uint Topo = 0xFFFFFFFF;
    private const uint Meio = 0x80000000;
    private const uint Quarto = 0x40000000;
    private const uint TresQuartos = 0xC0000000;

    /// <summary>
    /// O modelo: a frequencia acumulada de cada simbolo.
    ///
    /// Ele e estatico e vem da propria fonte, entao a comparacao com a entropia
    /// de ordem zero e justa: os dois sabem exatamente a mesma coisa.
    /// </summary>
    public sealed class Modelo
    {
        public Modelo(Fonte fonte)
        {
            ArgumentNullException.ThrowIfNull(fonte);

            Simbolos = fonte.Simbolos().ToArray();
            Acumulado = new uint[Simbolos.Length + 1];

            var soma = 0u;
            for (var i = 0; i < Simbolos.Length; i++)
            {
                Acumulado[i] = soma;
                soma += (uint)fonte.Contagem[Simbolos[i]];
            }

            Acumulado[Simbolos.Length] = soma;
            Total = soma;

            Indice = new int[256];
            Array.Fill(Indice, -1);
            for (var i = 0; i < Simbolos.Length; i++) Indice[Simbolos[i]] = i;
        }

        public int[] Simbolos { get; }

        public uint[] Acumulado { get; }

        public uint Total { get; }

        public int[] Indice { get; }
    }

    public static (byte[] Saida, long Bits) Comprimir(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var modelo = new Modelo(fonte);
        var escritor = new EscritorDeBits();

        uint baixo = 0, alto = Topo;
        var pendentes = 0;

        foreach (var b in fonte.Dados)
        {
            var i = modelo.Indice[b];
            var faixa = (ulong)(alto - baixo) + 1;

            alto = baixo + (uint)(faixa * modelo.Acumulado[i + 1] / modelo.Total) - 1;
            baixo += (uint)(faixa * modelo.Acumulado[i] / modelo.Total);

            while (true)
            {
                if (alto < Meio) { Despejar(escritor, false, ref pendentes); }
                else if (baixo >= Meio) { Despejar(escritor, true, ref pendentes); baixo -= Meio; alto -= Meio; }
                else if (baixo >= Quarto && alto < TresQuartos)
                {
                    // SUBFLUXO: o intervalo esta em volta do meio e nao decide o
                    // primeiro bit. Sem este caso, ele aperta ate virar vazio e a
                    // saida fica errada so em mensagens longas, que e o pior
                    // jeito de um erro aparecer.
                    pendentes++;
                    baixo -= Quarto;
                    alto -= Quarto;
                }
                else break;

                baixo <<= 1;
                alto = (alto << 1) | 1;
            }
        }

        // Fechar: dois bits bastam para fixar um numero dentro do intervalo.
        pendentes++;
        Despejar(escritor, baixo >= Quarto, ref pendentes);

        var bits = escritor.Bits;
        return (escritor.Fechar(), bits);
    }

    private static void Despejar(EscritorDeBits escritor, bool bit, ref int pendentes)
    {
        escritor.Escrever(bit);
        for (; pendentes > 0; pendentes--) escritor.Escrever(!bit);
    }

    public static byte[] Descomprimir(byte[] comprimido, Modelo modelo, int quantos)
    {
        ArgumentNullException.ThrowIfNull(comprimido);
        ArgumentNullException.ThrowIfNull(modelo);

        var leitor = new LeitorDeBits(comprimido);
        var saida = new byte[quantos];

        uint baixo = 0, alto = Topo, valor = 0;
        for (var i = 0; i < 32; i++) valor = (valor << 1) | (leitor.Ler() ? 1u : 0u);

        for (var n = 0; n < quantos; n++)
        {
            var faixa = (ulong)(alto - baixo) + 1;
            var alvo = (uint)(((((ulong)(valor - baixo) + 1) * modelo.Total) - 1) / faixa);

            var i = 0;
            while (modelo.Acumulado[i + 1] <= alvo) i++;

            saida[n] = (byte)modelo.Simbolos[i];

            alto = baixo + (uint)(faixa * modelo.Acumulado[i + 1] / modelo.Total) - 1;
            baixo += (uint)(faixa * modelo.Acumulado[i] / modelo.Total);

            while (true)
            {
                if (alto < Meio) { }
                else if (baixo >= Meio) { baixo -= Meio; alto -= Meio; valor -= Meio; }
                else if (baixo >= Quarto && alto < TresQuartos) { baixo -= Quarto; alto -= Quarto; valor -= Quarto; }
                else break;

                baixo <<= 1;
                alto = (alto << 1) | 1;
                valor = (valor << 1) | (leitor.Ler() ? 1u : 0u);
            }
        }

        return saida;
    }

    /// <summary>
    /// O custo TEORICO em bits de cada simbolo no modelo, que e o logaritmo de
    /// um sobre a probabilidade.
    ///
    /// Ele existe para a medida mostrar o que o Huffman nao consegue: um simbolo
    /// de probabilidade 0,9 custa 0,152 bit aqui e um bit inteiro la.
    /// </summary>
    public static double CustoDe(Fonte fonte, int simbolo)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        if (fonte.Contagem[simbolo] == 0) return double.PositiveInfinity;

        return -Math.Log2((double)fonte.Contagem[simbolo] / fonte.Tamanho);
    }
}

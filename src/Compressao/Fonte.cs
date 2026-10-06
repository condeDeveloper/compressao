namespace Conde.Compressao;

/// <summary>
/// Uma FONTE: os bytes que vao ser comprimidos, com a contagem de cada simbolo
/// junto.
///
/// As fontes daqui sao escolhidas para separar os metodos, e nao para fazer
/// bonito. A mais importante e a enviesada: dois simbolos, um deles aparecendo
/// nove em cada dez vezes. Ela tem entropia de meio bit por simbolo, e e nela
/// que o Huffman perde feio, porque ele nao consegue gastar menos de um bit
/// inteiro em simbolo nenhum.
/// </summary>
public sealed class Fonte
{
    public Fonte(string nome, byte[] dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        Nome = nome;
        Dados = dados;

        var contagem = new int[256];
        foreach (var b in dados) contagem[b]++;
        Contagem = contagem;
    }

    public string Nome { get; }

    public byte[] Dados { get; }

    /// <summary>Quantas vezes cada byte aparece.</summary>
    public int[] Contagem { get; }

    public int Tamanho => Dados.Length;

    public int BitsSemComprimir => Dados.Length * 8;

    /// <summary>Os simbolos que realmente aparecem, em ordem.</summary>
    public IEnumerable<int> Simbolos()
    {
        for (var i = 0; i < 256; i++)
            if (Contagem[i] > 0) yield return i;
    }

    public override string ToString() => Nome;

    /// <summary>
    /// Um gerador congruente proprio, e nao o sorteio da biblioteca.
    ///
    /// A documentacao do sorteio de biblioteca nao promete a mesma sequencia
    /// entre versoes nem entre sistemas, e a integracao continua roda em tres.
    /// Com este, a semente 7 produz exatamente os mesmos bytes nos tres.
    /// </summary>
    public sealed class Sorteio
    {
        private ulong estado;

        public Sorteio(ulong semente) => estado = semente == 0 ? 1 : semente;

        public uint Proximo()
        {
            estado = estado * 6364136223846793005UL + 1442695040888963407UL;

            // Os 32 bits de CIMA, e nao 31. Com o deslocamento de 33 o valor
            // nunca passa da metade da faixa, e "sortear com probabilidade 0,9"
            // virava "sortear sempre": a fonte enviesada saia com um simbolo so
            // e a entropia dela dava zero.
            return (uint)(estado >> 32);
        }

        /// <summary>Um numero entre zero e um, para sortear com viés.</summary>
        public double Fracao() => Proximo() / (double)uint.MaxValue;
    }

    /// <summary>
    /// Dois simbolos, um deles com a probabilidade dada.
    ///
    /// Com 0,9 a entropia e 0,469 bit por simbolo, e o Huffman gasta 1 bit
    /// cravado. Essa diferenca de 113% e o assunto do repositorio.
    /// </summary>
    public static Fonte Enviesada(double probabilidade, int tamanho = 20_000, ulong semente = 7)
    {
        var sorteio = new Sorteio(semente);
        var dados = new byte[tamanho];

        for (var i = 0; i < tamanho; i++) dados[i] = sorteio.Fracao() < probabilidade ? (byte)'a' : (byte)'b';

        return new Fonte($"enviesada {probabilidade:N2}", dados);
    }

    /// <summary>Todos os simbolos igualmente provaveis: o caso sem nada a ganhar.</summary>
    public static Fonte Uniforme(int simbolos = 256, int tamanho = 20_000, ulong semente = 7)
    {
        var sorteio = new Sorteio(semente);
        var dados = new byte[tamanho];

        for (var i = 0; i < tamanho; i++) dados[i] = (byte)(sorteio.Proximo() % (uint)simbolos);

        return new Fonte($"uniforme de {simbolos}", dados);
    }

    /// <summary>
    /// Texto em portugues, escrito a mao e repetido ate dar o tamanho.
    ///
    /// Ele tem as duas coisas que importam: letras com frequencias bem
    /// diferentes e REPETICAO, que e o que a entropia de ordem zero nao enxerga.
    /// </summary>
    public static Fonte Texto(int tamanho = 20_000)
    {
        const string trecho =
            "a compressao sem perda tem um limite e esse limite tem nome. " +
            "nenhum codigo que permita recuperar o original pode gastar, em media, " +
            "menos bits por simbolo do que a entropia da fonte. isso nao e opiniao " +
            "nem detalhe de implementacao: e um teorema, e ele vale para todo " +
            "codigo que alguem ja escreveu ou ainda vai escrever. ";

        var bytes = new List<byte>();
        while (bytes.Count < tamanho)
            foreach (var c in trecho)
            {
                bytes.Add((byte)c);
                if (bytes.Count >= tamanho) break;
            }

        return new Fonte("texto", bytes.ToArray());
    }

    /// <summary>
    /// Uma fonte toda feita de REPETICAO: blocos iguais, um atras do outro.
    ///
    /// A entropia de ordem zero dela e alta, porque os simbolos sao variados, e
    /// ela comprime quase a nada com um metodo que enxerga repeticao. E o
    /// contraexemplo que mostra que a entropia de ordem zero NAO e o limite que
    /// todo mundo cita.
    /// </summary>
    public static Fonte Repetida(int tamanho = 20_000, int bloco = 60, ulong semente = 7)
    {
        var sorteio = new Sorteio(semente);
        var padrao = new byte[bloco];
        for (var i = 0; i < bloco; i++) padrao[i] = (byte)(97 + sorteio.Proximo() % 26);

        var dados = new byte[tamanho];
        for (var i = 0; i < tamanho; i++) dados[i] = padrao[i % bloco];

        return new Fonte("repetida", dados);
    }

    /// <summary>
    /// Corridas longas do mesmo byte, que e o caso para o qual o RLE existe.
    /// </summary>
    public static Fonte Corridas(int tamanho = 20_000, ulong semente = 7)
    {
        var sorteio = new Sorteio(semente);
        var dados = new List<byte>();

        while (dados.Count < tamanho)
        {
            var simbolo = (byte)(97 + sorteio.Proximo() % 4);
            var quantas = (int)(5 + sorteio.Proximo() % 40);

            for (var i = 0; i < quantas && dados.Count < tamanho; i++) dados.Add(simbolo);
        }

        return new Fonte("corridas", dados.ToArray());
    }

    public static IEnumerable<Fonte> Todas()
    {
        yield return Enviesada(0.9);
        yield return Enviesada(0.99);
        yield return Texto();
        yield return Repetida();
        yield return Corridas();
        yield return Uniforme();
    }
}

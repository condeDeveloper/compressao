namespace Conde.Compressao;

/// <summary>
/// HUFFMAN, de 1952: o melhor codigo possivel com um numero INTEIRO de bits por
/// simbolo.
///
/// A construcao e de uma simplicidade que assusta: junte os dois simbolos menos
/// frequentes num so, repita ate sobrar um, e leia a arvore. Isso produz um
/// codigo otimo, e a prova e curta.
///
/// E e exatamente o "numero inteiro de bits" que limita ele. Um simbolo que
/// aparece nove em cada dez vezes deveria custar 0,152 bit, e o Huffman nao sabe
/// gastar menos de um bit inteiro. Numa fonte dessas ele gasta mais que o DOBRO
/// do minimo teorico, e nenhuma melhoria de implementacao conserta isso, porque
/// nao e defeito de implementacao.
///
/// O teorema que fecha a historia: o comprimento medio do Huffman fica entre a
/// entropia e a entropia mais um bit. A medida confere as duas pontas.
/// </summary>
public static class Huffman
{
    private sealed record No(int Simbolo, long Peso, No? Esquerda, No? Direita)
    {
        public bool EhFolha => Esquerda is null;
    }

    /// <summary>
    /// Monta a arvore e devolve o codigo de cada simbolo.
    ///
    /// O desempate importa e nao e detalhe: com dois nos de mesmo peso, escolher
    /// um ou outro muda os codigos. O comprimento MEDIO nao muda, e os codigos
    /// mudam, entao o desempate aqui e por simbolo, para a saida ser a mesma nos
    /// tres sistemas da integracao continua.
    /// </summary>
    public static Dictionary<int, string> Codigos(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var simbolos = fonte.Simbolos().ToList();
        if (simbolos.Count == 0) return [];

        // Um simbolo so ainda precisa de um bit: zero bit nao distingue nada, e
        // o decodificador nao teria como saber quantos simbolos ler.
        if (simbolos.Count == 1) return new Dictionary<int, string> { [simbolos[0]] = "0" };

        var fila = new List<No>();
        foreach (var simbolo in simbolos) fila.Add(new No(simbolo, fonte.Contagem[simbolo], null, null));

        while (fila.Count > 1)
        {
            fila.Sort((a, b) => a.Peso != b.Peso ? a.Peso.CompareTo(b.Peso) : a.Simbolo.CompareTo(b.Simbolo));

            var menor = fila[0];
            var segundo = fila[1];
            fila.RemoveRange(0, 2);

            fila.Add(new No(Math.Min(menor.Simbolo, segundo.Simbolo), menor.Peso + segundo.Peso, menor, segundo));
        }

        var codigos = new Dictionary<int, string>();
        Percorrer(fila[0], "", codigos);
        return codigos;
    }

    private static void Percorrer(No no, string caminho, Dictionary<int, string> codigos)
    {
        if (no.EhFolha) { codigos[no.Simbolo] = caminho; return; }

        Percorrer(no.Esquerda!, caminho + "0", codigos);
        Percorrer(no.Direita!, caminho + "1", codigos);
    }

    /// <summary>
    /// Comprime, contando os bits do CORPO.
    ///
    /// A tabela de codigos nao entra na conta, e isso e uma escolha que precisa
    /// ser dita: ela custa algumas centenas de bits e some num arquivo grande,
    /// mas a comparacao com a entropia e sobre o corpo. A medida do tamanho real
    /// conta a tabela a parte.
    /// </summary>
    public static (byte[] Saida, long Bits) Comprimir(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var codigos = Codigos(fonte);
        var escritor = new EscritorDeBits();

        foreach (var b in fonte.Dados) escritor.Escrever(codigos[b]);

        var bits = escritor.Bits;
        return (escritor.Fechar(), bits);
    }

    /// <summary>
    /// Descomprime, que e a unica prova de que a compressao esta certa.
    ///
    /// Razao de compressao sem volta nao vale nada: qualquer metodo "comprime"
    /// para zero byte se nao precisar devolver o original.
    /// </summary>
    public static byte[] Descomprimir(byte[] comprimido, IReadOnlyDictionary<int, string> codigos, int quantos)
    {
        ArgumentNullException.ThrowIfNull(comprimido);
        ArgumentNullException.ThrowIfNull(codigos);

        var porCodigo = codigos.ToDictionary(e => e.Value, e => e.Key, StringComparer.Ordinal);
        var leitor = new LeitorDeBits(comprimido);
        var saida = new byte[quantos];

        for (var i = 0; i < quantos; i++)
        {
            var caminho = "";

            while (true)
            {
                if (leitor.Acabou) throw new InvalidOperationException("o fluxo acabou no meio de um codigo");

                caminho += leitor.Ler() ? '1' : '0';

                if (porCodigo.TryGetValue(caminho, out var simbolo)) { saida[i] = (byte)simbolo; break; }
            }
        }

        return saida;
    }

    /// <summary>
    /// O custo da TABELA, em bits: para cada simbolo, o byte e o comprimento do
    /// codigo.
    ///
    /// Ela existe para a comparacao ficar honesta quando a fonte e pequena.
    /// </summary>
    public static long BitsDaTabela(IReadOnlyDictionary<int, string> codigos)
    {
        ArgumentNullException.ThrowIfNull(codigos);
        return codigos.Count * (8L + 5);
    }

    /// <summary>
    /// A DESIGUALDADE DE KRAFT: a soma de dois elevado a menos o comprimento de
    /// cada codigo nao passa de um.
    ///
    /// Ela e o que garante que os codigos formam um prefixo livre, e no Huffman
    /// ela da EXATAMENTE um: nenhum bit de endereco e desperdicado. Um codigo
    /// com soma menor que um esta deixando espaco na mesa.
    /// </summary>
    public static double Kraft(IReadOnlyDictionary<int, string> codigos)
    {
        ArgumentNullException.ThrowIfNull(codigos);

        var soma = 0.0;
        foreach (var codigo in codigos.Values) soma += Math.Pow(2, -codigo.Length);

        return soma;
    }

    /// <summary>Se nenhum codigo e prefixo de outro, que e o que permite decodificar.</summary>
    public static bool PrefixoLivre(IReadOnlyDictionary<int, string> codigos)
    {
        ArgumentNullException.ThrowIfNull(codigos);

        var todos = codigos.Values.ToList();

        foreach (var a in todos)
            foreach (var b in todos)
                if (!ReferenceEquals(a, b) && a.Length < b.Length && b.StartsWith(a, StringComparison.Ordinal))
                    return false;

        return true;
    }
}

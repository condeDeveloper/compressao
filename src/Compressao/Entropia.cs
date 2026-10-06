namespace Conde.Compressao;

/// <summary>
/// A ENTROPIA de Shannon, que e o JUIZ deste repositorio.
///
/// Ela responde a pergunta que todo metodo de compressao finge responder:
/// quanto, no minimo, custa guardar isto? E a resposta e um teorema, nao uma
/// opiniao. Nenhum codigo que permita recuperar o original pode gastar, em
/// media, menos bits por simbolo do que a entropia da fonte.
///
/// Ter esse numero muda a conversa inteira. Sem ele, "comprimiu 30%" nao diz
/// nada: pode ser otimo ou pode ser o dobro do necessario. Com ele, da para
/// dizer exatamente quanto cada metodo esta desperdicando.
///
/// E tem a parte que quase ninguem conta: a entropia de ORDEM ZERO, que e esta,
/// so e limite para fontes SEM MEMORIA. Numa fonte que se repete, ela e um
/// numero alto e completamente furado, e a medida mostra metodos passando por
/// baixo dele com folga.
/// </summary>
public static class Entropia
{
    /// <summary>
    /// A entropia de ordem zero, em bits por simbolo.
    ///
    /// E a soma de p vezes o logaritmo de um sobre p, sobre os simbolos que
    /// aparecem. Simbolo que nao aparece nao entra: p vezes log de p tende a
    /// zero, e zero vezes infinito nao e conta que o computador faca.
    /// </summary>
    public static double OrdemZero(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        if (fonte.Tamanho == 0) return 0;

        var soma = 0.0;

        foreach (var simbolo in fonte.Simbolos())
        {
            var p = (double)fonte.Contagem[simbolo] / fonte.Tamanho;
            soma -= p * Math.Log2(p);
        }

        return soma;
    }

    /// <summary>O minimo teorico em BITS para a fonte inteira.</summary>
    public static double MinimoEmBits(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        return OrdemZero(fonte) * fonte.Tamanho;
    }

    /// <summary>
    /// A entropia CONDICIONAL de primeira ordem: quanto custa cada simbolo
    /// sabendo o anterior.
    ///
    /// Ela existe para mostrar o buraco da de ordem zero. Numa fonte que se
    /// repete, saber o simbolo anterior derruba o custo do seguinte quase a
    /// zero, e a diferenca entre as duas e exatamente a estrutura que a de ordem
    /// zero nao enxerga.
    /// </summary>
    public static double PrimeiraOrdem(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        if (fonte.Tamanho < 2) return OrdemZero(fonte);

        var pares = new Dictionary<int, int>();
        var anteriores = new Dictionary<int, int>();

        for (var i = 1; i < fonte.Tamanho; i++)
        {
            var chave = fonte.Dados[i - 1] * 256 + fonte.Dados[i];
            pares[chave] = pares.GetValueOrDefault(chave) + 1;
            anteriores[fonte.Dados[i - 1]] = anteriores.GetValueOrDefault(fonte.Dados[i - 1]) + 1;
        }

        var total = fonte.Tamanho - 1;
        var soma = 0.0;

        foreach (var (chave, quantos) in pares)
        {
            var anterior = chave / 256;
            var conjunta = (double)quantos / total;
            var condicional = (double)quantos / anteriores[anterior];

            soma -= conjunta * Math.Log2(condicional);
        }

        return soma;
    }

    /// <summary>
    /// O comprimento MEDIO de um conjunto de codigos, em bits por simbolo.
    ///
    /// Com ele da para comparar o Huffman com a entropia sem comprimir nada: o
    /// comprimento medio e uma propriedade da tabela de codigos, nao da saida.
    /// </summary>
    public static double ComprimentoMedio(Fonte fonte, IReadOnlyDictionary<int, string> codigos)
    {
        ArgumentNullException.ThrowIfNull(fonte);
        ArgumentNullException.ThrowIfNull(codigos);
        if (fonte.Tamanho == 0) return 0;

        var soma = 0.0;

        foreach (var simbolo in fonte.Simbolos())
            soma += (double)fonte.Contagem[simbolo] / fonte.Tamanho * codigos[simbolo].Length;

        return soma;
    }

    /// <summary>
    /// O DESPERDICIO de um metodo: quantos por cento ele gasta acima do minimo
    /// teorico.
    ///
    /// Esse e o numero que falta em quase toda comparacao de compressao, e e o
    /// unico que diz se ainda ha o que ganhar.
    /// </summary>
    public static double Desperdicio(Fonte fonte, long bitsGastos)
    {
        var minimo = MinimoEmBits(fonte);
        return minimo <= 0 ? 0 : (bitsGastos - minimo) / minimo;
    }
}

namespace Conde.Compressao;

/// <summary>
/// RLE, a compressao mais simples que existe: guardar quantas vezes cada byte se
/// repete.
///
/// Ela esta aqui como contraexemplo honesto. Numa fonte de corridas longas, ela
/// ganha de todo mundo com dez linhas de codigo. Em texto comum, ela AUMENTA o
/// arquivo, porque quase toda corrida tem tamanho um e ela gasta dezesseis bits
/// para dizer "um byte, uma vez".
///
/// Nenhum metodo de compressao comprime tudo. Isso nao e limitacao de nenhum
/// deles em particular: e contagem. Se um metodo encolhe alguma entrada, ele
/// precisa aumentar outra, porque nao existem mais mensagens curtas do que
/// longas.
/// </summary>
public static class Rle
{
    public static (byte[] Saida, long Bits, int Corridas) Comprimir(Fonte fonte)
    {
        ArgumentNullException.ThrowIfNull(fonte);

        var dados = fonte.Dados;
        var saida = new List<byte>();
        var i = 0;

        while (i < dados.Length)
        {
            var atual = dados[i];
            var quantos = 1;

            while (i + quantos < dados.Length && dados[i + quantos] == atual && quantos < 255) quantos++;

            saida.Add((byte)quantos);
            saida.Add(atual);
            i += quantos;
        }

        return (saida.ToArray(), saida.Count * 8L, saida.Count / 2);
    }

    public static byte[] Descomprimir(byte[] comprimido)
    {
        ArgumentNullException.ThrowIfNull(comprimido);

        var saida = new List<byte>();

        for (var i = 0; i + 1 < comprimido.Length; i += 2)
            for (var k = 0; k < comprimido[i]; k++) saida.Add(comprimido[i + 1]);

        return saida.ToArray();
    }
}

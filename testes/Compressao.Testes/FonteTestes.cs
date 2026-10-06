using Conde.Compressao;
using Xunit;

namespace Conde.Compressao.Testes;

public class FonteTestes
{
    [Fact]
    public void AContagemBateComOsDados()
    {
        foreach (var fonte in Fonte.Todas())
        {
            Assert.Equal(fonte.Tamanho, fonte.Contagem.Sum());
            Assert.Equal(fonte.Tamanho * 8, fonte.BitsSemComprimir);

            foreach (var simbolo in fonte.Simbolos()) Assert.True(fonte.Contagem[simbolo] > 0);
        }
    }

    /// <summary>
    /// A fonte enviesada tem DOIS simbolos, e a proporcao sai perto da pedida.
    ///
    /// Isso me custou uma tabela inteira errada: o sorteio devolvia os 31 bits
    /// de cima do estado, entao o valor nunca passava da metade da faixa e
    /// "sortear com probabilidade 0,9" virava "sortear sempre". A fonte saia com
    /// um simbolo so e a entropia dela dava ZERO, o que parecia ate plausivel.
    /// </summary>
    [Fact]
    public void AEnviesadaTemMesmoDoisSimbolosNaProporcaoPedida()
    {
        foreach (var p in new[] { 0.5, 0.9, 0.99 })
        {
            var fonte = Fonte.Enviesada(p);

            Assert.Equal(2, fonte.Simbolos().Count());
            Assert.InRange((double)fonte.Contagem['a'] / fonte.Tamanho, p - 0.02, p + 0.02);
        }
    }

    [Fact]
    public void OSorteioCobreAFaixaInteira()
    {
        var sorteio = new Fonte.Sorteio(7);
        double menor = 1, maior = 0;

        for (var i = 0; i < 10_000; i++)
        {
            var f = sorteio.Fracao();
            menor = Math.Min(menor, f);
            maior = Math.Max(maior, f);
            Assert.InRange(f, 0, 1);
        }

        Assert.True(menor < 0.01, $"o menor sorteado foi {menor}");
        Assert.True(maior > 0.99, $"o maior sorteado foi {maior}");
    }

    /// <summary>
    /// O sorteio e deterministico: a mesma semente da os mesmos bytes. Sem isso,
    /// a integracao continua nos tres sistemas estaria medindo fontes
    /// diferentes em cada um.
    /// </summary>
    [Fact]
    public void OSorteioEhDeterministico()
    {
        Assert.Equal(Fonte.Enviesada(0.9).Dados, Fonte.Enviesada(0.9).Dados);
        Assert.Equal(Fonte.Uniforme().Dados, Fonte.Uniforme().Dados);
        Assert.NotEqual(Fonte.Enviesada(0.9, semente: 1).Dados, Fonte.Enviesada(0.9, semente: 2).Dados);
    }

    [Fact]
    public void ARepetidaRepeteMesmo()
    {
        var fonte = Fonte.Repetida(1000, 60);

        for (var i = 60; i < fonte.Tamanho; i++) Assert.Equal(fonte.Dados[i % 60], fonte.Dados[i]);
    }

    [Fact]
    public void AsCorridasTemCorridasLongas()
    {
        var fonte = Fonte.Corridas(5000);
        var corridas = 0;

        for (var i = 1; i < fonte.Tamanho; i++)
            if (fonte.Dados[i] != fonte.Dados[i - 1]) corridas++;

        Assert.True((double)fonte.Tamanho / (corridas + 1) > 10,
                    $"a corrida media deu {(double)fonte.Tamanho / (corridas + 1):N1} bytes");
    }

    [Fact]
    public void AUniformeUsaTodosOsSimbolos()
    {
        Assert.Equal(256, Fonte.Uniforme().Simbolos().Count());
        Assert.Equal(4, Fonte.Uniforme(4).Simbolos().Count());
    }

    [Fact]
    public void CadaFonteTemNomeETamanho()
    {
        foreach (var fonte in Fonte.Todas())
        {
            Assert.False(string.IsNullOrWhiteSpace(fonte.ToString()));
            Assert.True(fonte.Tamanho > 1000);
        }
    }

    [Fact]
    public void FonteVaziaNaoQuebra()
    {
        var vazia = new Fonte("vazia", Array.Empty<byte>());

        Assert.Equal(0, vazia.Tamanho);
        Assert.Empty(vazia.Simbolos());
        Assert.Equal(0, Entropia.OrdemZero(vazia));
    }
}

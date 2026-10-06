# compressao

Compressão sem perda do zero em C# e .NET 8: Huffman, codificação aritmética,
LZ77 e RLE. O juiz é a **entropia de Shannon**, e toda medida passa por
descomprimir e comparar byte a byte com a entrada.

```
$ dotnet medidor.dll huffman

fonte                   entropia     Huffman     sobra   desperdicio   Kraft
enviesada 0.90             0.467       1.000     0.533       114.2 %    1.00
enviesada 0.99             0.083       1.000     0.917     1,103.3 %    1.00
texto                      3.970       4.006     0.036         0.9 %    1.00
corridas                   1.999       2.000     0.001         0.0 %    1.00
uniforme de 256            7.992       8.000     0.008         0.1 %    1.00
```

O teorema diz que o Huffman fica entre a entropia e a entropia **mais um bit**, e
a coluna da sobra confirma: nenhuma linha passa de um.

Agora olhe a coluna do desperdício. Na fonte muito enviesada, essa sobra de
`0,92` bit é **onze vezes** a entropia. O teorema do mais um bit não é consolo
nenhum quando a entropia é menor que um bit.

## O Huffman não sabe gastar menos de um bit

```
  o que cada simbolo DEVERIA custar, na enviesada de 0,9:
    'a'  aparece 90.1 %   custo teorico  0.151 bits   Huffman da 1 bit
    'b'  aparece  9.9 %   custo teorico  3.332 bits   Huffman da 1 bit
```

Um símbolo que aparece nove em cada dez vezes deveria custar `0,151` bit. O
Huffman dá um bit inteiro, porque é isso que um código de prefixo sabe fazer.
Não é defeito de implementação: é a forma do método.

A codificação aritmética resolve isso trocando "um código por símbolo" por "um
número para a mensagem inteira":

```
$ dotnet medidor.dll aritmetica

fonte                   entropia     Huffman    aritmetica   desperdicio
enviesada 0.90             0.467       1.000         0.467        0.02 %
enviesada 0.99             0.083       1.000         0.083        0.11 %
texto                      3.970       4.006         3.970        0.00 %
uniforme de 256            7.992       8.000         7.992        0.00 %
```

Mesmo modelo, mesma informação, **um doze avos** dos bits na fonte enviesada. A
diferença inteira é poder gastar `0,0144` bit num símbolo em vez de um.

## O limite que não é limite

```
$ dotnet medidor.dll memoria

  fonte repetida, 20.000 bytes, 24 simbolos
    entropia de ordem zero:       4.411 bits/simbolo
    entropia de primeira ordem:   1.462 bits/simbolo
    Huffman gastou:               4.434 bits/simbolo
    LZ77 gastou:                  0.969 bits/simbolo
    e a volta confere:            True
```

O LZ77 gasta **menos** que a entropia de ordem zero, e isso parece violar o
teorema de Shannon. Não viola nada.

O teorema diz que nenhum código gasta menos que a entropia da **fonte**. A
entropia de ordem zero não é a entropia de uma fonte com memória: ela é o que
daria se os símbolos fossem independentes, e nesta fonte eles não são. A de
primeira ordem já mostra o buraco, caindo de `4,41` para `1,46`, e ainda não é o
fundo dele.

Citar a entropia de ordem zero como "o limite" é o erro mais comum sobre
compressão, e é fácil de cometer porque o número existe e é fácil de calcular.

## Nenhum método comprime tudo, e isso é contagem

```
$ dotnet medidor.dll contagem

   bytes    entradas    encolheram    iguais   cresceram
       2           4             0         2           2
       4          16             2         6           8
       6          64            12        20          32
       8         256            58        70         128
      10       1,024           260       252         512
      12       4,096         1,124       924       2,048
```

Todas as entradas de cada tamanho passaram pelo RLE, sem amostragem. Exatamente
**metade** cresce, em todas as linhas.

O argumento é de pombos e não de engenharia: há `2^n` mensagens de n bits e menos
de `2^n` mensagens mais curtas, então não existe função injetora de todas as
mensagens em mensagens mais curtas. Qualquer método que encolha alguma entrada
**precisa** aumentar outra. A compressão funciona porque os arquivos que existem
no mundo não são sorteados entre todos os possíveis.

## A tabela inteira, com a volta conferida

```
$ dotnet medidor.dll tabela

fonte                 entropia   Huffman  aritmetica      LZ77       RLE    voltou
enviesada 0.90            0.47      1.00        0.47      1.00      2.87       sim
enviesada 0.99            0.08      1.00        0.08      0.95      0.34       sim
texto                     3.97      4.01        3.97      1.05     15.85       sim
repetida                  4.41      4.43        4.41      0.97     15.20       sim
corridas                  2.00      2.00        2.00      0.96      0.49       sim
uniforme de 256           7.99      8.00        7.99      9.00     15.92       sim
```

Quatro fontes, quatro vencedores diferentes. Não existe método bom: existe método
que casa com a estrutura dos dados.

O RLE em texto **dobra** o arquivo, gastando dezesseis bits para dizer "um byte,
uma vez". Nas corridas ele ganha de todo mundo com dez linhas de código. E o
LZ77 na fonte uniforme gasta nove bits por símbolo para guardar oito: sem
repetição nenhuma para achar, o bit de marca de cada literal não tem como se
pagar.

A última coluna é o que faz o resto valer alguma coisa. Todo número desta tabela
veio de um fluxo que foi descomprimido e comparado byte a byte com a entrada.

## O que as medidas me corrigiram

**Meu sorteio nunca passava da metade da faixa, e a fonte enviesada saía com um
símbolo só.** O gerador devolvia os 31 bits de cima do estado, então
`Fracao()` nunca chegava perto de um, e "sortear com probabilidade 0,9" virava
"sortear sempre". A entropia da fonte dava **zero**, o Huffman dava um bit, e a
tabela ficava com um desperdício de `0,0 %` que parecia até plausível. O erro só
apareceu porque a coluna de símbolos distintos dizia `1`.

**A desigualdade `H(X|Y) ≤ H(X)` vale para a distribuição, não para a estimativa
amostral dela.** As duas contas olham conjuntos ligeiramente diferentes: a de
primeira ordem usa os `n-1` pares e a marginal das posições 1 a `n-1`, a de ordem
zero usa as `n` posições. Na fonte enviesada de 0,9 isso fez a primeira ordem dar
`0,4667823` contra `0,4667727`, cruzando por um centésimo de milésimo. Não é erro
de conta, é a diferença entre o teorema e a medida dele.

**A distância do LZ77 precisa ir gravada menos um, e o erro só aparece em fonte
grande.** A distância vale de 1 a 4096 e doze bits guardam de 0 a 4095. Sem o
menos um, a distância máxima vira zero na gravação e a descompressão lê de uma
posição que ainda não existe. Numa fonte pequena a janela nunca enche, e o bug
fica escondido: ele apareceu na primeira fonte de vinte mil bytes.

**O RLE ganhar da aritmética não é contradição, e eu precisei parar para ter
certeza.** A aritmética gasta o mínimo teórico de ordem zero, e o RLE gasta
menos que isso na fonte de corridas. Os dois estão certos: a fonte de corridas
tem memória, e o mínimo de ordem zero não é o mínimo dela.

## Por que o juiz é a entropia

"Comprimiu 30%" não diz nada sozinho: pode ser ótimo ou pode ser o dobro do
necessário. Com a entropia, dá para dizer exatamente quanto cada método está
desperdiçando, e quando não há mais o que ganhar.

E a volta é a outra metade do juiz. Razão de compressão sem descompressão não
vale nada: qualquer método "comprime" para zero byte se não precisar devolver o
original.

Nenhuma medida aqui usa relógio. Todas são contagem de bits, com a entropia
calculada à parte, então a saída é a mesma no Linux, no Windows e no macOS, e a
integração contínua roda nos três.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Fonte.cs` | as fontes de teste e o sorteio determinístico |
| `Entropia.cs` | a entropia de ordem zero e de primeira ordem, que é o juiz |
| `Bits.cs` | escrever e ler bit a bit, sem arredondar para byte |
| `Huffman.cs` | a árvore, os códigos, Kraft e o prefixo livre |
| `Aritmetica.cs` | o codificador inteiro com renormalização e subfluxo |
| `Lz77.cs` | a janela deslizante e o casamento mais longo |
| `Rle.cs` | o método de dez linhas que às vezes ganha de todos |
| `Comparacao.cs` | a volta conferida byte a byte e a contagem de todas as entradas |

## Como rodar

```
dotnet test testes/Compressao.Testes/Compressao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `entropia`, `huffman`, `aritmetica`, `tabela`, `memoria`,
`contagem` e `tudo`.

## Licença

MIT.

# BRSP_StockAlert

O projeto **BRSP_StockAlert** contempla a implementação de uma aplicação de linha de comando que realiza o monitoramento do valor de algum ativo determinado pelo usuário, e envia alertas por email quando o ativo ultrapassa valores de compra e venda - também estipulados pelo usuário.

## Como Usar

Para utilizar a aplicação, basta executar pela linha de comando utilizando o seguinte formato:

```cmd
> BRSP_StockAlert <ticker_ativo> <preco_compra> <preco_venda>
```

Onde os parâmetros representam, respectivamente:

* `<ticker_ativo>`: o código (ticker) do ativo a ser monitorado. Por exemplo: `PETR4`;
* `<preco_compra>`: o preço sugerido de compra informado pelo usuário em formato decimal (`nn.nn`), onde caso o ativo esteja com valor atual **menor** do que `<preco_compra>`, um email será enviado sugerindo a compra do ativo em questão;
* `<preco_venda>`: o preço sugerido de venda informado pelo usuário em formato decimal (`nn.nn`), onde caso o ativo esteja com valor atual **maior** do que `<preco_venda>`, um email será enviado sugerindo a venda do ativo em questão.

Desta forma, um exemplo de execução válida da aplicação por linha de comando seria:

```cmd
> BRSP_StockAlert PETR4 23.37 34.05
```

## Requerimentos

Para executar a aplicação, é necessário que o usuário tenha instalada a versão mais recente da plataforma **.NET**, disponível em: <https://dotnet.microsoft.com/en-us/download/dotnet>

Além disso, é necessário garantir que os arquivos de configuração necessários estão presentes junto aos binários da aplicação. Dentro da pasta `config/`, devem existir os arquivos `settings.ini`, `auth-brapi.ini` e `smtp-credentials.ini`.

Desta forma, a estrutura do diretório que conterá o binário da aplicação deve seguir a seguinte organização:

```text
config/
    auth-brapi.ini
    settings.ini
    smtp-credentials.ini
BRSP_StockAlert.exe
BRSP_StockAlert.dll
IniFileParser.dll
Newtonsoft.Json.dll
```

O arquivo `settings.ini` com valores de exemplo pré-configurados pode ser observado dentro pasta `config/` deste repositório.

### Sobre `auth-brapi.ini` e `smtp-credentials.ini`

Os arquivos `auth-brapi.ini` e `smtp-credentials.ini` não são disponibilizados neste repositório ou distribuídos abertamente. Desta forma, é necessário que o usuário da aplicação realize a própria criação e configuração correta de tais arquivos.

O arquivo `auth-brapi.ini` contém dados de autenticação para a API provedora de dados da B3, e deve seguir o seguinte formato:

```ini
[AUTH]
; BRAPI-Token: Token de acesso da API BRAPI
BRAPI-Token = <seu_token_de_acesso_da_api_brapi>
```

O arquivo `smtp-credentials.ini` contém dados relativos à autenticação do email fonte que enviará os alertas ao usuário destino, e deve seguir o seguinte formato:

```ini
[CREDENTIALS]
; Username: Usuário/email fonte dos alertas.
Username = <seu_username_smtp>
; Password: Senha de acesso do usuário fonte dos alertas.
Password = <sua_senha_de_acesso_smtp>
```

## Como Executar o Projeto

Para a execução da aplicação contida neste projeto, é possível escolher uma dentre as seguintes formas:

* Utilizar a IDE de sua preferência para abrir o projeto, por exemplo: *Visual Studio Code*, *Visual Studio*;
* Utilizar o comando `dotnet run <parametros>` pela linha de comando para executar a aplicação a partir da pasta raiz do projeto. Por exemplo: `dotnet run PETR4 23.37 34.05`
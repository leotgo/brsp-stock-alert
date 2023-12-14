
using Newtonsoft.Json;
using IniParser;
using IniParser.Model;

namespace BRSP.Providers.BRAPI
{
    public class BRAPIStockDataProvider : IStockDataProvider
    {
        private const string BRAPI_AUTH_CONFIG_FILE = "config/auth-brapi.ini";

        private bool _isInitialized = false;
        private string _apiToken = string.Empty;

        public BRAPIStockDataProvider()
        {
            _isInitialized = false;

            var brapiIniParser = new FileIniDataParser();
            IniData brapiIniData;
            try
            {
                brapiIniData = brapiIniParser.ReadFile(BRAPI_AUTH_CONFIG_FILE);
            }
            catch
            {
                LogUtils.LogConfigError(details: $"Erro: O arquivo de configurações \'{BRAPI_AUTH_CONFIG_FILE}\' não existe ou não pôde ser lido.",
                                        referenceFile: BRAPI_AUTH_CONFIG_FILE,
                                        whatToDo: $"Certificar que existe o arquivo de configurações \'{BRAPI_AUTH_CONFIG_FILE}\' com as configurações necessárias para a aplicação funcionar corretamente.");
                return;
            }

            if (!brapiIniData.Sections.ContainsSection("AUTH"))
            {
                LogUtils.LogConfigError(details: $"O arquivo de configurações {BRAPI_AUTH_CONFIG_FILE} não contém a seção [AUTH].",
                                        referenceFile: BRAPI_AUTH_CONFIG_FILE,
                                        whatToDo: $"Certificar que existe a seção [AUTH] no arquivo de configurações {BRAPI_AUTH_CONFIG_FILE}.");
                return;
            }

            string token = brapiIniData["AUTH"]["BRAPI-Token"];
            if(string.IsNullOrEmpty(token))
            {
                LogUtils.LogConfigError(details: $"O arquivo de configurações {BRAPI_AUTH_CONFIG_FILE} não contém a chave BRAPI-Token na seção [AUTH].",
                                        referenceFile: BRAPI_AUTH_CONFIG_FILE,
                                        whatToDo: $"Certificar que a chave BRAPI-Token no arquivo {BRAPI_AUTH_CONFIG_FILE} representa um token de acesso de API válido.");
                return;
            }

            _apiToken = token;
            _isInitialized = true;
        }

        public async Task<IStockData?> TryGetStockDataAsync(string stockCode)
        {
            if(!_isInitialized)
            {
                LogUtils.LogExecError(details: $"Não foi possível atualizar as informações do ativo \'{stockCode}\'. O provedor de dados \'{nameof(BRAPIStockDataProvider)}\' não foi inicializado corretamente.",

                                      whatToDo: $"Certificar que o arquivo de configurações \'{BRAPI_AUTH_CONFIG_FILE}\' existe e contém o campo BRAPI-Token com um token válido de acesso configurado.");
                return null;
            }

            using (HttpClient client = new HttpClient())
            {
                string url = $"https://brapi.dev/api/quote/{stockCode}?token={_apiToken}";
                HttpResponseMessage httpResponse = await client.GetAsync(url);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    LogUtils.LogError($"Resposta HTTP Status \'{httpResponse.StatusCode}\' ao tentar obter informações do ativo \'{stockCode}\'.");
                    return null;
                }

                string responseBody = await httpResponse.Content.ReadAsStringAsync();
                BRAPIResponse? brapiResponse = JsonConvert.DeserializeObject<BRAPIResponse>(responseBody);
                BRAPIStockResponse? stockInfo = brapiResponse?.results?.First(stock => stock.StockCode == stockCode);
                return stockInfo;
            }
        }
    }

    [Serializable]
    public class BRAPIResponse
    {
        public List<BRAPIStockResponse>? results { get; set; }
        public DateTime? requestedAt { get; set; }
        public string? took { get; set; }
    }

    [Serializable]
    public class BRAPIStockResponse : IStockData
    {
        public bool IsValidData => !error ?? true;
        public string StockCode => symbol ?? string.Empty;
        public string ShortName => shortName ?? string.Empty;
        public string LongName => longName ?? string.Empty;
        public double CurrentPrice => regularMarketPrice ?? -1.0;
        public DateTime Timestamp => updatedAt ?? DateTime.MinValue;

        public string? symbol { get; set; } // Ticker
        public string? currency { get; set; } // Moeda
        public double? twoHundredDayAverage { get; set; } // Média de 200 dias
        public double? twoHundredDayAverageChange { get; set; } // Variação da média de 200 dias
        public double? twoHundredDayAverageChangePercent { get; set; } // Variação percentual da média de 200 dias
        public long? marketCap { get; set; } // Capitalização de mercado
        public string? shortName { get; set; } // Nome da empresa
        public string? longName { get; set; } // Nome longo da empresa
        public double? regularMarketChange { get; set; } // Variação do preço diário
        public double? regularMarketChangePercent { get; set; } // Variação percentual do preço diário
        public string? regularMarketTime { get; set; } // Data e hora do último preço
        public double? regularMarketPrice { get; set; } // Preço atual
        public double? regularMarketDayHigh { get; set; } // Preço máximo do dia
        public string? regularMarketDayRange { get; set; } // Faixa de preço do dia
        public double? regularMarketDayLow { get; set; } // Preço mínimo do dia
        public int? regularMarketVolume { get; set; } // Volume do dia
        public double? regularMarketPreviousClose { get; set; } // Preço de fechamento do dia anterior
        public double? regularMarketOpen { get; set; } // Preço de abertura do dia
        public int? averageDailyVolume3Month { get; set; } // Volume médio dos últimos 3 meses
        public int? averageDailyVolume10Day { get; set; }
        public double? priceEarnings { get; set; }
        public double? earningsPerShare { get; set; }
        public DateTime? updatedAt { get; set; }

        public bool? error { get; set; }

        public string? message { get; set; }

        public override string ToString()
        {
            if (error.HasValue && error.Value)
                return $"{{ Erro: {message} }}";

            return $"{{ Ticker: \t{StockCode} | Preço Atual: \t{CurrentPrice} | Última Atualização: \t{Timestamp} }}";
        }
    }
}
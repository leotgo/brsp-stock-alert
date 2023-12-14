using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Mail;
using IniParser;
using IniParser.Model;
using System.Text.RegularExpressions;
using BRSP;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using BRSP.Providers.BRAPI;
using System.Security.Cryptography.X509Certificates;


class BRSP_StockAlert
{
    private class InputParams
    {
        public string? StockCode { get; set; }
        public double SellPrice { get; set; }
        public double BuyPrice { get; set; }
    }

    private class AlertConfigSettings
    {
        public int AlertInterval { get; set; }
        public int StockUpdateInterval { get; set; }
        public string? MailTo { get; set; }
        public bool ResendMailWhenStockTimestampEqual { get; set; }
    }

    static void Main(string[] args)
    {
        if(!TryReadInputParams(args, out InputParams inputParams))
            return;

        var iniParser = new FileIniDataParser();
        IniData? iniData = null;
        try
        {
            iniData = iniParser.ReadFile(Constants.CONFIG_FILE);
        }
        catch
        {
            LogUtils.LogConfigError(details: $"Erro: O arquivo de configurações \'{Constants.CONFIG_FILE}\' não existe ou não pôde ser lido.",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que existe o arquivo de configurações \'{Constants.CONFIG_FILE}\' com as configurações necessárias para a aplicação funcionar corretamente.");
            return;
        }

        if(!SmtpUtils.TryReadSmtpConfigSettings(iniData, out SmtpSettings smtpSettings))
            return;

        if(!SmtpUtils.TryReadSmtpCredentialsSettings(out SmtpCredentials smtpCredentials))
            return;

        if(!TryReadAlertConfigSettings(iniData, out AlertConfigSettings alertConfigSettings))
            return;

        IStockData? stockData = null;
        IStockData? lastStockData = null;
        IStockDataProvider stockDataProvider = new BRAPIStockDataProvider();

        _ = Task.Run(() => KeepUpdatingStockDataAsync(dataProvider: stockDataProvider, 
                                                      stockCode: inputParams.StockCode!, 
                                                      updateInterval: alertConfigSettings.StockUpdateInterval, 
                                                      onStockDataUpdate: (newStockData) => { stockData = newStockData; }));

        while (true)
        {
            if( stockData is null || !stockData.IsValidData)
            {
                LogUtils.LogError($"Alerta de email suprimido pois não foi possível obter informações sobre o ativo \'{inputParams.StockCode}\'.");
                Thread.Sleep(alertConfigSettings.StockUpdateInterval);
                continue;
            }

            bool isRepeatedStockData = lastStockData is not null && lastStockData.Timestamp >= stockData.Timestamp;
            if (isRepeatedStockData && !alertConfigSettings.ResendMailWhenStockTimestampEqual)
            {
                Thread.Sleep(alertConfigSettings.StockUpdateInterval);
                continue;
            }
            lastStockData = stockData;

            double currentStockPrice = stockData.CurrentPrice;
            bool stockPriceIsOutsideThresholds = false;
            string thresholdSubjectMessage = string.Empty;
            string thresholdBodyMessage = string.Empty;
            string actionTerm = string.Empty;
            string suggestedAction = string.Empty;
            string relativeThresholdTerm = string.Empty;
            double thresholdValue = -1.0;

            if(currentStockPrice < 0)
            {
                Thread.Sleep(alertConfigSettings.StockUpdateInterval);
                continue;
            }

            if (currentStockPrice > inputParams.SellPrice)
            {
                stockPriceIsOutsideThresholds = true;
                thresholdSubjectMessage = $"Acima do Preço de Venda";
                actionTerm = "Venda";
                relativeThresholdTerm = "acima";
                suggestedAction = "Vender o ativo";
                thresholdValue = inputParams.SellPrice;
            }

            if (currentStockPrice < inputParams.BuyPrice)
            {
                stockPriceIsOutsideThresholds = true;
                thresholdSubjectMessage = $"Abaixo do Preço de Compra";
                actionTerm = "Compra";
                relativeThresholdTerm = "abaixo";
                suggestedAction = "Comprar o ativo";
                thresholdValue = inputParams.BuyPrice;
            }

            if (stockPriceIsOutsideThresholds)
            {
                string emailSubject = $"{nameof(BRSP_StockAlert)}: Ativo {inputParams.StockCode} {thresholdSubjectMessage}";
                string emailBody = $"<p>Alerta do ativo <b>{inputParams.StockCode}</b> em ({DateTime.Now}): O preço atual do ativo é de <b>{currentStockPrice}</b>," +
                                   $" e se encontra <b>{relativeThresholdTerm}</b> do valor definido de {actionTerm} de {thresholdValue}.<br>" +
                                   $"Ação sugerida: <b>{suggestedAction}</b>.</p>" +
                                   "<hr>" +
                                   "<b>INFORMAÇÕES DO ATIVO</b><br>" +
                                   $"<p>Ticker: <b>{stockData.StockCode}</b><br>" +
                                   $"Nome da Empresa: <b>{stockData.ShortName}</b><br>" +
                                   $"Nome Longo da Empresa: <b>{stockData.LongName}</b><br>" +
                                   $"Preço Atual: <b>{currentStockPrice}</b><br>" +
                                   $"Preço definido para {actionTerm}: <b>{thresholdValue}</b></p>" +
                                   "<hr><br><br>" +
                                   $"<p>Alerta gerado automaticamente pelo sistema de alertas de ativos {nameof(BRSP_StockAlert)}</p>.";
                
                var sendMailTask = SmtpUtils.TrySendMailAsync(smtpSettings, smtpCredentials, alertConfigSettings.MailTo!, emailSubject, emailBody);
                sendMailTask.Wait();
                LogUtils.Log($"Alerta de email enviado com sucesso com sugestão de {actionTerm} do ativo {stockData.StockCode}.");
            }

            System.Threading.Thread.Sleep(alertConfigSettings.AlertInterval);
        }
    }

    private static bool TryReadInputParams(string[] args, out InputParams inputParams)
    {
        inputParams = new InputParams{ StockCode = string.Empty, SellPrice = -1, BuyPrice = -1 };
        string currentExecutable = System.AppDomain.CurrentDomain.FriendlyName;

        if (args.Length != 3)
        {
            
            LogUtils.LogExecError(details: "Aplicação foi iniciada com um número inválido de parâmetros.",
                                  whatToDo: $"Utilizar a aplicação com parâmetros no formato \'{currentExecutable} <{Constants.PARAM_STOCK_CODE}> <{Constants.PARAM_MIN_THRESHOLD}> <{Constants.PARAM_MAX_THRESHOLD}>\'." + 
                                            $" Exemplo: \'{currentExecutable} {Constants.EXAMPLE_STOCK_CODE} {Constants.EXAMPLE_MIN_THRESHOLD} {Constants.EXAMPLE_MAX_THRESHOLD}\'");
            return false;
        }

        string stockCode = args[0];
        if (string.IsNullOrEmpty(stockCode))
        {
            LogUtils.LogExecError(details:  $"O parâmetro <{Constants.PARAM_STOCK_CODE}> não pode ser vazio.",
                                  whatToDo: $"Utilizar a aplicação com parâmetros no formato \'{currentExecutable} <{Constants.PARAM_STOCK_CODE}> <{Constants.PARAM_MIN_THRESHOLD}> <{Constants.PARAM_MAX_THRESHOLD}>\'." +
                                            $" Exemplo: \'{currentExecutable} {Constants.EXAMPLE_STOCK_CODE} {Constants.EXAMPLE_MIN_THRESHOLD} {Constants.EXAMPLE_MAX_THRESHOLD}\'");
            return false;
        }
        if (!ValidationUtils.IsValidB3StockCode(stockCode))
        {
            LogUtils.LogExecError(details:  $"O Ticker de ativo \'<{Constants.PARAM_STOCK_CODE}> = {inputParams.StockCode}\' é inválido.",
                                  whatToDo: $"Certificar que o Ticker de ativo informado se refere a um ativo existente. Exemplo: PETR4.\n");
            return false;
        }
        inputParams.StockCode = stockCode;

        if (!double.TryParse(args[1], out double buyPrice))
        {
            LogUtils.LogExecError(details:  $"O parâmetro \'<{Constants.PARAM_MIN_THRESHOLD}> = {args[1]}\' não é um número decimal válido.",
                                  whatToDo: $"Certificar que o formato de entrada do parâmetro <{Constants.PARAM_MIN_THRESHOLD}> obedece o formato \'NN.NN\'. Exemplo: \'23.67\'.");
            return false;
        }
        if (buyPrice <= 0.0)
        {
            LogUtils.LogExecError(details: $"O parâmetro <{Constants.PARAM_MIN_THRESHOLD}> (atual: \'{args[1]}\') precisa ser maior que zero.",
                                  whatToDo: $"Certificar que <{Constants.PARAM_MIN_THRESHOLD}> é maior que zero.");
            return false;
        }
        inputParams.BuyPrice = buyPrice;

        if (!double.TryParse(args[2], out double sellPrice))
        {
            LogUtils.LogExecError(details: $"O parâmetro <{Constants.PARAM_MAX_THRESHOLD} = \'{args[2]}\'> não é um número decimal válido.",
                                  whatToDo: $"Certificar que o formato de entrada do parâmetro <{Constants.PARAM_MAX_THRESHOLD}> obedece o formato \'NN.NN\'. Exemplo: \'32.16\'.");
            return false;
        }
        if(buyPrice >= sellPrice)
        {
            LogUtils.LogExecError(details: $"O parâmetro <{Constants.PARAM_MAX_THRESHOLD}> (atual: \'{args[2]}\') precisa ser maior que <{Constants.PARAM_MIN_THRESHOLD}> (atual: \'{args[1]}\').",
                                  whatToDo: $"Certificar que <{Constants.PARAM_MAX_THRESHOLD}> é maior que <{Constants.PARAM_MIN_THRESHOLD}>.");
            return false;
        }
        inputParams.SellPrice = sellPrice;

        return true;
    }

    private static bool TryReadAlertConfigSettings(IniData iniData, out AlertConfigSettings alertConfigSettings)
    {
        alertConfigSettings = new AlertConfigSettings{ AlertInterval = -1, StockUpdateInterval = -1, MailTo = string.Empty, ResendMailWhenStockTimestampEqual = false };

        if (!iniData.Sections.ContainsSection("ALERT"))
        {
            LogUtils.LogConfigError(details: $"O arquivo de configurações {Constants.CONFIG_FILE} não contém a seção [ALERT].",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que existe a seção [ALERT] no arquivo de configurações {Constants.CONFIG_FILE}.");
            return false;
        }

        if(!int.TryParse(iniData["ALERT"]["AlertInterval"], out int alertInterval))
        {
            LogUtils.LogConfigError(details: $"O campo 'AlertInterval' no arquivo de configurações \'{Constants.CONFIG_FILE}\' precisa ser um valor numérico e não-vazio.",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que o campo 'AlertInterval' no arquivo de configurações \'{Constants.CONFIG_FILE}\' é um valor numérico e não-vazio.");
            return false;
        }
        alertConfigSettings.AlertInterval = alertInterval;

        if(!int.TryParse(iniData["ALERT"]["StockUpdateInterval"], out int stockUpdateInterval))
        {
            LogUtils.LogConfigError(details: $"O campo 'StockUpdateInterval' no arquivo de configurações \'{Constants.CONFIG_FILE}\' precisa ser um valor numérico e não-vazio.",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que o campo 'StockUpdateInterval' no arquivo de configurações \'{Constants.CONFIG_FILE}\' é um valor numérico e não-vazio.");
            return false;
        }
        alertConfigSettings.StockUpdateInterval = stockUpdateInterval;

        string mailTo = iniData["ALERT"]["MailTo"];
        if(string.IsNullOrEmpty(mailTo))
        {
            LogUtils.LogConfigError(details: $"O campo 'MailTo' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não pôde ser lido.",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que o campo 'MailTo' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não é vazio.");
            return false;
        }
        if(!ValidationUtils.IsValidEmailAddress(mailTo))
        {
            LogUtils.LogConfigError(details: $"O campo 'MailTo' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não é um endereço de e-mail válido. Valor lido: \'{mailTo}\'.",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que o campo 'MailTo' no arquivo de configurações \'{Constants.CONFIG_FILE}\' é um endereço de e-mail válido.");
            return false;
        }
        alertConfigSettings.MailTo = mailTo;

        if(!bool.TryParse(iniData["ALERT"]["ResendMailWhenStockTimestampEqual"], out bool resendMailWhenStockTimestampEqual))
        {
            LogUtils.LogConfigError(details: $"O campo 'ResendMailWhenStockTimestampEqual' no arquivo de configurações \'{Constants.CONFIG_FILE}\' precisa ser um valor booleano (true, false) e não-vazio.",
                                    referenceFile: Constants.CONFIG_FILE,
                                    whatToDo: $"Certificar que o campo 'ResendMailWhenStockTimestampEqual' no arquivo de configurações \'{Constants.CONFIG_FILE}\' é um valor booleano (true, false) e não-vazio.");
            return false;
        }
        alertConfigSettings.ResendMailWhenStockTimestampEqual = resendMailWhenStockTimestampEqual;

        return true;
    }

    private static async Task<IStockData?> KeepUpdatingStockDataAsync(IStockDataProvider dataProvider, string stockCode, int updateInterval, Action<IStockData?> onStockDataUpdate)
    {
        IStockData? stockData;
        while (true)
        {
            stockData = await dataProvider.TryGetStockDataAsync(stockCode);
            onStockDataUpdate(stockData);

            if (stockData is null)
                LogUtils.LogError($"Não foi possível atualizar as informações do ativo {stockCode}.");
            else
                LogUtils.Log($"Atualização de Ativo \'{stockCode}\': {stockData}.");

            System.Threading.Thread.Sleep(updateInterval);
        }
    }
}
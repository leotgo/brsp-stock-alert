using System.Net;
using System.Net.Mail;
using IniParser;
using IniParser.Model;

namespace BRSP
{
    public static class SmtpUtils
    {
        public const string SMTP_CREDENTIALS_CONFIG_FILE = "config/smtp-credentials.ini";

        public static bool TryReadSmtpConfigSettings(IniData iniData, out SmtpSettings smtpSettings)
        {
            smtpSettings = new SmtpSettings();

            if (!iniData.Sections.ContainsSection("SMTP"))
            {
                LogUtils.LogConfigError(details: $"O arquivo de configurações {Constants.CONFIG_FILE} não contém a seção [SMTP].",
                                        referenceFile: Constants.CONFIG_FILE,
                                        whatToDo: $"Certificar que existe a seção [SMTP] no arquivo de configurações {Constants.CONFIG_FILE}.");
                return false;
            }

            string smtpHost = iniData["SMTP"]["Host"];
            if (string.IsNullOrEmpty(smtpHost))
            {
                LogUtils.LogConfigError(details: $"O campo 'Host' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não pôde ser lido.",
                                        referenceFile: Constants.CONFIG_FILE,
                                        whatToDo: $"Certificar que o campo 'Host' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não está vazio e representa um endereço de servidor SMTP válido.");
                return false;
            }
            smtpSettings.Host = smtpHost;

            if (!int.TryParse(iniData["SMTP"]["Port"], out int smtpPort))
            {
                LogUtils.LogConfigError(details: $"O campo 'Port' no arquivo de configurações \'{Constants.CONFIG_FILE}\' precisa ser um valor numérico que representa uma porta válida para servidor SMTP.",
                                        referenceFile: Constants.CONFIG_FILE,
                                        whatToDo: $"Certificar que o campo 'Port' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não está vazio e representa uma porta válida para o servidor SMTP.");
                return false;
            }
            smtpSettings.Port = smtpPort;

            if (!bool.TryParse(iniData["SMTP"]["EnableSsl"], out bool smtpEnableSsl))
            {
                LogUtils.LogConfigError(details: $"O campo 'EnableSsl' no arquivo de configurações \'{Constants.CONFIG_FILE}\' precisa ser um valor booleano (true, false) e não-vazio.",
                                        referenceFile: Constants.CONFIG_FILE,
                                        whatToDo: $"Certificar que o campo 'EnableSsl' no arquivo de configurações \'{Constants.CONFIG_FILE}\' não está vazio e representa um valor booleano (true, false).");
                return false;
            }
            smtpSettings.EnableSsl = smtpEnableSsl;

            return true;
        }

        public static bool TryReadSmtpCredentialsSettings(out SmtpCredentials smtpCredentials)
        {
            smtpCredentials = new SmtpCredentials();

            var smtpCredentialsIniParser = new FileIniDataParser();
            IniData smtpCredentialsIniData;
            try
            {
                smtpCredentialsIniData = smtpCredentialsIniParser.ReadFile(SMTP_CREDENTIALS_CONFIG_FILE);
            }
            catch
            {
                LogUtils.LogConfigError(details: $"Erro: O arquivo de configurações \'{SMTP_CREDENTIALS_CONFIG_FILE}\' não existe ou não pôde ser lido.",
                                        referenceFile: SMTP_CREDENTIALS_CONFIG_FILE,
                                        whatToDo: $"Certificar que existe o arquivo de configurações \'{SMTP_CREDENTIALS_CONFIG_FILE}\' com as configurações necessárias para a aplicação funcionar corretamente.");
                return false;
            }

            if (!smtpCredentialsIniData.Sections.ContainsSection("CREDENTIALS"))
            {
                LogUtils.LogConfigError(details: $"O arquivo de configurações {SMTP_CREDENTIALS_CONFIG_FILE} não contém a seção [CREDENTIALS].",
                                        referenceFile: SMTP_CREDENTIALS_CONFIG_FILE,
                                        whatToDo: $"Certificar que existe a seção [CREDENTIALS] no arquivo de configurações {SMTP_CREDENTIALS_CONFIG_FILE}.");
                return false;
            }

            string smtpUser = smtpCredentialsIniData["CREDENTIALS"]["Username"];
            if (string.IsNullOrEmpty(smtpUser))
            {
                LogUtils.LogConfigError(details: $"O campo 'Username' no arquivo de configurações \'{SMTP_CREDENTIALS_CONFIG_FILE}\' não pôde ser lido.",
                                        referenceFile: SMTP_CREDENTIALS_CONFIG_FILE,
                                        whatToDo: $"Certificar que o campo 'Username' no arquivo de configurações \'{SMTP_CREDENTIALS_CONFIG_FILE}\' não está vazio e representa um usuário válido para autenticação SMTP.");
                return false;
            }
            smtpCredentials.Username = smtpUser;

            string smtpPass = smtpCredentialsIniData["CREDENTIALS"]["Password"];
            if (string.IsNullOrEmpty(smtpPass))
            {
                LogUtils.LogConfigError(details: $"O campo 'Password' no arquivo de configurações \'{SMTP_CREDENTIALS_CONFIG_FILE}\' não pôde ser lido.",
                                        referenceFile: SMTP_CREDENTIALS_CONFIG_FILE,
                                        whatToDo: $"Certificar que o campo 'Password' no arquivo de configurações \'{SMTP_CREDENTIALS_CONFIG_FILE}\' não está vazio e representa uma senha válida para autenticação SMTP.");
                return false;
            }
            smtpCredentials.Password = smtpPass;

            return true;
        }

        public static async Task<bool> TrySendMailAsync(SmtpSettings smtpSettings, SmtpCredentials credentials, string mailTo, string subject, string body)
        {
            var client = new SmtpClient(smtpSettings.Host, smtpSettings.Port)
            {
                Credentials = new NetworkCredential(credentials.Username, credentials.Password),
                EnableSsl = smtpSettings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(credentials.Username!),
                To = { mailTo },
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (SmtpException smtpException)
            {
                LogUtils.LogError($"Não foi possível enviar o e-mail para {mailTo}. Mensagem do Servidor SMTP: \"{smtpException.Message}\".");
                return false;
            }

            return true;
        }
    }

    public class SmtpSettings
    {
        public string? Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
    }

    public class SmtpCredentials
    {
        public bool IsValid => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
namespace BRSP
{
    public static class LogUtils
    {
        public static void Log(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }

        public static void LogError(string errorMessage)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[{DateTime.Now}] Erro: {errorMessage}");
        }

        public static void LogExecError(string details, string whatToDo = "")
        {
            string[] inputParams = Environment.GetCommandLineArgs();

            string[] args = new string[3];
            for (int i = 0; i < 3; ++i)
                args[i] = "[VAZIO]";
            for (int i = 1; i < inputParams.Length; ++i)
                args[i-1] = inputParams[i];

            string executableName = System.AppDomain.CurrentDomain.FriendlyName;
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[{DateTime.Now}] --------------------------------------------------------------\n" +
                                    $"[{DateTime.Now}]  {executableName}: ERRO AO EXECUTAR A APLICAÇÃO.\n" +
                                    $"[{DateTime.Now}] --------------------------------------------------------------\n" +
                                    $"[{DateTime.Now}]  > Detalhes: \t\t{details}\n" +
                                    $"[{DateTime.Now}]  > Executado: \t\t\"{executableName} {args[0]} {args[1]} {args[2]}\"\n" +
                                    (!string.IsNullOrEmpty(whatToDo) ? $"[{DateTime.Now}]  > Ação Sugerida: \t{whatToDo}\n" : ""));
        }

        public static void LogConfigError(string details, string referenceFile, string whatToDo = "")
        {
            string executableName = System.AppDomain.CurrentDomain.FriendlyName;
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[{DateTime.Now}] --------------------------------------------------------------\n" +
                                    $"[{DateTime.Now}]  {executableName}: ERRO AO CONFIGURAR A APLICAÇÃO.\n" +
                                    $"[{DateTime.Now}] --------------------------------------------------------------\n" +
                                    $"[{DateTime.Now}]  > Detalhes: \t\t{details}\n" +
                                    $"[{DateTime.Now}]  > Arquivo: \t\t{referenceFile}\n" +
                                    (!string.IsNullOrEmpty(whatToDo) ? $"[{DateTime.Now}]  > Ação Sugerida: \t{whatToDo}\n" : ""));
        }
    }
}
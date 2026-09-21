using Newtonsoft.Json;

namespace api_finances.src.Shared.Utils
{
    public static class Util
    {
        public static void ConsoleLog(dynamic obj)
        {
            Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        public static dynamic GenerateCodeAccess(int minutesExpiration = 5)
        {
            return new
            {
                CodeAccess = new Random().Next(100000, 999999).ToString(),
                CodeAccessExpiration = DateTime.UtcNow.AddMinutes(minutesExpiration)
            };
        }

        public static string NormalizeMessageLogger(int statusCode, string message)
        {
            string responseMessage;

            if (statusCode > 204)
            {
                if (message.Contains("Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. "))
                {
                    responseMessage = message.Split("Ocorreu um erro inesperado. Por favor, tente novamente mais tarde. ")[1];
                }
                else
                {
                    responseMessage = message;
                }
            }
            else
            {
                responseMessage = message;
            }

            return responseMessage;
        }
    }
}
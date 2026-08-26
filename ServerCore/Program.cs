using Library;
using Server.Envir;
using System;
using System.Reflection;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = Assembly.GetAssembly(typeof(Config));
            ConfigReader.Load(assembly);
            Config.LoadVersion();
            try
            {
                if (!string.IsNullOrEmpty(Config.EncryptionKey))
                    SEnvir.CryptoKey = Convert.FromBase64String(Config.EncryptionKey);
            }
            catch (Exception)
            {
                throw new ApplicationException($"加密密钥格式无效，应为 32 字节的 Base64 字符串");
            }

            if (Config.EncryptionEnabled && SEnvir.CryptoKey == null)
                throw new ApplicationException($"已启用加密，但未指定密钥 [System] => DatabaseKey");

            if (Config.EncryptionEnabled)
                Encryption.SetKey(SEnvir.CryptoKey);

            SEnvir.UseLogConsole = true;
            SEnvir.StartServer();

            Console.CancelKeyPress += Console_CancelKeyPress;

            // We check EnvirThread why when SEnvir is full stoped, set this to null...
            while (SEnvir.EnvirThread != null)
            {
                var command = Console.ReadLine();

            }

            ConfigReader.Save(typeof(Config).Assembly);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            SEnvir.Started = false;
        }
    }
}

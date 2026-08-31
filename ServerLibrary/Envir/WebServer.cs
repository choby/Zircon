using Library;
using MirDB;
using Server.DBModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server.Envir
{
    public static class WebServer
    {
        public static ConcurrentQueue<WebCommand> WebCommandQueue;
        public static bool WebServerStarted { get; set; }

        private static HttpListener WebListener;
        public const string ActivationCommand = "Activation", ResetCommand = "Reset", DeleteCommand = "Delete",
            SystemDBSyncCommand = "SystemDBSync";
        public const string ActivationKey = "ActivationKey", ResetKey = "ResetKey", DeleteKey = "DeleteKey";

        public const string Completed = "Completed";
        public const string Currency = "GBP";

        public static Dictionary<decimal, long> GoldTable = new Dictionary<decimal, long>
        {
            [5M] = 500,
            [10M] = 1030,
            [15M] = 1590,
            [20M] = 2180,
            [30M] = 3360,
            [50M] = 5750,
            [100M] = 12000,
        };

        public static readonly string VerifiedPath = PlatformPath.Resolve(@".\Database\Store\Verified\"),
            InvalidPath = PlatformPath.Resolve(@".\Database\Store\Invalid\"),
            CompletePath = PlatformPath.Resolve(@".\Database\Store\Complete\");

        private static HttpListener BuyListener, IPNListener;
        public static ConcurrentQueue<IPNMessage> Messages = new ConcurrentQueue<IPNMessage>();
        public static List<IPNMessage> PaymentList = new List<IPNMessage>(), HandledPayments = new List<IPNMessage>();

        static WebServer()
        {
            Messages = new ConcurrentQueue<IPNMessage>();

            PaymentList.Clear();

            if (Directory.Exists(VerifiedPath))
            {
                string[] files = Directory.GetFiles(VerifiedPath);

                foreach (string file in files)
                    Messages.Enqueue(new IPNMessage { FileName = file, Message = File.ReadAllText(file), Verified = true });
            }
        }

        public static void StartWebServer(bool log = true)
        {
            if (!Config.EnableWebServer) return;

            try
            {
                WebCommandQueue = new ConcurrentQueue<WebCommand>();

                WebListener = new HttpListener();
                WebListener.Prefixes.Add(Config.WebPrefix);

                WebListener.Start();
                WebListener.BeginGetContext(WebConnection, null);

                BuyListener = new HttpListener();
                BuyListener.Prefixes.Add(Config.BuyPrefix);

                IPNListener = new HttpListener();
                IPNListener.Prefixes.Add(Config.IPNPrefix);

                BuyListener.Start();
                BuyListener.BeginGetContext(BuyConnection, null);

                IPNListener.Start();
                IPNListener.BeginGetContext(IPNConnection, null);



                WebServerStarted = true;

                if (log) SEnvir.Log("Web 服务器已启动。");
            }
            catch (Exception ex)
            {
                WebServerStarted = false;
                SEnvir.Log(ex.ToString());

                if (WebListener != null && WebListener.IsListening)
                    WebListener?.Stop();
                WebListener = null;

                if (BuyListener != null && BuyListener.IsListening)
                    BuyListener?.Stop();
                BuyListener = null;

                if (IPNListener != null && IPNListener.IsListening)
                    IPNListener?.Stop();
                IPNListener = null;
            }
        }
        public static void StopWebServer(bool log = true)
        {
            if (!Config.EnableWebServer) return;

            HttpListener expiredWebListener = WebListener;
            WebListener = null;

            HttpListener expiredBuyListener = BuyListener;
            BuyListener = null;
            HttpListener expiredIPNListener = IPNListener;
            IPNListener = null;


            WebServerStarted = false;
            expiredWebListener?.Stop();
            expiredBuyListener?.Stop();
            expiredIPNListener?.Stop();

            WebCommandQueue = null;
            PaymentList.Clear();
            HandledPayments.Clear();
            while (Messages.TryDequeue(out _))
            {
            }

            if (log) SEnvir.Log("Web 服务器已停止。");
        }

        private static void WebConnection(IAsyncResult result)
        {
            try
            {
                HttpListenerContext context = WebListener.EndGetContext(result);

                string command = context.Request.QueryString["Type"];

                switch (command)
                {
                    case ActivationCommand:
                        Activation(context);
                        break;
                    case ResetCommand:
                        ResetPassword(context);
                        break;
                    case DeleteCommand:
                        DeleteAccount(context);
                        break;
                    case SystemDBSyncCommand:
                        SystemDBSync(context);
                        break;
                }
            }
            catch { }
            finally
            {
                if (WebListener != null && WebListener.IsListening)
                    WebListener.BeginGetContext(WebConnection, null);
            }
        }

        private static void SystemDBSync(HttpListenerContext context)
        {
            try
            {
                if (!Config.AllowSystemDBSync)
                {
                    SEnvir.Log($"尝试同步，但同步功能未启用");
                    context.Response.StatusCode = 401;
                    return;
                }

                if (context.Request.HttpMethod != "POST" || !context.Request.HasEntityBody)
                {
                    SEnvir.Log($"尝试同步，但请求方法不是 POST 或请求正文为空");
                    context.Response.StatusCode = 401;
                    return;
                }

                if (context.Request.ContentLength64 > 1024 * 1024 * 10)
                {
                    SEnvir.Log($"尝试同步，但数据超过 SystemDB 大小限制");
                    context.Response.StatusCode = 400;
                    return;
                }

                var masterPassword = context.Request.QueryString["Key"];
                if (string.IsNullOrEmpty(masterPassword) || !masterPassword.Equals(Config.SyncKey))
                {
                    SEnvir.Log($"尝试同步，但收到的密钥无效");
                    context.Response.StatusCode = 400;
                    return;
                }

                SEnvir.Log($"正在开始远程同步...");

                var buffer = new byte[context.Request.ContentLength64];
                var offset = 0;
                var length = 0;
                var bufferSize = 1024 * 16;

                while ((length = context.Request.InputStream.Read(buffer, offset, offset + bufferSize > buffer.Length ? buffer.Length - offset : bufferSize)) > 0)
                    offset += length;

                if (SEnvir.Session.BackUp && !Directory.Exists(SEnvir.Session.SystemBackupPath))
                    Directory.CreateDirectory(SEnvir.Session.SystemBackupPath);

                if (File.Exists(SEnvir.Session.SystemPath))
                {
                    if (SEnvir.Session.BackUp)
                    {
                        using (FileStream sourceStream = File.OpenRead(SEnvir.Session.SystemPath))
                        using (FileStream destStream = File.Create(SEnvir.Session.SystemBackupPath + "System " + SEnvir.Session.ToBackUpFileName(DateTime.UtcNow) + Session.Extension + Session.CompressExtension))
                        using (GZipStream compress = new GZipStream(destStream, CompressionMode.Compress))
                            sourceStream.CopyTo(compress);
                    }

                    File.Delete(SEnvir.Session.SystemPath);
                }

                File.WriteAllBytes(SEnvir.Session.SystemPath, buffer);

                context.Response.StatusCode = 200;

                SEnvir.Log($"同步完成...");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                var message = Encoding.UTF8.GetBytes(ex.ToString());
                context.Response.OutputStream.Write(message, 0, message.Length);
                SEnvir.Log("同步异常：" + ex.ToString());
            }
            finally
            {
                context.Response.Close();
            }
        }

        private static void Activation(HttpListenerContext context)
        {
            string key = context.Request.QueryString[ActivationKey];

            if (string.IsNullOrEmpty(key))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            AccountInfo account = null;
            for (int i = 0; i < SEnvir.AccountInfoList.Count; i++)
            {
                AccountInfo temp = SEnvir.AccountInfoList[i]; //Different Threads, Caution must be taken to prevent errors
                if (string.Compare(temp.ActivationKey, key, StringComparison.Ordinal) != 0) continue;

                account = temp;
                break;
            }

            if (Config.AllowWebActivation && account != null)
            {
                WebCommandQueue.Enqueue(new WebCommand(CommandType.Activation, account));
                context.Response.Redirect(Config.ActivationSuccessLink);
            }
            else
                context.Response.Redirect(Config.ActivationFailLink);

            context.Response.Close();
        }
        private static void ResetPassword(HttpListenerContext context)
        {
            string key = context.Request.QueryString[ResetKey];

            if (string.IsNullOrEmpty(key))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            AccountInfo account = null;
            for (int i = 0; i < SEnvir.AccountInfoList.Count; i++)
            {
                AccountInfo temp = SEnvir.AccountInfoList[i]; //Different Threads, Caution must be taken to prevent errors
                if (string.Compare(temp.ResetKey, key, StringComparison.Ordinal) != 0) continue;

                account = temp;
                break;
            }

            if (Config.AllowWebResetPassword && account != null && account.ResetTime.AddMinutes(25) > SEnvir.Now)
            {
                WebCommandQueue.Enqueue(new WebCommand(CommandType.PasswordReset, account));
                context.Response.Redirect(Config.ResetSuccessLink);
            }
            else
                context.Response.Redirect(Config.ResetFailLink);

            context.Response.Close();
        }
        private static void DeleteAccount(HttpListenerContext context)
        {
            string key = context.Request.QueryString[DeleteKey];

            if (string.IsNullOrEmpty(key))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            AccountInfo account = null;
            for (int i = 0; i < SEnvir.AccountInfoList.Count; i++)
            {
                AccountInfo temp = SEnvir.AccountInfoList[i]; //Different Threads, Caution must be taken to prevent errors
                if (string.Compare(temp.ActivationKey, key, StringComparison.Ordinal) != 0) continue;

                account = temp;
                break;
            }

            if (Config.AllowDeleteAccount && account != null)
            {
                WebCommandQueue.Enqueue(new WebCommand(CommandType.AccountDelete, account));
                context.Response.Redirect(Config.DeleteSuccessLink);
            }
            else
                context.Response.Redirect(Config.DeleteFailLink);

            context.Response.Close();
        }

        private static void BuyConnection(IAsyncResult result)
        {
            try
            {
                HttpListenerContext context = BuyListener.EndGetContext(result);

                string characterName = context.Request.QueryString["Character"];

                CharacterInfo character = null;
                for (int i = 0; i < SEnvir.CharacterInfoList.Count; i++)
                {
                    if (string.Compare(SEnvir.CharacterInfoList[i].CharacterName, characterName, StringComparison.OrdinalIgnoreCase) != 0) continue;

                    character = SEnvir.CharacterInfoList[i];
                    break;
                }

                if (character?.Account.Key != context.Request.QueryString["Key"])
                    character = null;

                string response = character == null ? Properties.Resources.CharacterNotFound : Properties.Resources.BuyGameGold.Replace("$CHARACTERNAME$", character.CharacterName);

                using (StreamWriter writer = new StreamWriter(context.Response.OutputStream, context.Request.ContentEncoding))
                    writer.Write(response);
            }
            catch { }
            finally
            {
                if (BuyListener != null && BuyListener.IsListening) //IsBound ?
                    BuyListener.BeginGetContext(BuyConnection, null);
            }

        }
        private static void IPNConnection(IAsyncResult result)
        {
            const string LiveURL = @"https://ipnpb.paypal.com/cgi-bin/webscr";

            const string verified = "VERIFIED";

            try
            {
                if (IPNListener == null || !IPNListener.IsListening) return;

                HttpListenerContext context = IPNListener.EndGetContext(result);

                string rawMessage;
                using (StreamReader readStream = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    rawMessage = readStream.ReadToEnd();


                Task.Run(async () =>
                {
                    string data = "cmd=_notify-validate&" + rawMessage;

                    // Create a handler with explicit TLS settings
                    var handler = new HttpClientHandler
                    {
                        SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                     | System.Security.Authentication.SslProtocols.Tls13
                    };

                    using (HttpClient httpClient = new HttpClient(handler))
                    {
                        var content = new StringContent(data, Encoding.ASCII, "application/x-www-form-urlencoded");
                        var response = await httpClient.PostAsync(LiveURL, content);

                        IPNMessage message = new IPNMessage { Message = rawMessage, Verified = await response.Content.ReadAsStringAsync() == verified };

                        if (!Directory.Exists(VerifiedPath))
                            Directory.CreateDirectory(VerifiedPath);

                        if (!Directory.Exists(InvalidPath))
                            Directory.CreateDirectory(InvalidPath);

                        string path = (message.Verified ? VerifiedPath : InvalidPath) + Path.GetRandomFileName();

                        File.WriteAllText(path, message.Message);

                        message.FileName = path;

                        Messages.Enqueue(message);
                    }
                });

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                SEnvir.Log(ex.ToString());
            }
            finally
            {
                if (IPNListener != null && IPNListener.IsListening) //IsBound ?
                    IPNListener.BeginGetContext(IPNConnection, null);
            }
        }

        public static void Process()
        {
            while (!WebCommandQueue.IsEmpty)
            {
                if (!WebCommandQueue.TryDequeue(out WebCommand webCommand)) continue;

                switch (webCommand.Command)
                {
                    case CommandType.None:
                        break;
                    case CommandType.Activation:
                        webCommand.Account.Activated = true;
                        webCommand.Account.ActivationKey = string.Empty;
                        break;
                    case CommandType.PasswordReset:
                        string password = Functions.RandomString(SEnvir.Random, 10);

                        webCommand.Account.Password = SEnvir.CreateHash(password);
                        webCommand.Account.ResetKey = string.Empty;
                        webCommand.Account.WrongPasswordCount = 0;
                        EmailService.SendResetPasswordEmail(webCommand.Account, password);
                        break;
                    case CommandType.AccountDelete:
                        if (webCommand.Account.Activated) continue;

                        webCommand.Account.Delete();
                        break;
                }
            }
        }

        public static void Save()
        {
            HandledPayments.AddRange(PaymentList);
        }

        public static void CommitChanges(object data)
        {
            foreach (IPNMessage message in HandledPayments)
            {
                if (message.Duplicate)
                {
                    File.Delete(message.FileName);
                    continue;
                }

                if (!Directory.Exists(CompletePath))
                    Directory.CreateDirectory(CompletePath);

                File.Move(message.FileName, CompletePath + Path.GetFileName(message.FileName) + ".txt");
                PaymentList.Remove(message);
            }
            HandledPayments.Clear();
        }
    }
}

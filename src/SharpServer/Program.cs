using System;
using System.Net;
using log4net;

namespace SharpServer
{
    class Program
    {
        protected static ILog _log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            using (SharpServer.Ftp.FtpServer s = new SharpServer.Ftp.FtpServer(new[] { new IPEndPoint(IPAddress.Any, 21), new IPEndPoint(IPAddress.IPv6Any, 21) }))
            {
                s.Start();

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);
            }

            return;

            using (Server<SharpServer.Email.ImapClientConnection> imapServer = new Server<SharpServer.Email.ImapClientConnection>(143))
            using (Server<SharpServer.Email.SmtpClientConnection> smtpServer = new Server<SharpServer.Email.SmtpClientConnection>(25))
            {
                smtpServer.Start();
                imapServer.Start();

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);
            }

            return;

            using (Server<SharpServer.Email.Pop3ClientConnection> pop3server = new Server<SharpServer.Email.Pop3ClientConnection>(110))
            using (Server<SharpServer.Email.SmtpClientConnection> smtpServer = new Server<SharpServer.Email.SmtpClientConnection>(25))
            {
                pop3server.Start();
                smtpServer.Start();

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);
            }


            return;

            using (Server<SharpServer.Email.SmtpClientConnection> Server = new Server<SharpServer.Email.SmtpClientConnection>(25))
            {
                Server.Start();

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);
            }

            return;

        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Fatal((Exception)e.ExceptionObject);
        }
    }
}

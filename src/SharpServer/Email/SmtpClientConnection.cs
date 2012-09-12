using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpServer.Email
{
    public class SmtpClientConnection : ClientConnection
    {
        private class MailData
        {
            public string RawData { get; set; }
        }

        private string _mailFrom;
        private List<string> _recipientTo;
        private MailData _message;

        public SmtpClientConnection() : base()
        {
            _recipientTo = new List<string>();
        }

        bool _dataFollows = false;

        protected override Response HandleCommand(Command cmd)
        {
            Response response;

            if (_dataFollows)
            {
                response = DataReceived(cmd.Raw);
            }
            else
            {
                switch (cmd.Code)
                {
                    case "HELO":
                    case "EHLO":
                        response = Hello(cmd.Arguments.FirstOrDefault());
                        break;
                    case "MAIL":
                        response = Mail(cmd.Arguments.FirstOrDefault());
                        break;
                    case "RCPT":
                        response = Recipient(cmd.Arguments.FirstOrDefault());
                        break;
                    case "DATA":
                        response = Data();
                        break;
                    case "RSET":
                        response = Reset();
                        break;
                    case "VRFY":
                        response = new Response { Code = "250", Text = "OK" };
                        break;
                    case "EXPN":
                        response = new Response { Code = "250", Text = "OK" };
                        break;
                    case "HELP":
                        response = new Response { Code = "250", Text = "OK" };
                        break;
                    case "NOOP":
                        response = new Response { Code = "250", Text = "OK" };
                        break;
                    case "QUIT":
                        response = new Response { Code = "221", Text = "localdomain Service closing transmission channel", ShouldQuit = true };
                        break;
                    default:
                        response = new Response { Code = "250", Text = "OK" };
                        break;
                }
            }

            return response;
        }

        protected override void OnConnected()
        {
            Write("220 localdomain Service ready");
            Read();
        }

        #region SMTP Commands

        /// <summary>
        /// 4.1.1.1
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static Response Hello(string domain)
        {
            return new Response { Code = "250", Text = "localdomain HI" };
        }

        /// <summary>
        /// 4.1.1.2
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        private Response Mail(string from)
        {
            if (from.StartsWith("FROM:<", StringComparison.OrdinalIgnoreCase) && from.EndsWith(">", StringComparison.OrdinalIgnoreCase))
            {
                from = from.Substring(0, from.Length - 1).Remove(6);
            }

            _mailFrom = from;

            return new Response { Code = "250", Text = "OK" };
        }

        /// <summary>
        /// 4.1.1.3
        /// </summary>
        /// <returns></returns>
        private Response Recipient(string to)
        {
            if (to.StartsWith("TO:<", StringComparison.OrdinalIgnoreCase) && to.EndsWith(">", StringComparison.OrdinalIgnoreCase))
            {
                to = to.Substring(0, to.Length - 1).Substring(4);
            }
            else
            {
                to = "devnull";
            }

            _recipientTo.Add(to);

            return new Response { Code = "250", Text = "OK" };
        }

        /// <summary>
        /// 4.1.1.4
        /// </summary>
        /// <returns></returns>
        private Response Data()
        {
            ExpectedTerminator = "\r\n.\r\n";
            _dataFollows = true;

            return new Response { Code = "354", Text = "Start mail input; end with <CRLF>.<CRLF>" };
        }

        private Response DataReceived(string data)
        {
            ExpectedTerminator = "\r\n";
            _dataFollows = false;

            _message = new MailData { RawData = data };

            string mailbox = _recipientTo[0];

            if (mailbox.IndexOf('@') >= 0)
            {
                mailbox = mailbox.Substring(0, _recipientTo[0].IndexOf('@'));
            }

            string dir = Path.Combine(".", "mail", mailbox, "tmp");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var epoch = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds).ToString("D20");

            var tempFileName = Path.Combine(dir, string.Concat(epoch, "-", Guid.NewGuid(), ".txt"));

            using (StreamWriter s = File.CreateText(tempFileName))
            {
                s.Write(_message.RawData);
            }

            dir = Path.Combine(new FileInfo(tempFileName).Directory.Parent.FullName, "new");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var permFileName = Path.Combine(dir, string.Concat(epoch, "-", Guid.NewGuid(), ".txt"));

            File.Move(tempFileName, permFileName);

            return new Response { Code = "250", Text = "OK" };
        }

        /// <summary>
        /// 4.1.1.5
        /// </summary>
        /// <returns></returns>
        private Response Reset()
        {
            _mailFrom = null;
            _recipientTo = null;
            _message = null;

            return new Response { Code = "250", Text = "OK" };
        }

        /// <summary>
        /// 4.1.1.6
        /// </summary>
        /// <returns></returns>
        private static Response Verify(string user)
        {

            return new Response { Code = "250", Text = "OK" };
        }

        /// <summary>
        /// 4.1.1.7
        /// </summary>
        /// <returns></returns>
        private static Response Expand(string listName)
        {

            return new Response { Code = "250", Text = "OK" };
        }

        /// <summary>
        /// 4.1.1.8
        /// </summary>
        /// <returns></returns>
        private static Response Help()
        {

            return new Response { Code = "250", Text = "OK" };
        }

        #endregion
    }
}

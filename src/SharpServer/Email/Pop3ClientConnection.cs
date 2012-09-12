using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpServer.Email
{
    public class Pop3ClientConnection : ClientConnection
    {
        private class MailMessage
        {
            public bool Deleted { get; set; }
            public FileInfo File { get; set; }
        }

        private List<MailMessage> _messages;

        private string _username;

        protected override void OnConnected()
        {
            Write("+OK POP3 server ready");

            Read();
        }

        protected override Response HandleCommand(Command cmd)
        {
            Response response = new Response();

            Console.WriteLine(cmd.Raw);

            switch (cmd.Code)
            {
                case "QUIT":
                    response = Quit();
                    break;
                case "STAT":
                    response = Stat();
                    break;
                case "LIST":
                    response = List(cmd.Arguments.ConvertAll<int?>(i => Convert.ToInt32(i)).FirstOrDefault());
                    break;
                case "RETR":
                    response = Retrieve(cmd.Arguments.ConvertAll<int>(i => Convert.ToInt32(i)).First());
                    break;
                case "DELE":
                    response = Delete(cmd.Arguments.ConvertAll<int>(i => Convert.ToInt32(i)).First());
                    break;
                case "NOOP":
                    response.Code = "+OK";
                    response.Text = "NOOP OK!";
                    break;
                case "RSET":
                    response = Reset();
                    break;
                case "TOP":
                    List<int> args = cmd.Arguments.ConvertAll<int>(i => Convert.ToInt32(i)).ToList();
                    response = Top(args[0], args[1]);
                    break;
                case "UIDL":
                    response = UniqueIdList(cmd.Arguments.ConvertAll<int?>(i => Convert.ToInt32(i)).FirstOrDefault());
                    break;
                case "USER":
                    _username = cmd.Arguments.FirstOrDefault();
                    response.Code = "+OK";
                    break;
                case "PASS":
                    response.Code = "+OK";
                    break;
                case "APOP":
                default:
                    response.Code = "-ERR";
                    response.Text = "I don't know what to do!";
                    break;
            }

            return response;
        }

        private Response Quit()
        {
            foreach (var msg in _messages.Where(m => m.Deleted))
            {
                msg.File.Delete();
            }

            _messages = null;

            return new Response { Code = "+OK", Text = "POP3 server signing off", ShouldQuit = true };
        }

        private Response Reset()
        {
            if (EnsureMessagesPopulated())
            {
                foreach (var msg in _messages.Where(m => m.Deleted))
                {
                    msg.Deleted = false;
                }

                return new Response { Code = "+OK" };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private Response Delete(int msg)
        {
            if (EnsureMessagesPopulated())
            {
                _messages[msg - 1].Deleted = true;

                return new Response { Code = "+OK" };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private Response Retrieve(int msg)
        {
            if (EnsureMessagesPopulated())
            {
                StringBuilder responseText = new StringBuilder();

                responseText.AppendLine();

                using (StreamReader rdr = new StreamReader(_messages[msg - 1].File.FullName))
                {
                    string line = null;

                    while ((line = rdr.ReadLine()) != null)
                    {
                        responseText.AppendLine(line);
                    }
                }

                return new Response { Code = "+OK", Text = responseText.ToString(0, responseText.Length - 2) };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private Response Top(int msg, int lines)
        {
            if (EnsureMessagesPopulated())
            {
                StringBuilder responseText = new StringBuilder();

                responseText.AppendLine();

                using (StreamReader rdr = new StreamReader(_messages[msg - 1].File.FullName))
                {
                    string line = null;
                    int currentLine = 0;
                    bool msgStarted = false;

                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.Length == 0)
                        {
                            msgStarted = true;
                        }

                        if (currentLine > lines)
                            break;

                        responseText.AppendLine(line);

                        if (msgStarted)
                            currentLine++;
                    }
                }

                responseText.Append(".");

                return new Response { Code = "+OK", Text = responseText.ToString() };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private Response List(int? msg)
        {
            if (EnsureMessagesPopulated())
            {
                StringBuilder responseText = new StringBuilder();

                List<MailMessage> messagesToProcess = new List<MailMessage>(_messages);

                if (msg == null)
                {
                    responseText.AppendFormat("{0} messages ({1} octets)", _messages.Count, _messages.Sum(f => f.File.Length));
                    responseText.AppendLine();
                }
                else
                    messagesToProcess = new List<MailMessage> { _messages[msg.Value - 1] };

                for (int i = 0; i < messagesToProcess.Count; i++)
                {
                    responseText.AppendFormat("{0} {1}", i + 1, messagesToProcess[i].File.Length);

                    if (msg == null)
                        responseText.AppendLine();
                }

                if (msg == null)
                    responseText.Append(".");

                return new Response { Code = "+OK", Text = responseText.ToString() };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private Response UniqueIdList(int? msg)
        {
            if (EnsureMessagesPopulated())
            {
                StringBuilder responseText = new StringBuilder();

                List<MailMessage> messagesToProcess = new List<MailMessage>(_messages);

                if (msg == null)
                    responseText.AppendLine();
                else
                    messagesToProcess = new List<MailMessage> { _messages[msg.Value - 1] };

                for (int i = 0; i < messagesToProcess.Count; i++)
                {
                    responseText.AppendFormat("{0} {1}", i + 1, Path.GetFileNameWithoutExtension(messagesToProcess[i].File.Name));

                    if (msg == null)
                        responseText.AppendLine();
                }

                if (msg == null)
                    responseText.Append(".");

                return new Response { Code = "+OK", Text = responseText.ToString() };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private Response Stat()
        {
            if (EnsureMessagesPopulated())
            {
                return new Response { Code = "+OK", Text = "{0} {1}", Data = new List<object>() { _messages.Count, _messages.Sum(f => f.File.Length) } };
            }

            return new Response { Code = "-ERR", Text = "Mailbox not found" };
        }

        private bool EnsureMessagesPopulated()
        {
            if (_messages == null)
            {
                DirectoryInfo mailbox = new DirectoryInfo(Path.Combine(".", "mail", _username));

                if (mailbox.Exists)
                {
                    List<FileInfo> messages = new List<FileInfo>(mailbox.GetFiles());

                    _messages = messages.Select(m => new MailMessage { Deleted = false, File = m }).OrderBy(m => m.File.Name).ToList();

                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}

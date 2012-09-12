using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpServer.Email
{
    public class ImapClientConnection : ClientConnection
    {
        #region Private Classes

        private enum State
        {
            NotAuthenticated,
            Authenticated,
            Selected,
            Logout,
        }

        private class ImapCommand : Command
        {
            public string Tag { get; set; }
        }

        private class ImapResponse : Response
        {
            private List<ImapResponse> _responses = new List<ImapResponse>();

            public string Tag { get; set; }

            public void AddResponseLine(ImapResponse response)
            {
                _responses.Add(response);
            }

            public override string ToString()
            {
                StringBuilder response = new StringBuilder();

                foreach (var r in _responses)
                {
                    response.AppendLine(r.ToString());
                }

                if (string.IsNullOrWhiteSpace(this.Tag))
                    this.Tag = "*";

                if (this.Culture == null)
                {
                    this.Culture = CultureInfo.CurrentCulture;
                }

                if (ResourceManager != null)
                {
                    response.Append(string.Concat(Tag, " ", Code, " ", string.Format(ResourceManager.GetString(Text, Culture), Data.ToArray())));
                }

                if (Text != null)
                    response.Append(string.Concat(Tag, " ", Code, " ", string.Format(Text, Data.ToArray())));
                else
                    response.Append(string.Concat(Tag, " ", Code));

                return response.ToString();
            }
        }
        
        #endregion

        private State _currentState = State.NotAuthenticated;
        private string _currentUser = null;

        protected override void OnConnected()
        {
            Write(new ImapResponse { Code = "OK", Text = "IMAPrev1 server ready" });

            Read();
        }

        protected override Command ParseCommandLine(string line)
        {
            ImapCommand c = new ImapCommand();
            c.Raw = line;

            string[] command = line.Split(' ');

            string tag = command[0];
            string cmd = command[1].ToUpperInvariant();

            c.Arguments = new List<string>(command.Skip(2));
            c.RawArguments = string.Join(" ", c.Arguments);

            c.Tag = tag;
            c.Code = cmd;

            return c;
        }

        protected override Response HandleCommand(Command cmd)
        {
            ImapCommand command = cmd as ImapCommand;

            ImapResponse response = null;

            Console.WriteLine(cmd.Raw);

            switch (command.Code)
            {
                case "CAPABILITY":
                    response = new ImapResponse { Code = "OK", Tag = command.Tag, Text = "CAPABILITY completed" };
                    response.AddResponseLine(new ImapResponse { Code = "CAPABILITY", Text = "IMAP4rev1 AUTH=PLAIN" });
                    break;
                case "LOGIN":
                    _currentUser = cmd.Arguments[0];
                    response = new ImapResponse { Code = "OK", Tag = command.Tag, Text = "LOGIN completed" };
                    break;
                case "NOOP":
                case "LOGOUT":
                default:
                    response = new ImapResponse { Code = "BAD", Tag = command.Tag };
                    break;
            }

            Console.WriteLine(response);

            return response;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Text;
using log4net;

namespace SharpServer
{
    public abstract class ClientConnectionBase
    {
        protected ILog _log = LogManager.GetLogger(typeof(ClientConnectionBase));

        protected TcpClient ControlClient { get; set; }
        protected NetworkStream ControlStream { get; set; }
        protected IPEndPoint RemoteEndPoint { get; set; }
        protected string ClientIP { get; set; }

        protected abstract void Write(string content);
        protected abstract Response HandleCommand(Command cmd);

        protected virtual Response HandleCommand(string cmd)
        {
            return HandleCommand(ParseCommandLine(cmd));
        }

        public abstract void HandleClient(object obj);

        protected virtual void Write(Response response)
        {
            Write(response.ToString());
        }

        protected virtual Command ParseCommandLine(string line)
        {
            Command c = new Command();
            c.Raw = line;

            string[] command = line.Split(' ');

            string cmd = command[0].ToUpperInvariant();

            c.Arguments = new List<string>(command.Skip(1));
            c.RawArguments = string.Join(" ", command.Skip(1));

            c.Code = cmd;

            return c;
        }

        protected virtual void OnConnected()
        {
        }

        protected virtual void OnCommandComplete(Command cmd)
        {
        }

        protected virtual long CopyStream(Stream input, Stream output, int bufferSize, Action<int> performanceCounterAction)
        {
            byte[] buffer = new byte[bufferSize];
            int count = 0;
            long total = 0;

            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, count);
                total += count;
                performanceCounterAction(count);
            }

            return total;
        }

        protected virtual long CopyStream(Stream input, Stream output, int bufferSize, Encoding encoding, Action<int> performanceCounterAction)
        {
            char[] buffer = new char[bufferSize];
            int count = 0;
            long total = 0;

            using (StreamReader rdr = new StreamReader(input))
            {
                using (StreamWriter wtr = new StreamWriter(output, encoding))
                {
                    while ((count = rdr.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        wtr.Write(buffer, 0, count);
                        total += count;
                        performanceCounterAction(count);
                    }
                }
            }

            return total;
        }
    }

    public class Response
    {
        public Response()
        {
            Data = new List<object>();
        }

        public string Code { get; set; }
        public string Text { get; set; }
        public bool ShouldQuit { get; set; }
        public List<object> Data { get; set; }
        public CultureInfo Culture { get; set; }

        public ResourceManager ResourceManager { get; set; }

        public Response SetData(params object[] data)
        {
            Data.Clear();
            Data.AddRange(data);

            return this;
        }

        public Response SetCulture(CultureInfo culture)
        {
            this.Culture = culture;

            return this;
        }

        public override string ToString()
        {
            if (this.Culture == null)
            {
                this.Culture = CultureInfo.CurrentCulture;
            }

            if (ResourceManager != null)
            {
                return string.Concat(Code, " ", string.Format(ResourceManager.GetString(Text, Culture), Data.ToArray()));
            }

            if (Text != null)
                return string.Concat(Code, " ", string.Format(Text, Data.ToArray()));
            else
                return Code;
        }
    }

    public class Command
    {
        public string Code { get; set; }
        public List<string> Arguments { get; set; }
        public string Raw { get; set; }
        public string RawArguments { get; set; }
    }
}

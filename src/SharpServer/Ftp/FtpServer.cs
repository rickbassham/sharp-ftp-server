using System;
using System.Net;
using System.Timers;

namespace SharpServer.Ftp
{
    public class FtpServer : Server<FtpClientConnection>
    {
        private DateTime _startTime;
        private Timer _timer;

        public FtpServer(string logHeader = null)
            : this(IPAddress.Any, 21, logHeader)
        {
        }

        public FtpServer(int port, string logHeader = null)
            : this(IPAddress.Any, port, logHeader)
        {
        }

        public FtpServer(IPAddress ipAddress, int port, string logHeader = null)
            : this(new IPEndPoint[] { new IPEndPoint(ipAddress, port) }, logHeader)
        {
        }

        public FtpServer(IPEndPoint[] localEndPoints, string logHeader = null)
            : base(localEndPoints, logHeader)
        {
            foreach (var endPoint in localEndPoints)
            {
                FtpPerformanceCounters.Initialize(endPoint.Port);
            }
        }

        protected override void OnConnectAttempt()
        {
            FtpPerformanceCounters.IncrementTotalConnectionAttempts();

            base.OnConnectAttempt();
        }

        protected override void OnStart()
        {
            _startTime = DateTime.Now;

            _timer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);

            _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);

            _timer.Start();
        }

        protected override void OnStop()
        {
            if (_timer != null)
                _timer.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            FtpClientConnection.PassiveListeners.ReleaseAll();

            if (_timer != null)
                _timer.Dispose();

            base.Dispose(disposing);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FtpPerformanceCounters.SetFtpServiceUptime(DateTime.Now - _startTime);
        }
    }
}

using System;
using System.Diagnostics;

namespace SharpServer.Ftp
{
    // TODO: Fix to handle multiple instances.
    public static class FtpPerformanceCounters
    {
        private const string CATEGORY = "SharpServerFTP";

        private static object _anonUsersLock = new object();
        private static object _nonAnonUsersLock = new object();
        private static object _currentConnectionsLock = new object();

        private static PerformanceCounter _bytesSentPerSec;
        private static PerformanceCounter _bytesReceivedPerSec;
        private static PerformanceCounter _bytesTotalPerSec;
        private static PerformanceCounter _totalFilesSent;
        private static PerformanceCounter _totalFilesReceived;
        private static PerformanceCounter _totalFilesTransferred;

        private static PerformanceCounter _currentAnonymousUsers;
        private static PerformanceCounter _currentNonAnonymousUsers;
        private static PerformanceCounter _totalAnonymousUsers;
        private static PerformanceCounter _totalNonAnonymousUsers;
        private static PerformanceCounter _maximumAnonymousUsers;
        private static PerformanceCounter _maximumNonAnonymousUsers;

        private static PerformanceCounter _currentConnections;
        private static PerformanceCounter _maximumConnections;
        private static PerformanceCounter _totalConnectionAttempts;
        private static PerformanceCounter _totalLogonAttempts;
        private static PerformanceCounter _ftpServiceUptime;

        private static PerformanceCounter _commandsExecutedPerSec;
        private static PerformanceCounter _commandsExecuted;

        private static long _maxAnonymousUsersCount = 0;
        private static long _maxNonAnonymousUsersCount = 0;
        private static long _maxConnectionsCount = 0;

        public static void Initialize(int port)
        {
            if (PerformanceCounterCategory.Exists(CATEGORY))
            {
                PerformanceCounterCategory.Delete(CATEGORY);
            }

            if (!PerformanceCounterCategory.Exists(CATEGORY))
            {
                var counters = new CounterCreationDataCollection();

                #region Bytes and Files Counters
                var bytesSentPerSec = new CounterCreationData
                {
                    CounterName = "Bytes Sent/sec",
                    CounterHelp = "The rate at which data bytes are being sent by the FTP service.",
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                };
                counters.Add(bytesSentPerSec);

                var bytesReceivedPerSec = new CounterCreationData
                {
                    CounterName = "Bytes Received/sec",
                    CounterHelp = "The rate at which data bytes are being received by the FTP service.",
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                };
                counters.Add(bytesReceivedPerSec);

                var bytesTotalPerSec = new CounterCreationData
                {
                    CounterName = "Bytes Total/sec",
                    CounterHelp = "The sum of Bytes Sent/sec and Bytes Received/sec.",
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                };
                counters.Add(bytesTotalPerSec);

                var totalFilesSent = new CounterCreationData
                {
                    CounterName = "Total Files Sent",
                    CounterHelp = "The total number of files that have been sent by the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalFilesSent);

                var totalFilesReceived = new CounterCreationData
                {
                    CounterName = "Total Files Received",
                    CounterHelp = "The total number of files that have been received by the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalFilesReceived);

                var totalFilesTransferred = new CounterCreationData
                {
                    CounterName = "Total Files Transferred",
                    CounterHelp = "The sum of Total Files Sent and Total Files Received. This is the total number of files transferred by the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalFilesTransferred);
                #endregion

                #region Users Counters
                var currentAnonymousUsers = new CounterCreationData
                {
                    CounterName = "Current Anonymous Users",
                    CounterHelp = "The number of users who currently have an anonymous connection that was made by using the FTP service.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(currentAnonymousUsers);

                var currentNonAnonymousUsers = new CounterCreationData
                {
                    CounterName = "Current NonAnonymous Users",
                    CounterHelp = "The number of users who currently have a nonanonymous connection that was made by using the FTP service.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(currentNonAnonymousUsers);

                var totalAnonymousUsers = new CounterCreationData
                {
                    CounterName = "Total Anonymous Users",
                    CounterHelp = "The number of users who have established an anonymous connection with the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalAnonymousUsers);

                var totalNonAnonymousUsers = new CounterCreationData
                {
                    CounterName = "Total NonAnonymous Users",
                    CounterHelp = "The number of users who have established nonanonymous connections with the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalNonAnonymousUsers);

                var maximumAnonymousUsers = new CounterCreationData
                {
                    CounterName = "Maximum Anonymous Users",
                    CounterHelp = "The maximum number of users who have established concurrent anonymous connections using the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(maximumAnonymousUsers);

                var maximumNonAnonymousUsers = new CounterCreationData
                {
                    CounterName = "Maximum NonAnonymous Users",
                    CounterHelp = "The maximum number of users who have established concurrent nonanonymous connections using the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(maximumNonAnonymousUsers);

                #endregion

                #region Connections, Attempts, and Uptime Counters

                var currentConnections = new CounterCreationData
                {
                    CounterName = "Current Connections",
                    CounterHelp = "The current number of connections that have been established with the FTP service.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(currentConnections);

                var maximumConnections = new CounterCreationData
                {
                    CounterName = "Maximum Connections",
                    CounterHelp = "The maximum number of simultaneous connections that have been established with the FTP service.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(maximumConnections);

                var totalConnectionAttempts = new CounterCreationData
                {
                    CounterName = "Total Connection Attempts",
                    CounterHelp = "The number of connections that have been attempted by using the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalConnectionAttempts);

                var totalLogonAttempts = new CounterCreationData
                {
                    CounterName = "Total Logon Attempts",
                    CounterHelp = "The number of logons that have been attempted by using the FTP service since the service started.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(totalLogonAttempts);

                var ftpServiceUptime = new CounterCreationData
                {
                    CounterName = "FTP Service Uptime",
                    CounterHelp = "The amount of time, in seconds, that the FTP service has been running.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(ftpServiceUptime);

                #endregion

                #region Command Counters

                var commandsExecuted = new CounterCreationData
                {
                    CounterName = "Commands Executed",
                    CounterHelp = "The total number of commands that have been executed with the FTP service.",
                    CounterType = PerformanceCounterType.NumberOfItems32
                };
                counters.Add(commandsExecuted);

                var commandsExecutedPerSec = new CounterCreationData
                {
                    CounterName = "Commands Executed/sec",
                    CounterHelp = "The rate at which commands are being executed with the FTP service.",
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                };
                counters.Add(commandsExecutedPerSec);

                #endregion

                PerformanceCounterCategory.Create(CATEGORY, "Sharp FTP Server", PerformanceCounterCategoryType.MultiInstance, counters);
            }

            string instanceName = string.Concat("Port: ", port);

            _bytesSentPerSec = new PerformanceCounter(CATEGORY, "Bytes Sent/sec", instanceName, false);
            _bytesReceivedPerSec = new PerformanceCounter(CATEGORY, "Bytes Received/sec", instanceName, false);
            _bytesTotalPerSec = new PerformanceCounter(CATEGORY, "Bytes Total/sec", instanceName, false);
            _totalFilesSent = new PerformanceCounter(CATEGORY, "Total Files Sent", instanceName, false);
            _totalFilesReceived = new PerformanceCounter(CATEGORY, "Total Files Received", instanceName, false);
            _totalFilesTransferred = new PerformanceCounter(CATEGORY, "Total Files Transferred", instanceName, false);

            _currentAnonymousUsers = new PerformanceCounter(CATEGORY, "Current Anonymous Users", instanceName, false);
            _currentNonAnonymousUsers = new PerformanceCounter(CATEGORY, "Current NonAnonymous Users", instanceName, false);
            _totalAnonymousUsers = new PerformanceCounter(CATEGORY, "Total Anonymous Users", instanceName, false);
            _totalNonAnonymousUsers = new PerformanceCounter(CATEGORY, "Total NonAnonymous Users", instanceName, false);
            _maximumAnonymousUsers = new PerformanceCounter(CATEGORY, "Maximum Anonymous Users", instanceName, false);
            _maximumNonAnonymousUsers = new PerformanceCounter(CATEGORY, "Maximum NonAnonymous Users", instanceName, false);

            _currentConnections = new PerformanceCounter(CATEGORY, "Current Connections", instanceName, false);
            _maximumConnections = new PerformanceCounter(CATEGORY, "Maximum Connections", instanceName, false);
            _totalConnectionAttempts = new PerformanceCounter(CATEGORY, "Total Connection Attempts", instanceName, false);
            _totalLogonAttempts = new PerformanceCounter(CATEGORY, "Total Logon Attempts", instanceName, false);
            _ftpServiceUptime = new PerformanceCounter(CATEGORY, "FTP Service Uptime", instanceName, false);

            _commandsExecuted = new PerformanceCounter(CATEGORY, "Commands Executed", instanceName, false);
            _commandsExecutedPerSec = new PerformanceCounter(CATEGORY, "Commands Executed/sec", instanceName, false);

            _totalFilesSent.RawValue = 0;
            _totalFilesReceived.RawValue = 0;
            _totalFilesTransferred.RawValue = 0;

            _currentAnonymousUsers.RawValue = 0;
            _currentNonAnonymousUsers.RawValue = 0;
            _totalAnonymousUsers.RawValue = 0;
            _totalNonAnonymousUsers.RawValue = 0;
            _maximumAnonymousUsers.RawValue = 0;
            _maximumNonAnonymousUsers.RawValue = 0;

            _currentConnections.RawValue = 0;
            _maximumConnections.RawValue = 0;
            _totalConnectionAttempts.RawValue = 0;
            _totalLogonAttempts.RawValue = 0;
            _ftpServiceUptime.RawValue = 0;

            _commandsExecutedPerSec.RawValue = 0;
            _commandsExecuted.RawValue = 0;
        }

        public static void IncrementBytesSent(int count)
        {
            _bytesSentPerSec.IncrementBy(count);
            _bytesTotalPerSec.IncrementBy(count);
        }

        public static void IncrementBytesReceived(int count)
        {
            _bytesReceivedPerSec.IncrementBy(count);
            _bytesTotalPerSec.IncrementBy(count);
        }

        public static void IncrementFilesSent()
        {
            _totalFilesSent.Increment();
            _totalFilesTransferred.Increment();
        }

        public static void IncrementFilesReceived()
        {
            _totalFilesReceived.Increment();
            _totalFilesTransferred.Increment();
        }

        public static void IncrementAnonymousUsers()
        {
            lock (_anonUsersLock)
            {
                long currentAnonUsers = _currentAnonymousUsers.Increment();
                _totalAnonymousUsers.Increment();

                if (currentAnonUsers > _maxAnonymousUsersCount)
                {
                    _maxAnonymousUsersCount = currentAnonUsers;
                    _maximumAnonymousUsers.RawValue = currentAnonUsers;
                }
            }
        }

        public static void IncrementNonAnonymousUsers()
        {
            lock (_nonAnonUsersLock)
            {
                long currentNonAnonUsers = _currentNonAnonymousUsers.Increment();
                _totalNonAnonymousUsers.Increment();

                if (currentNonAnonUsers > _maxNonAnonymousUsersCount)
                {
                    _maxNonAnonymousUsersCount = currentNonAnonUsers;
                    _maximumNonAnonymousUsers.RawValue = currentNonAnonUsers;
                }
            }
        }

        public static void DecrementAnonymousUsers()
        {
            lock (_anonUsersLock)
            {
//                if (_currentAnonymousUsers.RawValue > 0)
                    _currentAnonymousUsers.Decrement();
            }
        }

        public static void DecrementNonAnonymousUsers()
        {
            lock (_nonAnonUsersLock)
            {
//                if (_currentNonAnonymousUsers.RawValue > 0)
                    _currentNonAnonymousUsers.Decrement();
            }
        }

        public static void IncrementCurrentConnections()
        {
            lock (_currentConnectionsLock)
            {
                long currentConnections = _currentConnections.Increment();

                if (currentConnections > _maxConnectionsCount)
                {
                    _maxConnectionsCount = currentConnections;
                    _maximumConnections.RawValue = currentConnections;
                }
            }
        }

        public static void DecrementCurrentConnections()
        {
            lock (_currentConnectionsLock)
            {
//                if (_currentConnections.RawValue > 0)
                    _currentConnections.Decrement();
            }
        }

        public static void IncrementTotalConnectionAttempts()
        {
            _totalConnectionAttempts.Increment();
        }

        public static void IncrementTotalLogonAttempts()
        {
            _totalLogonAttempts.Increment();
        }

        public static void SetFtpServiceUptime(TimeSpan value)
        {
            _ftpServiceUptime.RawValue = (long)value.TotalSeconds;
        }

        public static void IncrementCommandsExecuted()
        {
            _commandsExecuted.Increment();
            _commandsExecutedPerSec.Increment();
        }
    }
}

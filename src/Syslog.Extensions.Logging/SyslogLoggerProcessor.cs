using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Syslog.Extensions.Logging
{
    public class SyslogLoggerProcessor : IDisposable
    {
        private const int MaxQueuedMessages = 1024;

        private readonly BlockingCollection<string> _messageQueue = new BlockingCollection<string>(MaxQueuedMessages);
        private readonly Task _outputTask;
        private readonly UdpClient _udp;

        public SyslogLoggerProcessor(string serverHost, int serverPort)
        {
            if (serverHost == null)
                throw new ArgumentNullException(nameof(serverHost));

            _udp = new UdpClient(serverHost, serverPort);

            // Start Syslog message queue processor
            _outputTask = Task.Factory.StartNew(
                ProcessLogQueue,
                this,
                TaskCreationOptions.LongRunning);
        }

        public virtual void EnqueueMessage(string message)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }

            // Adding is completed so just log the message
            WriteMessage(message);
        }

        // for testing
        internal virtual void WriteMessage(string message)
        {
            var raw = Encoding.ASCII.GetBytes(message);

            _udp.Send(raw, Math.Min(raw.Length, 1024));
        }

        private void ProcessLogQueue()
        {
            foreach (var message in _messageQueue.GetConsumingEnumerable())
            {
                WriteMessage(message);
            }
        }

        private static void ProcessLogQueue(object state)
        {
            var syslogLoggerProcessor = (SyslogLoggerProcessor)state;

            syslogLoggerProcessor.ProcessLogQueue();
        }

        public void Dispose()
        {
            _messageQueue.CompleteAdding();

            try
            {
                _udp?.Dispose();
                _outputTask.Wait(1500); // with timeout in-case UDP is locked
            }
            catch (TaskCanceledException) { }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }
        }
    }
}
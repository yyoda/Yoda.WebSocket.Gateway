using System;
using System.Threading;

namespace Yoda.WebSocket.Gateway.Core
{
    public class GatewayMetrics
    {
        public GatewayMetrics()
        {
            Threads = new ThreadMetrics();
            WebSocketConnectionCount = GatewayConnection.Instance.Count;
        }

        private static readonly string Started = DateTimeOffset.Now.ToString("yyyy/MM/dd hh:mm:ss");
        private static readonly int Processor = Environment.ProcessorCount;

        public string StartedAt => Started;
        public int ProcessorCount => Processor;
        public ThreadMetrics Threads { get; }
        public int WebSocketConnectionCount { get; }
        public int MemoryCacheCount { get; set; }
        public GatewayOptions Options { get; set; }

        public class ThreadMetrics
        {
            public ThreadMetrics()
            {
                ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
                AvailableWorkerThreads = availableWorkerThreads;
                AvailableCompletionPortThreads = availableCompletionPortThreads;
                MaxWorkerThreads = maxWorkerThreads;
                MaxCompletionPortThreads = maxCompletionPortThreads;
            }

            public int AvailableWorkerThreads { get; }
            public int AvailableCompletionPortThreads { get; }
            public int MaxWorkerThreads { get; }
            public int MaxCompletionPortThreads { get; }
        }
    }
}
using System;
using System.Threading;

namespace Yoda.WebSocket.Gateway.Core
{
    public class GatewayStatus
    {
        public GatewayStatus()
        {
            ThreadsPool = new ThreadPoolCounter();
            WebSocketConnections = WebSocketReference.Instance.Count;
        }

        private static readonly string Started = DateTimeOffset.Now.ToString("yyyy/MM/dd hh:mm:ss");
        private static readonly int Processor = Environment.ProcessorCount;

        public string StartedAt => Started;
        public int Processors => Processor;
        public ThreadPoolCounter ThreadsPool { get; }
        public int WebSocketConnections { get; }
        public GatewayOptions Options { get; set; }

        public class ThreadPoolCounter
        {
            public ThreadPoolCounter()
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
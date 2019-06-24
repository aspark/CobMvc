using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{
    /// <summary>
    /// 管理Websocket，接收或发送消息
    /// </summary>
    /// <typeparam name="TRec"></typeparam>
    /// <typeparam name="TSend"></typeparam>
    internal abstract class WebSocketWrapper<TRec, TSend> : IDisposable where TRec : JsonRpcBase where TSend: JsonRpcBase
    {
        private ILogger<WebSocketWrapper<TRec, TSend>> _logger = null;
        private CancellationTokenSource _cts = null;

        public WebSocketWrapper(ILoggerFactory loggerFactory)
        {
            _cts = new CancellationTokenSource();
            _logger = loggerFactory.CreateLogger<WebSocketWrapper<TRec, TSend>>();
        }

        private int _hasStart = 0;
        private Task _waitHandle = null;
        public virtual void Start()
        {
            if (Interlocked.CompareExchange(ref _hasStart, 1, 0) == 1)
                return;

            var socket = HandleSocket();

            var messages = Task.Factory.StartNew(HandleMessages, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var send = Task.Factory.StartNew(SendResponse, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _waitHandle = socket.ContinueWith(t=> {
                Dispose();
            });
        }

        protected abstract Task<WebSocket> GetWebSocket();

        public void Wait()
        {
            _waitHandle.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected CancellationToken Cancellation { get => _cts.Token; }

        private WebSocket _websocket = null;
        private async Task HandleSocket()
        {
            _websocket = GetWebSocket().ConfigureAwait(false).GetAwaiter().GetResult();

            //change to Rx?
            var pipe = new Pipe();
            var write = FillPipe(_websocket, pipe.Writer);
            var read = ReadPipe(_websocket, pipe.Reader);

            await Task.WhenAll(write, read);
        }

        private const int _minBufferSize = 512;

        private async Task FillPipe(WebSocket socket, PipeWriter writer)
        {
            while(socket.State < WebSocketState.Closed)
            {
                var memory = writer.GetMemory(_minBufferSize + 2);

                try
                {
                    var buffer = new byte[_minBufferSize];
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.Count > 0)
                    {
                        buffer.CopyTo(memory);
                    }

                    var count = result.Count;

                    if(result.EndOfMessage)
                    {
                        var split = memory.Slice(count);
                        split.Span[0] = 30;
                        split.Span[1] = 31;
                        count += 2;
                    }

                    writer.Advance(count);
                }
                catch (WebSocketException ex)
                {
                    _logger.LogError(ex, "consume websocket failed");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "consume websocket failed");
                }

                if ((await writer.FlushAsync()).IsCompleted)
                    break;
            }

            writer.Complete();
        }

        ////cob[00][ver]\n[4-length]\n[data]\n
        ////cob_00_01\n00_00_00_0F\n[8bytes]
        ////ver 1:jsonp
        private async Task ReadPipe(WebSocket socket, PipeReader reader)
        {
            while (socket.State < WebSocketState.Closed)
            {
                ReadResult result = await reader.ReadAsync();
                try
                {

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SequencePosition? position = null;

                    do
                    {
                        position = LastPositionOf(buffer, (byte)30);

                        if (position != null)
                        {
                            var spliter = buffer.Slice(position.Value);
                            if (spliter.Length >= 2 && spliter.First.Span[1] == 31)
                            {
                                var block = buffer.Slice(0, position.Value);

                                ReceiveMessage(block);

                                buffer = buffer.Slice(buffer.GetPosition(2, position.Value));
                            }
                        }
                        else
                            break;
                    }
                    while (buffer.Length > 0);

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
                catch (Exception ex)
                {

                }

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private SequencePosition? LastPositionOf(ReadOnlySequence<byte> buffer, byte find)
        {
            ////var index = buffer.Length;
            //foreach(var item in buffer)
            //{
            //    for (var i = item.Span.Length - 1; i >= 0; i--)
            //    {
            //        if (item.Span[i] == find)
            //            return new SequencePosition(item, i);
            //    }
            //}

            //return null;

            return buffer.PositionOf(find);
        }

        private BlockingCollection<TRec> _messages = new BlockingCollection<TRec>();
        private void ReceiveMessage(ReadOnlySequence<byte> buffer)
        {
            var body = Encoding.UTF8.GetString(buffer.ToArray());

            Console.WriteLine(body);

            try
            {
                var rpc = JsonConvert.DeserializeObject<TRec>(body);
                _messages.Add(rpc);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"parse to '{typeof(TRec).Name}' failed");
            }

        }

        private async Task HandleMessages()
        {
            while (!_messages.IsCompleted && !_cts.IsCancellationRequested)
            {
                try
                {
                    if (_messages.TryTake(out TRec request))
                    {
                        await OnReceiveMessage(request);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "HandleMessages");
                }
            }
        }

        protected abstract Task OnReceiveMessage(TRec msg);

        public event EventHandler OnDispose;

        public bool IsDisposing { get; private set; } = false;
        public virtual void Dispose()
        {
            if (IsDisposing)
                return;

            lock (this)
            {
                if (IsDisposing)
                    return;

                IsDisposing = true;
            }

            _logger.LogDebug("websocket disposed");

            OnDispose?.Invoke(this, EventArgs.Empty);

            _cts.Cancel();
            _messages.Dispose();
            _sendList.Dispose();
            _websocket?.Dispose();
        }

        BlockingCollection<(TaskCompletionSource<bool> Source, TSend Content)> _sendList = new BlockingCollection<(TaskCompletionSource<bool>, TSend)>();

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public virtual Task<bool> Send(TSend response)
        {
            if (response == null || IsDisposing)
                return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            _sendList.Add((tcs, response));

            return tcs.Task;
        }

        public virtual void SendAndForget(TSend response)
        {
            Send(response).ConfigureAwait(false);
        }

        private void SendResponse()
        {
            while (!_sendList.IsCompleted && !_cts.IsCancellationRequested)
            {
                try
                {
                    if (_sendList.TryTake(out (TaskCompletionSource<bool> Source, TSend Content) pair))
                    {
                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pair.Content));
                        for (var i = 0; i < bytes.Length; i += _minBufferSize)
                        {
                            var length = Math.Min(bytes.Length - i, _minBufferSize);

                            _websocket.SendAsync(new ArraySegment<byte>(bytes, i, length), WebSocketMessageType.Text, length < _minBufferSize, _cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                        }

                        pair.Source.TrySetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SendResponse");
                }
            }
        }

        public override string ToString()
        {
            return $"sending:{_sendList.Count} receiving:{_messages.Count}";
        }
    }
}

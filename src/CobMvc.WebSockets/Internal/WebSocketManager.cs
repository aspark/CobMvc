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
        private WebSocket _websocket = null;
        private Task _waitHandle = null;
        public virtual void Start()
        {
            if (Interlocked.CompareExchange(ref _hasStart, 1, 0) == 1)
                return;

            try
            {
                _websocket = GetWebSocket().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "get websocket exception");
                throw ex;
            }

            var socket = HandleSocket();//_websocket

            var messages = Task.Factory.StartNew(HandleMessages, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var send = Task.Factory.StartNew(SendImpl, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _waitHandle = socket.ContinueWith(t=> {
                if(t.Exception!=null)
                {
                    //这里有异常，如果在Dispose中直观表现：socketException
                    _logger.LogError(t.Exception, "socket exception");
                }

                Dispose();
            });
        }

        protected abstract Task<WebSocket> GetWebSocket();

        public void Wait()
        {
            _waitHandle.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected CancellationToken Cancellation { get => _cts.Token; }

        private async Task HandleSocket()
        {
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
                    TRec request = _messages.Take(Cancellation);
                    await OnReceiveMessage(request);
                }
                catch (InvalidOperationException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
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

        private BlockingCollection<(TaskCompletionSource<bool> Source, TSend Content)> _sendList = new BlockingCollection<(TaskCompletionSource<bool>, TSend)>();

        /// <summary>
        /// 发送队列长度
        /// </summary>
        public int SendingCount { get => _sendList.Count; }

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

        private void SendImpl()
        {
            while (!_sendList.IsCompleted && !_cts.IsCancellationRequested)
            {
                var isSuccess = false;
                (TaskCompletionSource<bool> Source, TSend Content) pair = (null, null);
                try
                {
                    if(_websocket != null)
                    {
                        pair = _sendList.Take(Cancellation);
                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pair.Content));
                        for (var i = 0; i < bytes.Length; i += _minBufferSize)
                        {
                            var length = Math.Min(bytes.Length - i, _minBufferSize);

                            _websocket.SendAsync(new ArraySegment<byte>(bytes, i, length), WebSocketMessageType.Text, length < _minBufferSize, _cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                        }

                        isSuccess = true;
                    }
                }
                catch (InvalidOperationException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Send response failed");
                }
                finally
                {
                    pair.Source?.TrySetResult(isSuccess);
                }
            }
        }

        public virtual string GetDebugInfo()
        {
            return $"sending:{_sendList.Count} receiving:{_messages.Count}";
        }
    }
}

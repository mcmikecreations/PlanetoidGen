using Google.Protobuf;
using Grpc.Core;
using PlanetoidGen.Client.Contracts.Services.Controllers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Controllers
{
    public abstract class StreamControllerBase<TRequest, TResponse, TArgs> : IStreamControllerBase<TArgs>
        where TRequest : IMessage
        where TResponse : IMessage
        where TArgs : EventArgs
    {
        private event EventHandler<TArgs> _responseRetrieved;

        protected CancellationTokenSource _cancellationTokenSource;
        protected AsyncDuplexStreamingCall<TRequest, TResponse> _stream;
        protected Task _readResponseStreamTask;

        public StreamControllerBase()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public virtual async Task StartStream(CancellationToken token)
        {
            await StopStreamIfExists();

            _stream = OpenDuplexStream();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _readResponseStreamTask = RunResponseStreamReadingTask(_cancellationTokenSource.Token);
        }

        public virtual async Task StopStreamIfExists()
        {
            if (_stream != null)
            {
                try
                {
                    await _stream.RequestStream.CompleteAsync();
                }
                catch (RpcException ex)
                {
                }

                _cancellationTokenSource?.Cancel();
            }
        }

        public virtual async Task SendStreamRequest(TRequest model)
        {
            try
            {
                if (_stream == null)
                {
                    throw new InvalidOperationException("Stream was not initialized.");
                }

                await _stream.RequestStream.WriteAsync(model);
            }
            catch (RpcException ex)
            {
                throw new InvalidOperationException("Stream was stopped.");
            }
        }

        public virtual void Subscribe(EventHandler<TArgs> action)
        {
            _responseRetrieved += action;
        }

        public virtual void Unsubscribe(EventHandler<TArgs> action)
        {
            _responseRetrieved -= action;
        }

        protected virtual Task RunResponseStreamReadingTask(Func<TResponse, TArgs> argsBuilder, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (await _stream.ResponseStream.MoveNext(token))
                    {
                        if (_stream.ResponseStream.Current != null)
                        {
                            var args = argsBuilder(_stream.ResponseStream.Current);

                            _responseRetrieved?.Invoke(this, args);
                        }
                    }
                }
                catch (RpcException ex)
                {
                }

                _stream = null;
            });
        }

        protected abstract Task RunResponseStreamReadingTask(CancellationToken token);

        protected abstract AsyncDuplexStreamingCall<TRequest, TResponse> OpenDuplexStream();
    }
}

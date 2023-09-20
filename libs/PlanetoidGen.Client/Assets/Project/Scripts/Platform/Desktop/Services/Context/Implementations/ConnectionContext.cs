using Grpc.Core;
using PlanetoidGen.Client.Platform.Desktop.Services.Context.Abstractions;
using System;

namespace PlanetoidGen.Client.Platform.Desktop.Services.Context.Implementations
{
    public class ConnectionContext : IConnectionContext
    {
        private Channel _grpcChannel;

        public Channel Channel
        {
            get
            {
                if (_grpcChannel != null && _grpcChannel.State != ChannelState.Shutdown && _grpcChannel.State != ChannelState.TransientFailure)
                {
                    return _grpcChannel;
                }

                try
                {
                    if (_grpcChannel != null)
                    {
                        _grpcChannel.ShutdownAsync().Wait();
                    }

                    _grpcChannel = new Channel("localhost:5000", ChannelCredentials.Insecure, new []
                    {
                        new ChannelOption("grpc.max_receive_message_length", -1)
                    });
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("gRPC channel cannnot be created.");
                }

                return _grpcChannel;
            }
        }
    }
}

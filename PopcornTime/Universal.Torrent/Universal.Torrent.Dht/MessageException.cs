#if !DISABLE_DHT
using System;

namespace Universal.Torrent.Dht
{
    internal class MessageException : Exception
    {
        public MessageException(ErrorCode errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public ErrorCode ErrorCode { get; }
    }
}

#endif
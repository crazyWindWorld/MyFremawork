using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TestTcpServer   // 改成你自己的项目命名空间
{
    public static class Protocol
    {
        public static async Task<Frame> ReadFrameAsync(NetworkStream stream, int maxFrameSize)
        {
            byte[] lenBuf = new byte[4];
            int n = await ReadExactlyAsync(stream, lenBuf, 0, 4);
            if (n == 0) return null;
            if (n < 4) throw new IOException("Unexpected EOF when reading length.");

            int length = ReadInt32BE(lenBuf, 0);
            if (length < 4 || length > maxFrameSize)
                throw new InvalidDataException("Invalid frame length: " + length);

            byte[] payload = new byte[length];
            n = await ReadExactlyAsync(stream, payload, 0, length);
            if (n < length) throw new IOException("Unexpected EOF when reading payload.");

            int msgId = ReadInt32BE(payload, 0);
            int bodyLen = length - 4;
            byte[] body = new byte[bodyLen];
            if (bodyLen > 0) Buffer.BlockCopy(payload, 4, body, 0, bodyLen);

            return new Frame(msgId, body);
        }

        public static async Task WriteFrameAsync(NetworkStream stream, Frame frame)
        {
            int length = 4 + frame.Body.Length;
            byte[] header = new byte[8];
            WriteInt32BE(header, 0, length);
            WriteInt32BE(header, 4, frame.MessageId);

            await stream.WriteAsync(header, 0, 8);
            if (frame.Body.Length > 0)
                await stream.WriteAsync(frame.Body, 0, frame.Body.Length);

            await stream.FlushAsync();
        }

        private static async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int n = await stream.ReadAsync(buffer, offset + total, count - total);
                if (n == 0) return total == 0 ? 0 : total;
                total += n;
            }
            return total;
        }

        private static int ReadInt32BE(byte[] b, int o)
            => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];

        private static void WriteInt32BE(byte[] b, int o, int v)
        {
            b[o] = (byte)((v >> 24) & 0xFF);
            b[o + 1] = (byte)((v >> 16) & 0xFF);
            b[o + 2] = (byte)((v >> 8) & 0xFF);
            b[o + 3] = (byte)(v & 0xFF);
        }
    }
}

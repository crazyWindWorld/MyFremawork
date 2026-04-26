using Google.Protobuf;
using LoginPB;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestTcpServer
{
    public class RelayServer
    {
        private readonly int _listenPort;
        private readonly string _targetHost;
        private readonly int _targetPort;
        private readonly int _maxFrameSize;

        public RelayServer(int listenPort, string targetHost, int targetPort, int maxFrameSize = 10 * 1024 * 1024)
        {
            _listenPort = listenPort;
            _targetHost = targetHost;
            _targetPort = targetPort;
            _maxFrameSize = maxFrameSize;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var listener = new TcpListener(IPAddress.Any, _listenPort);
            listener.Start();
            Console.WriteLine("[Relay] Listen 0.0.0.0:{0}, upstream => {1}:{2}", _listenPort, _targetHost, _targetPort);

            using (ct.Register(() =>
            {
                try { listener.Stop(); } catch { }
            }))
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client = null;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync(); // 4.7.2 无 ct 重载
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        if (ct.IsCancellationRequested) break;
                        throw;
                    }

                    Task.Run(() => HandleSessionAsync(client, ct));
                }
            }

            Console.WriteLine("[Relay] stopped.");
        }

        private async Task HandleSessionAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            {
                Console.WriteLine("[Session] client connected. (local-only)");

                using (var cStream = client.GetStream())
                {
                    while (!ct.IsCancellationRequested)
                    {
                        Frame frame;
                        try
                        {
                            frame = await ReadFrameAsync(cStream, _maxFrameSize);
                            if (frame == null)
                            {
                                Console.WriteLine("[Local] EOF");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[Local] read error: " + ex.Message);
                            break;
                        }

                        Console.WriteLine("[Local] recv msgId={0}, bodyLen={1}", frame.MessageId, frame.Body.Length);

                        if (frame.MessageId == ProtoCmds.LoginReq)
                        {
                            LoginRsp rsp;
                            try
                            {
                                var req = LoginReq.Parser.ParseFrom(frame.Body);

                                rsp = new LoginRsp
                                {
                                    Result = LoginResult.LoginSuccess
                                };
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[Local][Login] parse failed: " + ex.Message);
                                rsp = new LoginRsp
                                {
                                    Result = LoginResult.LoginFailed
                                };
                            }

                            await WriteFrameAsync(cStream, new Frame(ProtoCmds.LoginRsp, rsp.ToByteArray()));
                            Console.WriteLine("[Local][Login] LoginRsp sent");
                        }
                        else
                        {
                            // 纯本地模式：非登录消息不转发
                            Console.WriteLine("[Local] drop msgId={0} (upstream disabled)", frame.MessageId);
                        }
                    }
                }
            }

            Console.WriteLine("[Session] closed.");
        }


        /// <summary>
        /// 客户端->上游：拦截 messageId=1001，直接回 2001 给客户端；其他透传到上游。
        /// </summary>
        private async Task RelayClientToUpstreamWithLocalReplyAsync(NetworkStream clientStream, NetworkStream upstreamStream, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Frame frame;
                try
                {
                    frame = await ReadFrameAsync(clientStream, _maxFrameSize);
                    if (frame == null)
                    {
                        Console.WriteLine("[C->U] EOF");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[C->U] read error: " + ex.Message);
                    break;
                }

                Console.WriteLine("[C->U] recv msgId={0}, bodyLen={1}", frame.MessageId, frame.Body.Length);

                // ===== 你的“收到一个消息，处理后回另一个消息”就在这里 =====
                if (frame.MessageId == ProtoCmds.LoginReq)
                {
                    LoginReq req;
                    try
                    {
                        req = LoginReq.Parser.ParseFrom(frame.Body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Relay][Login] parse LoginReq failed: " + ex.Message);

                        // 解析失败也回一个 LoginRsp（按你proto字段自行赋值）
                        var badRsp = new LoginRsp();
                        badRsp.Result = LoginResult.LoginFailed;
                        // 例如如果你有 Code/Message 字段：
                        // badRsp.Code = -1;
                        // badRsp.Message = "bad request";

                        await Protocol.WriteFrameAsync(clientStream, new Frame(ProtoCmds.LoginRsp, badRsp.ToByteArray()));
                        continue;
                    }

                    // ===== 你的登录业务处理 =====
                    var rsp = new LoginRsp();
                    rsp.Result = LoginResult.LoginSuccess;
                    // 下面字段名请按你的 proto 改（我这里只写示例注释，避免字段名不一致编译失败）
                    // if (req.Username == "admin" && req.Password == "123456")
                    // {
                    //     rsp.Code = 0;
                    //     rsp.Message = "ok";
                    //     rsp.Token = Guid.NewGuid().ToString("N");
                    // }
                    // else
                    // {
                    //     rsp.Code = 10001;
                    //     rsp.Message = "invalid username or password";
                    // }

                    await Protocol.WriteFrameAsync(clientStream, new Frame(ProtoCmds.LoginRsp, rsp.ToByteArray()));
                    Console.WriteLine("[Relay][Login] LoginRsp sent");
                    continue; // 登录请求本地处理，不转发上游
                }

                try
                {
                    await WriteFrameAsync(upstreamStream, frame);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[C->U] write upstream error: " + ex.Message);
                    break;
                }
            }
        }

        /// <summary>
        /// 纯透传循环（例如上游->客户端）
        /// </summary>
        private async Task RelayRawAsync(NetworkStream src, NetworkStream dst, string tag, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Frame frame;
                try
                {
                    frame = await ReadFrameAsync(src, _maxFrameSize);
                    if (frame == null)
                    {
                        Console.WriteLine("[{0}] EOF", tag);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[{0}] read error: {1}", tag, ex.Message);
                    break;
                }

                try
                {
                    await WriteFrameAsync(dst, frame);
                    Console.WriteLine("[{0}] msgId={1}, bodyLen={2}", tag, frame.MessageId, frame.Body.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[{0}] write error: {1}", tag, ex.Message);
                    break;
                }
            }
        }

        private static async Task<Frame> ReadFrameAsync(NetworkStream stream, int maxFrameSize)
        {
            byte[] lenBuf = new byte[4];
            int n = await ReadExactlyAsync(stream, lenBuf, 0, 4);
            if (n == 0) return null;     // 正常断开
            if (n < 4) throw new IOException("EOF when reading length.");

            int length = ReadInt32BE(lenBuf, 0); // = 4 + bodyLen
            if (length < 4 || length > maxFrameSize)
                throw new InvalidDataException("Invalid frame length: " + length);

            byte[] payload = new byte[length];
            n = await ReadExactlyAsync(stream, payload, 0, length);
            if (n < length) throw new IOException("EOF when reading payload.");

            int msgId = ReadInt32BE(payload, 0);
            int bodyLen = length - 4;
            byte[] body = new byte[bodyLen];
            if (bodyLen > 0)
                Buffer.BlockCopy(payload, 4, body, 0, bodyLen);

            return new Frame(msgId, body);
        }

        private static async Task WriteFrameAsync(NetworkStream stream, Frame frame)
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

        private static int ReadInt32BE(byte[] buf, int offset)
        {
            return (buf[offset] << 24) |
                   (buf[offset + 1] << 16) |
                   (buf[offset + 2] << 8) |
                   buf[offset + 3];
        }

        private static void WriteInt32BE(byte[] buf, int offset, int value)
        {
            buf[offset] = (byte)((value >> 24) & 0xFF);
            buf[offset + 1] = (byte)((value >> 16) & 0xFF);
            buf[offset + 2] = (byte)((value >> 8) & 0xFF);
            buf[offset + 3] = (byte)(value & 0xFF);
        }


    }

}

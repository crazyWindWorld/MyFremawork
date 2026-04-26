using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTcpServer
{
    public sealed class Frame
    {
        public int MessageId { get; private set; }
        public byte[] Body { get; private set; }

        public Frame(int messageId, byte[] body)
        {
            MessageId = messageId;
            Body = body ?? new byte[0];
        }
    }
}

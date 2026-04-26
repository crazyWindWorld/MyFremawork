using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace NetFramework
{
    public class MessageHandler
    {
        private static Dictionary<ushort, IMessage> _msgTypes = new Dictionary<ushort, IMessage>();
        private static Dictionary<IMessage, ushort> _msgIds = new Dictionary<IMessage, ushort>();

        public static void Register<T>(ushort msgId) where T : IMessage<T>, new()
        {
            _msgTypes[msgId] = new T();
            _msgIds[new T()] = msgId;
        }

        public byte[] Pack(ushort msgId, IMessage msg)
        {
            byte[] payload = msg.ToByteArray();
            int length = payload.Length + 8; // 4 length + 4 msgId + payload

            byte[] data = new byte[length];
            BitConverter.GetBytes(length).CopyTo(data, 0);
            BitConverter.GetBytes((uint)msgId).CopyTo(data, 4);
            payload.CopyTo(data, 8);
            return data;
        }

        public IMessage Unpack(ushort msgId, byte[] payload)
        {
            if (!_msgTypes.TryGetValue(msgId, out IMessage msg))
                return null;
            msg.MergeFrom(payload);
            return msg;
        }

        public static ushort GetMsgId(IMessage msg)
        {
            return _msgIds.TryGetValue(msg, out ushort id) ? id : (ushort)0;
        }
    }
}

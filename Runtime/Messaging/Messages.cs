using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace VaporNetcode
{
    public struct EmptyMessage : INetMessage
    {
        public EmptyMessage(NetworkReader r)
        {

        }

        public void Serialize(NetworkWriter w)
        {

        }
    }

    public struct CommandMessage : INetMessage, ICommandPacket
    {
        public byte Command { get; set; }
        public ArraySegment<byte> data; // Recieves The Data

        public CommandMessage(NetworkReader r)
        {
            Command = r.ReadByte();
            data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteByte(Command);
            w.WriteBytesAndSizeSegment(data);
        }

        public T GetPacket<T>() where T : struct, INetMessage
        {
            using var r = NetworkReaderPool.Get(data);
            return PacketHelper.Deserialize<T>(r);
        }
    }

    public struct PacketMessage : INetMessage
    {
        public ArraySegment<byte> data; // Recieves The Data

        public PacketMessage(NetworkReader r)
        {
            data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteBytesAndSizeSegment(data);
        }

        public T GetPacket<T>() where T : struct, INetMessage
        {
            using var r = NetworkReaderPool.Get(data);
            return PacketHelper.Deserialize<T>(r);
        }
    }

    public struct SyncDataMessage : INetMessage
    {
        public ArraySegment<byte> data;

        public SyncDataMessage(NetworkReader r)
        {
            data = r.ReadBytesAndSizeSegment();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteBytesAndSizeSegment(data);
        }

        public T GetPacket<T>() where T : struct, INetMessage
        {
            using var r = NetworkReaderPool.Get(data);
            return PacketHelper.Deserialize<T>(r);
        }
    }

    public struct ResponseTimeoutPacket : INetMessage, IResponsePacket
    {
        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public ResponseTimeoutPacket(NetworkReader r)
        {
            ResponseID = r.ReadUShort();
            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteUShort(ResponseID);
            w.WriteByte((byte)Status);
        }
    }

    // A client sends this message to the server
    // to calculate RTT and synchronize time
    public struct NetworkPingMessage : INetMessage
    {
        public double clientTime;

        public NetworkPingMessage(NetworkReader r)
        {
            clientTime = r.ReadDouble();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteDouble(clientTime);
        }
    }

    // The server responds with this message
    // The client can use this to calculate RTT and sync time
    public struct NetworkPongMessage : INetMessage
    {
        public double clientTime;

        public NetworkPongMessage(NetworkReader r)
        {
            clientTime = r.ReadDouble();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteDouble(clientTime);
        }
    }

    public struct StringMessage : INetMessage
    {
        public string message;

        public StringMessage(NetworkReader r)
        {
            message = r.ReadString();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(message);
        }
    }
}

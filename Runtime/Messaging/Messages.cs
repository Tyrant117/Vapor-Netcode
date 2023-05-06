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

    public struct CommandMessage : INetMessage
    {
        public int Command;
        public byte[] Packet; // Recieves The Data

        public CommandMessage(NetworkReader r)
        {
            Command = r.ReadInt();
            Packet = r.ReadBytesAndSize();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteInt(Command);
            w.WriteBytesAndSize(Packet);
        }

        public T GetPacket<T>() where T : struct, ISerializablePacket
        {
            using var r = NetworkReaderPool.Get(Packet);
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

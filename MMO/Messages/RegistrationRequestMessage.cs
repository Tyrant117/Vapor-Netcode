using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public struct RegistrationRequestMessage : INetMessage, IResponsePacket
    {
        public string AccountName;
        public ulong SteamID;
        public string EpicUserID;
        public string Email;
        public string Password;

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }

        public RegistrationRequestMessage(NetworkReader r)
        {
            AccountName = r.ReadString();
            SteamID = Compression.DecompressVarUInt(r);
            EpicUserID = r.ReadString();
            Email = r.ReadString();
            Password = r.ReadString();

            ResponseID = r.ReadUShort();
            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(AccountName);
            Compression.CompressVarUInt(w, SteamID);
            w.WriteString(EpicUserID);
            w.WriteString(Email);
            w.WriteString(Password);

            w.WriteUShort(ResponseID);
            w.WriteByte((byte)Status);
        }
    }

    public struct RegistrationResponseMessage : INetMessage, IResponsePacket
    {

        public ushort ResponseID { get; set; }
        public ResponseStatus Status { get; set; }


        public RegistrationResponseMessage(NetworkReader r)
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
}

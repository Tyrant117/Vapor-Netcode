using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporNetcode;

namespace VaporMMO
{
    public struct RegistrationRequestMessage : INetMessage
    {
        public string AccountName;
        public ulong SteamID;
        public string EpicUserID;
        public string Email;
        public byte[] Password;

        public RegistrationRequestMessage(NetworkReader r)
        {
            AccountName = r.ReadString();
            SteamID = Compression.DecompressVarUInt(r);
            EpicUserID = r.ReadString();
            Email = r.ReadString();
            Password = r.ReadBytesAndSize();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(AccountName);
            Compression.CompressVarUInt(w, SteamID);
            w.WriteString(EpicUserID);
            w.WriteString(Email);
            w.WriteBytesAndSize(Password);
        }
    }

    public struct RegistrationResponseMessage : IResponseMessage
    {
        public string AccountName;
        public byte[] Password;
        public ResponseStatus Status { get; set; }


        public RegistrationResponseMessage(NetworkReader r)
        {
            AccountName = r.ReadString();
            Password = r.ReadBytesAndSize();
            Status = (ResponseStatus)r.ReadByte();
        }

        public void Serialize(NetworkWriter w)
        {
            w.WriteString(AccountName);
            w.WriteBytesAndSize(Password);
            w.WriteByte((byte)Status);
        }
    }
}

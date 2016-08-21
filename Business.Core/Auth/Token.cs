﻿/*==================================
             ########
            ##########

             ########
            ##########
          ##############
         #######  #######
        ######      ######
        #####        #####
        ####          ####
        ####   ####   ####
        #####  ####  #####
         ################
          ##############
==================================*/

namespace Business.Auth
{
    public interface IToken
    {
        string Key { get; set; }

        string Remote { get; set; }
    }

    [ProtoBuf.ProtoContract(SkipConstructor = true)]
    public struct Token : IToken
    {
        public static implicit operator Token(string value)
        {
            return Business.Extensions.Help.JsonDeserialize<Token>(value);
        }
        public static implicit operator Token(byte[] value)
        {
            return Business.Extensions.Help.ProtoBufDeserialize<Token>(value);
        }
        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
        public byte[] ToBytes()
        {
            return Business.Extensions.Help.ProtoBufSerialize(this);
        }

        [ProtoBuf.ProtoMember(1)]
        public string Key { get; set; }

        [ProtoBuf.ProtoMember(2)]
        public string Remote { get; set; }
    }
}

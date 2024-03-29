﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ND = Notus.Date;
using NVG = Notus.Variable.Globals;
namespace Notus.Prepare
{
    public class Token
    {
        public static async Task<Notus.Variable.Struct.BlockResponseStruct> Generate(
            string PublicKeyHex,
            string Sign,
            Notus.Variable.Struct.TokenInfoStruct InfoData,
            Notus.Variable.Struct.SupplyStruct TokenSupplyData,
            Notus.Variable.Enum.NetworkType currentNetwork,
            string whichNodeIpAddress = ""
        )
        {
            //Notus.Wallet.ID.GetAddressWithPublicKey(PublicKeyHex, currentNetwork)
            Notus.Variable.Struct.BlockStruct_160 Obj_Token = new Notus.Variable.Struct.BlockStruct_160()
            {
                Version = 1000,
                Info = new Notus.Variable.Struct.TokenInfoStruct()
                {
                    Name = InfoData.Name,
                    Tag = InfoData.Tag,
                    Logo = new Notus.Variable.Struct.FileStorageStruct()
                    {
                        Base64 = InfoData.Logo.Base64,
                        Source = InfoData.Logo.Source,
                        Url = InfoData.Logo.Url,
                        Used = InfoData.Logo.Used
                    }
                },
                Creation = new Notus.Variable.Struct.CreationStruct()
                {
                    UID = Notus.Block.Key.Generate(ND.NowObj(), ""),
                    PublicKey = PublicKeyHex,
                    Sign = Sign
                },
                Reserve = new Notus.Variable.Struct.SupplyStruct()
                {
                    Decimal = TokenSupplyData.Decimal,
                    Resupplyable = TokenSupplyData.Resupplyable,
                    Supply = TokenSupplyData.Supply
                }
            };

            bool exitInnerLoop = false;
            string WalletKeyStr = Notus.Wallet.ID.GetAddressWithPublicKey(PublicKeyHex, currentNetwork);
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = Notus.Variable.Constant.ListMainNodeIp[a];
                    if (whichNodeIpAddress != "")
                    {
                        nodeIpAddress = whichNodeIpAddress;
                    }
                    string MainResultStr = string.Empty;
                    try
                    {
                        string fullUrlAddress =
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(currentNetwork, Notus.Variable.Enum.NetworkLayer.Layer1)
                            ) + "token/generate/" + WalletKeyStr + "/";
                        MainResultStr = await Notus.Communication.Request.Post(
                            fullUrlAddress,
                            new Dictionary<string, string>
                            {
                                { "data" , JsonSerializer.Serialize(Obj_Token) }
                            }
                        );
                        Notus.Variable.Struct.BlockResponseStruct tmpResponse = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockResponseStruct>(MainResultStr);
                        return tmpResponse;
                    }
                    catch (Exception err)
                    {
                        //Notus.Print.Basic(true, "Error Text [9a5f4g12v3f]: " + err.Message);
                        return new Notus.Variable.Struct.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                            Status = "UnknownError -> " + MainResultStr + " >> " + err.Message
                        };
                    }
                }
            }
            return new Notus.Variable.Struct.BlockResponseStruct()
            {
                UID = "",
                Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                Status = "UnknownError"
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus.Core.Prepare
{
    public class Token
    {
        public static async Task<Notus.Core.Variable.BlockResponseStruct> Generate(
            string PublicKeyHex,
            string Sign,
            Notus.Core.Variable.TokenInfoStruct InfoData,
            Notus.Core.Variable.SupplyStruct TokenSupplyData
        )
        {
            Notus.Core.Variable.BlockStruct_160 Obj_Token = new Notus.Core.Variable.BlockStruct_160()
            {
                Version = 1000,
                Info = new Notus.Core.Variable.TokenInfoStruct()
                {
                    Name = InfoData.Name,
                    Tag = InfoData.Tag,
                    Logo = new Notus.Core.Variable.FileStorageStruct()
                    {
                        Base64 = InfoData.Logo.Base64,
                        Source = InfoData.Logo.Source,
                        Url = InfoData.Logo.Url,
                        Used = InfoData.Logo.Used
                    }
                },
                Creation = new Notus.Core.Variable.CreationStruct()
                {
                    UID = Notus.Core.Function.GenerateBlockKey(true),
                    PublicKey = PublicKeyHex,
                    Sign = Sign
                },
                Reserve = new Notus.Core.Variable.SupplyStruct()
                {
                    Decimal = TokenSupplyData.Decimal,
                    Resupplyable = TokenSupplyData.Resupplyable,
                    Supply = TokenSupplyData.Supply
                }
            };

            bool exitInnerLoop = false;
            string WalletKeyStr = Notus.Core.Wallet.ID.GetAddressWithPublicKey(PublicKeyHex);
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < Notus.Core.Variable.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = Notus.Core.Variable.ListMainNodeIp[a];
                    try
                    {
                        
                        string fullUrlAddress =
                            Notus.Core.Function.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Core.Variable.PortNo_HttpListener
                            ) + "token/generate/" + WalletKeyStr + "/";
                        string MainResultStr = await Notus.Core.Function.PostRequest(
                            fullUrlAddress,
                            new Dictionary<string, string>
                            {
                                { "data" , JsonSerializer.Serialize(Obj_Token) }
                            }
                        );
                        Console.WriteLine("Control-Point - Token.Generate");
                        Console.WriteLine(MainResultStr);
                        Console.WriteLine("Control-Point - Token.Generate");
                        Notus.Core.Variable.BlockResponseStruct tmpResponse = JsonSerializer.Deserialize<Notus.Core.Variable.BlockResponseStruct>(MainResultStr);
                        return tmpResponse;
                    }
                    catch (Exception err)
                    {
                        Notus.Core.Function.Print(true, "Error Text [8ae5cf]: " + err.Message);
                        return new Notus.Core.Variable.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Core.Variable.ErrorNoList.UnknownError,
                            Status = "UnknownError"
                        };
                    }
                }
            }

            return new Notus.Core.Variable.BlockResponseStruct()
            {
                UID = "",
                Code = Notus.Core.Variable.ErrorNoList.UnknownError,
                Status = "UnknownError"
            };
        }
    }
}

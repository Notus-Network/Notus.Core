using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Notus.Core.Wallet
{
    public class Transaction
    {
        //bu fonksiyon ile gönderilen transferin durumu kontrol edilecek...
        public static string Status(string TransferId)
        {
            return TransferId;
            //return string.Empty;
        }
        public static async Task<Notus.Core.Variable.CryptoTransactionResult> Send(Notus.Core.Variable.CryptoTransactionStruct PreTransfer, Notus.Core.Variable.NetworkType currentNetwork)
        {
            try
            {
                bool tmpTransactionVerified = Verify(PreTransfer);
                if (tmpTransactionVerified == false)
                {
                    return new Notus.Core.Variable.CryptoTransactionResult()
                    {
                        ID = string.Empty,
                        Result = Notus.Core.Variable.CryptoTransactionResultCode.WrongSignature,
                    };
                }

                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    for (int a = 0; a < Notus.Core.Variable.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        string nodeIpAddress = Notus.Core.Variable.ListMainNodeIp[a];
                        try
                        {
                            bool RealNetwork = PreTransfer.Network == Notus.Core.Variable.NetworkType.MainNet;
                            string fullUrlAddress =
                                Notus.Core.Function.MakeHttpListenerPath(
                                    nodeIpAddress,
                                    Notus.Core.Function.GetNetworkPort(currentNetwork)
                                ) +
                                "send/" +
                                PreTransfer.Sender + "/" +
                                PreTransfer.Receiver + "/" +
                                PreTransfer.Volume + "/" +
                                PreTransfer.PublicKey + "/" +
                                PreTransfer.Sign + "/";

                            string MainResultStr = await Notus.Core.Function.GetRequest(fullUrlAddress, 10, true);
                            Notus.Core.Variable.CryptoTransactionResult tmpTransferResult = JsonSerializer.Deserialize<Notus.Core.Variable.CryptoTransactionResult>(MainResultStr);
                            exitInnerLoop = true;
                            return tmpTransferResult;
                        }
                        catch (Exception err)
                        {
                            Notus.Core.Function.Print(true, "Error Text [8ae5cf]: " + err.Message);
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Notus.Core.Function.Print(true, "Error Text [3aebc9]: " + err.Message);
            }
            return new Notus.Core.Variable.CryptoTransactionResult()
            {
                ID = string.Empty,
                Result = Notus.Core.Variable.CryptoTransactionResultCode.AnErrorOccurred,
            };
        }

        public static bool Verify(Notus.Core.Variable.CryptoTransactionStruct PreTransfer)
        {
            if (Notus.Core.Wallet.ID.CheckAddress(PreTransfer.Sender, PreTransfer.Network) == false)
            {
                return false;
            }

            if (Notus.Core.Wallet.ID.CheckAddress(PreTransfer.Receiver, PreTransfer.Network) == false)
            {
                return false;
            }
            
            return Notus.Core.Wallet.ID.Verify(Notus.Core.MergeRawData.Transaction(
                   PreTransfer.Sender,
                   PreTransfer.Receiver,
                   PreTransfer.Volume
                ), PreTransfer.Sign,
                PreTransfer.PublicKey,
                PreTransfer.CurveName
            );
        }

        public static Notus.Core.Variable.CryptoTransactionStruct Sign(Notus.Core.Variable.CryptoTransactionBeforeStruct PreTransfer)
        {

            if (Notus.Core.Wallet.ID.CheckAddress(PreTransfer.Sender, PreTransfer.Network) == false)
            {
                return new Notus.Core.Variable.CryptoTransactionStruct()
                {
                    ErrorNo = 2
                };
            }

            if (Notus.Core.Wallet.ID.CheckAddress(PreTransfer.Receiver, PreTransfer.Network) == false)
            {
                return new Notus.Core.Variable.CryptoTransactionStruct()
                {
                    ErrorNo = 6
                };
            }

            return new Notus.Core.Variable.CryptoTransactionStruct()
            {
                ErrorNo = 0,
                Sender = PreTransfer.Sender,
                Receiver = PreTransfer.Receiver,
                Volume = PreTransfer.Volume,
                PublicKey = Notus.Core.Wallet.ID.Generate(
                    PreTransfer.PrivateKey,
                    PreTransfer.CurveName
                ),
                Sign = Notus.Core.Wallet.ID.Sign(
                    Notus.Core.MergeRawData.Transaction(
                        PreTransfer.Sender,
                        PreTransfer.Receiver,
                        PreTransfer.Volume
                    ),
                    PreTransfer.PrivateKey,
                    PreTransfer.CurveName
                ),
                CurveName = PreTransfer.CurveName,
                Network = PreTransfer.Network
            };
        }


    }
}

// Copyright (C) 2020-2022 Notus Network
// 
// Notus Network is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Notus.Wallet
{
    /// <summary>
    /// A helper class related to wallet transactions.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// DONE
        /// </summary>
        public static Notus.Variable.Enum.BlockStatusCode Status(
            string TransferId, 
            Notus.Variable.Enum.NetworkType WhichNetwork,
            Notus.Variable.Enum.NetworkLayer WhichLayer
        )
        {
            string requestResult=Notus.Network.Node.FindAvailableSync(
                "transaction/status/" + TransferId,
                WhichNetwork,
                WhichLayer
            );
            if (requestResult.Length == 0)
            {
                return Notus.Variable.Enum.BlockStatusCode.Unknown;
            }
            try
            {
                return JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(requestResult);
            }
            catch (Exception err)
            {
                return Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred;
            }

            /*
            using (
                Notus.Mempool ObjMp_CryptoTranStatus= new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        WhichNetwork, WhichLayer, Notus.Variable.Constant.StorageFolderName.Common 
                    ) + "crypto_transfer_status")
            )
            {
                string tmpDataResultStr = ObjMp_CryptoTranStatus.Get(TransferId, string.Empty);
                if (tmpDataResultStr.Length > 5)
                {
                    try
                    {
                        Notus.Variable.Struct.CryptoTransferStatus Obj_CryptTrnStatus = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransferStatus>(tmpDataResultStr);
                        return Obj_CryptTrnStatus.Code;
                    }
                    catch (Exception err)
                    {
                        //Console.WriteLine("Error Text [ba09c83fe] : " + err.Message);
                        return Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred;
                    }
                }
            }
            return Notus.Variable.Enum.BlockStatusCode.Unknown;
            */
        }

        /// <summary>
        /// Makes Transaction with given network and transaction struct via HTTP request. 
        /// </summary>
        /// <param name="preTransfer">Crypto Transaction informations.</param>
        /// <param name="currentNetwork">Current Network for Request.</param>
        /// <param name="whichNodeIpAddress">Node IP Address for Request.</param>
        /// <returns>Returns Result of the Transaction as <see cref="Notus.Variable.Struct.CryptoTransactionResult"/>.</returns>
        public static async Task<Notus.Variable.Struct.CryptoTransactionResult?>? Send(
            Notus.Variable.Struct.CryptoTransactionStruct preTransfer,
            Notus.Variable.Enum.NetworkType currentNetwork,
            string whichNodeIpAddress = ""
        )
        {
            try
            {
                if (Verify(preTransfer) == false)
                {
                    return new Notus.Variable.Struct.CryptoTransactionResult()
                    {
                        ID = string.Empty,
                        Result = Notus.Variable.Enum.BlockStatusCode.WrongSignature,
                    };
                }

                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    for (int a = 0; a < Notus.Variable.Constant.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        try
                        {
                            //bool RealNetwork = preTransfer.Network == Notus.Variable.Enum.NetworkType.MainNet;
                            string fullUrlAddress =
                                Notus.Network.Node.MakeHttpListenerPath(
                                    (
                                        whichNodeIpAddress == "" 
                                            ? 
                                        Notus.Variable.Constant.ListMainNodeIp[a] 
                                            : 
                                        whichNodeIpAddress
                                    ),
                                    Notus.Network.Node.GetNetworkPort(
                                        currentNetwork, 
                                        Notus.Variable.Enum.NetworkLayer.Layer1
                                    )
                                ) + "send/";

                            string MainResultStr = await Notus.Communication.Request.Post(
                                fullUrlAddress,
                                new System.Collections.Generic.Dictionary<string, string>()
                                {
                                    { "data" , JsonSerializer.Serialize(preTransfer) }
                                },
                                10,true,
                                true
                            );
                            Notus.Print.Basic(true, "Request Response : " + MainResultStr);
                            Notus.Variable.Struct.CryptoTransactionResult? tmpTransferResult = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionResult>(MainResultStr);
                            exitInnerLoop = true;
                            return tmpTransferResult;
                        }
                        catch (Exception err)
                        {
                            Notus.Print.Basic(true, "Error Text [8ae5cf]: " + err.Message);
                            Thread.Sleep(2000);
                        }
                        if (whichNodeIpAddress != "")
                        {
                            return new Notus.Variable.Struct.CryptoTransactionResult()
                            {
                                ID = string.Empty,
                                Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred,
                            };
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Notus.Print.Basic(true, "Error Text [3aebc9]: " + err.Message);
            }
            return new Notus.Variable.Struct.CryptoTransactionResult()
            {
                ID = string.Empty,
                Result = Notus.Variable.Enum.BlockStatusCode.AnErrorOccurred,
            };
        }

        /// <summary>
        /// Validates sent transaction information.
        /// </summary>
        /// <param name="preTransfer">Crypto Transaction informations.</param>
        /// <returns>Returns Result of the Verification.</returns>
        public static bool Verify(Notus.Variable.Struct.CryptoTransactionStruct preTransfer)
        {
            if (Notus.Wallet.ID.CheckAddress(preTransfer.Sender, preTransfer.Network) == false)
            {
                return false;
            }

            if (Notus.Wallet.ID.CheckAddress(preTransfer.Receiver, preTransfer.Network) == false)
            {
                return false;
            }

            return Notus.Wallet.ID.Verify(Notus.Core.MergeRawData.Transaction(
                   preTransfer.Sender,
                   preTransfer.Receiver,
                   preTransfer.Volume,
                   preTransfer.UnlockTime.ToString(),
                   preTransfer.Currency
                ), preTransfer.Sign,
                preTransfer.PublicKey,
                preTransfer.CurveName
            );
        }

        /// <summary>
        /// Signs current transaction informations
        /// </summary>
        /// <param name="preTransfer">Crypto Transaction informations.</param>
        /// <returns>Returns Signed Transaction Struct.</returns>
        public static Notus.Variable.Struct.CryptoTransactionStruct Sign(Notus.Variable.Struct.CryptoTransactionBeforeStruct preTransfer)
        {

            if (Notus.Wallet.ID.CheckAddress(preTransfer.Sender, preTransfer.Network) == false)
            {
                return new Notus.Variable.Struct.CryptoTransactionStruct()
                {
                    ErrorNo = 2
                };
            }

            if (Notus.Wallet.ID.CheckAddress(preTransfer.Receiver, preTransfer.Network) == false)
            {
                return new Notus.Variable.Struct.CryptoTransactionStruct()
                {
                    ErrorNo = 6
                };
            }

            return new Notus.Variable.Struct.CryptoTransactionStruct()
            {
                Currency = preTransfer.Currency,
                ErrorNo = 0,
                UnlockTime = preTransfer.UnlockTime,
                Sender = preTransfer.Sender,
                Receiver = preTransfer.Receiver,
                Volume = preTransfer.Volume,
                PublicKey = Notus.Wallet.ID.Generate(
                    preTransfer.PrivateKey,
                    preTransfer.CurveName
                ),
                Sign = Notus.Wallet.ID.Sign(
                    Notus.Core.MergeRawData.Transaction(
                        preTransfer.Sender,
                        preTransfer.Receiver,
                        preTransfer.Volume,
                        preTransfer.UnlockTime.ToString(),
                        preTransfer.Currency
                    ),
                    preTransfer.PrivateKey,
                    preTransfer.CurveName
                ),
                CurveName = preTransfer.CurveName,
                Network = preTransfer.Network
            };
        }


    }
}

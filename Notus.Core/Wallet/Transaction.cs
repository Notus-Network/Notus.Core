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

namespace Notus.Core.Wallet
{
    /// <summary>
    /// A helper class related to wallet transactions.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// TO DO.
        /// </summary>
        public static string Status(string TransferId)
        {
            return TransferId;
        }

        /// <summary>
        /// Makes Transaction with given network and transaction struct via HTTP request. 
        /// </summary>
        /// <param name="preTransfer">Crypto Transaction informations.</param>
        /// <param name="currentNetwork">Current Network for Request.</param>
        /// <returns>Returns Result of the Transaction as <see cref="Notus.Core.Variable.CryptoTransactionResult"/>.</returns>
        public static async Task<Notus.Core.Variable.CryptoTransactionResult> Send(Notus.Core.Variable.CryptoTransactionStruct preTransfer, Notus.Core.Variable.NetworkType currentNetwork)
        {
            try
            {
                bool tmpTransactionVerified = Verify(preTransfer);
                if (tmpTransactionVerified == false)
                {
                    return new Notus.Core.Variable.CryptoTransactionResult()
                    {
                        ID = string.Empty,
                        Result = Notus.Core.Variable.BlockStatusCode.WrongSignature,
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
                            bool RealNetwork = preTransfer.Network == Notus.Core.Variable.NetworkType.MainNet;
                            string fullUrlAddress =
                                Notus.Core.Function.MakeHttpListenerPath(
                                    nodeIpAddress,
                                    Notus.Core.Function.GetNetworkPort(currentNetwork, Notus.Core.Variable.NetworkLayer.Layer1)
                                ) + "send/" ;

                            string MainResultStr = await Notus.Core.Function.PostRequest(fullUrlAddress, 
                                new System.Collections.Generic.Dictionary<string, string>()
                                {
                                    { "data" , JsonSerializer.Serialize(preTransfer) }
                                }
                            );
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
                Result = Notus.Core.Variable.BlockStatusCode.AnErrorOccurred,
            };
        }

        /// <summary>
        /// Validates sent transaction information.
        /// </summary>
        /// <param name="preTransfer">Crypto Transaction informations.</param>
        /// <returns>Returns Result of the Verification.</returns>
        public static bool Verify(Notus.Core.Variable.CryptoTransactionStruct preTransfer)
        {
            if (Notus.Core.Wallet.ID.CheckAddress(preTransfer.Sender, preTransfer.Network) == false)
            {
                return false;
            }

            if (Notus.Core.Wallet.ID.CheckAddress(preTransfer.Receiver, preTransfer.Network) == false)
            {
                return false;
            }
            
            return Notus.Core.Wallet.ID.Verify(Notus.Core.MergeRawData.Transaction(
                   preTransfer.Sender,
                   preTransfer.Receiver,
                   preTransfer.Volume,
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
        public static Notus.Core.Variable.CryptoTransactionStruct Sign(Notus.Core.Variable.CryptoTransactionBeforeStruct preTransfer)
        {

            if (Notus.Core.Wallet.ID.CheckAddress(preTransfer.Sender, preTransfer.Network) == false)
            {
                return new Notus.Core.Variable.CryptoTransactionStruct()
                {
                    ErrorNo = 2
                };
            }

            if (Notus.Core.Wallet.ID.CheckAddress(preTransfer.Receiver, preTransfer.Network) == false)
            {
                return new Notus.Core.Variable.CryptoTransactionStruct()
                {
                    ErrorNo = 6
                };
            }

            return new Notus.Core.Variable.CryptoTransactionStruct()
            {
                Currency = preTransfer.Currency,
                ErrorNo = 0,
                Sender = preTransfer.Sender,
                Receiver = preTransfer.Receiver,
                Volume = preTransfer.Volume,
                PublicKey = Notus.Core.Wallet.ID.Generate(
                    preTransfer.PrivateKey,
                    preTransfer.CurveName
                ),
                Sign = Notus.Core.Wallet.ID.Sign(
                    Notus.Core.MergeRawData.Transaction(
                        preTransfer.Sender,
                        preTransfer.Receiver,
                        preTransfer.Volume,
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

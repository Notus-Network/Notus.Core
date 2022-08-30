using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Notus.Wallet
{
    public static class Safe
    {
        public static void Lock (
            Notus.Variable.Struct.LockWalletStruct LockObj,
            Notus.Variable.Enum.NetworkType networkType
        )
        {
            //string publicKey=Notus.Wallet.ID.GetAddress(privateKey, networkType);
            //string rawData=Notus.Core.MergeRawData.WalletSafe(walletKey, publicKey,pass, unlockTime);
            //string sign =Notus.Wallet.ID.Sign(rawData, privateKey);
        }
    }
}

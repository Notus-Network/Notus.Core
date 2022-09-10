using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Notus.Wallet
{
    public static class MultiID
    {
        public static string GetWalletID
        (
            string creatorWallet,
            List<string> walletList,
            Notus.Variable.Enum.MultiWalletType walletType,
            Notus.Variable.Enum.NetworkType whichNetworkFor
        )
        {
            walletList.Sort();
            string walletListText = string.Join(Notus.Variable.Constant.CommonDelimeterChar, walletList.ToArray());

            string keyPrefix = Notus.Variable.Constant.MultiWalletPrefix_MainNetwork;
            string networkByteStr = "60";

            if (whichNetworkFor == Notus.Variable.Enum.NetworkType.TestNet)
            {
                keyPrefix = Notus.Variable.Constant.MultiWalletPrefix_TestNetwork;
                networkByteStr = "70";
            }
            if (whichNetworkFor == Notus.Variable.Enum.NetworkType.DevNet)
            {
                keyPrefix = Notus.Variable.Constant.MultiWalletPrefix_DevelopmentNetwork;
                networkByteStr = "80";
            }

            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            string hashCreatorStr =
                Notus.Toolbox.Text.ShrinkHex(sashaObj.Calculate(sashaObj.Calculate(creatorWallet)), 6);

            string hashWalletListText =
                Notus.Toolbox.Text.ShrinkHex(sashaObj.Calculate(sashaObj.Calculate(walletListText)), 16);

            string checkSumStr = Notus.Toolbox.Text.ShrinkHex(
                sashaObj.Calculate(
                    sashaObj.Calculate(
                        networkByteStr +
                        creatorWallet +
                        walletListText +
                        walletType.ToString()
                    )
                ), 4
            );

            BigInteger number = BigInteger.Parse(
                "0" + 
                networkByteStr + 
                hashCreatorStr + 
                hashWalletListText + 
                checkSumStr,
                NumberStyles.AllowHexSpecifier
            );
            int howManyLen = Notus.Variable.Constant.MultiWalletTextLength -
                Notus.Variable.Constant.MultiWalletPrefix_MainNetwork.Length;
            string walletAddressStr = Notus.Wallet.Toolbox.EncodeBase58(number, howManyLen);
            return keyPrefix + walletAddressStr;
        }

        public static bool New(
            List<string> walletList,
            string publicKey,
            string sign,
            Notus.Variable.Enum.NetworkType whichNetworkFor = Notus.Variable.Enum.NetworkType.MainNet,
            string curveName = Notus.Variable.Constant.Default_EccCurveName
        )
        {

            return false;
        }
    }
}

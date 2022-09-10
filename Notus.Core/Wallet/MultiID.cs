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

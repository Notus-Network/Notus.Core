﻿// Copyright (C) 2020-2022 Notus Network
// 
// Notus Network is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Notus.Core.Wallet
{
    /// <summary>
    /// A helper class related to wallet IDs
    /// </summary>
    public class ID
    {
        public static bool Verify(string messageData, string signHex, string publicKeyHex)
        {
            return Verify_SubFunction(messageData, signHex, publicKeyHex, Notus.Core.Variable.Default_EccCurveName);
        }
        public static bool Verify(string messageData, string signHex, string publicKeyHex, string curveName)
        {
            return Verify_SubFunction(messageData, signHex, publicKeyHex, curveName);
        }
        private static bool Verify_SubFunction(string messageData, string signHex, string publicKeyHex, string curveName)
        {
            bool verifyResult = false;
            try
            {
                PublicKey yPubKey = PublicKey.fromString(
                    Notus.Core.Convert.Hex2Byte(publicKeyHex),
                    curveName,
                    true
                );
                verifyResult = Ecdsa.verify(
                    messageData,
                    Signature.fromBase64(System.Convert.ToBase64String(Notus.Core.Convert.Hex2Byte(signHex))),
                    yPubKey
                );
            }
            catch (Exception err)
            {
                Console.WriteLine("Error Text [8cfe9085] : " + err.Message);
            }
            return verifyResult;
        }
        
        public static string Sign(string messageData, string privateKeyHex)
        {
            return Sign_SubFunction(messageData, privateKeyHex, Notus.Core.Variable.Default_EccCurveName);
        }
        public static string Sign(string messageData, string privateKeyHex, string curveName)
        {
            return Sign_SubFunction(messageData, privateKeyHex, curveName);
        }
        private static string Sign_SubFunction(string messageData, string privateKeyHex, string curveName)
        {
            PrivateKey yPrivKey;
            if (curveName == "secp256k1")
            {
                
                yPrivKey = new PrivateKey("secp256k1", Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(privateKeyHex));
            }
            else
            {
                yPrivKey = new PrivateKey("p256", Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(privateKeyHex));
            }
            Signature signObj = Ecdsa.sign(messageData, yPrivKey);
            return Notus.Core.Convert.Byte2Hex(
                System.Convert.FromBase64String(
                    signObj.toBase64()
                )
            );
        }

        public static string GetAddress_StandartWay(string privateKeyHex, Notus.Core.Variable.NetworkType WhichNetworkFor = Notus.Core.Variable.NetworkType.MainNet, string CurveName = "secp256k1")
        {
            PrivateKey yPrivKey = new PrivateKey(CurveName, Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(privateKeyHex));
            PublicKey yPubKey = yPrivKey.publicKey();
            BigInteger pkPointVal = yPubKey.point.x;
            string publicKeyX = (yPubKey.point.y % 2 == 0 ? "02" : "03") + pkPointVal.ToString("x");
            string networkByteStr = "00";
            if (WhichNetworkFor == Notus.Core.Variable.NetworkType.TestNet)
            {
                networkByteStr = "04";
            }

            Notus.HashLib.RIPEMD160 ripObj = new Notus.HashLib.RIPEMD160();
            Notus.HashLib.SHA256 shaObj = new Notus.HashLib.SHA256();


            string hashPubKeyStr = ripObj.ComputeHash(shaObj.ComputeHash(publicKeyX));
            string checkSumStr = shaObj.ComputeHash(shaObj.ComputeHash(networkByteStr + hashPubKeyStr));
            BigInteger number = BigInteger.Parse(
                networkByteStr + hashPubKeyStr + checkSumStr.Substring(0, 8),
                NumberStyles.AllowHexSpecifier);

            string walletAddressStr = Notus.Core.Wallet.Function.EncodeBase58(number);
            return walletAddressStr;
        }

        public static bool CheckAddress(string walletAddress, Notus.Core.Variable.NetworkType WhichNetworkFor = Notus.Core.Variable.NetworkType.MainNet)
        {
            if (walletAddress != null)
            {
                if (walletAddress.Length == 38)
                {
                    if (WhichNetworkFor == Notus.Core.Variable.NetworkType.TestNet)
                    {
                        if (walletAddress.Substring(0, Notus.Core.Variable.Prefix_TestNetwork.Length) == Notus.Core.Variable.Prefix_TestNetwork)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (walletAddress.Substring(0, Notus.Core.Variable.Prefix_MainNetwork.Length) == Notus.Core.Variable.Prefix_MainNetwork)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        public static string GetAddressWithPublicKey(string privateKeyHex)
        {
            return GetAddress_SubFunction_FromPublicKey(PublicKey.fromString(Notus.Core.Convert.Hex2Byte(privateKeyHex),Notus.Core.Variable.Default_EccCurveName, true ), Notus.Core.Variable.NetworkType.MainNet, Notus.Core.Variable.Default_EccCurveName);
        }
        public static string GetAddressWithPublicKey(string privateKeyHex, string CurveName)
        {
            return GetAddress_SubFunction_FromPublicKey(PublicKey.fromString(Notus.Core.Convert.Hex2Byte(privateKeyHex),CurveName,true), Notus.Core.Variable.NetworkType.MainNet,CurveName);
        }
        public static string GetAddressWithPublicKey(string privateKeyHex, Notus.Core.Variable.NetworkType WhichNetworkFor)
        {
            return GetAddress_SubFunction_FromPublicKey(PublicKey.fromString(Notus.Core.Convert.Hex2Byte(privateKeyHex), Notus.Core.Variable.Default_EccCurveName, true), WhichNetworkFor,Notus.Core.Variable.Default_EccCurveName);
        }
        public static string GetAddressWithPublicKey(string privateKeyHex, Notus.Core.Variable.NetworkType WhichNetworkFor, string CurveName)
        {
            return GetAddress_SubFunction_FromPublicKey(PublicKey.fromString(Notus.Core.Convert.Hex2Byte(privateKeyHex), CurveName, true), WhichNetworkFor,CurveName);
        }

        public static string GetAddress(string privateKeyHex)
        {
            return GetAddress_SubFunction(privateKeyHex, Notus.Core.Variable.NetworkType.MainNet, Notus.Core.Variable.Default_EccCurveName);
        }
        public static string GetAddress(string privateKeyHex, string CurveName)
        {
            return GetAddress_SubFunction(privateKeyHex, Notus.Core.Variable.NetworkType.MainNet, CurveName);
        }
        public static string GetAddress(string privateKeyHex, Notus.Core.Variable.NetworkType WhichNetworkFor)
        {
            return GetAddress_SubFunction(privateKeyHex, WhichNetworkFor, Notus.Core.Variable.Default_EccCurveName);
        }
        public static string GetAddress(string privateKeyHex, Notus.Core.Variable.NetworkType WhichNetworkFor, string CurveName)
        {
            return GetAddress_SubFunction(privateKeyHex, WhichNetworkFor, CurveName);
        }
        private static string GetAddress_SubFunction(string privateKeyHex, Notus.Core.Variable.NetworkType WhichNetworkFor, string CurveName = "secp256k1")
        {
            PrivateKey yPrivKey = new PrivateKey(CurveName, Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(privateKeyHex));
            PublicKey yPubKey = yPrivKey.publicKey();
            return GetAddress_SubFunction_FromPublicKey(yPubKey, WhichNetworkFor, CurveName);
        }

        private static string GetAddress_SubFunction_FromPublicKey(PublicKey yPubKey, Notus.Core.Variable.NetworkType WhichNetworkFor, string CurveName = "secp256k1")
        {
            BigInteger pkPointVal = yPubKey.point.x;
            string fullPublicKey = yPubKey.point.x.ToString("x") + yPubKey.point.y.ToString("x");

            string publicKeyX = (yPubKey.point.y % 2 == 0 ? "02" : "03") + pkPointVal.ToString("x");

            string keyPrefix = Notus.Core.Variable.Prefix_MainNetwork;
            string networkByteStr = "10";
            if (WhichNetworkFor == Notus.Core.Variable.NetworkType.TestNet)
            {
                keyPrefix = Notus.Core.Variable.Prefix_TestNetwork;
                networkByteStr = "20";
            }

            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            string hashPubKeyStr = Notus.Core.Function.ShrinkHex(
                sashaObj.Calculate(
                    sashaObj.Calculate(publicKeyX)
                ), 22
            );

            string checkSumStr = Notus.Core.Function.ShrinkHex(
                sashaObj.Calculate(
                    sashaObj.Calculate(networkByteStr + fullPublicKey)
                ), 4
            );

            BigInteger number = BigInteger.Parse(
                networkByteStr + hashPubKeyStr + checkSumStr,
                NumberStyles.AllowHexSpecifier);

            string walletAddressStr = Notus.Core.Wallet.Function.EncodeBase58(number, 36);
            return keyPrefix + walletAddressStr;
        }
        public static string PrivateKeyFromWordList(string[] WordList)
        {
            BigInteger PrivateKeySeedNumber = PrivateKeyFromPassPhrase(WordList);
            string privateHexStr = New(Notus.Core.Variable.Default_EccCurveName, PrivateKeySeedNumber);
            return privateHexStr;
        }
        public static BigInteger PrivateKeyFromPassPhrase(string[] WordList)
        {
            if (WordList.Length != 16) { return 0; }
            string[] tmpWordList = new string[Notus.Core.Variable.Default_WordListArrayCount];
            string fullWordLine = "";
            for (int i = 0; i < Notus.Core.Variable.Default_WordListArrayCount; i++)
            {
                if (i == 0)
                {
                    fullWordLine = WordList[i] + Notus.Core.Variable.CommonDelimeterChar;
                }
                else
                {
                    fullWordLine = fullWordLine + WordList[i];
                    if (Notus.Core.Variable.Default_WordListArrayCount - 1 > i)
                    {
                        fullWordLine = fullWordLine + Notus.Core.Variable.CommonDelimeterChar;
                    }
                }
                tmpWordList[i] = new Notus.Hash().CommonHash("md5", WordList[i]);
            }
            fullWordLine = new Notus.Hash().CommonHash("sasha",
                new Notus.Hash().CommonHash("sasha",
                    fullWordLine
                ) +
                Notus.Core.Variable.CommonDelimeterChar +
                fullWordLine
            );
            string hexResultStr = "";
            for (int i = 0; i < Notus.Core.Variable.Default_WordListArrayCount; i++)
            {
                tmpWordList[i] = Notus.Core.Function.ShrinkHex(
                    new Notus.Hash().CommonHash("sha1",
                        fullWordLine +
                        Notus.Core.Variable.CommonDelimeterChar +
                        tmpWordList[i]
                    )
                , 2);
                hexResultStr = hexResultStr + tmpWordList[i];
            }
            return BigInteger.Parse("0" + hexResultStr, NumberStyles.AllowHexSpecifier);
        }

        public static Notus.Core.Variable.EccKeyPair GenerateKeyPair()
        {
            return GenerateKeyPair(Notus.Core.Variable.Default_EccCurveName, Notus.Core.Variable.NetworkType.MainNet);
        }
        public static Notus.Core.Variable.EccKeyPair GenerateKeyPair(string curveName)
        {
            return GenerateKeyPair(curveName, Notus.Core.Variable.NetworkType.MainNet);
        }
        public static Notus.Core.Variable.EccKeyPair GenerateKeyPair(Notus.Core.Variable.NetworkType WhichNetworkFor)
        {
            return GenerateKeyPair(Notus.Core.Variable.Default_EccCurveName, WhichNetworkFor);
        }
        public static Notus.Core.Variable.EccKeyPair GenerateKeyPair(string curveName, Notus.Core.Variable.NetworkType WhichNetworkFor)
        {
            if (curveName == "")
            {
                curveName = Notus.Core.Variable.Default_EccCurveName;
            }
            string[] WordList = Notus.Core.Wallet.Function.SeedPhraseList();
            BigInteger PrivateKeySeedNumber = PrivateKeyFromPassPhrase(WordList);
            string privateHexStr = New(curveName, PrivateKeySeedNumber);
            return new Notus.Core.Variable.EccKeyPair()
            {
                CurveName = curveName,
                Words = WordList,
                PrivateKey = privateHexStr,
                PublicKey = Generate(privateHexStr, curveName),
                WalletKey = GetAddress(privateHexStr, WhichNetworkFor, curveName)
            };
        }

        public static string Generate(string[] words, string curveName = Notus.Core.Variable.Default_EccCurveName)
        {
            if (words.Length > 0)
            {

            }
            return "";
        }
        // generate ECC public key from private key
        public static string Generate(string privateKeyHex, string curveName = Notus.Core.Variable.Default_EccCurveName)
        {
            PrivateKey privKey;
            if (curveName.ToLower() == "secp256k1")
            {
                privKey = new PrivateKey("secp256k1", Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(privateKeyHex));
            }
            else
            {
                privKey = new PrivateKey("p256", Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(privateKeyHex));
            }
            //string privateKeyStr = privKey.toHex().ToLower();
            PublicKey yPubKey = privKey.publicKey();
            return Notus.Core.Convert.Byte2Hex(yPubKey.toString(false)).ToLower();
        }

        // generate new ECC private key
        public static string New(string curveName = Notus.Core.Variable.Default_EccCurveName, BigInteger? PrivateKeySeedValue = null)
        {
            PrivateKey privKey;
            if (curveName.ToLower() == "secp256k1")
            {
                privKey = new PrivateKey("secp256k1", PrivateKeySeedValue);
            }
            else
            {
                privKey = new PrivateKey("p256", PrivateKeySeedValue);
            }
            return privKey.toHex().ToLower();
        }

        private class CurveFp
        {
            public BigInteger A { get; private set; }
            public BigInteger B { get; private set; }
            public BigInteger P { get; private set; }
            public BigInteger N { get; private set; }
            public CurvePointStruct G { get; private set; }
            public string name { get; private set; }
            public int[] oid { get; private set; }
            public string nistName { get; private set; }


            public CurveFp(BigInteger A, BigInteger B, BigInteger P, BigInteger N, BigInteger Gx, BigInteger Gy, string name, int[] oid, string nistName = "")
            {
                this.A = A;
                this.B = B;
                this.P = P;
                this.N = N;
                G = new CurvePointStruct(Gx, Gy);
                this.name = name;
                this.nistName = nistName;
                this.oid = oid;
            }

            public bool contains(CurvePointStruct p)
            {
                return Notus.Core.Wallet.Function.Integer_modulo(
                    BigInteger.Pow(p.y, 2) - (BigInteger.Pow(p.x, 3) + A * p.x + B),
                    P
                ).IsZero;
            }

            public int length()
            {
                return N.ToString("X").Length / 2;
            }

        }
        private static class Curves
        {

            public static CurveFp getCurveByName(string name)
            {
                name = name.ToLower();

                if (name == "secp256k1")
                {
                    return secp256k1;
                }
                if (name == "p256" | name == "prime256v1")
                {
                    return prime256v1;
                }

                throw new ArgumentException("unknown curve " + name);
            }

            public static CurveFp secp256k1 = new CurveFp(
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("0000000000000000000000000000000000000000000000000000000000000000"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("0000000000000000000000000000000000000000000000000000000000000007"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8"),
                "secp256k1",
                new int[] { 1, 3, 132, 0, 10 }
            );

            public static CurveFp prime256v1 = new CurveFp(
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("ffffffff00000001000000000000000000000000fffffffffffffffffffffffc"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("5ac635d8aa3a93e7b3ebbd55769886bc651d06b0cc53b0f63bce3c3e27d2604b"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("ffffffff00000001000000000000000000000000ffffffffffffffffffffffff"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("ffffffff00000000ffffffffffffffffbce6faada7179e84f3b9cac2fc632551"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("6b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296"),
                Notus.Core.Wallet.Function.BinaryAscii_numberFromHex("4fe342e2fe1a7f9b8ee7eb4a7c0f9e162bce33576b315ececbb6406837bf51f5"),
                "prime256v1",
                new int[] { 1, 2, 840, 10045, 3, 1, 7 },
                "P-256"
            );

            public static CurveFp p256 = prime256v1;

            public static CurveFp[] supportedCurves = { secp256k1, prime256v1 };

            public static Dictionary<string, CurveFp> curvesByOid = new Dictionary<string, CurveFp>() {
            {string.Join(",", secp256k1.oid), secp256k1},
            {string.Join(",", prime256v1.oid), prime256v1}
        };

        }
        private class PublicKey
        {

            public CurvePointStruct point { get; }

            public CurveFp curve { get; private set; }

            public PublicKey(CurvePointStruct point, CurveFp curve)
            {
                this.point = point;
                this.curve = curve;
            }

            public byte[] toString(bool encoded = false)
            {
                byte[] xString = Notus.Core.Wallet.Function.BinaryAscii_stringFromNumber(point.x, curve.length());
                byte[] yString = Notus.Core.Wallet.Function.BinaryAscii_stringFromNumber(point.y, curve.length());

                if (encoded)
                {
                    return Notus.Core.Wallet.Function.Der_combineByteArrays(new List<byte[]> {
                        Notus.Core.Wallet.Function.BinaryAscii_binaryFromHex("00"),
                        Notus.Core.Wallet.Function.BinaryAscii_binaryFromHex("04"),
                        xString,
                        yString
                    });
                }
                return Notus.Core.Wallet.Function.Der_combineByteArrays(new List<byte[]> {
                xString,
                yString
            });
            }

            public byte[] toDer()
            {
                int[] oidEcPublicKey = { 1, 2, 840, 10045, 2, 1 };
                byte[] encodedEcAndOid = Notus.Core.Wallet.Function.Der_encodeSequence(
                    new List<byte[]> {
                    Notus.Core.Wallet.Function.Der_encodeOid(oidEcPublicKey),
                    Notus.Core.Wallet.Function.Der_encodeOid(curve.oid)
                    }
                );

                return Notus.Core.Wallet.Function.Der_encodeSequence(
                    new List<byte[]> {
                    encodedEcAndOid,
                    Notus.Core.Wallet.Function.Der_encodeBitString(toString(true))
                    }
                );
            }

            public string toPem()
            {
                return Notus.Core.Wallet.Function.Der_toPem(toDer(), "PUBLIC KEY");
            }


            public static PublicKey fromPem(string pem)
            {
                return fromDer(Notus.Core.Wallet.Function.Der_fromPem(pem));
            }

            public static PublicKey fromDer(byte[] der)
            {
                Tuple<byte[], byte[]> removeSequence1 = Notus.Core.Wallet.Function.Der_removeSequence(der);
                byte[] s1 = removeSequence1.Item1;

                if (removeSequence1.Item2.Length > 0)
                {
                    throw new ArgumentException(
                        "trailing junk after DER public key: " +
                        Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(removeSequence1.Item2)
                    );
                }

                Tuple<byte[], byte[]> removeSequence2 = Notus.Core.Wallet.Function.Der_removeSequence(s1);
                byte[] s2 = removeSequence2.Item1;
                byte[] pointBitString = removeSequence2.Item2;

                Tuple<int[], byte[]> removeObject1 = Notus.Core.Wallet.Function.Der_removeObject(s2);
                byte[] rest = removeObject1.Item2;

                Tuple<int[], byte[]> removeObject2 = Notus.Core.Wallet.Function.Der_removeObject(rest);
                int[] oidCurve = removeObject2.Item1;

                if (removeObject2.Item2.Length > 0)
                {
                    throw new ArgumentException(
                        "trailing junk after DER public key objects: " +
                        Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(removeObject2.Item2)
                    );
                }

                string stringOid = string.Join(",", oidCurve);

                if (!Curves.curvesByOid.ContainsKey(stringOid))
                {
                    int numCurves = Curves.supportedCurves.Length;
                    string[] supportedCurves = new string[numCurves];
                    for (int i = 0; i < numCurves; i++)
                    {
                        supportedCurves[i] = Curves.supportedCurves[i].name;
                    }
                    throw new ArgumentException(
                        "Unknown curve with oid [" +
                        string.Join(", ", oidCurve) +
                        "]. Only the following are available: " +
                        string.Join(", ", supportedCurves)
                    );
                }

                CurveFp curve = Curves.curvesByOid[stringOid];

                Tuple<byte[], byte[]> removeBitString = Notus.Core.Wallet.Function.Der_removeBitString(pointBitString);
                byte[] pointString = removeBitString.Item1;

                if (removeBitString.Item2.Length > 0)
                {
                    throw new ArgumentException("trailing junk after public key point-string");
                }

                return fromString(Notus.Core.Wallet.Function.Bytes_sliceByteArray(pointString, 2), curve.name);

            }

            public static PublicKey fromString(byte[] str, string curve = "secp256k1", bool validatePoint = true)
            {
                CurveFp curveObject = Curves.getCurveByName(curve);

                int baseLen = curveObject.length();

                if (str.Length != 2 * baseLen)
                {
                    throw new ArgumentException("string length [" + str.Length + "] should be " + 2 * baseLen);
                }

                string xs = Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(Notus.Core.Wallet.Function.Bytes_sliceByteArray(str, 0, baseLen));
                string ys = Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(Notus.Core.Wallet.Function.Bytes_sliceByteArray(str, baseLen));

                CurvePointStruct p = new CurvePointStruct(
                    Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(xs),
                    Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(ys)
                );

                if (validatePoint & !curveObject.contains(p))
                {
                    throw new ArgumentException(
                        "point (" +
                        p.x.ToString() +
                        ", " +
                        p.y.ToString() +
                        ") is not valid for curve " +
                        curveObject.name
                    );
                }

                return new PublicKey(p, curveObject);

            }

        }
        private class Signature
        {

            public BigInteger r { get; }
            public BigInteger s { get; }

            public Signature(BigInteger r, BigInteger s)
            {
                this.r = r;
                this.s = s;
            }

            public byte[] toDer()
            {
                List<byte[]> sequence = new List<byte[]> { Notus.Core.Wallet.Function.Der_encodeInteger(r), Notus.Core.Wallet.Function.Der_encodeInteger(s) };
                return Notus.Core.Wallet.Function.Der_encodeSequence(sequence);
            }

            public string toBase64()
            {
                return Notus.Core.Wallet.Function.Base64_encode(toDer());
            }

            public static Signature fromDer(byte[] bytes)
            {
                Tuple<byte[], byte[]> removeSequence = Notus.Core.Wallet.Function.Der_removeSequence(bytes);
                byte[] rs = removeSequence.Item1;
                byte[] removeSequenceTrail = removeSequence.Item2;

                if (removeSequenceTrail.Length > 0)
                {
                    throw new ArgumentException("trailing junk after DER signature: " + Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(removeSequenceTrail));
                }

                Tuple<BigInteger, byte[]> removeInteger = Notus.Core.Wallet.Function.Der_removeInteger(rs);
                BigInteger r = removeInteger.Item1;
                byte[] rest = removeInteger.Item2;

                removeInteger = Notus.Core.Wallet.Function.Der_removeInteger(rest);
                BigInteger s = removeInteger.Item1;
                byte[] removeIntegerTrail = removeInteger.Item2;

                if (removeIntegerTrail.Length > 0)
                {
                    throw new ArgumentException("trailing junk after DER numbers: " + Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(removeIntegerTrail));
                }

                return new Signature(r, s);

            }

            public static Signature fromBase64(string str)
            {
                return fromDer(Notus.Core.Wallet.Function.Base64_decode(str));
            }

        }
        private static class Ecdsa
        {

            public static Signature sign(string message, PrivateKey privateKey)
            {
                string hashMessage = hashCalculate(message);
                BigInteger numberMessage = Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(hashMessage);
                CurveFp curve = privateKey.curve;
                BigInteger randNum = Notus.Core.Wallet.Function.Integer_randomBetween(BigInteger.One, curve.N - 1);
                CurvePointStruct randSignPoint = EcdsaMath.multiply(curve.G, randNum, curve.N, curve.A, curve.P);
                BigInteger r = Notus.Core.Wallet.Function.Integer_modulo(randSignPoint.x, curve.N);
                BigInteger s = Notus.Core.Wallet.Function.Integer_modulo((numberMessage + r * privateKey.secret) * (EcdsaMath.inv(randNum, curve.N)), curve.N);

                return new Signature(r, s);
            }

            public static bool verify(string message, Signature signature, PublicKey publicKey)
            {
                string hashMessage = hashCalculate(message);
                BigInteger numberMessage = Notus.Core.Wallet.Function.BinaryAscii_numberFromHex(hashMessage);
                CurveFp curve = publicKey.curve;
                BigInteger sigR = signature.r;
                BigInteger sigS = signature.s;
                BigInteger inv = EcdsaMath.inv(sigS, curve.N);

                CurvePointStruct u1 = EcdsaMath.multiply(
                    curve.G,
                    Notus.Core.Wallet.Function.Integer_modulo((numberMessage * inv), curve.N),
                    curve.N,
                    curve.A,
                    curve.P
                );
                CurvePointStruct u2 = EcdsaMath.multiply(
                    publicKey.point,
                    Notus.Core.Wallet.Function.Integer_modulo((sigR * inv), curve.N),
                    curve.N,
                    curve.A,
                    curve.P
                );
                CurvePointStruct add = EcdsaMath.add(
                    u1,
                    u2,
                    curve.A,
                    curve.P
                );

                return sigR == add.x;
            }
            private static string hashCalculate(string message)
            {
                Notus.Hash hashObj = new Notus.Hash();
                return hashObj.CommonHash("sasha", message).Substring(0, 64);
            }
        }

        private static class EcdsaMath
        {

            public static CurvePointStruct multiply(CurvePointStruct p, BigInteger n, BigInteger N, BigInteger A, BigInteger P)
            {
                //Fast way to multily point and scalar in elliptic curves

                //:param p: First Point to mutiply
                //:param n: Scalar to mutiply
                //: param N: Order of the elliptic curve
                // : param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
                //:param A: Coefficient of the first-order term of the equation Y ^ 2 = X ^ 3 + A * X + B(mod p)
                //:return: Point that represents the sum of First and Second Point

                return fromJacobian(
                    jacobianMultiply(
                        toJacobian(p),
                        n,
                        N,
                        A,
                        P
                    ),
                    P
                );
            }

            public static CurvePointStruct add(CurvePointStruct p, CurvePointStruct q, BigInteger A, BigInteger P)
            {
                //Fast way to add two points in elliptic curves

                //:param p: First Point you want to add
                //:param q: Second Point you want to add
                //:param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
                //:param A: Coefficient of the first-order term of the equation Y ^ 2 = X ^ 3 + A * X + B(mod p)
                //:return: Point that represents the sum of First and Second Point

                return fromJacobian(
                    jacobianAdd(
                        toJacobian(p),
                        toJacobian(q),
                        A,
                        P
                    ),
                    P
                );
            }

            public static BigInteger inv(BigInteger x, BigInteger n)
            {
                //Extended Euclidean Algorithm.It's the 'division' in elliptic curves

                //:param x: Divisor
                //: param n: Mod for division
                //:return: Value representing the division

                if (x.IsZero)
                {
                    return 0;
                }

                BigInteger lm = BigInteger.One;
                BigInteger hm = BigInteger.Zero;
                BigInteger low = Notus.Core.Wallet.Function.Integer_modulo(x, n);
                BigInteger high = n;
                BigInteger r, nm, newLow;

                while (low > 1)
                {
                    r = high / low;

                    nm = hm - (lm * r);
                    newLow = high - (low * r);

                    high = low;
                    hm = lm;
                    low = newLow;
                    lm = nm;
                }

                return Notus.Core.Wallet.Function.Integer_modulo(lm, n);

            }

            private static CurvePointStruct toJacobian(CurvePointStruct p)
            {
                //Convert point to Jacobian coordinates

                //: param p: First Point you want to add
                //:return: Point in Jacobian coordinates

                return new CurvePointStruct(p.x, p.y, 1);
            }

            private static CurvePointStruct fromJacobian(CurvePointStruct p, BigInteger P)
            {
                //Convert point back from Jacobian coordinates

                //:param p: First Point you want to add
                //:param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
                //:return: Point in default coordinates

                BigInteger z = inv(p.z, P);

                return new CurvePointStruct(
                    Notus.Core.Wallet.Function.Integer_modulo(p.x * BigInteger.Pow(z, 2), P),
                    Notus.Core.Wallet.Function.Integer_modulo(p.y * BigInteger.Pow(z, 3), P)
                );
            }

            private static CurvePointStruct jacobianDouble(CurvePointStruct p, BigInteger A, BigInteger P)
            {
                //Double a point in elliptic curves

                //:param p: Point you want to double
                //:param P: Prime number in the module of the equation Y^2 = X ^ 3 + A * X + B(mod p)
                //:param A: Coefficient of the first-order term of the equation Y ^ 2 = X ^ 3 + A * X + B(mod p)
                //:return: Point that represents the sum of First and Second Point

                if (p.y.IsZero)
                {
                    return new CurvePointStruct(
                        BigInteger.Zero,
                        BigInteger.Zero,
                        BigInteger.Zero
                    );
                }

                BigInteger ysq = Notus.Core.Wallet.Function.Integer_modulo(
                    BigInteger.Pow(p.y, 2),
                    P
                );
                BigInteger S = Notus.Core.Wallet.Function.Integer_modulo(
                    4 * p.x * ysq,
                    P
                );
                BigInteger M = Notus.Core.Wallet.Function.Integer_modulo(
                    3 * BigInteger.Pow(p.x, 2) + A * BigInteger.Pow(p.z, 4),
                    P
                );

                BigInteger nx = Notus.Core.Wallet.Function.Integer_modulo(
                    BigInteger.Pow(M, 2) - 2 * S,
                    P
                );
                BigInteger ny = Notus.Core.Wallet.Function.Integer_modulo(
                    M * (S - nx) - 8 * BigInteger.Pow(ysq, 2),
                    P
                );
                BigInteger nz = Notus.Core.Wallet.Function.Integer_modulo(
                    2 * p.y * p.z,
                    P
                );

                return new CurvePointStruct(
                    nx,
                    ny,
                    nz
                );
            }

            private static CurvePointStruct jacobianAdd(CurvePointStruct p, CurvePointStruct q, BigInteger A, BigInteger P)
            {
                // Add two points in elliptic curves

                // :param p: First Point you want to add
                // :param q: Second Point you want to add
                // :param P: Prime number in the module of the equation Y^2 = X^3 + A*X + B (mod p)
                // :param A: Coefficient of the first-order term of the equation Y^2 = X^3 + A*X + B (mod p)
                // :return: Point that represents the sum of First and Second Point

                if (p.y.IsZero)
                {
                    return q;
                }
                if (q.y.IsZero)
                {
                    return p;
                }

                BigInteger U1 = Notus.Core.Wallet.Function.Integer_modulo(
                    p.x * BigInteger.Pow(q.z, 2),
                    P
                );
                BigInteger U2 = Notus.Core.Wallet.Function.Integer_modulo(
                    q.x * BigInteger.Pow(p.z, 2),
                    P
                );
                BigInteger S1 = Notus.Core.Wallet.Function.Integer_modulo(
                    p.y * BigInteger.Pow(q.z, 3),
                    P
                );
                BigInteger S2 = Notus.Core.Wallet.Function.Integer_modulo(
                    q.y * BigInteger.Pow(p.z, 3),
                    P
                );

                if (U1 == U2)
                {
                    if (S1 != S2)
                    {
                        return new CurvePointStruct(BigInteger.Zero, BigInteger.Zero, BigInteger.One);
                    }
                    return jacobianDouble(p, A, P);
                }

                BigInteger H = U2 - U1;
                BigInteger R = S2 - S1;
                BigInteger H2 = Notus.Core.Wallet.Function.Integer_modulo(H * H, P);
                BigInteger H3 = Notus.Core.Wallet.Function.Integer_modulo(H * H2, P);
                BigInteger U1H2 = Notus.Core.Wallet.Function.Integer_modulo(U1 * H2, P);
                BigInteger nx = Notus.Core.Wallet.Function.Integer_modulo(
                    BigInteger.Pow(R, 2) - H3 - 2 * U1H2,
                    P
                );
                BigInteger ny = Notus.Core.Wallet.Function.Integer_modulo(
                    R * (U1H2 - nx) - S1 * H3,
                    P
                );
                BigInteger nz = Notus.Core.Wallet.Function.Integer_modulo(
                    H * p.z * q.z,
                    P
                );

                return new CurvePointStruct(
                    nx,
                    ny,
                    nz
                );
            }

            private static CurvePointStruct jacobianMultiply(CurvePointStruct p, BigInteger n, BigInteger N, BigInteger A, BigInteger P)
            {
                // Multily point and scalar in elliptic curves

                // :param p: First Point to mutiply
                // :param n: Scalar to mutiply
                // :param N: Order of the elliptic curve
                // :param P: Prime number in the module of the equation Y^2 = X^3 + A*X + B (mod p)
                // :param A: Coefficient of the first-order term of the equation Y^2 = X^3 + A*X + B (mod p)
                // :return: Point that represents the sum of First and Second Point

                if (p.y.IsZero | n.IsZero)
                {
                    return new CurvePointStruct(
                        BigInteger.Zero,
                        BigInteger.Zero,
                        BigInteger.One
                    );
                }

                if (n.IsOne)
                {
                    return p;
                }

                if (n < 0 | n >= N)
                {
                    return jacobianMultiply(
                        p,
                        Notus.Core.Wallet.Function.Integer_modulo(n, N),
                        N,
                        A,
                        P
                    );
                }

                if (Notus.Core.Wallet.Function.Integer_modulo(n, 2).IsZero)
                {
                    return jacobianDouble(
                        jacobianMultiply(
                            p,
                            n / 2,
                            N,
                            A,
                            P
                        ),
                        A,
                        P
                    );
                }

                // (n % 2) == 1:
                return jacobianAdd(
                    jacobianDouble(
                        jacobianMultiply(
                            p,
                            n / 2,
                            N,
                            A,
                            P
                        ),
                        A,
                        P
                    ),
                    p,
                    A,
                    P
                );

            }

        }

        private class CurvePointStruct
        {

            public BigInteger x { get; }
            public BigInteger y { get; }
            public BigInteger z { get; }

            public CurvePointStruct(BigInteger x, BigInteger y, BigInteger? z = null)
            {
                BigInteger zeroZ = z ?? BigInteger.Zero;

                this.x = x;
                this.y = y;
                this.z = zeroZ;
            }
        }

        private class PrivateKey
        {

            public CurveFp curve { get; private set; }
            public BigInteger secret { get; private set; }

            public PrivateKey(string curve = "secp256k1", BigInteger? secret = null)
            {
                this.curve = Curves.getCurveByName(curve);

                if (secret == null)
                {
                    secret = Notus.Core.Wallet.Function.Integer_randomBetween(1, this.curve.N - 1);
                }
                this.secret = (BigInteger)secret;
            }

            public PublicKey publicKey()
            {
                CurvePointStruct publicPoint = EcdsaMath.multiply(curve.G, secret, curve.N, curve.A, curve.P);
                return new PublicKey(publicPoint, curve);
            }

            public string toHex()
            {
                return Notus.Core.Wallet.Function.BinaryAscii_hexFromNumber(secret, curve.length());
            }
            public byte[] toString()
            {
                return Notus.Core.Wallet.Function.BinaryAscii_stringFromNumber(secret, curve.length());
            }

            public byte[] toDer()
            {
                byte[] encodedPublicKey = publicKey().toString(true);

                return Notus.Core.Wallet.Function.Der_encodeSequence(
                    new List<byte[]> {
                    Notus.Core.Wallet.Function.Der_encodeInteger(1),
                    Notus.Core.Wallet.Function.Der_encodeOctetString(toString()),
                    Notus.Core.Wallet.Function.Der_encodeConstructed(0, Notus.Core.Wallet.Function.Der_encodeOid(curve.oid)),
                    Notus.Core.Wallet.Function.Der_encodeConstructed(1, encodedPublicKey)
                    }
                );
            }

            public string toPem()
            {
                return Notus.Core.Wallet.Function.Der_toPem(toDer(), "EC PRIVATE KEY");
            }

            public static PrivateKey fromPem(string str)
            {
                string[] split = str.Split(new string[] { "-----BEGIN EC PRIVATE KEY-----" }, StringSplitOptions.None);

                if (split.Length != 2)
                {
                    throw new ArgumentException("invalid PEM");
                }

                return fromDer(Notus.Core.Wallet.Function.Der_fromPem(split[1]));
            }

            public static PrivateKey fromDer(byte[] der)
            {
                Tuple<byte[], byte[]> removeSequence = Notus.Core.Wallet.Function.Der_removeSequence(der);
                if (removeSequence.Item2.Length > 0)
                {
                    throw new ArgumentException("trailing junk after DER private key: " + Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(removeSequence.Item2));
                }

                Tuple<BigInteger, byte[]> removeInteger = Notus.Core.Wallet.Function.Der_removeInteger(removeSequence.Item1);
                if (removeInteger.Item1 != 1)
                {
                    throw new ArgumentException("expected '1' at start of DER private key, got " + removeInteger.Item1.ToString());
                }

                Tuple<byte[], byte[]> removeOctetString = Notus.Core.Wallet.Function.Der_removeOctetString(removeInteger.Item2);
                byte[] privateKeyStr = removeOctetString.Item1;

                Tuple<int, byte[], byte[]> removeConstructed = Notus.Core.Wallet.Function.Der_removeConstructed(removeOctetString.Item2);
                int tag = removeConstructed.Item1;
                byte[] curveOidString = removeConstructed.Item2;
                if (tag != 0)
                {
                    throw new ArgumentException("expected tag 0 in DER private key, got " + tag.ToString());
                }

                Tuple<int[], byte[]> removeObject = Notus.Core.Wallet.Function.Der_removeObject(curveOidString);
                int[] oidCurve = removeObject.Item1;
                if (removeObject.Item2.Length > 0)
                {
                    throw new ArgumentException(
                        "trailing junk after DER private key curve_oid: " +
                        Notus.Core.Wallet.Function.BinaryAscii_hexFromBinary(removeObject.Item2)
                    );
                }

                string stringOid = string.Join(",", oidCurve);

                if (!Curves.curvesByOid.ContainsKey(stringOid))
                {
                    int numCurves = Curves.supportedCurves.Length;
                    string[] supportedCurves = new string[numCurves];
                    for (int i = 0; i < numCurves; i++)
                    {
                        supportedCurves[i] = Curves.supportedCurves[i].name;
                    }
                    throw new ArgumentException(
                        "Unknown curve with oid [" +
                        string.Join(", ", oidCurve) +
                        "]. Only the following are available: " +
                        string.Join(", ", supportedCurves)
                    );
                }

                CurveFp curve = Curves.curvesByOid[stringOid];

                if (privateKeyStr.Length < curve.length())
                {
                    int length = curve.length() - privateKeyStr.Length;
                    string padding = "";
                    for (int i = 0; i < length; i++)
                    {
                        padding += "00";
                    }
                    privateKeyStr = Notus.Core.Wallet.Function.Der_combineByteArrays(new List<byte[]> { Notus.Core.Wallet.Function.BinaryAscii_binaryFromHex(padding), privateKeyStr });
                }

                return fromString(privateKeyStr, curve.name);

            }

            public static PrivateKey fromString(byte[] str, string curve = "secp256k1")
            {
                return new PrivateKey(curve, Notus.Core.Wallet.Function.BinaryAscii_numberFromString(str));
            }
        }


    }
}

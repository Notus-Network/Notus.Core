﻿
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
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Notus.Wallet
{
    /// <summary>
    /// A helper class related to wallet IDs
    /// </summary>
    public class ID
    {
        public static Notus.Variable.Enum.NetworkType GetNetworkType(string walletId)
        {
            
            if (
                walletId.Length == Notus.Variable.Constant.SingleWalletTextLength
                ||
                walletId.Length == Notus.Variable.Constant.MultiWalletTextLength
            )
            {
                if (
                    string.Equals(
                        walletId.Substring(0, Notus.Variable.Constant.SingleWalletPrefix_MainNetwork.Length),
                        Notus.Variable.Constant.SingleWalletPrefix_MainNetwork
                    )
                    ||
                    string.Equals(
                        walletId.Substring(0, Notus.Variable.Constant.MultiWalletPrefix_MainNetwork.Length),
                        Notus.Variable.Constant.MultiWalletPrefix_MainNetwork
                    )
                )
                {
                    return Notus.Variable.Enum.NetworkType.MainNet;
                }
                
                if (
                    string.Equals(
                        walletId.Substring(0, Notus.Variable.Constant.SingleWalletPrefix_TestNetwork.Length),
                        Notus.Variable.Constant.SingleWalletPrefix_TestNetwork
                    )
                    ||
                    string.Equals(
                        walletId.Substring(0, Notus.Variable.Constant.MultiWalletPrefix_TestNetwork.Length),
                        Notus.Variable.Constant.MultiWalletPrefix_TestNetwork
                    )
                )
                {
                    return Notus.Variable.Enum.NetworkType.TestNet;
                }
                if (
                    string.Equals(
                        walletId.Substring(0, Notus.Variable.Constant.SingleWalletPrefix_DevelopmentNetwork.Length),
                        Notus.Variable.Constant.SingleWalletPrefix_DevelopmentNetwork
                    )
                    ||
                    string.Equals(
                        walletId.Substring(0, Notus.Variable.Constant.MultiWalletPrefix_DevelopmentNetwork.Length),
                        Notus.Variable.Constant.MultiWalletPrefix_DevelopmentNetwork
                    )
                )
                {
                    return Notus.Variable.Enum.NetworkType.DevNet;
                }

            }

            return Notus.Variable.Enum.NetworkType.Unknown;
        }
        /// <summary>
        /// Verifies data and returns verify status
        /// </summary>
        /// <param name="messageData">Message that will verify</param>
        /// <param name="signHex">Sign Hex <see cref="string"/></param>
        /// <param name="publicKey">Public Key Hex <see cref="string"/></param>
        /// <returns>Returns Result of the Verification.</returns>
        public static bool Verify(string messageData, string signHex, string publicKey)
        {
            return Verify_SubFunction(messageData, signHex, publicKey, Notus.Variable.Constant.Default_EccCurveName);
        }

        /// <summary>
        /// Verifies data and returns verify status with given Curve Name
        /// </summary>
        /// <param name="messageData">Message that will verify</param>
        /// <param name="signHex">Sign Hex <see cref="string"/></param>
        /// <param name="publicKey">Public Key Hex <see cref="string"/></param>
        /// <param name="curveName">Curve Type <see cref="string"/></param>
        /// <returns>Returns Result of the Verification.</returns>
        public static bool Verify(string messageData, string signHex, string publicKey, string curveName)
        {
            return Verify_SubFunction(messageData, signHex, publicKey, curveName);
        }
        private static bool Verify_SubFunction(string messageData, string signHex, string publicKey, string curveName)
        {
            bool verifyResult = false;
            try
            {
                PublicKey yPubKey = PublicKey.fromString(
                    Notus.Convert.Hex2Byte(
                        CheckPublicKeyPrefix(
                            publicKey
                        )
                    ),
                    curveName,
                    true
                );
                verifyResult = Ecdsa.verify(
                    messageData,
                    Signature.fromBase64(
                        System.Convert.ToBase64String(Notus.Convert.Hex2Byte(signHex))),
                    yPubKey
                );
            }
            catch (Exception err)
            {
                Console.WriteLine("Error Text [8cfe9085] : " + err.Message);
            }
            return verifyResult;
        }

        /// <summary>
        /// Signs data and with Private Key <see cref="string"/>
        /// </summary>
        /// <param name="messageData">Message that will verify</param>
        /// <param name="privateKey">Private Key <see cref="string"/></param>
        /// <returns>Returns Result of the Signing as <see cref="string"/>.</returns>
        public static string Sign(string messageData, string privateKey)
        {
            return Sign_SubFunction(messageData, privateKey, Notus.Variable.Constant.Default_EccCurveName);
        }

        /// <summary>
        /// Signs data and with Private Key <see cref="string"/> with given Curve Name
        /// </summary>
        /// <param name="messageData">Message that will verify</param>
        /// <param name="privateKey">Private Key <see cref="string"/></param>
        /// <param name="curveName">Curve Type <see cref="string"/></param>
        /// <returns>Returns Result of the Signing as <see cref="string"/>.</returns>
        public static string Sign(string messageData, string privateKey, string curveName)
        {
            return Sign_SubFunction(messageData, privateKey, curveName);
        }
        private static string Sign_SubFunction(string messageData, string privateKey, string curveName)
        {
            PrivateKey yPrivKey;
            if (curveName == "secp256k1")
            {
                yPrivKey = new PrivateKey("secp256k1", Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(privateKey));
            }
            else
            {
                yPrivKey = new PrivateKey("p256", Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(privateKey));
            }
            Signature signObj = Ecdsa.sign(messageData, yPrivKey);
            return Notus.Convert.Byte2Hex(
                System.Convert.FromBase64String(
                    signObj.toBase64()
                )
            );
        }

        /// <summary>
        /// Returns the wallet address created with the entered Private Key
        /// </summary>
        /// <param name="privateKey">Private Key <see cref="string"/></param>
        /// <param name="WhichNetworkFor">Current Network for Request (optional).</param>
        /// <param name="CurveName">Current curve (optional).</param>
        /// <returns>Returns Wallet Address</returns>
        public static string GetAddress_StandartWay(string privateKey, Notus.Variable.Enum.NetworkType WhichNetworkFor = Notus.Variable.Enum.NetworkType.MainNet, string CurveName = "secp256k1")
        {
            PrivateKey yPrivKey = new PrivateKey(CurveName, Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(privateKey));
            PublicKey yPubKey = yPrivKey.publicKey();
            BigInteger pkPointVal = yPubKey.point.x;
            string publicKeyX = (yPubKey.point.y % 2 == 0 ? "02" : "03") + pkPointVal.ToString("x");
            string networkByteStr = "00";
            if (WhichNetworkFor == Notus.Variable.Enum.NetworkType.TestNet)
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

            string walletAddressStr = Notus.Wallet.Toolbox.EncodeBase58(number);
            return walletAddressStr;
        }

        /// <summary>
        /// Checks if wallet adress is correct or not.
        /// </summary>
        /// <param name="walletAddress">Wallet Address <see cref="string"/></param>
        /// <param name="WhichNetworkFor">Current Network for Request (optional).</param>
        /// <returns>Returns true if wallet address is correct. Returns false if wallet address is incorrect.</returns>
        public static bool CheckAddress(string walletAddress, Notus.Variable.Enum.NetworkType WhichNetworkFor = Notus.Variable.Enum.NetworkType.MainNet)
        {
            if (walletAddress == null)
            {
                return false;
            }
            if (walletAddress.Length < 5)
            {
                return false;
            }


            if (WhichNetworkFor == Notus.Variable.Enum.NetworkType.TestNet)
            {
                if (walletAddress.Length == Notus.Variable.Constant.SingleWalletTextLength)
                {
                    if (walletAddress.Substring(0, Notus.Variable.Constant.SingleWalletPrefix_TestNetwork.Length) == Notus.Variable.Constant.SingleWalletPrefix_TestNetwork)
                    {
                        return true;
                    }
                }
                if (walletAddress.Length == Notus.Variable.Constant.MultiWalletTextLength)
                {
                    if (walletAddress.Substring(0, Notus.Variable.Constant.MultiWalletPrefix_TestNetwork.Length) == Notus.Variable.Constant.MultiWalletPrefix_TestNetwork)
                    {
                        return true;
                    }
                }
            }

            if (WhichNetworkFor == Notus.Variable.Enum.NetworkType.MainNet)
            {
                if (walletAddress.Length == Notus.Variable.Constant.SingleWalletTextLength)
                {
                    if (walletAddress.Substring(0, Notus.Variable.Constant.SingleWalletPrefix_MainNetwork.Length) == Notus.Variable.Constant.SingleWalletPrefix_MainNetwork)
                    {
                        return true;
                    }
                }
                if (walletAddress.Length == Notus.Variable.Constant.MultiWalletTextLength)
                {
                    if (walletAddress.Substring(0, Notus.Variable.Constant.MultiWalletPrefix_MainNetwork.Length) == Notus.Variable.Constant.MultiWalletPrefix_MainNetwork)
                    {
                        return true;
                    }
                }
            }

            if (WhichNetworkFor == Notus.Variable.Enum.NetworkType.DevNet)
            {
                if (walletAddress.Length == Notus.Variable.Constant.SingleWalletTextLength)
                {
                    if (walletAddress.Substring(0, Notus.Variable.Constant.SingleWalletPrefix_DevelopmentNetwork.Length) == Notus.Variable.Constant.SingleWalletPrefix_DevelopmentNetwork)
                    {
                        return true;
                    }
                }
                if (walletAddress.Length == Notus.Variable.Constant.SingleWalletTextLength)
                {
                    if (walletAddress.Substring(0, Notus.Variable.Constant.MultiWalletPrefix_DevelopmentNetwork.Length) == Notus.Variable.Constant.MultiWalletPrefix_DevelopmentNetwork)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private static string CheckPublicKeyPrefix(string publicKeyHex)
        {
            if (publicKeyHex.Length == 130 && publicKeyHex.Substring(0, 2) == "04")
            {
                return publicKeyHex.Substring(2);
            }
            return publicKeyHex;
        }
        /// <summary>
        /// Returns wallet key via given public key.
        /// </summary>
        /// <param name="publicKey">Public Key <see cref="string"/></param>
        /// <param name="WhichNetworkFor">Current Network for Request.</param>
        /// <returns>Returns Wallet Address</returns>
        public static string GetAddressWithPublicKey(string publicKey, Notus.Variable.Enum.NetworkType WhichNetworkFor)
        {
            return GetAddress_SubFunction_FromPublicKey(
                PublicKey.fromString(
                    Notus.Convert.Hex2Byte(
                        CheckPublicKeyPrefix(publicKey)
                    ),
                    Notus.Variable.Constant.Default_EccCurveName,
                    true
                ), WhichNetworkFor, Notus.Variable.Constant.Default_EccCurveName);
        }

        /// <summary>
        /// Returns wallet key via given public key.
        /// </summary>
        /// <param name="publicKey">Public Key <see cref="string"/></param>
        /// <param name="WhichNetworkFor">Current Network for Request.</param>
        /// <param name="CurveName">Current curve.</param>
        /// <returns>Returns Wallet Address</returns>
        public static string GetAddressWithPublicKey(
            string publicKey, 
            Notus.Variable.Enum.NetworkType WhichNetworkFor, 
            string CurveName
        )
        {
            return GetAddress_SubFunction_FromPublicKey(
                PublicKey.fromString(
                    Notus.Convert.Hex2Byte(
                        CheckPublicKeyPrefix(
                            publicKey
                        )
                    ), 
                    CurveName, 
                    true
                ), 
                WhichNetworkFor, 
                CurveName
            );
        }

        /// <summary>
        /// Returns wallet address via given private key.
        /// </summary>
        /// <param name="privateKey">Private Key <see cref="string"/></param>
        /// <param name="WhichNetworkFor">Current Network for Request.</param>
        /// <returns>Returns Wallet Address</returns>
        public static string GetAddress(
            string privateKey, 
            Notus.Variable.Enum.NetworkType WhichNetworkFor
        )
        {
            return GetAddress_SubFunction(
                privateKey, 
                WhichNetworkFor, 
                Notus.Variable.Constant.Default_EccCurveName
            );
        }

        /// <summary>
        /// Returns wallet address via given private key.
        /// </summary>
        /// <param name="privateKey">Private Key <see cref="string"/></param>
        /// <param name="WhichNetworkFor">Current Network for Request.</param>
        /// <param name="CurveName">Current curve.</param>
        /// <returns>Returns Wallet Address</returns>
        public static string GetAddress(
            string privateKey,
            Notus.Variable.Enum.NetworkType WhichNetworkFor,
            string CurveName
        )
        {
            return GetAddress_SubFunction(
                privateKey, 
                WhichNetworkFor, 
                CurveName
            );
        }

        private static string GetAddress_SubFunction(
            string privateKey,
            Notus.Variable.Enum.NetworkType WhichNetworkFor,
            string CurveName = "secp256k1"
        )
        {
            PrivateKey yPrivKey = new PrivateKey(CurveName, Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(privateKey));
            PublicKey yPubKey = yPrivKey.publicKey();
            return GetAddress_SubFunction_FromPublicKey(
                yPubKey,
                WhichNetworkFor,
                CurveName
            );
        }

        private static string GetAddress_SubFunction_FromPublicKey(
            PublicKey yPubKey,
            Notus.Variable.Enum.NetworkType WhichNetworkFor,
            //string CurveName = "secp256k1"
            string CurveName = Notus.Variable.Constant.Default_EccCurveName
        )
        {
            
            //BigInteger pkPointVal = yPubKey.point.x;
            string pubXval = yPubKey.point.x.ToString("x").PadLeft(64,'0');
            string pubYval = yPubKey.point.y.ToString("x").PadLeft(64, '0');
            if (pubXval.Length == 65 && pubXval[0] == '0')
            {
                pubXval = pubXval.Substring(1);
            }
            if (pubYval.Length == 65 && pubYval[0] == '0')
            {
                pubYval = pubYval.Substring(1);
            }
            string fullPublicKey = pubXval+ pubYval;
            /*
            Console.WriteLine("pubXval : " + pubXval);
            Console.WriteLine("pubYval : " + pubYval);
            Console.WriteLine("pubXval : " + pubXval.Length.ToString());
            Console.WriteLine("pubYval : " + pubYval.Length.ToString());
            Console.WriteLine("fullPublicKey      : " + fullPublicKey);

pubCorsX : 0000f5d044d93197f31a147b6a4e373b21d36b9948b3f37349f1afd00c372485
              0f5d044d93197f31a147b6a4e373b21d36b9948b3f37349f1afd00c372485
pubCorsY : 03ad8a16c44c21e26806261b1dfb4b5bcd2e6ffea30f2504a40c8685c2551269
            3ad8a16c44c21e26806261b1dfb4b5bcd2e6ffea30f2504a40c8685c2551269
pubCorsMain: 030000f5d044d93197f31a147b6a4e373b21d36b9948b3f37349f1afd00c372485
             030000f5d044d93197f31a147b6a4e373b21d36b9948b3f37349f1afd00c372485
hashPubKeyStr : 7ba5c091090c84ebb093d2fae98acabe3176ca23d7ed
checkSumStr : 451829de
walletAddress : EDAqkHFMPF8v9thkjbzCazUf6uDYzPHHdRXK
             NTSEDAqkHFMPF8v9thkjbzCazUf6uDYzPHHdRXK
            Console.WriteLine(fullPublicKey.Length);
             */
            //string publicKeyX = (yPubKey.point.y % 2 == 0 ? "02" : "03") + pkPointVal.ToString("x");
            string publicKeyX = (yPubKey.point.y % 2 == 0 ? "02" : "03") + pubXval;

            string keyPrefix = Notus.Variable.Constant.SingleWalletPrefix_MainNetwork;
            string networkByteStr = "10";

            if (WhichNetworkFor == Notus.Variable.Enum.NetworkType.TestNet)
            {
                keyPrefix = Notus.Variable.Constant.SingleWalletPrefix_TestNetwork;
                networkByteStr = "20";
            }

            if (WhichNetworkFor == Notus.Variable.Enum.NetworkType.DevNet)
            {
                keyPrefix = Notus.Variable.Constant.SingleWalletPrefix_DevelopmentNetwork;
                networkByteStr = "30";
            }
            //Console.WriteLine("publicKeyX : " + publicKeyX);
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            string hashPubKeyStr = Notus.Toolbox.Text.ShrinkHex(
                sashaObj.Calculate(
                    sashaObj.Calculate(publicKeyX)
                ), 22
            );
            //Console.WriteLine("networkByteStr + fullPublicKey : " + networkByteStr + fullPublicKey);
            string checkSumStr = Notus.Toolbox.Text.ShrinkHex(
                sashaObj.Calculate(
                    sashaObj.Calculate(networkByteStr + fullPublicKey)
                ), 4
            );

            BigInteger number = BigInteger.Parse(
                networkByteStr + hashPubKeyStr + checkSumStr,
                NumberStyles.AllowHexSpecifier);

            string walletAddressStr = Notus.Wallet.Toolbox.EncodeBase58(number, Notus.Variable.Constant.WalletEncodeTextLength);
            return keyPrefix + walletAddressStr;
        }

        /// <summary>
        /// Returns private key via given word list <see cref="string"/>[]
        /// </summary>
        /// <param name="WordList">Word List to create private key</param>
        /// <returns>Returns Private Key</returns>
        public static string PrivateKeyFromWordList(string[] WordList)
        {
            BigInteger PrivateKeySeedNumber = PrivateKeyFromPassPhrase(WordList);
            string privateHexStr = New(Notus.Variable.Constant.Default_EccCurveName, PrivateKeySeedNumber);
            return privateHexStr;
        }

        /// <inheritdoc cref="PrivateKeyFromWordList(string[])"/>
        public static BigInteger PrivateKeyFromPassPhrase(string[] WordList)
        {
            if (WordList.Length != 16) { return 0; }
            string[] tmpWordList = new string[Notus.Variable.Constant.Default_WordListArrayCount];
            string fullWordLine = "";
            for (int i = 0; i < Notus.Variable.Constant.Default_WordListArrayCount; i++)
            {
                if (i == 0)
                {
                    fullWordLine = WordList[i] + Notus.Variable.Constant.CommonDelimeterChar;
                }
                else
                {
                    fullWordLine = fullWordLine + WordList[i];
                    if (Notus.Variable.Constant.Default_WordListArrayCount - 1 > i)
                    {
                        fullWordLine = fullWordLine + Notus.Variable.Constant.CommonDelimeterChar;
                    }
                }
                tmpWordList[i] = new Notus.Hash().CommonHash("md5", WordList[i]);
            }
            fullWordLine = new Notus.Hash().CommonHash("sasha",
                new Notus.Hash().CommonHash("sasha",
                    fullWordLine
                ) +
                Notus.Variable.Constant.CommonDelimeterChar +
                fullWordLine
            );
            string hexResultStr = "";
            for (int i = 0; i < Notus.Variable.Constant.Default_WordListArrayCount; i++)
            {
                tmpWordList[i] = Notus.Toolbox.Text.ShrinkHex(
                    new Notus.Hash().CommonHash("sha1",
                        fullWordLine +
                        Notus.Variable.Constant.CommonDelimeterChar +
                        tmpWordList[i]
                    )
                , 2);
                hexResultStr = hexResultStr + tmpWordList[i];
            }
            return BigInteger.Parse("0" + hexResultStr, NumberStyles.AllowHexSpecifier);
        }

        /// <summary>
        /// Generates a new <see cref="Notus.Variable.Struct.EccKeyPair"/>
        /// </summary>
        /// <returns>Returns Wallet Key Pair</returns>
        public static Notus.Variable.Struct.EccKeyPair GenerateKeyPair()
        {
            return GenerateKeyPair(Notus.Variable.Constant.Default_EccCurveName, Notus.Variable.Enum.NetworkType.MainNet);
        }

        /// <summary>
        /// Generates a new <see cref="Notus.Variable.Struct.EccKeyPair"/> via given curve name.
        /// </summary>
        /// <param name="curveName">Current curve.</param>
        /// <returns>Returns Wallet Key Pair</returns>
        public static Notus.Variable.Struct.EccKeyPair GenerateKeyPair(string curveName)
        {
            return GenerateKeyPair(curveName, Notus.Variable.Enum.NetworkType.MainNet);
        }

        /// <summary>
        /// Generates a new <see cref="Notus.Variable.Struct.EccKeyPair"/> via given network.
        /// </summary>
        /// <param name="WhichNetworkFor">Current Network for Request.</param>
        /// <returns>Returns Wallet Key Pair</returns>
        public static Notus.Variable.Struct.EccKeyPair GenerateKeyPair(Notus.Variable.Enum.NetworkType WhichNetworkFor)
        {
            return GenerateKeyPair(Notus.Variable.Constant.Default_EccCurveName, WhichNetworkFor);
        }

        /// <summary>
        /// Generates a new <see cref="Notus.Variable.Struct.EccKeyPair"/> via given curve name and network.
        /// </summary>
        /// <param name="curveName">Current curve.</param>
        /// <param name="WhichNetworkFor">Current Network for Request.</param>
        /// <returns>Returns Wallet Key Pair</returns>
        public static Notus.Variable.Struct.EccKeyPair GenerateKeyPair(string curveName, Notus.Variable.Enum.NetworkType WhichNetworkFor)
        {
            if (curveName == "")
            {
                curveName = Notus.Variable.Constant.Default_EccCurveName;
            }
            string[] WordList = Notus.Wallet.Toolbox.SeedPhraseList();
            BigInteger PrivateKeySeedNumber = PrivateKeyFromPassPhrase(WordList);
            string privateHexStr = New(curveName, PrivateKeySeedNumber);
            return new Notus.Variable.Struct.EccKeyPair()
            {
                CurveName = curveName,
                Words = WordList,
                PrivateKey = privateHexStr,
                PublicKey = Generate(privateHexStr, curveName),
                WalletKey = GetAddress(privateHexStr, WhichNetworkFor, curveName)
            };
        }

        public static string GetPublicKeyFromPrivateKey(string privateKey, string curveName = Notus.Variable.Constant.Default_EccCurveName)
        {
            return Generate(privateKey, curveName);
        }
        /// <summary>
        /// Returns public key via given private key.
        /// </summary>
        /// <param name="privateKey">Private Key <see cref="string"/></param>
        /// <param name="curveName">Current curve.</param>
        /// <returns>Returns Public Key</returns>
        public static string Generate(string privateKey, string curveName = Notus.Variable.Constant.Default_EccCurveName)
        {
            PrivateKey privKey;
            if (curveName.ToLower() == "secp256k1")
            {
                privKey = new PrivateKey("secp256k1", Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(privateKey));
            }
            else
            {
                privKey = new PrivateKey("p256", Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(privateKey));
            }
            PublicKey yPubKey = privKey.publicKey();
            return Notus.Convert.Byte2Hex(yPubKey.toString(false)).ToLower();
        }

        /// <summary>
        /// Generates a new Private Key via given curve name and seed
        /// </summary>
        /// <param name="curveName">Current curve.</param>
        /// <param name="PrivateKeySeedValue">Seed to be used</param>
        /// <returns>Returns Private Key</returns>
        public static string New(string curveName = Notus.Variable.Constant.Default_EccCurveName, BigInteger? PrivateKeySeedValue = null)
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
                return Notus.Wallet.Toolbox.Integer_modulo(
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
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("0000000000000000000000000000000000000000000000000000000000000000"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("0000000000000000000000000000000000000000000000000000000000000007"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8"),
                "secp256k1",
                new int[] { 1, 3, 132, 0, 10 }
            );

            public static CurveFp prime256v1 = new CurveFp(
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("ffffffff00000001000000000000000000000000fffffffffffffffffffffffc"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("5ac635d8aa3a93e7b3ebbd55769886bc651d06b0cc53b0f63bce3c3e27d2604b"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("ffffffff00000001000000000000000000000000ffffffffffffffffffffffff"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("ffffffff00000000ffffffffffffffffbce6faada7179e84f3b9cac2fc632551"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("6b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296"),
                Notus.Wallet.Toolbox.BinaryAscii_numberFromHex("4fe342e2fe1a7f9b8ee7eb4a7c0f9e162bce33576b315ececbb6406837bf51f5"),
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
                byte[] xString = Notus.Wallet.Toolbox.BinaryAscii_stringFromNumber(point.x, curve.length());
                byte[] yString = Notus.Wallet.Toolbox.BinaryAscii_stringFromNumber(point.y, curve.length());

                if (encoded)
                {
                    return Notus.Wallet.Toolbox.Der_combineByteArrays(new List<byte[]> {
                        Notus.Wallet.Toolbox.BinaryAscii_binaryFromHex("00"),
                        Notus.Wallet.Toolbox.BinaryAscii_binaryFromHex("04"),
                        xString,
                        yString
                    });
                }
                return Notus.Wallet.Toolbox.Der_combineByteArrays(new List<byte[]> {
                xString,
                yString
            });
            }

            public byte[] toDer()
            {
                int[] oidEcPublicKey = { 1, 2, 840, 10045, 2, 1 };
                byte[] encodedEcAndOid = Notus.Wallet.Toolbox.Der_encodeSequence(
                    new List<byte[]> {
                    Notus.Wallet.Toolbox.Der_encodeOid(oidEcPublicKey),
                    Notus.Wallet.Toolbox.Der_encodeOid(curve.oid)
                    }
                );

                return Notus.Wallet.Toolbox.Der_encodeSequence(
                    new List<byte[]> {
                    encodedEcAndOid,
                    Notus.Wallet.Toolbox.Der_encodeBitString(toString(true))
                    }
                );
            }

            public string toPem()
            {
                return Notus.Wallet.Toolbox.Der_toPem(toDer(), "PUBLIC KEY");
            }


            public static PublicKey fromPem(string pem)
            {
                return fromDer(Notus.Wallet.Toolbox.Der_fromPem(pem));
            }

            public static PublicKey fromDer(byte[] der)
            {
                Tuple<byte[], byte[]> removeSequence1 = Notus.Wallet.Toolbox.Der_removeSequence(der);
                byte[] s1 = removeSequence1.Item1;

                if (removeSequence1.Item2.Length > 0)
                {
                    throw new ArgumentException(
                        "trailing junk after DER public key: " +
                        Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(removeSequence1.Item2)
                    );
                }

                Tuple<byte[], byte[]> removeSequence2 = Notus.Wallet.Toolbox.Der_removeSequence(s1);
                byte[] s2 = removeSequence2.Item1;
                byte[] pointBitString = removeSequence2.Item2;

                Tuple<int[], byte[]> removeObject1 = Notus.Wallet.Toolbox.Der_removeObject(s2);
                byte[] rest = removeObject1.Item2;

                Tuple<int[], byte[]> removeObject2 = Notus.Wallet.Toolbox.Der_removeObject(rest);
                int[] oidCurve = removeObject2.Item1;

                if (removeObject2.Item2.Length > 0)
                {
                    throw new ArgumentException(
                        "trailing junk after DER public key objects: " +
                        Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(removeObject2.Item2)
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

                Tuple<byte[], byte[]> removeBitString = Notus.Wallet.Toolbox.Der_removeBitString(pointBitString);
                byte[] pointString = removeBitString.Item1;

                if (removeBitString.Item2.Length > 0)
                {
                    throw new ArgumentException("trailing junk after public key point-string");
                }

                return fromString(Notus.Wallet.Toolbox.Bytes_sliceByteArray(pointString, 2), curve.name);

            }

            public static PublicKey fromString(byte[] str, string curve = "secp256k1", bool validatePoint = true)
            {
                CurveFp curveObject = Curves.getCurveByName(curve);

                int baseLen = curveObject.length();

                if (str.Length != 2 * baseLen)
                {
                    throw new ArgumentException("string length [" + str.Length + "] should be " + 2 * baseLen);
                }

                string xs = Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(Notus.Wallet.Toolbox.Bytes_sliceByteArray(str, 0, baseLen));
                string ys = Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(Notus.Wallet.Toolbox.Bytes_sliceByteArray(str, baseLen));

                CurvePointStruct p = new CurvePointStruct(
                    Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(xs),
                    Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(ys)
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
                List<byte[]> sequence = new List<byte[]> { Notus.Wallet.Toolbox.Der_encodeInteger(r), Notus.Wallet.Toolbox.Der_encodeInteger(s) };
                return Notus.Wallet.Toolbox.Der_encodeSequence(sequence);
            }

            public string toBase64()
            {
                return Notus.Wallet.Toolbox.Base64_encode(toDer());
            }

            public static Signature fromDer(byte[] bytes)
            {
                Tuple<byte[], byte[]> removeSequence = Notus.Wallet.Toolbox.Der_removeSequence(bytes);
                byte[] rs = removeSequence.Item1;
                byte[] removeSequenceTrail = removeSequence.Item2;

                if (removeSequenceTrail.Length > 0)
                {
                    throw new ArgumentException("trailing junk after DER signature: " + Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(removeSequenceTrail));
                }

                Tuple<BigInteger, byte[]> removeInteger = Notus.Wallet.Toolbox.Der_removeInteger(rs);
                BigInteger r = removeInteger.Item1;
                byte[] rest = removeInteger.Item2;

                removeInteger = Notus.Wallet.Toolbox.Der_removeInteger(rest);
                BigInteger s = removeInteger.Item1;
                byte[] removeIntegerTrail = removeInteger.Item2;

                if (removeIntegerTrail.Length > 0)
                {
                    throw new ArgumentException("trailing junk after DER numbers: " + Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(removeIntegerTrail));
                }

                return new Signature(r, s);

            }

            public static Signature fromBase64(string str)
            {
                return fromDer(Notus.Wallet.Toolbox.Base64_decode(str));
            }

        }
        private static class Ecdsa
        {

            public static Signature sign(string message, PrivateKey privateKey)
            {
                string hashMessage = hashCalculate(message);
                BigInteger numberMessage = Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(hashMessage);

                //Console.WriteLine("hashMessage [sign]: " + hashMessage);
                //Console.WriteLine("numberMessage.ToString() : " + numberMessage.ToString());

                CurveFp curve = privateKey.curve;

                //javascript ile senkron yapmak için random değeri 1 ile sabitlendi
                //javascript ile senkron yapmak için random değeri 1 ile sabitlendi
                //javascript ile senkron yapmak için random değeri 1 ile sabitlendi
                //javascript ile senkron yapmak için random değeri 1 ile sabitlendi
                //javascript ile senkron yapmak için random değeri 1 ile sabitlendi
                BigInteger randNum = Notus.Wallet.Toolbox.Integer_randomBetween(BigInteger.One, curve.N - 1);
                //BigInteger randNum = BigInteger.Parse("1");
                CurvePointStruct randSignPoint = EcdsaMath.multiply(curve.G, randNum, curve.N, curve.A, curve.P);
                BigInteger r = Notus.Wallet.Toolbox.Integer_modulo(
                    randSignPoint.x, 
                    curve.N
                );
                BigInteger s = Notus.Wallet.Toolbox.Integer_modulo(
                    (numberMessage + r * privateKey.secret) * (EcdsaMath.inv(randNum, curve.N)), 
                    curve.N
                );

                return new Signature(r, s);
            }

            public static bool verify(string message, Signature signature, PublicKey publicKey)
            {
                string hashMessage = hashCalculate(message);
                //Console.WriteLine("hashMessage [verify]: " + hashMessage);

                BigInteger numberMessage = Notus.Wallet.Toolbox.BinaryAscii_numberFromHex(hashMessage);
                CurveFp curve = publicKey.curve;
                BigInteger sigR = signature.r;
                BigInteger sigS = signature.s;
                BigInteger inv = EcdsaMath.inv(sigS, curve.N);

                CurvePointStruct u1 = EcdsaMath.multiply(
                    curve.G,
                    Notus.Wallet.Toolbox.Integer_modulo((numberMessage * inv), curve.N),
                    curve.N,
                    curve.A,
                    curve.P
                );
                CurvePointStruct u2 = EcdsaMath.multiply(
                    publicKey.point,
                    Notus.Wallet.Toolbox.Integer_modulo((sigR * inv), curve.N),
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
                BigInteger low = Notus.Wallet.Toolbox.Integer_modulo(x, n);
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

                return Notus.Wallet.Toolbox.Integer_modulo(lm, n);

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
                    Notus.Wallet.Toolbox.Integer_modulo(p.x * BigInteger.Pow(z, 2), P),
                    Notus.Wallet.Toolbox.Integer_modulo(p.y * BigInteger.Pow(z, 3), P)
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

                BigInteger ysq = Notus.Wallet.Toolbox.Integer_modulo(
                    BigInteger.Pow(p.y, 2),
                    P
                );
                BigInteger S = Notus.Wallet.Toolbox.Integer_modulo(
                    4 * p.x * ysq,
                    P
                );
                BigInteger M = Notus.Wallet.Toolbox.Integer_modulo(
                    3 * BigInteger.Pow(p.x, 2) + A * BigInteger.Pow(p.z, 4),
                    P
                );

                BigInteger nx = Notus.Wallet.Toolbox.Integer_modulo(
                    BigInteger.Pow(M, 2) - 2 * S,
                    P
                );
                BigInteger ny = Notus.Wallet.Toolbox.Integer_modulo(
                    M * (S - nx) - 8 * BigInteger.Pow(ysq, 2),
                    P
                );
                BigInteger nz = Notus.Wallet.Toolbox.Integer_modulo(
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

                BigInteger U1 = Notus.Wallet.Toolbox.Integer_modulo(
                    p.x * BigInteger.Pow(q.z, 2),
                    P
                );
                BigInteger U2 = Notus.Wallet.Toolbox.Integer_modulo(
                    q.x * BigInteger.Pow(p.z, 2),
                    P
                );
                BigInteger S1 = Notus.Wallet.Toolbox.Integer_modulo(
                    p.y * BigInteger.Pow(q.z, 3),
                    P
                );
                BigInteger S2 = Notus.Wallet.Toolbox.Integer_modulo(
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
                BigInteger H2 = Notus.Wallet.Toolbox.Integer_modulo(H * H, P);
                BigInteger H3 = Notus.Wallet.Toolbox.Integer_modulo(H * H2, P);
                BigInteger U1H2 = Notus.Wallet.Toolbox.Integer_modulo(U1 * H2, P);
                BigInteger nx = Notus.Wallet.Toolbox.Integer_modulo(
                    BigInteger.Pow(R, 2) - H3 - 2 * U1H2,
                    P
                );
                BigInteger ny = Notus.Wallet.Toolbox.Integer_modulo(
                    R * (U1H2 - nx) - S1 * H3,
                    P
                );
                BigInteger nz = Notus.Wallet.Toolbox.Integer_modulo(
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
                        Notus.Wallet.Toolbox.Integer_modulo(n, N),
                        N,
                        A,
                        P
                    );
                }

                if (Notus.Wallet.Toolbox.Integer_modulo(n, 2).IsZero)
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
                    secret = Notus.Wallet.Toolbox.Integer_randomBetween(1, this.curve.N - 1);
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
                return Notus.Wallet.Toolbox.BinaryAscii_hexFromNumber(secret, curve.length());
            }
            public byte[] toString()
            {
                return Notus.Wallet.Toolbox.BinaryAscii_stringFromNumber(secret, curve.length());
            }

            public byte[] toDer()
            {
                byte[] encodedPublicKey = publicKey().toString(true);

                return Notus.Wallet.Toolbox.Der_encodeSequence(
                    new List<byte[]> {
                    Notus.Wallet.Toolbox.Der_encodeInteger(1),
                    Notus.Wallet.Toolbox.Der_encodeOctetString(toString()),
                    Notus.Wallet.Toolbox.Der_encodeConstructed(0, Notus.Wallet.Toolbox.Der_encodeOid(curve.oid)),
                    Notus.Wallet.Toolbox.Der_encodeConstructed(1, encodedPublicKey)
                    }
                );
            }

            public string toPem()
            {
                return Notus.Wallet.Toolbox.Der_toPem(toDer(), "EC PRIVATE KEY");
            }

            public static PrivateKey fromPem(string str)
            {
                string[] split = str.Split(new string[] { "-----BEGIN EC PRIVATE KEY-----" }, StringSplitOptions.None);

                if (split.Length != 2)
                {
                    throw new ArgumentException("invalid PEM");
                }

                return fromDer(Notus.Wallet.Toolbox.Der_fromPem(split[1]));
            }

            public static PrivateKey fromDer(byte[] der)
            {
                Tuple<byte[], byte[]> removeSequence = Notus.Wallet.Toolbox.Der_removeSequence(der);
                if (removeSequence.Item2.Length > 0)
                {
                    throw new ArgumentException("trailing junk after DER private key: " + Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(removeSequence.Item2));
                }

                Tuple<BigInteger, byte[]> removeInteger = Notus.Wallet.Toolbox.Der_removeInteger(removeSequence.Item1);
                if (removeInteger.Item1 != 1)
                {
                    throw new ArgumentException("expected '1' at start of DER private key, got " + removeInteger.Item1.ToString());
                }

                Tuple<byte[], byte[]> removeOctetString = Notus.Wallet.Toolbox.Der_removeOctetString(removeInteger.Item2);
                byte[] privateKeyStr = removeOctetString.Item1;

                Tuple<int, byte[], byte[]> removeConstructed = Notus.Wallet.Toolbox.Der_removeConstructed(removeOctetString.Item2);
                int tag = removeConstructed.Item1;
                byte[] curveOidString = removeConstructed.Item2;
                if (tag != 0)
                {
                    throw new ArgumentException("expected tag 0 in DER private key, got " + tag.ToString());
                }

                Tuple<int[], byte[]> removeObject = Notus.Wallet.Toolbox.Der_removeObject(curveOidString);
                int[] oidCurve = removeObject.Item1;
                if (removeObject.Item2.Length > 0)
                {
                    throw new ArgumentException(
                        "trailing junk after DER private key curve_oid: " +
                        Notus.Wallet.Toolbox.BinaryAscii_hexFromBinary(removeObject.Item2)
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
                    privateKeyStr = Notus.Wallet.Toolbox.Der_combineByteArrays(new List<byte[]> { Notus.Wallet.Toolbox.BinaryAscii_binaryFromHex(padding), privateKeyStr });
                }

                return fromString(privateKeyStr, curve.name);

            }

            public static PrivateKey fromString(byte[] str, string curve = "secp256k1")
            {
                return new PrivateKey(curve, Notus.Wallet.Toolbox.BinaryAscii_numberFromString(str));
            }
        }
    }
}

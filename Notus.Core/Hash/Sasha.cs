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
using System.Linq;
using System.Text;

namespace Notus.HashLib
{
    /// <summary>
    /// Helper methods for Sasha hashing.
    /// </summary>
    public class Sasha
    {
        private readonly string SimpleHashAlphabetForHexResult = "fedcba9876543210";
        private readonly string SimpleKeyTextForSign = "sasha-key-text";
        private readonly string SimpleHashAlphabetForSign = "zyxwvutsrqponmlkjihgfedcba987654321";

        private bool KeyEquals = true;
        public void SetKey(string key = "")
        {
            KeyEquals = string.Equals(key, "deneme");
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to Sasha Hash <see cref="string"/>
        /// </summary>
        /// <param name="inputArr"><see cref="byte"/>[] to convert.</param>
        /// <param name="ReverseArray">If reverse array is true, reverses input array (optional)</param>
        /// <returns>Returns Sasha Hash <see cref="string"/>.</returns>
        private string PureCalculate(byte[] inputArr,bool ReverseArray=false)
        {
            if (ReverseArray == true)
            {
                Array.Reverse(inputArr);
            }

            Notus.HashLib.BLAKE2B blake2b_obj = new Notus.HashLib.BLAKE2B();
            Notus.HashLib.MD5 md5_obj = new Notus.HashLib.MD5();
            Notus.HashLib.SHA1 sha1_obj = new Notus.HashLib.SHA1();
            Notus.HashLib.RIPEMD160 ripemd160_obj = new Notus.HashLib.RIPEMD160();
                
            string[] blakeArray = Notus.Toolbox.Text.SplitByLength(Notus.Convert.Byte2Hex(blake2b_obj.ComputeHash(inputArr)), 16).ToArray();
            string[] md5Array = Notus.Toolbox.Text.SplitByLength(md5_obj.Calculate(inputArr), 4).ToArray();
            string[] sha1Array = Notus.Toolbox.Text.SplitByLength(sha1_obj.Calculate(inputArr), 5).ToArray();
            string[] ripemdArray = Notus.Toolbox.Text.SplitByLength(ripemd160_obj.ComputeHashWithArray(inputArr), 5).ToArray();
            string hashResult = "";
            for (int i = 0; i < 8; i++)
            {
                if (i < 4)
                {
                    hashResult = hashResult + blakeArray[i] + md5Array[i] + sha1Array[i] + ripemdArray[i];
                }
                else
                {
                    hashResult = hashResult + ripemdArray[i] + sha1Array[i] + md5Array[i] + blakeArray[i];
                }
            }
            return hashResult;
        }

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to Sasha Hash <see cref="string"/>
        /// </summary>
        /// <param name="rawInput">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns Sasha Hash <see cref="string"/>.</returns>
        public string Calculate(string rawInput)
        {
            return Notus.Toolbox.Text.ReplaceChar(
                PureCalculate(
                    Encoding.UTF8.GetBytes(rawInput),
                    true
                ),
                Notus.Variable.Constant.DefaultHexAlphabetString,
                SimpleHashAlphabetForHexResult
            );
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to Sasha Hash <see cref="string"/>
        /// </summary>
        /// <param name="inputArr"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns Sasha Hash <see cref="string"/>.</returns>
        public string Calculate(byte[] inputArr)
        {
            return Notus.Toolbox.Text.ReplaceChar(
                PureCalculate(inputArr,true),
                Notus.Variable.Constant.DefaultHexAlphabetString,
                SimpleHashAlphabetForHexResult
            );
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> to Sasha Signature <see cref="string"/>
        /// </summary>
        /// <param name="input">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns Sasha Signature <see cref="string"/>.</returns>
        public string Sign(string input)
        {
            return ComputeSign(input, true, SimpleHashAlphabetForSign, SimpleKeyTextForSign);
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to Sasha Signature <see cref="string"/>
        /// </summary>
        /// <param name="inputArr"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns Sasha Signature <see cref="string"/>.</returns>
        public string Sign(byte[] inputArr)
        {
            return ComputeSign(Encoding.UTF8.GetString(inputArr),true, SimpleHashAlphabetForSign, SimpleKeyTextForSign);
        }

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to Sasha Signature <see cref="string"/>
        /// </summary>
        /// <param name="rawInput">Plain <see cref="string"/> to convert.</param>
        /// <param name="returnAsHex">If return as hex is true, returns <see cref="string"/> as hex (optional).</param>
        /// <param name="newHashAlphabet">Plain <see cref="string"/> to convert (optional).</param>
        /// <param name="signKeyText">Salt <see cref="string"/> (optional).</param>
        /// <returns>Returns Sasha Signature <see cref="string"/>.</returns>
        public string ComputeSign(string rawInput, bool returnAsHex = false, string newHashAlphabet = "", string signKeyText = "")
        {
            if (KeyEquals == false)
            {
                return "err";
            }
            Notus.HashLib.SHA1 hashObjSha1 = new Notus.HashLib.SHA1();
            Notus.HashLib.RIPEMD160 hashObj160 = new Notus.HashLib.RIPEMD160();
            Notus.HashLib.BLAKE2B hashObj2b = new Notus.HashLib.BLAKE2B();
            Notus.HashLib.MD5 hashObjMd5 = new Notus.HashLib.MD5();

            string[] sha1Array = Notus.Toolbox.Text.SplitByLength(hashObjSha1.SignWithHashMethod(signKeyText, rawInput), 5).ToArray();
            string[] ripemdArray = Notus.Toolbox.Text.SplitByLength(hashObj160.SignWithHashMethod(signKeyText, rawInput), 5).ToArray();
            string[] blakeArray = Notus.Toolbox.Text.SplitByLength(hashObj2b.SignWithHashMethod(signKeyText, rawInput), 16).ToArray();
            string[] md5Array = Notus.Toolbox.Text.SplitByLength(hashObjMd5.SignWithHashMethod(signKeyText, rawInput), 4).ToArray();

            string hashResult = "";
            for (int i = 0; i < 8; i++)
            {
                if (i < 4)
                {
                    hashResult = hashResult + blakeArray[i] + md5Array[i] + sha1Array[i] + ripemdArray[i];
                }
                else
                {
                    hashResult = hashResult + ripemdArray[i] + sha1Array[i] + md5Array[i] + blakeArray[i];
                }
            }

            if (returnAsHex == true)
            {
                return hashResult;
            }

            if (newHashAlphabet.Length == 35)
            {

                return Notus.Toolbox.Text.ReplaceChar(
                    Notus.Convert.ToBase35(hashResult),
                    Notus.Variable.Constant.DefaultBase35AlphabetString,
                    newHashAlphabet
                );
            }
            return Notus.Convert.ToBase35(hashResult);
        }

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to Sasha Hash <see cref="string"/>
        /// </summary>
        /// <param name="rawInput">Plain <see cref="string"/> to convert.</param>
        /// <param name="returnAsHex">If return as hex is true, returns <see cref="string"/> as hex (optional)</param>
        /// <param name="newHashAlphabet">Plain <see cref="string"/> to convert (optional).</param>
        /// <returns>Returns Sasha Hash <see cref="string"/>.</returns>
        public string ComputeHash(string rawInput, bool returnAsHex = false, string newHashAlphabet = "")
        {
            if (KeyEquals == false)
            {
                return "err";
            }

            if (returnAsHex == true)
            {
                return PureCalculate(Encoding.UTF8.GetBytes(rawInput));
            }

            string hashResult = PureCalculate(Encoding.UTF8.GetBytes(rawInput));
            if (newHashAlphabet.Length == 35)
            {
                return Notus.Toolbox.Text.ReplaceChar(
                    Notus.Convert.ToBase35(hashResult),
                    Notus.Variable.Constant.DefaultBase35AlphabetString,
                    newHashAlphabet
                );
            }
            return Notus.Convert.ToBase35(hashResult);
        }
    }
}

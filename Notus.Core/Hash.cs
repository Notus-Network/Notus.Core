// Copyright (C) 2020-2022 Notus Network
// 
// Notus Network is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Linq;
using System.Text;

namespace Notus
{
    /// <summary>
    /// Main method and constructor for hashing
    /// </summary>
    public class Hash
    {
        /// <summary>
        /// Algorithm Result Types
        /// </summary>
        public enum ShortAlgorithmResultType
        {
            ///<summary>Mixes Iteration</summary>
            Mix = 2,
            ///<summary>One by One Iteration</summary>
            OneByOne = 4
        }

        /// <summary>
        /// Shrinks given <see cref="byte"/>[]
        /// </summary>
        /// <param name="rawInput"><see cref="byte"/>[] to shrink.</param>
        /// <param name="howManyByte">Result Byte Length (optional).</param>
        /// <param name="resultType">Iteration Mode (optional).</param>
        /// <returns>Returns hash with shrink <see cref="string"/></returns>
        public string Short(byte[] rawInput, byte howManyByte = 8, Notus.Core.Variable.ShortAlgorithmResultType resultType = Notus.Core.Variable.ShortAlgorithmResultType.OneByOne)
        {
            if (resultType== Notus.Core.Variable.ShortAlgorithmResultType.Mix)
            {
                string resultStr = "";
                string[] md5Result = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("md5", rawInput), howManyByte), 1).ToArray();
                string[] whirlpoolResult = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("whirlpool", rawInput), howManyByte), 1).ToArray();
                string[] sha1Result = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("sha1", rawInput), howManyByte), 1).ToArray();
                string[] sha256Result = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("sha256", rawInput), howManyByte), 1).ToArray();
                string[] sha512Result = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("sha512", rawInput), howManyByte), 1).ToArray();
                string[] blake2bResult = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("blake2b", rawInput), howManyByte), 1).ToArray();
                string[] ripemd160bResult = Notus.Core.Function.SplitByLength(Notus.Core.Function.ShrinkHex(CommonHash("ripemd160", rawInput), howManyByte), 1).ToArray();

                for (int a = 0; a < howManyByte * 2; a++)
                {
                    resultStr = resultStr +
                        md5Result[a] +
                        whirlpoolResult[a] +
                        sha1Result[a] +
                        sha256Result[a] +
                        sha512Result[a] +
                        blake2bResult[a] +
                        ripemd160bResult[a]
                    ;
                }
                return resultStr;
            }
            else
            {
                return Notus.Core.Function.ShrinkHex(CommonHash("md5", rawInput), howManyByte) +
                    Notus.Core.Function.ShrinkHex(CommonHash("whirlpool", rawInput), howManyByte) +
                    Notus.Core.Function.ShrinkHex(CommonHash("sha1", rawInput), howManyByte) +
                    Notus.Core.Function.ShrinkHex(CommonHash("sha256", rawInput), howManyByte) +
                    Notus.Core.Function.ShrinkHex(CommonHash("sha512", rawInput), howManyByte) +
                    Notus.Core.Function.ShrinkHex(CommonHash("blake2b", rawInput), howManyByte) +
                    Notus.Core.Function.ShrinkHex(CommonHash("ripemd160", rawInput), howManyByte);
            }
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] with specificed hash method <see cref="string"/>
        /// </summary>
        /// <param name="hashMethodName,">Hash Method to use.</param>
        /// <param name="rawInput"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns hash <see cref="string"/> with given hash type</returns>
        public string CommonHash(string hashMethodName, byte[] rawInput)
        {
            if (string.Equals("sasha", hashMethodName))
            {
                Notus.HashLib.Sasha hashObjSasha = new Notus.HashLib.Sasha();
                return hashObjSasha.Calculate(rawInput);
            }
            if (string.Equals("whirlpool", hashMethodName))
            {
                Notus.HashLib.Whirlpool hashObjMd5 = new Notus.HashLib.Whirlpool();
                return Notus.Core.Convert.Byte2Hex(
                    hashObjMd5.ComputeHash(rawInput)
                );
            }
            if (string.Equals("md5", hashMethodName))
            {
                Notus.HashLib.MD5 hashObjMd5 = new Notus.HashLib.MD5();
                return hashObjMd5.Calculate(rawInput);
            }
            if (string.Equals("sha1", hashMethodName))
            {
                Notus.HashLib.SHA1 hashObj = new Notus.HashLib.SHA1();
                return hashObj.Calculate(rawInput);
            }
            if (string.Equals("sha512", hashMethodName))
            {
                Notus.HashLib.SHA512 hashObj = new Notus.HashLib.SHA512();
                return hashObj.Calculate(rawInput);
            }
            if (string.Equals("sha256", hashMethodName))
            {
                Notus.HashLib.SHA256 hashObj = new Notus.HashLib.SHA256();
                return hashObj.Calculate(rawInput);
            }
            if (string.Equals("blake2b", hashMethodName))
            {
                Notus.HashLib.BLAKE2B hashObj2b = new Notus.HashLib.BLAKE2B();
                return Notus.Core.Convert.Byte2Hex(
                    hashObj2b.ComputeHash(rawInput)
                );
            }
            if (string.Equals("ripemd160", hashMethodName))
            {
                Notus.HashLib.RIPEMD160 hashObj160 = new Notus.HashLib.RIPEMD160();
                return hashObj160.ComputeHashWithArray(rawInput);
            }
            return string.Empty;
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> with specificed hash method <see cref="string"/>
        /// </summary>
        /// <param name="hashMethodName,">Hash Method to use.</param>
        /// <param name="rawInput"><see cref="string"/> to convert.</param>
        /// <returns>Returns hash <see cref="string"/> with given hash type</returns>
        public string CommonHash(string hashMethodName, string rawInput)
        {
            if (string.Equals("sasha", hashMethodName) )
            {
                Notus.HashLib.Sasha hashObjSasha = new Notus.HashLib.Sasha();
                return hashObjSasha.ComputeHash(rawInput, true);
            }
            if (string.Equals("whirlpool", hashMethodName))
            {
                Notus.HashLib.Whirlpool hashObjMd5 = new Notus.HashLib.Whirlpool();
                return hashObjMd5.ComputeHash(rawInput);
            }
            if (string.Equals("md5", hashMethodName) )
            {
                Notus.HashLib.MD5 hashObjMd5 = new Notus.HashLib.MD5();
                return hashObjMd5.Calculate(Encoding.UTF8.GetBytes(rawInput));
            }
            if (string.Equals("sha1", hashMethodName) )
            {
                Notus.HashLib.SHA1 hashObj = new Notus.HashLib.SHA1();
                return hashObj.Calculate(Encoding.UTF8.GetBytes(rawInput));
            }
            if (string.Equals("sha512", hashMethodName) )
            {
                Notus.HashLib.SHA512 hashObj = new Notus.HashLib.SHA512();
                return hashObj.ComputeHash(rawInput);
            }
            if (string.Equals("sha256", hashMethodName) )
            {
                Notus.HashLib.SHA256 hashObj = new Notus.HashLib.SHA256();
                return hashObj.Calculate(rawInput);
            }
            if (string.Equals("blake2b", hashMethodName) )
            {
                Notus.HashLib.BLAKE2B hashObj2b = new Notus.HashLib.BLAKE2B();
                return Notus.Core.Convert.Byte2Hex(
                    hashObj2b.ComputeHash(
                        Encoding.UTF8.GetBytes(rawInput)
                    )
                );
            }
            if (string.Equals("ripemd160", hashMethodName) )
            {
                Notus.HashLib.RIPEMD160 hashObj160 = new Notus.HashLib.RIPEMD160();
                return hashObj160.ComputeHashWithArray(Encoding.UTF8.GetBytes(rawInput));
            }
            return string.Empty;
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> with specificed hash method <see cref="string"/>
        /// </summary>
        /// <param name="hashMethodName,">Hash Method to use.</param>
        /// <param name="rawInput"><see cref="string"/> to convert.</param>
        /// <returns>Returns hash <see cref="byte"/>[] with given hash type</returns>
        public byte[] CommonHashByte(string hashMethodName, string rawInput)
        {
            if (string.Equals("sasha", hashMethodName))
            {
                Notus.HashLib.Sasha hashObjSasha = new Notus.HashLib.Sasha();
                return Notus.Core.Convert.Hex2Byte(
                    hashObjSasha.ComputeHash(rawInput, true)
                );
            }
            if (string.Equals("whirlpool", hashMethodName))
            {
                Notus.HashLib.Whirlpool hashObjMd5 = new Notus.HashLib.Whirlpool();
                return Notus.Core.Convert.Hex2Byte(hashObjMd5.ComputeHash(rawInput));
            }
            if (string.Equals("md5", hashMethodName))
            {
                Notus.HashLib.MD5 hashObjMd5 = new Notus.HashLib.MD5();
                return Notus.Core.Convert.Hex2Byte(hashObjMd5.Calculate(Encoding.UTF8.GetBytes(rawInput)));
            }
            if (string.Equals("sha1", hashMethodName))
            {
                Notus.HashLib.SHA1 hashObj = new Notus.HashLib.SHA1();
                return Notus.Core.Convert.Hex2Byte(hashObj.Calculate(Encoding.UTF8.GetBytes(rawInput)));
            }
            if (string.Equals("sha512", hashMethodName))
            {
                Notus.HashLib.SHA512 hashObj = new Notus.HashLib.SHA512();
                return Notus.Core.Convert.Hex2Byte(hashObj.ComputeHash(rawInput));
            }
            if (string.Equals("sha256", hashMethodName))
            {
                Notus.HashLib.SHA256 hashObj = new Notus.HashLib.SHA256();
                return Notus.Core.Convert.Hex2Byte(hashObj.Calculate(rawInput));
            }
            if (string.Equals("blake2b", hashMethodName))
            {
                Notus.HashLib.BLAKE2B hashObj2b = new Notus.HashLib.BLAKE2B();
                return hashObj2b.ComputeHash(
                    Encoding.UTF8.GetBytes(rawInput)
                );
            }
            if (string.Equals("ripemd160", hashMethodName))
            {
                Notus.HashLib.RIPEMD160 hashObj160 = new Notus.HashLib.RIPEMD160();
                return Notus.Core.Convert.Hex2Byte(
                    hashObj160.ComputeHashWithArray(Encoding.UTF8.GetBytes(rawInput))
                );
            }
            return new byte[] { };
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> with specificed hash method <see cref="string"/>
        /// </summary>
        /// <param name="hashMethodName,">Hash Method to use.</param>
        /// <param name="rawInput"><see cref="string"/> to convert.</param>
        /// <returns>Returns signature <see cref="string"/> with given hash type</returns>
        public string CommonSign(string hashMethodName, string rawInput)
        {
            if (string.Equals("sasha", hashMethodName) )
            {
                Notus.HashLib.Sasha hashObjSasha = new Notus.HashLib.Sasha();
                return hashObjSasha.Sign(rawInput);
            }
            if (string.Equals("whirlpool", hashMethodName))
            {
                Notus.HashLib.Whirlpool hashObjMd5 = new Notus.HashLib.Whirlpool();
                return hashObjMd5.Sign(rawInput);
            }
            if (string.Equals("md5", hashMethodName) )
            {
                Notus.HashLib.MD5 hashObjMd5 = new Notus.HashLib.MD5();
                return hashObjMd5.Sign(rawInput);
            }
            if (string.Equals("sha1", hashMethodName) )
            {
                Notus.HashLib.SHA1 hashObj = new Notus.HashLib.SHA1();
                return hashObj.Sign(rawInput);
            }
            if (string.Equals("sha512", hashMethodName) )
            {
                Notus.HashLib.SHA512 hashObj = new Notus.HashLib.SHA512();
                return hashObj.Sign(rawInput);
            }
            if (string.Equals("sha256", hashMethodName) )
            {
                Notus.HashLib.SHA256 hashObj = new Notus.HashLib.SHA256();
                return hashObj.Sign(rawInput);
            }
            if (string.Equals("blake2b", hashMethodName) )
            {
                Notus.HashLib.BLAKE2B hashObj2b = new Notus.HashLib.BLAKE2B();
                return hashObj2b.Sign(rawInput);
            }
            if (string.Equals("ripemd160", hashMethodName) )
            {
                Notus.HashLib.RIPEMD160 hashObj160 = new Notus.HashLib.RIPEMD160();
                return hashObj160.Sign(rawInput);
            }
            return string.Empty;
        }
    }
}

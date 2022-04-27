using System;
using System.Linq;
using System.Text;

namespace Notus.HashLib
{
    public class Sasha
    {
        //private string SimpleHashAlphabetForHexResult = "5d480bac17962ef3";



        //bu değerleri değiştirme
        //bu değerleri değiştirme
        private readonly string SimpleHashAlphabetForHexResult = "fedcba9876543210";
        private readonly string SimpleKeyTextForSign = "sasha-key-text";
        private readonly string SimpleHashAlphabetForSign = "zyxwvutsrqponmlkjihgfedcba987654321";
        //bu değerleri değiştirme
        //bu değerleri değiştirme

        private bool KeyEquals = true;
        public void SetKey(string key = "")
        {
            KeyEquals = string.Equals(key, "deneme");
        }

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
                
            string[] blakeDizi = Notus.Core.Function.SplitByLength(Notus.Core.Convert.Byte2Hex(blake2b_obj.ComputeHash(inputArr)), 16).ToArray();
            string[] md5Dizi = Notus.Core.Function.SplitByLength(md5_obj.Calculate(inputArr), 4).ToArray();
            string[] sha1Dizi = Notus.Core.Function.SplitByLength(sha1_obj.Calculate(inputArr), 5).ToArray();
            string[] ripemdDizi = Notus.Core.Function.SplitByLength(ripemd160_obj.ComputeHashWithArray(inputArr), 5).ToArray();
            string hashResult = "";
            for (int i = 0; i < 8; i++)
            {
                if (i < 4)
                { //1. aşama
                    hashResult = hashResult + blakeDizi[i] + md5Dizi[i] + sha1Dizi[i] + ripemdDizi[i];
                }
                else
                { //2. aşama
                    hashResult = hashResult + ripemdDizi[i] + sha1Dizi[i] + md5Dizi[i] + blakeDizi[i];
                }
            }
            return hashResult;
        }
        
        //standart olarak herkesin ulaşacağı hash hesaplama işlemi
        public string Calculate(string rawInput)
        {
            return Notus.Core.Function.ReplaceChar(
                PureCalculate(
                    Encoding.UTF8.GetBytes(rawInput),
                    true
                ),
                Notus.Core.Variable.DefaultHexAlphabetString,
                SimpleHashAlphabetForHexResult
            );
        }
        public string Calculate(byte[] inputArr)
        {
            return Notus.Core.Function.ReplaceChar(
                PureCalculate(inputArr,true),
                Notus.Core.Variable.DefaultHexAlphabetString,
                SimpleHashAlphabetForHexResult
            );
        }
        public string Sign(string input)
        {
            return ComputeSign(input, true, SimpleHashAlphabetForSign, SimpleKeyTextForSign);
        }
        public string Sign(byte[] inputArr)
        {
            return ComputeSign(Encoding.UTF8.GetString(inputArr),true, SimpleHashAlphabetForSign, SimpleKeyTextForSign);
        }

        public string ComputeSign(string rawInput, bool returnAsHex = false, string newHashAlphabet = "", string SignKeyText = "")
        {
            if (KeyEquals == false)
            {
                return "err";
            }
            Notus.HashLib.SHA1 hashObjSha1 = new Notus.HashLib.SHA1();
            Notus.HashLib.RIPEMD160 hashObj160 = new Notus.HashLib.RIPEMD160();
            Notus.HashLib.BLAKE2B hashObj2b = new Notus.HashLib.BLAKE2B();
            Notus.HashLib.MD5 hashObjMd5 = new Notus.HashLib.MD5();

            string[] sha1Dizi = Notus.Core.Function.SplitByLength(hashObjSha1.SignWithHashMethod(SignKeyText, rawInput), 5).ToArray();
            string[] ripemdDizi = Notus.Core.Function.SplitByLength(hashObj160.SignWithHashMethod(SignKeyText, rawInput), 5).ToArray();
            string[] blakeDizi = Notus.Core.Function.SplitByLength(hashObj2b.SignWithHashMethod(SignKeyText, rawInput), 16).ToArray();
            string[] md5Dizi = Notus.Core.Function.SplitByLength(hashObjMd5.SignWithHashMethod(SignKeyText, rawInput), 4).ToArray();

            string hashResult = "";
            for (int i = 0; i < 8; i++)
            {
                if (i < 4)
                { //1. aşama
                    hashResult = hashResult + blakeDizi[i] + md5Dizi[i] + sha1Dizi[i] + ripemdDizi[i];
                }
                else
                { //2. aşama
                    hashResult = hashResult + ripemdDizi[i] + sha1Dizi[i] + md5Dizi[i] + blakeDizi[i];
                }
            }

            if (returnAsHex == true)
            {
                return hashResult;
            }

            if (newHashAlphabet.Length == 35)
            {

                return Notus.Core.Function.ReplaceChar(
                    Notus.Core.Convert.ToBase35(hashResult),
                    Notus.Core.Variable.DefaultBase35AlphabetString,
                    newHashAlphabet
                );
            }
            return Notus.Core.Convert.ToBase35(hashResult);
        }

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
                return Notus.Core.Function.ReplaceChar(
                    Notus.Core.Convert.ToBase35(hashResult),
                    Notus.Core.Variable.DefaultBase35AlphabetString,
                    newHashAlphabet
                );
            }
            return Notus.Core.Convert.ToBase35(hashResult);
        }
    }
}

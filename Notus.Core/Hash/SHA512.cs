using System;
using System.Security.Cryptography;
using System.Text;

namespace Notus.HashLib
{
    public class SHA512
    {
        public string Calculate(string rawData)
        {
            return ComputeHash(rawData);
        }
        public string Calculate(byte[] inputData)
        {
            using (System.Security.Cryptography.SHA512 shaM = new SHA512Managed())
            {
                return Notus.Core.Convert.Byte2Hex(
                    shaM.ComputeHash(
                        inputData
                    )
                );
            }
        }
        public string ComputeHash(string rawData)
        {
            using (System.Security.Cryptography.SHA512 shaM = new SHA512Managed())
            {
                return Notus.Core.Convert.Byte2Hex(
                    shaM.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            rawData
                        )
                    )
                );
            }
        }
        public string Sign(string input)
        {
            return SignWithHashMethod("", input);
        }
        public string Sign(byte[] inputArr)
        {
            return SignWithHashMethod("", Encoding.UTF8.GetString(inputArr));
        }

        public string SignWithHashMethod(string keyText, string input)
        {
            int keySize = 256;
            int b = keySize;
            if (keyText.Length > b)
            {
                keyText = ComputeHash(keyText).ToLower();
            }

            byte[] iPadDizi = Encoding.ASCII.GetBytes(
                Notus.Core.Function.AddRightPad("", b, "6")
            );
            byte[] oPadDizi = Encoding.ASCII.GetBytes(
                Notus.Core.Function.AddRightPad("", b, System.Convert.ToChar(92).ToString())
            );
            byte[] keyDizi = Encoding.ASCII.GetBytes(
                Notus.Core.Function.AddRightPad(keyText, b, System.Convert.ToChar(0).ToString())
            );

            string k_ipad = "";
            string k_opad = "";
            for (int a = 0; a < keySize; a++)
            {
                k_ipad = k_ipad + ((char)(keyDizi[a] ^ iPadDizi[a])).ToString();
                k_opad = k_opad + ((char)(keyDizi[a] ^ oPadDizi[a])).ToString();
            }
            return ComputeHash(
                k_opad +
                ComputeHash(k_ipad + input).ToLower()
            ).ToLower();
        }
    }
}

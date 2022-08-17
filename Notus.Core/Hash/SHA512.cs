using System.Security.Cryptography;
using System.Text;

namespace Notus.HashLib
{
    /// <summary>
    /// Helper methods for SHA512 hashing.
    /// </summary>
    public class SHA512
    {
        /// <inheritdoc cref="ComputeHash(string)"/>
        public string Calculate(string rawData)
        {
            return ComputeHash(rawData);
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to SHA512 Hash <see cref="string"/>
        /// </summary>
        /// <param name="data"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns SHA512 Hash <see cref="string"/>.</returns>
        public string Calculate(byte[] inputData)
        {
            using (System.Security.Cryptography.SHA512 shaM = System.Security.Cryptography.SHA512.Create())
            {
                return Notus.Convert.Byte2Hex(
                    shaM.ComputeHash(
                        inputData
                    )
                );
            }
        }

        /// <summary>
        /// Converts the specified plain <see cref="string"/> to SHA512 Hash <see cref="string"/>
        /// </summary>
        /// <param name="rawData">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns SHA512 Hash <see cref="string"/>.</returns>
        public string ComputeHash(string rawData)
        {
            using (System.Security.Cryptography.SHA512 shaM = new SHA512Managed())
            {
                return Notus.Convert.Byte2Hex(
                    shaM.ComputeHash(
                        Encoding.UTF8.GetBytes(
                            rawData
                        )
                    )
                );
            }
        }

        /// <summary>
        /// Converts the specified <see cref="string"/> to SHA512 Signature <see cref="string"/>
        /// </summary>
        /// <param name="input">Plain <see cref="string"/> to convert.</param>
        /// <returns>Returns SHA512 Signature <see cref="string"/>.</returns>
        public string Sign(string input)
        {
            return SignWithHashMethod("", input);
        }

        /// <summary>
        /// Converts the specified <see cref="byte"/>[] to SHA512 Signature <see cref="string"/>
        /// </summary>
        /// <param name="inputArr"><see cref="byte"/>[] to convert.</param>
        /// <returns>Returns SHA512 Signature <see cref="string"/>.</returns>
        public string Sign(byte[] inputArr)
        {
            return SignWithHashMethod("", Encoding.UTF8.GetString(inputArr));
        }

        /// <summary>
        /// Converts the specified key <see cref="string"/> and specified <see cref="string"/> to SHA512 Signature <see cref="string"/>
        /// </summary>
        /// <param name="keyText"><see cref="string"/> MD5 Key</param>
        /// <param name="input"><see cref="string"/> to convert.</param>
        /// <returns>Returns SHA512 Signature <see cref="string"/>.</returns>
        public string SignWithHashMethod(string keyText, string input)
        {
            int keySize = 256;
            int b = keySize;
            if (keyText.Length > b)
            {
                keyText = ComputeHash(keyText).ToLower();
            }

            byte[] iPadDizi = Encoding.ASCII.GetBytes(
                Notus.Toolbox.Text.AddRightPad("", b, "6")
            );
            byte[] oPadDizi = Encoding.ASCII.GetBytes(
                Notus.Toolbox.Text.AddRightPad("", b, System.Convert.ToChar(92).ToString())
            );
            byte[] keyDizi = Encoding.ASCII.GetBytes(
                Notus.Toolbox.Text.AddRightPad(keyText, b, System.Convert.ToChar(0).ToString())
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Notus.Encryption
{
    public class Cipher:IDisposable
    {
        /*

               DE  ->  DES ENCRYPTION
               AE  ->  AES ENCRYPTION
               CE  ->  CHACHA20 ENCRYPTION

               BC  ->  BASE64 CONVERT
               HC  ->  HEX CONVERT

               BI  ->  BASE64 ITERATION ( CONTAINS BASE CONVERT OPERATIONS )
               HI  ->  HEX ITERATION  ( CONTAINS HEX CONVERT OPERATIONS )

               RT  ->  REVERSE TEXT
               RA  ->  REVERSE ARRAY

               A1  ->  ANONYMOUS DATA - 31 CHARS  
               A2  ->  ANONYMOUS DATA - 32 CHARS  
               A3  ->  ANONYMOUS DATA - 33 CHARS  
               A4  ->  ANONYMOUS DATA - 34 CHARS  
               A5  ->  ANONYMOUS DATA - 35 CHARS  
               A6  ->  ANONYMOUS DATA - 36 CHARS  
               A7  ->  ANONYMOUS DATA - 37 CHARS  
               A8  ->  ANONYMOUS DATA - 38 CHARS  
               A9  ->  ANONYMOUS DATA - 39 CHARS  

               */

        private readonly Dictionary<string, string> DataSecurityPattern = new Dictionary<string, string>()
        {
            { "default", "A8:DE:AE:CE:DE:AE:CE" },    // BLOCK TYPE -> 0, 1
            { "ues02", "A7:AE:CE:AE:CE:DE" },         // BLOCK TYPE -> 7, 360
            { "ues04", "A6:AE:CE:AE:CE:DE" },         // BLOCK TYPE -> 0, 1
            { "ups04", "A9:AE:CE:AE:CE:DE" },         // BLOCK TYPE -> 3

            { "ctes05", "A4:AE:CE:AE:DE:CE" },        // BLOCK TYPE -> 0,1
            { "ubpces06", "A3:AE:CE:DE:AE:CE" },      // BLOCK TYPE -> 0,1

            { "sames01", "A4:AE:CE:AE:CE" },          // BLOCK TYPE -> 0,1

            { "ges01", "A5:AE:CE:AE:DE:CE" },         // BLOCK TYPE -> 20
            { "umnes03", "A1:AE:DE:CE:AE:CE" },       // BLOCK TYPE -> 9
            { "tfes01", "A2:CE:DE:AE:CE" },           // BLOCK TYPE -> 0,1
            { "eces02", "A3:DE:AE:CE:AE:CE" },        // BLOCK TYPE -> 99, 106, 127, 129, 147, 159, 163, 179, 193, 199, 233
            { "deneme", "A3:CE:AE:DE:CE:AE:DE" }
        };

        public string Decrypt(string rawDataStr, string TypeStr, string BlockKey, string TimeStr, bool testEncMethod = false)
        {
            BlockKey = BlockKey.Length == 0 ? "default" : BlockKey;
            TimeStr = TimeStr.Length == 0 ? "default" : TimeStr;
            string generatedKey = new Notus.Hash().CommonHash("sha1", BlockKey);
            string generatedIV = new Notus.Hash().CommonHash("md5", TimeStr);
            TypeStr = TypeStr.ToLower();
            TypeStr = (DataSecurityPattern.ContainsKey(TypeStr) == false ? "default" : TypeStr);

            byte[] decArray = System.Convert.FromBase64String(
                Notus.Toolbox.Text.ReplaceChar(rawDataStr,
                        Notus.Toolbox.Text.Iteration(
                            64,
                            new Notus.Hash().CommonSign("sha1", generatedKey) +
                            new Notus.Hash().CommonSign("sha1", generatedIV)
                        ),
                        Notus.Variable.Constant.DefaultBase64AlphabetString
                )
            );

            Array.Reverse(decArray);

            string[] patternArray = DataSecurityPattern[TypeStr].Split(':');
            Array.Reverse(patternArray);

            for (int a = 0; a < patternArray.Length - 1; a++)
            {
                if (string.Equals(patternArray[a], "DE")) // DES ENCRYPTION
                {
                    byte[] newDecArray = Notus.Encryption.Toolbox.DecryptDesWithByte(
                        decArray,
                        BlockKey,
                        generatedKey,
                        generatedIV
                    );
                    Array.Resize(ref decArray, newDecArray.Length);
                    Array.Copy(newDecArray, decArray, newDecArray.Length);
                }
                if (string.Equals(patternArray[a], "AE")) // AES ENCRYPTION
                {
                    byte[] newDecArray = Notus.Encryption.Toolbox.DecryptWithAes(
                        decArray,
                        BlockKey,
                        generatedKey,
                        generatedIV
                    );
                    Array.Resize(ref decArray, newDecArray.Length);
                    Array.Copy(newDecArray, decArray, newDecArray.Length);
                }
                if (string.Equals(patternArray[a], "CE")) // CHACHA20 ENCRYPTION
                {
                    byte[] newDecArray = Notus.Encryption.Toolbox.DecryptWithChaCha20(
                        decArray,
                        BlockKey,
                        generatedKey,
                        generatedIV
                    );
                    Array.Resize(ref decArray, newDecArray.Length);
                    Array.Copy(newDecArray, decArray, newDecArray.Length);
                }
            }
            return Encoding.UTF8.GetString(decArray).Substring(30 + byte.Parse(patternArray[patternArray.Length - 1].Substring(1)));
        }
        public string Encrypt(string rawDataStr, string TypeStr, string BlockKey, string TimeStr, bool testEncMethod = false)
        {
            BlockKey = BlockKey.Length == 0 ? "default" : BlockKey;
            TimeStr = TimeStr.Length == 0 ? "default" : TimeStr;
            string generatedKey = new Notus.Hash().CommonHash("sha1", BlockKey);
            string generatedIV = new Notus.Hash().CommonHash("md5", TimeStr);
            
            TypeStr = TypeStr.ToLower();
            TypeStr = (DataSecurityPattern.ContainsKey(TypeStr) == false ? "default" : TypeStr);
            string[] patternArray = DataSecurityPattern[TypeStr].Split(':');

            
            rawDataStr =
                Notus.Encryption.Toolbox.GenerateText(30 + byte.Parse(patternArray[0].Substring(1))) +
                rawDataStr;


            byte[] rawData = Encoding.UTF8.GetBytes(rawDataStr);
            for (int a = 1; a < patternArray.Length; a++)
            {
                if (string.Equals(patternArray[a], "DE")) // DES ENCRYPTION
                {
                    byte[] newRawData = Notus.Encryption.Toolbox.EncryptWithDes(rawData, BlockKey, generatedKey, generatedIV);
                    Array.Resize(ref rawData, newRawData.Length);
                    Array.Copy(newRawData, rawData, newRawData.Length);
                }
                if (string.Equals(patternArray[a], "AE")) // AES ENCRYPTION
                {
                    byte[] newRawData = Notus.Encryption.Toolbox.EncryptWithAes(
                        rawData,
                        BlockKey,
                        generatedKey,
                        generatedIV
                    );
                    Array.Resize(ref rawData, newRawData.Length);
                    Array.Copy(newRawData, rawData, newRawData.Length);
                }
                if (string.Equals(patternArray[a], "CE")) // CHACHA20 ENCRYPTION
                {
                    byte[] newRawData = Notus.Encryption.Toolbox.EncryptWithChaCha20(
                        rawData,
                        BlockKey,
                        generatedKey,
                        generatedIV
                    );
                    Array.Resize(ref rawData, newRawData.Length);
                    Array.Copy(newRawData, rawData, newRawData.Length);
                }
            }
            Array.Reverse(rawData);
            
            return Notus.Toolbox.Text.ReplaceChar(
                System.Convert.ToBase64String(rawData),
                Notus.Variable.Constant.DefaultBase64AlphabetString,
                Notus.Toolbox.Text.Iteration(
                    64,
                    new Notus.Hash().CommonSign("sha1", generatedKey) +
                    new Notus.Hash().CommonSign("sha1", generatedIV)
                )
            );
        }
        public Cipher()
        {

        }
        ~Cipher()
        {

        }
        public void Dispose()
        {
            
        }
    }
}

using System;

namespace Notus.Nonce
{
    public class Calculate : IDisposable
    {
        private static readonly int LargestControlNumber = 60000000;
        //en büyük sayıdan çıkarma yapılarak gösterilsin
        //private readonly bool SubFromBiggestNumber = false;
        //private readonly string NonceDelimeterChar = ":";
        public string Execute(string methodName, int hashMethodNo, string hashStr, int difficulty)
        {
            if (string.Equals("slide", methodName) == true)
            {
                return Slide(hashMethodNo, hashStr, difficulty);
            }

            if (string.Equals("bounce", methodName) == true)
            {
                return Bounce(hashMethodNo, hashStr, difficulty);
            }

            return string.Empty;
        }

        // 1- tekli kayar hesaplamalı (2 zorluk derecesi için örnek
        //00xxxx
        //x00xxx
        //xx00xx
        //xxx00x
        //public string KayarNonce(int hashMethodNo, string hashStr, int difficulty)
        public string Slide(int hashMethodNo, string hashStr, int difficulty)
        {
            int HashResultLen = Notus.Variable.Constant.NonceHashLength[hashMethodNo];

            int arrLength = (HashResultLen - difficulty) + 1;
            int[] nonceArray = new int[arrLength];

            string referenceHash = new String('0', HashResultLen);
            int zeroStartFrom = 0;
            bool itsDone = false;
            int biggestNumberVal = 0;
            int theNumberFound = 0;

            for (int arrPos = 0; itsDone == false; arrPos++)
            {
                bool boolNumberFounded = false;
                for (int y = 1; y < LargestControlNumber && boolNumberFounded == false; y++)
                {
                    string tmpBckpStr = "";
                    if (hashMethodNo == 1) //md5 metodu için
                    {
                        tmpBckpStr = new Notus.Hash().CommonHash("md5", hashStr + "x" + y.ToString());
                    }
                    if (hashMethodNo == 2) //sha1 metodu için
                    {
                        tmpBckpStr = new Notus.Hash().CommonHash("sha1", hashStr + "x" + y.ToString());
                    }
                    if (hashMethodNo == 100) //sasha metodu için
                    {
                        tmpBckpStr = new Notus.Hash().CommonHash("sasha", hashStr + "x" + y.ToString());
                    }
                    string controlStr = tmpBckpStr.Substring(zeroStartFrom, difficulty);
                    string referenceStr = referenceHash.Substring(zeroStartFrom, difficulty);

                    if (string.Compare(controlStr, referenceStr) == 0)
                    {
                        boolNumberFounded = true;
                        theNumberFound = y;
                        nonceArray[arrPos] = theNumberFound;
                        if (theNumberFound > biggestNumberVal)
                        {
                            biggestNumberVal = theNumberFound;
                        }
                    }
                }

                zeroStartFrom++;
                if (zeroStartFrom > (HashResultLen - difficulty))
                {
                    itsDone = true;
                }
            }

            string biggestHexStr = biggestNumberVal.ToString("x");
            
            string strNumberToSubtract = new string('f', biggestHexStr.Length);

            int intNumberToSubtract = int.Parse(strNumberToSubtract, System.Globalization.NumberStyles.HexNumber);
            string resultStr = "";

            for (int a = 0; a < nonceArray.Length; a++)
            {
                int intConvertNumber = nonceArray[a];
                if (Notus.Variable.Constant.SubFromBiggestNumber == true)
                {
                    intConvertNumber = intNumberToSubtract - intConvertNumber;
                }
                string tmpStr = intConvertNumber.ToString("x");
                int yedFark = biggestHexStr.Length - tmpStr.Length;
                if (yedFark > 0)
                {
                    string tmpAddStr = new String('0', yedFark);
                    tmpStr = tmpAddStr + tmpStr;
                }
                resultStr = resultStr + tmpStr + Notus.Variable.Constant.NonceDelimeterChar;
            }
            
            string lastCharStr = resultStr.Substring(resultStr.Length - 1);
            if (lastCharStr == Notus.Variable.Constant.NonceDelimeterChar)
            {
                resultStr = resultStr.Substring(0, resultStr.Length - 1);
            }
            return resultStr;
        }


        public int NonceStepCount(int NonceType, int HashMethodNo, int Difficulty)
        {
            if (NonceType == 1)
            {
                return (Notus.Variable.Constant.NonceHashLength[HashMethodNo] - Difficulty) + 1;
            }
            else
            {
                return (int)Math.Ceiling(
                    (decimal)Notus.Variable.Constant.NonceHashLength[HashMethodNo]
                    /
                    (decimal)Difficulty
                );

            }
        }

        //2- atlamalı hesaplama yöntemi ile ( 2 zorluk derecesi için örnek
        //00xxxx
        //xx00xx
        //xxxx00
        //public string AtlamaliNonce(int hashMethodNo, string hashStr, int difficulty)
        public string Bounce(int hashMethodNo, string hashStr, int difficulty)
        {
            int HashResultLen = Notus.Variable.Constant.NonceHashLength[hashMethodNo];
            /*
            int HashResultLen = 32; //burada kullaınlacak olan hashin string uzunluğu yazılacak
            if (hashMethodNo == 1) //md5 metodu için
            {
                HashResultLen = 32;
            }
            if (hashMethodNo == 2) //sha1 metodu için
            {
                HashResultLen = 40;
            }
            if (hashMethodNo == 100) //sasha metodu için
            {
                HashResultLen = 240;
            }
            */

            decimal tLengthVal = HashResultLen;
            decimal dLevel = difficulty;

            decimal divideResult = Math.Ceiling(tLengthVal / dLevel);
            int tmpArraySize = (int)divideResult;
            int[] tmpNonceArray = new int[tmpArraySize];

            string referansHash = new String('0', HashResultLen);
            string tmpAddString = "";
            int tmpAddChar = ((int)divideResult * difficulty) - HashResultLen;
            if (tmpAddChar > 0)
            {
                tmpAddString = new string('0', tmpAddChar);
                HashResultLen += tmpAddChar;
            }
            int theNumberFound = 0;
            int tmpZeroStartFrom = 0;
            bool tmpItsDone = false;
            int biggestNumberVal = 0;
            for (int arrLoc = 0; tmpItsDone == false; arrLoc++)
            {
                bool numberFound = false;
                for (int y = 1; y < LargestControlNumber && numberFound == false; y++)
                {
                    string tmpBckpResultStr = "";
                    if (hashMethodNo == 1) //md5 metodu için
                    {
                        tmpBckpResultStr = new Notus.Hash().CommonHash("md5", hashStr + "x" + y.ToString()) + tmpAddString;
                    }
                    if (hashMethodNo == 2) //sha1 metodu için
                    {
                        tmpBckpResultStr = new Notus.Hash().CommonHash("sha1", hashStr + "x" + y.ToString()) + tmpAddString;
                    }
                    if (hashMethodNo == 100) //sasha metodu için
                    {
                        tmpBckpResultStr = new Notus.Hash().CommonHash("sasha", hashStr + "x" + y.ToString()) + tmpAddString;
                    }

                    string controlStr = tmpBckpResultStr.Substring(tmpZeroStartFrom, difficulty);
                    string referenceStr = referansHash.Substring(tmpZeroStartFrom, difficulty);
                    if (string.Compare(controlStr, referenceStr) == 0)
                    {
                        numberFound = true;
                        theNumberFound = y;
                        tmpNonceArray[arrLoc] = theNumberFound;
                        if (theNumberFound > biggestNumberVal)
                        {
                            biggestNumberVal = theNumberFound;
                        }
                    }
                }

                tmpZeroStartFrom += difficulty;
                if (tmpZeroStartFrom > (HashResultLen - difficulty))
                {
                    tmpItsDone = true;
                }
            }

            string tmpBiggestHexStr = biggestNumberVal.ToString("x");
            string tmpNumberToSubtractStr = new string('f', tmpBiggestHexStr.Length);
            
            int tmpNumberToSubtractVal = int.Parse(tmpNumberToSubtractStr, System.Globalization.NumberStyles.HexNumber);
            string resultStr = "";

            for (int a = 0; a < tmpNonceArray.Length; a++)
            {
                int tmpConvertNo = tmpNonceArray[a];
                if (Notus.Variable.Constant.SubFromBiggestNumber == true)
                {
                    tmpConvertNo = tmpNumberToSubtractVal - tmpConvertNo;
                }

                string tmpTempString = tmpConvertNo.ToString("x");
                int tmpBckpDiff = tmpBiggestHexStr.Length - tmpTempString.Length;
                if (tmpBckpDiff > 0)
                {
                    string tmpFrntStr = new String('0', tmpBckpDiff);
                    tmpTempString = tmpFrntStr + tmpTempString;
                }
                resultStr = resultStr + tmpTempString + Notus.Variable.Constant.NonceDelimeterChar;
            }

            string tmpLastChar = resultStr.Substring(resultStr.Length - 1);
            if (tmpLastChar == Notus.Variable.Constant.NonceDelimeterChar)
            {
                resultStr = resultStr.Substring(0, resultStr.Length - 1);
            }
            return resultStr;
        }
        public Calculate()
        {

        }

        ~Calculate()
        {
            Dispose();
        }
        public void Dispose()
        {

        }
    }
}

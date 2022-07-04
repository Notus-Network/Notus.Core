using System;

namespace Notus.Nonce
{
    public class Verify : IDisposable
    {
        public bool Execute(string methodName, int hashMethodNo, string hashStr, int difficulty, string valueStr)
        {
            if (string.Equals("slide", methodName) == true)
            {
                return Slide(hashMethodNo, hashStr, difficulty, valueStr);
            }

            if (string.Equals("bounce", methodName) == true)
            {
                return Bounce(hashMethodNo, hashStr, difficulty, valueStr);
            }

            return false;
        }

        // 1- tekli kayar hesaplamalı (2 zorluk derecesi için örnek
        //00xxxx
        //x00xxx
        //xx00xx
        //xxx00x
        //public bool KayarNonce(int hashMethodNo, string hashStr, int difficulty, string valueStr)
        public bool Slide(int hashMethodNo, string hashStr, int difficulty, string valueStr)
        {
            int uzunluk = Notus.Variable.Constant.NonceHashLength[hashMethodNo];
            /*
            int uzunluk = 32; //burada kullaınlacak olan hashin string uzunluğu yazılacak
            if (hashMethodNo == 1) //md5 metodu için
            {
                uzunluk = 32;
            }
            if (hashMethodNo == 2) //sha1 metodu için
            {
                uzunluk = 40;
            }
            if (hashMethodNo == 100) //sasha metodu için
            {
                uzunluk = 240;
            }
            */
            //int ilkKonum = valueStr.IndexOf(AsynchronousSocketListener.NonceDelimeterChar);
            //int hexLenth = int.Parse(valueStr.Substring(0, 2));
            //string[] valueArray = hmac.SplitByLength(valueStr.Substring(2), hexLenth).ToArray();

            string[] valueArray = valueStr.Split(Notus.Variable.Constant.NonceDelimeterChar);
            int hexLenth = valueArray[0].Length;
            bool herSeyEsit = true;
            string referansHash = new String('0', uzunluk);
            int sifirKonumBaslangic = 0;
            string enBuyukHexStr = hexLenth.ToString("x");
            for (int a = 0; a < valueArray.Length && herSeyEsit == true; a++)
            {
                int gecerliSayi = int.Parse(valueArray[a], System.Globalization.NumberStyles.HexNumber);
                int hesaplananSayi = gecerliSayi;

                if (Notus.Variable.Constant.SubFromBiggestNumber == true)
                {
                    string _cikarilacakSayiTemp = new string('f', int.Parse(enBuyukHexStr));
                    int _cikarilacakSayi = int.Parse(_cikarilacakSayiTemp, System.Globalization.NumberStyles.HexNumber);
                    hesaplananSayi = _cikarilacakSayi - hesaplananSayi;
                }

                string yedSonuc = "";
                if (hashMethodNo == 1) //md5 metodu için
                {
                    yedSonuc = new Notus.Hash().CommonHash("md5", hashStr + "x" + hesaplananSayi.ToString());
                }
                if (hashMethodNo == 2) //sha1 metodu için
                {
                    yedSonuc = new Notus.Hash().CommonHash("sha1", hashStr + "x" + hesaplananSayi.ToString());
                }
                if (hashMethodNo == 100) //sasha metodu için
                {
                    yedSonuc = new Notus.Hash().CommonHash("sasha", hashStr + "x" + hesaplananSayi.ToString());
                }

                string kontrolStr = yedSonuc.Substring(sifirKonumBaslangic, difficulty);
                string referansStr = referansHash.Substring(sifirKonumBaslangic, difficulty);
                if (string.Compare(kontrolStr, referansStr) == 0)
                {
                    sifirKonumBaslangic++;
                }
                else
                {
                    herSeyEsit = false;
                }
            }
            return herSeyEsit;
        }



        //2- atlamalı hesaplama yöntemi ile ( 2 zorluk derecesi için örnek
        //00xxxx
        //xx00xx
        //xxxx00
        //public bool AtlamaliNonce(int hashMethodNo, string hashStr, int difficulty, string valueStr)
        public bool Bounce(int hashMethodNo, string hashStr, int difficulty, string valueStr)
        {
            int uzunluk = Notus.Variable.Constant.NonceHashLength[hashMethodNo];
            /*
            int uzunluk = 32; //burada kullaınlacak olan hashin string uzunluğu yazılacak
            if (hashMethodNo == 1) //md5 metodu için
            {
                uzunluk = 32;
            }
            if (hashMethodNo == 2) //sha1 metodu için
            {
                uzunluk = 40;
            }
            if (hashMethodNo == 100) //sasha metodu için
            {
                uzunluk = 240;
            }
            */
            decimal dUzunluk = uzunluk;
            decimal dLevel = difficulty;

            decimal divideResult = Math.Ceiling(dUzunluk / dLevel);
            string[] valueArray = valueStr.Split(Notus.Variable.Constant.NonceDelimeterChar);
            int hexLenth = valueArray[0].Length;

            //int hexLenth = int.Parse(valueStr.Substring(0, 2));
            //string[] valueArray = hmac.SplitByLength(valueStr.Substring(2), hexLenth).ToArray();

            string ilaveString = "";
            int ilaveChar = ((int)divideResult * difficulty) - uzunluk;
            if (ilaveChar > 0)
            {
                ilaveString = new string('0', ilaveChar);
                uzunluk += ilaveChar;
            }
            bool herSeyEsit = true;
            string referansHash = new String('0', uzunluk);
            int sifirKonumBaslangic = 0;
            string enBuyukHexStr = hexLenth.ToString("x");
            for (int a = 0; a < valueArray.Length && herSeyEsit == true; a++)
            {
                int gecerliSayi = int.Parse(valueArray[a], System.Globalization.NumberStyles.HexNumber);
                int hesaplananSayi = gecerliSayi;
                if (Notus.Variable.Constant.SubFromBiggestNumber == true)
                {
                    string _cikarilacakSayiTemp = new string('f', int.Parse(enBuyukHexStr));
                    int _cikarilacakSayi = int.Parse(_cikarilacakSayiTemp, System.Globalization.NumberStyles.HexNumber);
                    hesaplananSayi = _cikarilacakSayi - hesaplananSayi;
                }

                string yedSonuc = "";
                if (hashMethodNo == 1) //md5 metodu için
                {
                    yedSonuc = new Notus.Hash().CommonHash("md5", hashStr + "x" + hesaplananSayi.ToString()) + ilaveString;
                }
                if (hashMethodNo == 2) //sha1 metodu için
                {
                    yedSonuc = new Notus.Hash().CommonHash("sha1", hashStr + "x" + hesaplananSayi.ToString()) + ilaveString;
                }
                if (hashMethodNo == 100) //sasha metodu için
                {
                    yedSonuc = new Notus.Hash().CommonHash("md5", hashStr + "x" + hesaplananSayi.ToString()) + ilaveString;
                }
                string kontrolStr = yedSonuc.Substring(sifirKonumBaslangic, difficulty);
                string referansStr = referansHash.Substring(sifirKonumBaslangic, difficulty);
                if (string.Compare(kontrolStr, referansStr) == 0)
                {
                    sifirKonumBaslangic += difficulty;
                }
                else
                {
                    herSeyEsit = false;
                }
            }
            return herSeyEsit;
        }


        public Verify()
        {

        }

        ~Verify()
        {
            Dispose();
        }
        public void Dispose()
        {

        }

    }
}

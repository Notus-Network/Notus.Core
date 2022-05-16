using System;

namespace Notus.Core
{
    public class MergeRawData
    {
        public static Notus.Core.Variable.GenericSignStruct GenericSign(string PrivateKey)
        {
            string PublicKeyStr = Notus.Core.Wallet.ID.Generate(PrivateKey);
            string TimeStr = DateTime.Now.ToString(Notus.Core.Variable.DefaultDateTimeFormatText);
            return new Variable.GenericSignStruct()
            {
                PublicKey = PublicKeyStr,
                Time = TimeStr,
                Sign = Notus.Core.Wallet.ID.Sign(PublicKeyStr + TimeStr, PrivateKey)
            };
        }
        public static string Transaction(string Sender, string Receiver, string Volume, string Currency)
        {
            return Sender + Notus.Core.Variable.CommonDelimeterChar +
            Receiver + Notus.Core.Variable.CommonDelimeterChar +
            Volume + Notus.Core.Variable.CommonDelimeterChar +
            Currency;
        }

        public static Notus.Core.Variable.FileTransferStruct FileUpload(Notus.Core.Variable.FileTransferStruct uploadFile)
        {

            uploadFile.Sign =
                uploadFile.FileName + Notus.Core.Variable.CommonDelimeterChar +
                uploadFile.FileSize.ToString() + Notus.Core.Variable.CommonDelimeterChar +
                uploadFile.FileHash + Notus.Core.Variable.CommonDelimeterChar +
                uploadFile.ChunkSize.ToString() + Notus.Core.Variable.CommonDelimeterChar +
                uploadFile.ChunkCount.ToString() + Notus.Core.Variable.CommonDelimeterChar +
                uploadFile.Level.ToString() + Notus.Core.Variable.CommonDelimeterChar +
                Notus.Core.Function.BoolToStr(uploadFile.WaterMarkIsLight) + Notus.Core.Variable.CommonDelimeterChar +
                uploadFile.PublicKey;
            return uploadFile;
        }

        public static string TokenGenerate(
            string PublicKey,
            Notus.Core.Variable.TokenInfoStruct InfoData,
            Notus.Core.Variable.SupplyStruct TokenSupplyData
        )
        {
            //Notus.Core.Variable.
            return
                PublicKey + Notus.Core.Variable.CommonDelimeterChar +

                InfoData.Name + Notus.Core.Variable.CommonDelimeterChar +
                InfoData.Tag + Notus.Core.Variable.CommonDelimeterChar +

                    Notus.Core.Function.BoolToStr(InfoData.Logo.Used) + Notus.Core.Variable.CommonDelimeterChar +
                    InfoData.Logo.Base64 + Notus.Core.Variable.CommonDelimeterChar +
                    InfoData.Logo.Url + Notus.Core.Variable.CommonDelimeterChar +
                    InfoData.Logo.Source + Notus.Core.Variable.CommonDelimeterChar +

                TokenSupplyData.Supply.ToString() + Notus.Core.Variable.CommonDelimeterChar +
                TokenSupplyData.Decimal.ToString() + Notus.Core.Variable.CommonDelimeterChar +
                Notus.Core.Function.BoolToStr(TokenSupplyData.Resupplyable);
        }
    }
}

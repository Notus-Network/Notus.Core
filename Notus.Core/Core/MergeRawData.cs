using System;

namespace Notus.Core
{
    public class MergeRawData
    {
        public static Notus.Variable.Struct.GenericSignStruct GenericSign(string PrivateKey)
        {
            string PublicKeyStr = Notus.Wallet.ID.Generate(PrivateKey);
            string TimeStr = DateTime.Now.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText);
            return new Notus.Variable.Struct.GenericSignStruct()
            {
                PublicKey = PublicKeyStr,
                Time = TimeStr,
                Sign = Notus.Wallet.ID.Sign(PublicKeyStr + TimeStr, PrivateKey)
            };
        }
        public static string Transaction(string Sender, string Receiver, string Volume, string UnlockTime, string Currency)
        {
            return Sender + Notus.Variable.Constant.CommonDelimeterChar +
            Receiver + Notus.Variable.Constant.CommonDelimeterChar +
            Volume + Notus.Variable.Constant.CommonDelimeterChar +
            UnlockTime + Notus.Variable.Constant.CommonDelimeterChar +
            Currency;
        }

        public static Notus.Variable.Struct.FileTransferStruct FileUpload(Notus.Variable.Struct.FileTransferStruct uploadFile)
        {

            uploadFile.Sign =
                uploadFile.BlockType.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.FileName + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.FileSize.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.FileHash + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.ChunkSize.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.ChunkCount.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.Level.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                Notus.Toolbox.Text.BoolToStr(uploadFile.WaterMarkIsLight) + Notus.Variable.Constant.CommonDelimeterChar +
                uploadFile.PublicKey;
            return uploadFile;
        }
        public static string StorageOnChain(Notus.Variable.Struct.StorageOnChainStruct StorageData)
        {

            return 
                StorageData.Name + Notus.Variable.Constant.CommonDelimeterChar +
                StorageData.Size.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                StorageData.Hash + Notus.Variable.Constant.CommonDelimeterChar +
                Notus.Toolbox.Text.BoolToStr(StorageData.Encrypted) + Notus.Variable.Constant.CommonDelimeterChar +
                StorageData.PublicKey;
        }

        public static string TokenGenerate(
            string PublicKey,
            Notus.Variable.Struct.TokenInfoStruct InfoData,
            Notus.Variable.Struct.SupplyStruct TokenSupplyData
        )
        {
            //Notus.Variable.Struct.
            return
                PublicKey + Notus.Variable.Constant.CommonDelimeterChar +

                InfoData.Name + Notus.Variable.Constant.CommonDelimeterChar +
                InfoData.Tag + Notus.Variable.Constant.CommonDelimeterChar +

                    Notus.Toolbox.Text.BoolToStr(InfoData.Logo.Used) + Notus.Variable.Constant.CommonDelimeterChar +
                    InfoData.Logo.Base64 + Notus.Variable.Constant.CommonDelimeterChar +
                    InfoData.Logo.Url + Notus.Variable.Constant.CommonDelimeterChar +
                    InfoData.Logo.Source + Notus.Variable.Constant.CommonDelimeterChar +

                TokenSupplyData.Supply.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                TokenSupplyData.Decimal.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                Notus.Toolbox.Text.BoolToStr(TokenSupplyData.Resupplyable);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Notus.Core
{
    public class MergeRawData
    {
        public static string MultiWalletID
        (
            string creatorWallet,
            List<string> walletList,
            Notus.Variable.Enum.MultiWalletType walletType
        )
        {
            walletList.Sort();
            string walletListText = string.Join(Notus.Variable.Constant.CommonDelimeterChar, walletList.ToArray());
            string signRawStr =
                creatorWallet + Notus.Variable.Constant.CommonDelimeterChar +
                walletListText + Notus.Variable.Constant.CommonDelimeterChar +
                walletType.ToString();

            return signRawStr;
        }
        public static string WalletSafe(
            string walletKey,
            string publicKey,
            string pass,
            ulong unlockTime
        )
        {
            return
                walletKey + Notus.Variable.Constant.CommonDelimeterChar +
                publicKey + Notus.Variable.Constant.CommonDelimeterChar +
                pass + Notus.Variable.Constant.CommonDelimeterChar +
                unlockTime.ToString();
        }

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
        public static string Transaction(Notus.Variable.Struct.CryptoTransactionStruct transactionData)
        {
            return Transaction(
                transactionData.Sender,
                transactionData.Receiver,
                transactionData.Volume,
                transactionData.CurrentTime.ToString(),
                transactionData.UnlockTime.ToString(),
                transactionData.Currency
            );
        }
        public static string Transaction(
            string Sender, 
            string Receiver, 
            string Volume, 
            string CurrentTime, 
            string UnlockTime, 
            string Currency
        )
        {
            return Sender + Notus.Variable.Constant.CommonDelimeterChar +
            Receiver + Notus.Variable.Constant.CommonDelimeterChar +
            Volume + Notus.Variable.Constant.CommonDelimeterChar +
            CurrentTime + Notus.Variable.Constant.CommonDelimeterChar +
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

        public static string ApproveMultiWalletTransaction(bool Approve, string TransactionId, ulong CurrentTime)
        {
            return
                Notus.Toolbox.Text.BoolToStr(Approve) + Notus.Variable.Constant.CommonDelimeterChar +
                TransactionId + Notus.Variable.Constant.CommonDelimeterChar +
                CurrentTime.ToString().Substring(0, 14);
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

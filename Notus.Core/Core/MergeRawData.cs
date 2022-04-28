using System;
using System.Text.Json;
namespace Notus.Core
{
    public class MergeRawData
    {
        public static string Transaction(string Sender, string Receiver, string Volume)
        {
            return Sender + Notus.Core.Variable.CommonDelimeterChar +
            Receiver + Notus.Core.Variable.CommonDelimeterChar +
            Volume;
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

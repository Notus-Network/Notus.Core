/*
using System;
using System.Text.Json;
namespace Notus.Cache
{
    public class Token : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        public bool Add(Notus.Variable.Struct.BlockStruct_160 TokenObj)
        {
            Console.WriteLine("Con")
            bool returnVal = false;

            string tmpTokenTagHexName = Notus.Core.Function.ConvertTagName(TokenObj.Info.Tag);
            //string tmpCreatorAddress = Notus.Wallet.ID.GetAddressWithPublicKey(TokenObj.Creation.Key);
            
            using
                (Notus.Mempool MP_TokenList =
                new Notus.Mempool(
                    Notus.Toolbox.IO.GetFolderName(Obj_Settings.Network, Notus.Variable.Constant.StorageFolderName.Common) +
                    "token_name_list"
                )
            )
            {
                MP_TokenList.AsyncActive = false;
                string CurrentTagNameStr = MP_TokenList.Get(Notus.Core.Function.ConvertTagName(TokenObj.Info.Tag), "");
                if (CurrentTagNameStr.Length > 0)
                {
                    MP_TokenList.Set(tmpTokenTagHexName, JsonSerializer.Serialize(TokenObj), true);
                    returnVal = false;
                }
                else
                {
                    MP_TokenList.Add(Notus.Core.Function.ConvertTagName(TokenObj.Info.Tag), JsonSerializer.Serialize(TokenObj));
                }
            }
            //using (
                //Notus.Mempool ObjMp_LockTokenTagList =
                //new Notus.Mempool(
                    //Notus.Variable.Constant.LocalDbStorageDirectoryName +
                    //Notus.Core.Function.NetworkTypeStr(Obj_Settings.Network)+
                    //"lock_token_name"
                //)
            //)
            //{
                //ObjMp_LockTokenTagList.AsyncActive = false;
                //ObjMp_LockTokenTagList.Remove(tmpTokenTagHexName);
            //}
            using (
                Notus.Mempool ObjMp_TokenBalance =
                new Notus.Mempool(
                    Notus.Toolbox.IO.GetFolderName(Obj_Settings.Network, Notus.Variable.Constant.StorageFolderName.Common) +
                    "token_balance"
                )
            )
            {
                ObjMp_TokenBalance.AsyncActive = false;

                string tmpTotalSupply = TokenObj.Reserve.Supply.ToString();
                if (TokenObj.Reserve.Decimal > 0)
                {
                    tmpTotalSupply += new string('0', TokenObj.Reserve.Decimal);
                }

                string tmpCreatorAddress = Notus.Wallet.ID.GetAddressWithPublicKey(TokenObj.Creation.PublicKey);

                string tmpCurrentList = ObjMp_TokenBalance.Get(tmpCreatorAddress, "");
                if (tmpCurrentList.Length == 0)
                {
                    System.Collections.Generic.Dictionary<string, string> tmpTokenList = new System.Collections.Generic.Dictionary<string, string>();
                    tmpTokenList.Add(tmpTokenTagHexName, tmpTotalSupply);
                    ObjMp_TokenBalance.Add(tmpCreatorAddress, JsonSerializer.Serialize(tmpTokenList));
                }
                else
                {
                    System.Collections.Generic.Dictionary<string, string> tmpTokenList = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(tmpCurrentList);
                    tmpTokenList.Add(tmpTokenTagHexName, tmpTotalSupply);
                    ObjMp_TokenBalance.Set(tmpCreatorAddress, JsonSerializer.Serialize(tmpTokenList));
                }
            }
            return returnVal;
        }
        public Token()
        {

        }
        ~Token()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}
*/
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Notus.Block
{
    public class Generate : IDisposable
    {
        private string ValidatorWalletKeyStr = "validatorKey";
        public string ValidatorWalletKey
        {
            set
            {
                ValidatorWalletKeyStr = value;
            }
            get
            {
                return ValidatorWalletKeyStr;
            }
        }
        
        private Notus.Variable.Class.BlockData FillValidatorKeyData(Notus.Variable.Class.BlockData BlockData)
        {
            using (Notus.Nonce.Calculate CalculateObj = new Notus.Nonce.Calculate())
            {
                BlockData.validator.map.block.Clear();
                BlockData.validator.map.data.Clear();
                BlockData.validator.map.info.Clear();
                BlockData.validator.count.Clear();

                int HowManyNonceStep = CalculateObj.NonceStepCount(
                                    BlockData.info.nonce.type,
                                    BlockData.info.nonce.method,
                                    BlockData.info.nonce.difficulty
                                );
                BlockData.validator.map.block.Add(1000, ValidatorWalletKeyStr);
                BlockData.validator.map.data.Add(1000, ValidatorWalletKeyStr);
                BlockData.validator.map.info.Add(1000, ValidatorWalletKeyStr);
                BlockData.validator.count.Add(ValidatorWalletKeyStr, (HowManyNonceStep * 3));
            }
            return BlockData;
        }
        private string GenerateNonce(string PureTextForNonce, Notus.Variable.Class.BlockData BlockData)
        {
            if (BlockData.info.nonce.type == 1)
            {
                return new Notus.Nonce.Calculate().Slide(
                    BlockData.info.nonce.method,
                    PureTextForNonce,
                    BlockData.info.nonce.difficulty
                );
            }
            return new Notus.Nonce.Calculate().Bounce(
                BlockData.info.nonce.method,
                PureTextForNonce,
                BlockData.info.nonce.difficulty
            );
        }

        private bool CheckValidNonce(string PureTextForNonce, Notus.Variable.Class.BlockData BlockData, string NonceValueStr)
        {
            if (BlockData.info.nonce.type == 1)
            {
                return new Notus.Nonce.Verify().Slide(
                    BlockData.info.nonce.method,
                    PureTextForNonce,
                    BlockData.info.nonce.difficulty,
                    NonceValueStr
                );
            }
            else
            {
                return new Notus.Nonce.Verify().Bounce(
                    BlockData.info.nonce.method,
                    PureTextForNonce,
                    BlockData.info.nonce.difficulty,
                    NonceValueStr
                );
            }
        }
        private Notus.Variable.Class.BlockData Make_Info(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = FirstString_Info(BlockData);
            BlockData.nonce.info = GenerateNonce(TmpText, BlockData);
            Notus.HashLib.Sasha hashObj = new Notus.HashLib.Sasha();
            BlockData.hash.info = hashObj.ComputeHash(
                TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.nonce.info,
                false
            );
            return BlockData;
        }

        private Notus.Variable.Class.BlockData Make_Validator(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = FirstString_Validator(BlockData);
            Notus.Hash hashObj = new Notus.Hash();
            BlockData.validator.sign = hashObj.CommonHash("sasha", TmpText);

            return BlockData;
        }
        private Notus.Variable.Class.BlockData Make_Data(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = BlockData.cipher.data + Notus.Variable.Constant.CommonDelimeterChar + BlockData.cipher.ver;
            Notus.Hash hashObj = new Notus.Hash();

            BlockData.cipher.sign = hashObj.CommonHash("sasha", TmpText);

            TmpText = TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.cipher.sign;

            BlockData.nonce.data = GenerateNonce(TmpText, BlockData);

            BlockData.hash.data = new Notus.HashLib.Sasha().ComputeHash(
                TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.nonce.data,
                false
            );
            return BlockData;
        }

        private Notus.Variable.Class.BlockData Make_Block(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = BlockData.hash.data + Notus.Variable.Constant.CommonDelimeterChar + BlockData.hash.info;
            BlockData.nonce.block = GenerateNonce(TmpText, BlockData);
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            BlockData.hash.block = sashaObj.ComputeHash(
                TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.nonce.block,
                false
            );
            return BlockData;
        }

        private Notus.Variable.Class.BlockData Make_FINAL(Notus.Variable.Class.BlockData BlockData)
        {

            string TmpText = FirstString_Block(BlockData);
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            BlockData.hash.FINAL = sashaObj.ComputeHash(
                TmpText,
                false
            );
            Notus.HashLib.Sasha sashaObj2 = new Notus.HashLib.Sasha();
            BlockData.sign = sashaObj2.ComputeHash(
                BlockData.hash.info + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.data + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.block + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.FINAL,
                false
            );
            return BlockData;
        }

        public Notus.Variable.Class.BlockData Make(Notus.Variable.Class.BlockData BlockData, int BlockVersion = 1000)
        {
            BlockData.info.version = BlockVersion;
            if (BlockVersion == 1000)
            {
                if (BlockData.info.nonce.method == 0)
                {
                    BlockData.info.nonce.method = 1;
                }
                else
                {
                    if (Notus.Variable.Constant.NonceHashLength.ContainsKey(BlockData.info.nonce.method) == false)
                    {
                        BlockData.info.nonce.method = 1;
                    }
                }

                if (BlockData.info.nonce.difficulty == 0)
                {
                    BlockData.info.nonce.difficulty = 1;
                }

                BlockData = FillValidatorKeyData(BlockData);
                BlockData = Make_Data(BlockData);
                BlockData = Make_Info(BlockData);
                BlockData = Make_Block(BlockData);
                BlockData = Make_Validator(BlockData);
                BlockData = Make_FINAL(BlockData);
            }

            return BlockData;
        }


        public bool Verify(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = BlockData.cipher.data + Notus.Variable.Constant.CommonDelimeterChar + BlockData.cipher.ver;
            Notus.Hash hashObj = new Notus.Hash();
            string ControlStr = hashObj.CommonHash("sasha", TmpText);
            if (string.Equals(BlockData.cipher.sign, ControlStr) == false)
            {
                return false;
            }
            TmpText = TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.cipher.sign;
            if (CheckValidNonce(TmpText, BlockData, BlockData.nonce.data) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            ControlStr = sashaObj.ComputeHash(
                TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.nonce.data,
                false
            );
            if (string.Equals(BlockData.hash.data, ControlStr) == false)
            {
                return false;
            }
            TmpText = FirstString_Info(BlockData);
            if (CheckValidNonce(TmpText, BlockData, BlockData.nonce.info) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj2 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj2.ComputeHash(
                TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.nonce.info,
                false
            );
            if (string.Equals(BlockData.hash.info, ControlStr) == false)
            {
                return false;
            }
            TmpText = BlockData.hash.data + Notus.Variable.Constant.CommonDelimeterChar + BlockData.hash.info;
            if (CheckValidNonce(TmpText, BlockData, BlockData.nonce.block) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj3 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj3.ComputeHash(
                TmpText + Notus.Variable.Constant.CommonDelimeterChar + BlockData.nonce.block,
                false
            );
            if (string.Equals(BlockData.hash.block, ControlStr) == false)
            {
                return false;
            }
            TmpText = FirstString_Validator(BlockData);
            Notus.Hash hashObj2 = new Notus.Hash();
            ControlStr = hashObj2.CommonHash("sasha", TmpText);
            if (string.Equals(BlockData.validator.sign, ControlStr) == false)
            {
                return false;
            }

            TmpText = FirstString_Block(BlockData);
            Notus.HashLib.Sasha sashaObj4 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj4.ComputeHash(TmpText, false);
            if (string.Equals(BlockData.hash.FINAL, ControlStr) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj5 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj5.ComputeHash(
                BlockData.hash.info + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.data + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.block + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.FINAL,
                false
            );
            if (string.Equals(BlockData.sign, ControlStr) == false)
            {
                return false;
            }
            return true;
        }




        private string FirstString_Block(Notus.Variable.Class.BlockData BlockData)
        {
            return
                BlockData.validator.sign + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.prev + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.info.rowNo.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
                BlockNonce_GetPrevListStr(BlockData) + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.data + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.info + Notus.Variable.Constant.CommonDelimeterChar +
                BlockData.hash.block;
        }
        private string FirstString_Validator(Notus.Variable.Class.BlockData BlockData)
        {
            return
            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.data) +
            Notus.Variable.Constant.CommonDelimeterChar +

            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.info) +
            Notus.Variable.Constant.CommonDelimeterChar +

            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.block) +
            Notus.Variable.Constant.CommonDelimeterChar +

            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.block) +
            Notus.Variable.Constant.CommonDelimeterChar +

            BlockNonce_ValidatorMapList_StringAndInt(BlockData.validator.count);
        }
        private string FirstString_Info(Notus.Variable.Class.BlockData BlockData)
        {
            return
            BlockData.info.version.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
            BlockData.info.type.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
            BlockData.info.uID + Notus.Variable.Constant.CommonDelimeterChar +
            BlockData.info.time + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.multi) + Notus.Variable.Constant.CommonDelimeterChar +

            BlockData.info.nonce.method.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
            BlockData.info.nonce.type.ToString() + Notus.Variable.Constant.CommonDelimeterChar +
            BlockData.info.nonce.difficulty.ToString() + Notus.Variable.Constant.CommonDelimeterChar +

            BlockData.info.node.id + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.master) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.replicant) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.broadcaster) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.validator) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.executor) + Notus.Variable.Constant.CommonDelimeterChar +

            BoolToStr(BlockData.info.node.keeper.key) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.keeper.block) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.keeper.file) + Notus.Variable.Constant.CommonDelimeterChar +
            BoolToStr(BlockData.info.node.keeper.tor) + Notus.Variable.Constant.CommonDelimeterChar;
        }

        private string BlockNonce_ValidatorMapList_StringAndInt(Dictionary<string, int> DicList)
        {
            string TmpStr = "";
            bool isFirst = true;
            foreach (System.Collections.Generic.KeyValuePair<string, int> entry in DicList)
            {
                if (isFirst)
                {
                    TmpStr = $"{entry.Key}={entry.Value}";
                    isFirst = false;
                }
                else
                {
                    TmpStr = TmpStr + $";{entry.Key}={entry.Value}";
                }
            }
            return TmpStr;
        }
        private string BlockNonce_ValidatorMapList_IntAndString(Dictionary<int, string> DicList)
        {
            string TmpStr = "";
            bool isFirst = true;
            foreach (System.Collections.Generic.KeyValuePair<int, string> entry in DicList)
            {
                if (isFirst)
                {
                    TmpStr = $"{entry.Key}={entry.Value}";
                    isFirst = false;
                }
                else
                {
                    TmpStr = TmpStr + $";{entry.Key}={entry.Value}";
                }
            }
            return TmpStr;
        }
        private string BlockNonce_GetPrevListStr(Notus.Variable.Class.BlockData BlockPool)
        {
            string TmpStr = "";
            bool isFirst = true;
            foreach (System.Collections.Generic.KeyValuePair<int, string> entry in BlockPool.info.prevList)
            {
                if (isFirst)
                {
                    TmpStr = $"{entry.Key}={entry.Value}";
                    isFirst = false;
                }
                else
                {
                    TmpStr = TmpStr + $";{entry.Key}={entry.Value}";
                }
            }
            return TmpStr;
        }
        private string BoolToStr(bool tmpBoolVal)
        {
            if (tmpBoolVal == true) { return "1"; }
            return "0";
        }
        public Generate()
        {

        }
        public Generate(string validatorWalletKey)
        {
            ValidatorWalletKeyStr = validatorWalletKey;
        }
        ~Generate()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}

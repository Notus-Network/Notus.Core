/*
using System;
using System.Collections.Generic;
using System.Linq;

namespace Notus.Hashlib
{
    public class Account
    {
        public readonly static Dictionary<string, string> AlphabetForAccountType = new Dictionary<string, string>()
        {
            { "m", "865b1oxj34cf2k7nhlqtwysrzmigapud9ve" },
            { "i", "b7n3lqt2azvs6xdmchfkojir4pue95wyg18" },
            { "p", "u45il9f2bpvc8s1eorxjndwmthakg3qyz76" },
            { "s", "5lsv3hknew49btqj17fr6ycxpa8doi2mzug" },
            { "c", "m51z3lkjhnabyew48dprs2oi6vcf9gutqx7" },
            { "g", "5ro6ynea4bzx7d9ipuhkfc2w1qg8svlmjt3" },
            { "a", "imdzafk4w8oqbx671utjysph9lec2vnrg35" },
            { "u", "hmtz2fp3ykdie17o4v96ubnsg5xwcajrq8l" }
        };

        public string Calculate(
            string accountTypeStr,            // gelen verinin türü, mail, tc. no
            string adminHashStr,        // hesabın admin hash'i
            string rawAccountStr,            // işlenecek hesap verisi
            string hostStr,             // hesap sahibinin host adresi
            string setupNo              // kurulum numarası
        )
        {
            if (hostStr[0] != '@') { hostStr = '@' + hostStr; }

            string typeAlfabeStr = string.Empty;
            if(AlphabetForAccountType.ContainsKey(accountTypeStr)==false){
				typeAlfabeStr = AlphabetForAccountType["u"];
			}else{
				typeAlfabeStr = AlphabetForAccountType[accountTypeStr];
			}
			
            string commonKeyStr = accountTypeStr + "|" + setupNo + "|" + adminHashStr;
            string pureQueryStr = accountTypeStr + "|" + rawAccountStr;
            string hostQueryStr = accountTypeStr + "|" + rawAccountStr + hostStr;
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();

            string[] pureDizi = Notus.Core.Function.SplitByLength(
                sashaObj.ComputeSign(
                    pureQueryStr, 
                    false, 
                    typeAlfabeStr, 
                    commonKeyStr
                ),
                2
            ).ToArray();

            string[] hostDizi = Notus.Core.Function.SplitByLength(
                sashaObj.ComputeSign(
                    hostQueryStr,
                    false,
                    typeAlfabeStr,
                    commonKeyStr
                ),
                2
            ).ToArray();

            string sonucStr = "";
            for (int i = 0; i < 96; i++)
            {
                sonucStr = sonucStr + pureDizi[i] + hostDizi[i];
            }
            return accountTypeStr + sonucStr;

        }
    }
}
*/
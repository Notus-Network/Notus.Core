using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Notus.Communication
{
    public static class Web3
    {
        public static async Task<Notus.Variable.Struct.CryptoTransactionResult> LockAccount(
            string WalletKey,
            ulong UnlockTime,
            string PublicKey,
            string Sign,
            Notus.Variable.Enum.NetworkType currentNetwork = Notus.Variable.Enum.NetworkType.MainNet,
            bool isSsl = false
        )
        {
            string tmpResult = await Notus.Network.Node.FindAvailable("currency/list/", currentNetwork, Notus.Variable.Enum.NetworkLayer.Layer1, isSsl);
            return JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionResult>(tmpResult);
        }

        /// <summary>
        /// Gets Currency List with given network via HTTP request. 
        /// </summary>
        /// <param name="currentNetwork">Current Network for Request.</param>
        /// <returns>Returns <see cref="Notus.Variable.Struct.CurrencyList"/>.</returns>
        public static async Task<List<Notus.Variable.Struct.CurrencyList>> GetCurrencyList(Notus.Variable.Enum.NetworkType currentNetwork = Notus.Variable.Enum.NetworkType.MainNet, bool isSsl = false)
        {
            string tmpResult = await Notus.Network.Node.FindAvailable("currency/list/", currentNetwork, Notus.Variable.Enum.NetworkLayer.Layer1, isSsl);
            return JsonSerializer.Deserialize<List<Notus.Variable.Struct.CurrencyList>>(tmpResult);
        }

        /// <summary>
        /// Gets Balance with given network and wallet key via HTTP request. 
        /// </summary>
        /// <param name="WalletKey">Wallet key of the wallet whose balance will be shown.</param>
        /// <param name="currentNetwork">Current Network for Request.</param>
        /// <returns>Returns <see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static async Task<Dictionary<string, Dictionary<ulong, string>>> Balance(
            string WalletKey,
            Notus.Variable.Enum.NetworkType currentNetwork = Notus.Variable.Enum.NetworkType.MainNet, bool isSsl = false
        )
        {
            string tmpResult = await Notus.Network.Node.FindAvailable("balance/" + WalletKey + "/", currentNetwork, Notus.Variable.Enum.NetworkLayer.Layer1, isSsl);
            Notus.Variable.Struct.WalletBalanceStruct tmpBalanceVal = JsonSerializer.Deserialize<Notus.Variable.Struct.WalletBalanceStruct>(tmpResult);
            return tmpBalanceVal.Balance;
        }

        /// <summary>
        /// It performs the airdrop operation with the given wallet address and network type via HTTP request.
        /// </summary>
        /// <param name="WalletKey">The wallet key of the wallet to be airdropped.</param>
        /// <param name="currentNetwork">Current Network for Request.</param>
        /// <returns>Returns <see cref="Notus.Variable.Struct.CryptoTransactionResult"/>.</returns>
        public static async Task<Notus.Variable.Struct.CryptoTransactionResult> AirDrop(string WalletKey, Notus.Variable.Enum.NetworkType currentNetwork = Notus.Variable.Enum.NetworkType.MainNet, bool isSsl = false)
        {
            string tmpResult = await Notus.Network.Node.FindAvailable("airdrop/" + WalletKey + "/", currentNetwork, Notus.Variable.Enum.NetworkLayer.Layer1, isSsl);
            Notus.Variable.Struct.CryptoTransactionResult tmpAirDrop = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionResult>(tmpResult);
            return tmpAirDrop;
        }

        /// <summary>
        /// TO DO.
        /// </summary>
        public static async Task<Notus.Variable.Enum.BlockStatusCode> GetStatus(string BlockUid, Notus.Variable.Enum.NetworkType CurrentNetwork, Notus.Variable.Enum.NetworkLayer CurrentLayer = Notus.Variable.Enum.NetworkLayer.Layer1, bool isSsl = false)
        {
            string tmpResult = await Notus.Network.Node.FindAvailable("block/status/" + BlockUid + "/", CurrentNetwork, CurrentLayer, isSsl);
            Notus.Variable.Enum.BlockStatusCode tmpAirDrop = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(tmpResult);
            return tmpAirDrop;
        }

        /// <summary>
        /// TO DO.
        /// </summary>
        public static Dictionary<string, Notus.Variable.Enum.BlockStatusCode> GetWhichMultiTransactionNeedMySign(
            string WalletKey,
            Notus.Variable.Enum.NetworkType CurrentNetwork, bool isSsl = false)
        {
            Dictionary<string, Notus.Variable.Enum.BlockStatusCode>? resultList = new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
            string responseData = Notus.Network.Node.FindAvailableSync(
                "multi/pool/" + WalletKey, CurrentNetwork,
                Notus.Variable.Enum.NetworkLayer.Layer1
            );
            if (responseData.Length > 0)
            {
                try
                {
                    resultList = JsonSerializer.Deserialize<Dictionary<string, Notus.Variable.Enum.BlockStatusCode>>(responseData);
                }
                catch
                {

                }
            }
            if (resultList == null)
            {
                return new Dictionary<string, Notus.Variable.Enum.BlockStatusCode>();
            }
            return resultList;
        }

        public static Notus.Variable.Struct.CryptoTransactionResult ApproveMultiWalletTransaction(
            string MultiWalletKey,
            bool Approve,
            string TransactionId,
            ulong currentTime,
            string Sign,
            string PublicKey,
            Notus.Variable.Enum.NetworkType CurrentNetwork,
            bool isSsl = false
        )
        {
            Notus.Variable.Struct.CryptoTransactionResult? resultList = new Notus.Variable.Struct.CryptoTransactionResult();
            string responseData = Notus.Network.Node.FindAvailableSync(
                "multi/transaction/approve/" + MultiWalletKey,
                new Dictionary<string, string>()
                {
                    { "data",
                        JsonSerializer.Serialize(
                            new Notus.Variable.Struct.MultiWalletTransactionApproveStruct()
                            {
                                Approve=Approve,
                                CurrentTime=currentTime,
                                PublicKey=PublicKey,
                                TransactionId= TransactionId,
                                Sign=Sign
                            }
                        )
                    }
                },
                CurrentNetwork,
                Notus.Variable.Enum.NetworkLayer.Layer1
            );
            if (responseData.Length > 0)
            {
                try
                {
                    resultList = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionResult>(responseData);
                }
                catch
                {

                }
            }
            if (resultList == null)
            {
                return new Notus.Variable.Struct.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "",
                    ID = "",
                    Result = Variable.Enum.BlockStatusCode.WrongParameter
                };
            }
            return resultList;
        }

        public static Notus.Variable.Enum.BlockStatusCode StoreFileOnChain(string PrivateKeyHex, string FileAddress, bool LocalFile, Notus.Variable.Enum.NetworkType CurrentNetwork = Notus.Variable.Enum.NetworkType.MainNet, bool isSsl = false)
        {
            int sleepTime = 2500;
            byte errorCountForSleepTime = 0;

            using MemoryStream ms = new MemoryStream();
            string fileName = "";
            if (LocalFile == false)
            {
                WebClient client = new WebClient();
                Stream imgstream = client.OpenRead(FileAddress);

                byte[] buffer = new byte[4096];
                int read;
                while ((read = imgstream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                fileName = System.IO.Path.GetFileName(new Uri(FileAddress).LocalPath);
            }
            else
            {
                fileName = System.IO.Path.GetFileName(FileAddress);
                using (FileStream file = new FileStream(FileAddress, FileMode.Open, FileAccess.Read))
                {
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);
                    ms.Write(bytes, 0, (int)file.Length);
                }
            }
            byte[] fileArray = ms.ToArray();
            uint fileSize = (uint)ms.Length;

            Notus.Variable.Struct.StorageOnChainStruct storageObj = new Notus.Variable.Struct.StorageOnChainStruct()
            {
                Name = fileName,
                Size = fileSize,
                Hash = new Notus.Hash().CommonHash("sasha", ms.ToArray()),
                Encrypted = false,
                PublicKey = Notus.Wallet.ID.Generate(PrivateKeyHex),
                Sign = "",
                Balance = new Notus.Variable.Struct.BalanceAfterBlockStruct()
                {
                    Wallet = "",
                    Balance = new Dictionary<string, string>(),
                    Fee = "0",
                    RowNo = 0,
                    UID = ""
                }
            };
            storageObj.Sign = Notus.Wallet.ID.Sign(Notus.Core.MergeRawData.StorageOnChain(storageObj), PrivateKeyHex);

            string responseData = Notus.Network.Node.FindAvailableSync(
                "storage/file/new",
                new Dictionary<string, string>()
                {
                    {
                        "data",
                        JsonSerializer.Serialize(storageObj)
                    }
                },
                Notus.Variable.Enum.NetworkType.MainNet,
                Notus.Variable.Enum.NetworkLayer.Layer1
            );

            Console.WriteLine(responseData);
            Notus.Variable.Struct.BlockResponse tmpStartObj = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockResponse>(responseData);

            if (tmpStartObj.UID.Length > 0 && tmpStartObj.Result == Notus.Variable.Enum.BlockStatusCode.AddedToQueue)
            {
                Console.WriteLine("Pre-Wait");
                Thread.Sleep(4000);

                bool exitWhileLoop = false;
                while (exitWhileLoop == false)
                {
                    string controlResponse = Notus.Network.Node.FindAvailableSync(
                        "block/" + tmpStartObj.UID,
                        Notus.Variable.Enum.NetworkType.MainNet,
                        Notus.Variable.Enum.NetworkLayer.Layer1
                    );
                    Console.WriteLine(controlResponse);
                    if (controlResponse.Length > 100)
                    {
                        // block oluşturulmuş demektir.
                        exitWhileLoop = true;
                    }
                    else
                    {
                        if (errorCountForSleepTime < 10)
                        {
                            errorCountForSleepTime++;
                        }
                        else
                        {
                            sleepTime = 10000;
                        }
                        Console.WriteLine("Sleep for wait : " + sleepTime.ToString());
                        Thread.Sleep(sleepTime);
                    }
                }
            }
            // Console.ReadLine();

            int chunkSize = Notus.Variable.Constant.DefaultChunkSize;
            int chunkLength = (int)Math.Ceiling(System.Convert.ToDouble(fileArray.Length / chunkSize));
            int chunk = 0;
            for (int i = 0; i < chunkLength; i++)
            {
                sleepTime = 2500;
                errorCountForSleepTime = 0;

                byte[] tmpArray = new byte[chunkSize];
                Array.Copy(fileArray, chunk, tmpArray, 0, tmpArray.Length);
                string tmpBaseStr = System.Convert.ToBase64String(tmpArray);
                string tmpDataStr = System.Uri.EscapeDataString(tmpBaseStr);
                string sendDataStr = JsonSerializer.Serialize(
                    new Notus.Variable.Struct.FileChunkStruct()
                    {
                        Count = i,
                        Data = tmpDataStr,
                        UID = tmpStartObj.UID
                    }
                );
                bool innerLoop = false;
                while (innerLoop == false)
                {
                    string responseChunk = Notus.Network.Node.FindAvailableSync(
                        "storage/file/update",
                        new Dictionary<string, string>() {
                            {
                                "data", sendDataStr
                            }
                        },
                        Notus.Variable.Enum.NetworkType.MainNet,
                        Notus.Variable.Enum.NetworkLayer.Layer3
                    );
                    Notus.Variable.Struct.BlockResponse tmpChunkObj = JsonSerializer.Deserialize<Notus.Variable.Struct.BlockResponse>(responseChunk);
                    // Console.WriteLine(responseChunk);
                    // Console.WriteLine("-------------------------");
                    // Console.ReadLine();
                    if (tmpChunkObj.Result == Notus.Variable.Enum.BlockStatusCode.AddedToQueue)
                    {
                        innerLoop = true;
                    }
                    else
                    {
                        if (errorCountForSleepTime < 10)
                        {
                            errorCountForSleepTime++;
                        }
                        else
                        {
                            sleepTime = 10000;
                        }
                        Console.WriteLine("Sleep for wait : " + sleepTime.ToString());
                        Thread.Sleep(sleepTime);
                    }

                }
                chunk += chunkSize;
            }

            // işlemi beklemeye alıyor ve orada kalıyor.
            // dosya yüklenince tüm dosyaları birleştir ve blok içeriğine al

            bool loop = true;
            sleepTime = 2500;
            errorCountForSleepTime = 0;
            while (loop)
            {
                string response = Notus.Network.Node.FindAvailableSync(
                    $"storage/file/status/{tmpStartObj.UID}",
                    Notus.Variable.Enum.NetworkType.MainNet,
                    Notus.Variable.Enum.NetworkLayer.Layer3
                );
                Notus.Variable.Enum.BlockStatusCode ResStruct = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(response);
                if (ResStruct == Notus.Variable.Enum.BlockStatusCode.Completed)
                {
                    loop = false;
                }
                else
                {
                    if (errorCountForSleepTime < 10)
                    {
                        errorCountForSleepTime++;
                    }
                    else
                    {
                        sleepTime = 10000;
                    }
                    Console.WriteLine("Sleep for wait : " + sleepTime.ToString());
                    Thread.Sleep(sleepTime);
                }
            }
            return Notus.Variable.Enum.BlockStatusCode.Completed;
        }

    }
}

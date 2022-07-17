using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Notus.Block
{
    public class Storage : IDisposable
    {
        private Notus.Variable.Enum.NetworkType Val_NetworkType = Notus.Variable.Enum.NetworkType.MainNet;
        public Notus.Variable.Enum.NetworkType Network
        {
            get { return Val_NetworkType; }
            set { Val_NetworkType = value; }
        }
        private Notus.Variable.Enum.NetworkLayer Val_NetworkLayer= Notus.Variable.Enum.NetworkLayer.Layer1;
        public Notus.Variable.Enum.NetworkLayer Layer
        {
            get { return Val_NetworkLayer; }
            set { Val_NetworkLayer = value; }
        }

        private int DefaultBlockGenerateInterval = 3000;

        private string OpenFileName = string.Empty;
        private DateTime FileOpeningTime = DateTime.Now;

        private bool BlockStorageIsRunning = false;
        private Notus.Mempool MP_BlockFile;
        private Notus.Threads.Timer TimerObj;

        private string StoragePreviousHashVal = "";
        public string PreviousId
        {
            get { return StoragePreviousHashVal; }
        }

        private string StorageHashVal = string.Empty;
        public string TotalHash
        {
            get { return StorageHashVal; }
        }
        private bool DebugModeActivated = false;
        public bool DebugMode
        {
            set
            {
                DebugModeActivated = value;
            }
            get
            {
                return DebugModeActivated;
            }
        }
        public void Add(Notus.Variable.Class.BlockData NewBlock)
        {
            MP_BlockFile.Add(NewBlock.info.uID, JsonSerializer.Serialize(NewBlock));
        }
        private void LoadZipFromDirectory()
        {
            List<string> tmp_hashList = new List<string>();

            StoragePreviousHashVal = string.Empty;
            StorageHashVal = string.Empty;
            //Notus.Toolbox.IO.GetFolderName(Val_NetworkType, Notus.Variable.Constant.StorageFolderName.Block)
            
            foreach (string fileName in 
                Directory.GetFiles(
                    Notus.Toolbox.IO.GetFolderName(Val_NetworkType, Val_NetworkLayer, Notus.Variable.Constant.StorageFolderName.Block), "*.zip"
                )
            )
            {
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    System.IO.FileInfo fif = new System.IO.FileInfo(fileName);
                    using (FileStream stream = File.OpenRead(fif.FullName))
                    {
                        
                        string tmp_HashStr = Notus.Convert.Byte2Hex(md5.ComputeHash(stream));
                        tmp_hashList.Add(tmp_HashStr);
                    }
                }
            }
            if (tmp_hashList.Count > 0)
            {
                tmp_hashList.Sort();
                StorageHashVal = string.Join(";", tmp_hashList.ToArray());
            }
        }
        public void ClearStorage()
        {
            DirectoryInfo d = new DirectoryInfo(Notus.Toolbox.IO.GetFolderName(Val_NetworkType, Val_NetworkLayer, Notus.Variable.Constant.StorageFolderName.Block));
            FileInfo[] Files = d.GetFiles("*.zip");
            foreach (FileInfo file in Files)
            {
                File.Delete(file.FullName);
            }

        }
        public (bool, Notus.Variable.Class.BlockData) ReadBlock(string BlockUid)
        {
            try
            {
                bool BlockExist = false;
                string BlockFileName = Notus.Block.Key.GetBlockStorageFileName(BlockUid, true);
                string ZipFileName = Notus.Toolbox.IO.GetFolderName(Val_NetworkType, Val_NetworkLayer, Notus.Variable.Constant.StorageFolderName.Block) + BlockFileName + ".zip";
                if (File.Exists(ZipFileName) == true)
                {
                    Notus.Variable.Class.BlockData NewBlock = null;
                    using (ZipArchive archive = ZipFile.OpenRead(ZipFileName))
                    {
                        ZipArchiveEntry zipEntry = archive.GetEntry(BlockUid + ".json");
                        if (zipEntry != null)
                        {
                            using (StreamReader zipEntryStream = new StreamReader(zipEntry.Open()))
                            {
                                string BlockText = zipEntryStream.ReadToEnd();
                                NewBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(BlockText);
                                BlockExist = true;
                            }
                        }
                    }
                    if (BlockExist == true)
                    {
                        return (true, NewBlock);
                    }
                }
                return (false, null);
            }
            catch (Exception err)
            {
                Notus.Print.Basic(DebugModeActivated, "Storage Error Text : " + err.Message);
                return (false, null);
            }

        }
        private void DeleteBlockFromArchive(string ZipFileName, string BlockJsonFileName)
        {
            using (ZipArchive archive = ZipFile.Open(ZipFileName, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry(BlockJsonFileName);
                if (entry != null)
                {
                    entry.Delete();
                }
            }
        }
        public void AddSync(Notus.Variable.Class.BlockData NewBlock, bool UpdateBlock = false)
        {
            string BlockFileName = Notus.Block.Key.GetBlockStorageFileName(NewBlock.info.uID, true);
            string ZipFileName = Notus.Toolbox.IO.GetFolderName(Val_NetworkType, Val_NetworkLayer, Notus.Variable.Constant.StorageFolderName.Block) + BlockFileName + ".zip";
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                if (string.Equals(OpenFileName, ZipFileName) == false)
                {
                    exitInnerLoop = true;
                }
                else
                {
                    if ((DateTime.Now - FileOpeningTime).TotalSeconds > 3)
                    {
                        exitInnerLoop = true;
                    }
                }
            }

            OpenFileName = ZipFileName;
            FileOpeningTime = DateTime.Now;

            string blockFileName = NewBlock.info.uID + ".json";
            if (UpdateBlock == true)
            {
                DeleteBlockFromArchive(ZipFileName, blockFileName);
            }
            FileMode fileModeObj = FileMode.Open;
            ZipArchiveMode zipModeObj = ZipArchiveMode.Update;
            if (File.Exists(ZipFileName) == false)
            {
                fileModeObj = FileMode.Create;
                zipModeObj = ZipArchiveMode.Create;
            }

            using (FileStream fileStream = new FileStream(ZipFileName, fileModeObj))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, zipModeObj, true))
                {
                    ZipArchiveEntry zipArchiveEntry = archive.CreateEntry(blockFileName, CompressionLevel.Optimal);
                    using (Stream zipStream = zipArchiveEntry.Open())
                    {
                        byte[] blockBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(NewBlock));
                        zipStream.Write(blockBytes, 0, blockBytes.Length);
                    }
                }
            }
            OpenFileName = string.Empty;
        }

        private void AddToZip()
        {
            MP_BlockFile.GetOne((string blockUniqueId, string BlockText) =>
            {

                Notus.Variable.Class.BlockData NewBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(BlockText);
                AddSync(NewBlock);

                MP_BlockFile.Remove(blockUniqueId);
                BlockStorageIsRunning = false;
            });
        }

        public void Close()
        {
            if (TimerObj != null)
            {
                TimerObj.Dispose();
            }
            if (MP_BlockFile != null)
            {
                MP_BlockFile.Dispose();
            }
        }

        public void Start()
        {
            LoadZipFromDirectory();
            MP_BlockFile = new Notus.Mempool(
                Notus.Toolbox.IO.GetFolderName(Val_NetworkType, Val_NetworkLayer, Notus.Variable.Constant.StorageFolderName.Common) + 
                Notus.Variable.Constant.MemoryPoolName["MempoolListBeforeBlockStorage"]);

            TimerObj = new Notus.Threads.Timer(DefaultBlockGenerateInterval);
            TimerObj.Start(() =>
            {
                if (BlockStorageIsRunning == false)
                {
                    BlockStorageIsRunning = true;
                    if (MP_BlockFile.Count() > 0)
                    {
                        AddToZip();
                    }
                    else
                    {
                        BlockStorageIsRunning = false;
                    }
                }
            }, true);
        }
        public Storage(bool AutoStart = true)
        {
            if (AutoStart == true)
            {
                Start();
            }
        }
        public void Dispose()
        {
            Close();
        }
        ~Storage()
        {
            Dispose();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;

namespace Notus.Block
{
    public class Storage : IDisposable
    {
        private int DefaultBlockGenerateInterval = 3000;

        private string OpenFileName = string.Empty;
        private DateTime FileOpeningTime = NVG.NOW.Obj;

        private bool AutoStartObj = false;
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

        public void Add(Notus.Variable.Class.BlockData NewBlock)
        {
            MP_BlockFile.Add(NewBlock.info.uID, JsonSerializer.Serialize(NewBlock));
        }
        private void LoadZipFromDirectory()
        {
            List<string> tmp_hashList = new List<string>();

            StoragePreviousHashVal = string.Empty;
            StorageHashVal = string.Empty;

            foreach (string fileName in
                Directory.GetFiles(
                    Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Block), "*.zip"
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

        public Notus.Variable.Class.BlockData? ReadBlock(string BlockUid)
        {
            //tgz-exception
            /*
            try
            {
                Notus.TGZArchiver BS_Storage = new Notus.TGZArchiver(false, 10);
                string blockDataStr = BS_Storage.getFileFromGZ(BlockUid).GetAwaiter().GetResult();
                Notus.Variable.Class.BlockData? NewBlock =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(
                        blockDataStr
                    );
                if (NewBlock != null)
                {
                    return NewBlock;
                }
            }
            catch (Exception err)
            {
            }
            */

            try
            {
                string BlockFileName = Notus.Block.Key.GetBlockStorageFileName(BlockUid, true);
                string ZipFileName = Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Block) + BlockFileName + ".zip";
                if (File.Exists(ZipFileName) == true)
                {
                    using (ZipArchive archive = ZipFile.OpenRead(ZipFileName))
                    {
                        ZipArchiveEntry? zipEntry = archive.GetEntry(BlockUid + ".json");
                        if (zipEntry != null)
                        {
                            using (StreamReader zipEntryStream = new StreamReader(zipEntry.Open()))
                            {
                                Notus.Variable.Class.BlockData? NewBlock =
                                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(
                                        zipEntryStream.ReadToEnd()
                                    );
                                if (NewBlock != null)
                                {
                                    return NewBlock;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                NP.Basic(NVG.Settings.DebugMode, "Storage Error Text : " + err.Message);
            }
            return null;
        }
        public void AddSync(Notus.Variable.Class.BlockData NewBlock, bool UpdateBlock = false)
        {
            string BlockFileName = Notus.Block.Key.GetBlockStorageFileName(NewBlock.info.uID, true);
            string ZipFileName = Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Block) + BlockFileName + ".zip";
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                if (string.Equals(OpenFileName, ZipFileName) == false)
                {
                    exitInnerLoop = true;
                }
                else
                {
                    if ((NVG.NOW.Obj - FileOpeningTime).TotalSeconds > 3)
                    {
                        exitInnerLoop = true;
                    }
                }
            }

            OpenFileName = ZipFileName;
            FileOpeningTime = NVG.NOW.Obj;

            string blockFileName = NewBlock.info.uID + ".json";
            if (UpdateBlock == true)
            {
                Notus.Archive.DeleteFromInside(ZipFileName, blockFileName);
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
            FileInfo fi2 = new FileInfo(ZipFileName);

            OpenFileName = string.Empty;

            /*
            
            //tgz-exception
            //tgz-exception
            //tgz-exception
            //tgz-exception
            Notus.TGZArchiver BS_Storage = new Notus.TGZArchiver(true, 50);
            Guid guid = BS_Storage.addFileToGZ(NewBlock);
            BS_Storage.WaitUntilIsDone(guid);
            */
        }

        private void AddToZip()
        {
            MP_BlockFile.GetOne((string blockUniqueId, string BlockText) =>
            {

                Notus.Variable.Class.BlockData? NewBlock = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(BlockText);
                if (NewBlock != null)
                {
                    AddSync(NewBlock);
                    MP_BlockFile.Remove(blockUniqueId);
                }
                else
                {

                }
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
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
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
            AutoStartObj = AutoStart;
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

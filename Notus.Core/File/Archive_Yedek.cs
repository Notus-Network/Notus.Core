﻿/*
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;
using Notus.Compression.TGZ;
using System;

namespace Notus
{
    public static class Archive_Yedek
    {
        public static void ClearBlocks(Notus.Globals.Variable.Settings objSettings)
        {
            ClearBlocks(objSettings.Network, objSettings.Layer);
        }
        public static void ClearBlocks(
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            Notus.Print.Log(
                Notus.Variable.Enum.LogLevel.Info,
                10,
                "ClearBlocks",
                "ClearBlocks",
                null,
                null
            );
            DirectoryInfo d = new DirectoryInfo(
                Notus.IO.GetFolderName(
                    networkType,
                    networkLayer,
                    Notus.Variable.Constant.StorageFolderName.Block
                )
            );
            FileInfo[] filesList = d.GetFiles("*.zip");
            foreach (FileInfo fileObj in filesList)
            {
                File.Delete(fileObj.FullName);
            }
        }
        public static void DeleteFromInside(
            string blockUid,
            Notus.Globals.Variable.Settings objSettings,
            bool deleteZipIfEmpty = false
        )
        {
            DeleteFromInside(blockUid, objSettings.Network, objSettings.Layer, deleteZipIfEmpty);
        }
        public static void DeleteFromInside(
            string blockUid,
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool deleteZipIfEmpty = false
        )
        {
            string ZipFileName = Notus.IO.GetFolderName(
                networkType, networkLayer,
                Notus.Variable.Constant.StorageFolderName.Block
            ) + Notus.Block.Key.GetBlockStorageFileName(blockUid, true) + ".zip";
            DeleteFromInside(ZipFileName, blockUid, deleteZipIfEmpty);
        }
        public static void DeleteFromInside(string ZipFileName, List<string> insideFileList, bool deleteZipIfEmpty = false)
        {
            bool removeFile = false;
            using (ZipArchive archive = ZipFile.Open(ZipFileName, ZipArchiveMode.Update))
            {
                for (int i = 0; i < insideFileList.Count; i++)
                {
                    bool fileDeleted = false;
                    while (fileDeleted == false)
                    {
                        ZipArchiveEntry? entry = archive.GetEntry(AddExtensionToBlockUid(insideFileList[i]));
                        if (entry == null)
                        {
                            fileDeleted = true;
                        }
                        else
                        {
                            entry.Delete();
                        }
                    }
                }
                if (deleteZipIfEmpty == true)
                {
                    if (archive.Entries.Count == 0)
                    {
                        removeFile = true;
                    }
                }
            }
            if (removeFile == true)
            {
                Thread.Sleep(1);
                File.Delete(ZipFileName);
            }
        }
        public static void DeleteFromInside(string ZipFileName, string insideFileName, bool deleteZipIfEmpty = false)
        {
            bool removeFile = false;
            using (ZipArchive archive = ZipFile.Open(ZipFileName, ZipArchiveMode.Update))
            {
                ZipArchiveEntry? entry = archive.GetEntry(AddExtensionToBlockUid(insideFileName));
                if (entry != null)
                {
                    entry.Delete();
                }
                if (deleteZipIfEmpty == true)
                {
                    if (archive.Entries.Count == 0)
                    {
                        removeFile = true;
                    }
                }
            }
            if (removeFile == true)
            {
                Thread.Sleep(1);
                File.Delete(ZipFileName);
            }
        }
        private static string AddExtensionToBlockUid(string blockUid)
        {
            return blockUid + (blockUid.IndexOf(".") >= 0 ? "" : ".json");
        }
    }

}

*/
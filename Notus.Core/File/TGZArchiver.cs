using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;
using Notus.Compression.TGZ;
using System;
using System.Threading.Tasks;
using NVG = Notus.Variable.Globals;
namespace Notus
{
    public class TGZArchiver
    {
        private enum TaskType
        {
            AddFile,
            RemoveFile,
            UpdateFile
        }
        // Private Task List for the Thread
        private ConcurrentDictionary<Guid, (TaskType, object)> TaskList = new ConcurrentDictionary<Guid, (TaskType, object)>();
        private bool isRunning = false;
        private static readonly string extension = "tar.gz";
        private string path = "";
        public TGZArchiver(bool fullModeAction = true, int intervalTime=1000)
        {
            path = Notus.IO.GetFolderName(
                NVG.Settings.Network,
                NVG.Settings.Layer,
                Notus.Variable.Constant.StorageFolderName.BlockForTgz
            );
            if (fullModeAction == true)
            {
                Notus.Threads.Timer timer = new Notus.Threads.Timer(intervalTime);
                timer.Start(() =>
                {
                    while (TaskList.Count > 0)
                    {
                        if (isRunning == false)
                        {
                            isRunning = true;
                            KeyValuePair<Guid, (TaskType, object)> task = TaskList.First();
                            bool removeItem = false;
                            switch (task.Value.Item1)
                            {
                                case TaskType.AddFile:
                                    {
                                        (string data, string JsonFileName, string ArchiveFileName) = ((string, string, string))task.Value.Item2;
                                        addFileViaTextToGZPrivate(data, JsonFileName, ArchiveFileName).GetAwaiter().GetResult();
                                        removeItem = true;
                                    }
                                    break;
                                case TaskType.RemoveFile:
                                    {
                                        (string JsonFileName, string ArchiveFileName) = ((string, string))task.Value.Item2;
                                        removeFileFromGZPrivate(JsonFileName, ArchiveFileName).GetAwaiter().GetResult();
                                        removeItem = true;
                                    }
                                    break;
                                case TaskType.UpdateFile:
                                    {
                                        (string data, string JsonFileName, string ArchiveFileName) = ((string, string, string))task.Value.Item2;
                                        updateFileFromGZPrivate(data, JsonFileName, ArchiveFileName).GetAwaiter().GetResult();
                                        removeItem = true;
                                    }
                                    break;
                            }
                            if (removeItem == true)
                            {
                                TaskList.TryRemove(task.Key, out _);
                            }
                        }
                    }
                });
            }
        }
        public static void ClearBlocks()
        {
            ClearBlocks(NVG.Settings.Network, NVG.Settings.Layer);
        }
        public static void ClearBlocks(
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            DirectoryInfo d = new DirectoryInfo(
                Notus.IO.GetFolderName(
                    networkType,
                    networkLayer,
                    Notus.Variable.Constant.StorageFolderName.BlockForTgz
                )
            );
            FileInfo[] filesList = d.GetFiles("*." + extension);
            for(int i=0;i<filesList.Length; i++)
            {
                File.Delete(filesList[i].FullName);
            }
        }
        public void WaitUntilIsDone(Guid guid)
        {
            bool exitLoop=false;
            while (exitLoop == false)
            {
                if (TaskList.ContainsKey(guid) == false)
                {
                    exitLoop = true;
                }
                Thread.Sleep(50);
            }
        }
        public Guid addFileToGZ(string data, string JsonFileName, string ArchiveFileName)
        {
            Guid guid = Guid.NewGuid();
            TaskList.TryAdd(
                guid,
                (TaskType.AddFile, (data, JsonFileName, ArchiveFileName))
            );
            return guid;
        }
        public Guid addFileToGZ(Notus.Variable.Class.BlockData data)
        {
            Guid guid = Guid.NewGuid();
            TaskList.TryAdd(
                guid,
                (TaskType.AddFile, (JsonSerializer.Serialize(data), getFileName(data.info.uID).JsonFileName, getFileName(data.info.uID).ArchiveFileName))
            );
            return guid;
        }
        public void removeFileFromGZ(string JsonFileName, string ArchiveFileName)
        {
            TaskList.TryAdd(
                Guid.NewGuid(),
                (TaskType.RemoveFile, (JsonFileName, ArchiveFileName))
            );
        }
        public void removeFileFromGZ(string uid)
        {
            TaskList.TryAdd(
                Guid.NewGuid(),
                (TaskType.RemoveFile, (getFileName(uid).JsonFileName, getFileName(uid).ArchiveFileName))
            );
        }
        public void updateFileFromGZ(string uid, string data)
        {
            TaskList.TryAdd(
                Guid.NewGuid(),
                (TaskType.UpdateFile, (data, getFileName(uid).JsonFileName, getFileName(uid).ArchiveFileName))
            );
        }
        public void updateFileFromGZ(string data, string JsonFileName, string ArchiveFileName)
        {
            TaskList.TryAdd(
                Guid.NewGuid(),
                (TaskType.UpdateFile, (data, JsonFileName, ArchiveFileName))
            );
        }
        private async Task addFileViaTextToGZPrivate(string data, string JsonFileName, string ArchiveFileName)
        {
            try
            {
                if (JsonFileName.Contains(".json") == false)
                    JsonFileName += ".json";
                if (ArchiveFileName.Contains("."+ extension) == false)
                    ArchiveFileName += "."+ extension;

                // Check if targz file exists
                if (File.Exists(path + ArchiveFileName) == false)
                {
                    // Create new targz file
                    using (Stream stream = File.Create(path + ArchiveFileName))
                    using (Stream gzipStream = new GZipOutputStream(stream))
                    using (TarOutputStream tarArchive = new TarOutputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        TarEntry entry = TarEntry.CreateTarEntry(JsonFileName);
                        entry = fixHash(entry);
                        entry.TarHeader.Size = data.Length;
                        entry.TarHeader.Name = JsonFileName;
                        tarArchive.PutNextEntry(entry);
                        tarArchive.Write(System.Text.Encoding.UTF8.GetBytes(data));
                        tarArchive.CloseEntry();
                        tarArchive.Close();
                    }
                }
                else
                {
                    // Add file to existing targz file
                    using (Stream stream = File.Open(path + ArchiveFileName, FileMode.Open))
                    using (Stream gzipStream = new GZipInputStream(stream))
                    using (TarInputStream tarArchive = new TarInputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        // Read all TarArchive and pass it to output stream
                        TarEntry? entry = tarArchive.GetNextEntry();
                        using (Stream outputStream = File.Create(path + ArchiveFileName + ".tmp"))
                        using (Stream gzipOutputStream = new GZipOutputStream(outputStream))
                        using (TarOutputStream tarArchiveOutput = new TarOutputStream(gzipOutputStream, System.Text.Encoding.UTF8))
                        {
                            while (entry != null)
                            {
                                if (entry.Name != JsonFileName)
                                {
                                    tarArchiveOutput.PutNextEntry(entry);
                                    tarArchiveOutput.Write(tarArchive.ReadBytes((int)entry.Size));
                                    tarArchiveOutput.CloseEntry();
                                }
                                entry = tarArchive.GetNextEntry();
                            }
                            // Add new file to TarArchive
                            TarEntry newEntry = TarEntry.CreateTarEntry(JsonFileName);
                            newEntry = fixHash(newEntry);
                            newEntry.TarHeader.Size = data.Length;
                            newEntry.TarHeader.Name = JsonFileName;
                            tarArchiveOutput.PutNextEntry(newEntry);
                            tarArchiveOutput.Write(System.Text.Encoding.UTF8.GetBytes(data));
                            tarArchiveOutput.CloseEntry();
                            tarArchiveOutput.Close();
                        }
                    }

                    // Delete old file and rename new file
                    File.Delete(path + ArchiveFileName);
                    File.Move(path + ArchiveFileName + ".tmp", path + ArchiveFileName);
                }
            }
            catch
            {
                // If file in use, wait and try again
                await Task.Delay(100);
                await addFileViaTextToGZPrivate(data, JsonFileName, ArchiveFileName);
            }


            isRunning = false;
        }
        private async Task removeFileFromGZPrivate(string JsonFileName, string ArchiveFileName)
        {
            try
            {
                if (JsonFileName.Contains(".json") == false)
                    JsonFileName += ".json";
                if (ArchiveFileName.Contains("."+ extension) == false)
                    ArchiveFileName += "."+ extension;

                // Check if targz file exists, if it does not exist, do nothing
                // If it exists, try to remove json file from targz file
                // If json file does not exist, do nothing
                // If targz file is empty after removing json file, delete targz file
                // If targz file is not empty after removing json file, do nothing
                if (File.Exists(path + ArchiveFileName) == true)
                {
                    // Add file to existing targz file
                    using (Stream stream = File.Open(path + ArchiveFileName, FileMode.Open))
                    using (Stream gzipStream = new GZipInputStream(stream))
                    using (TarInputStream tarArchive = new TarInputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        // Read all TarArchive and pass it to output stream
                        TarEntry? entry = tarArchive.GetNextEntry();
                        using (Stream outputStream = File.Create(path + ArchiveFileName + ".tmp"))
                        using (Stream gzipOutputStream = new GZipOutputStream(outputStream))
                        using (TarOutputStream tarArchiveOutput = new TarOutputStream(gzipOutputStream, System.Text.Encoding.UTF8))
                        {
                            while (entry != null)
                            {
                                if (entry.Name != JsonFileName)
                                {
                                    tarArchiveOutput.PutNextEntry(entry);
                                    tarArchiveOutput.Write(tarArchive.ReadBytes((int)entry.Size));
                                    tarArchiveOutput.CloseEntry();
                                }
                                entry = tarArchive.GetNextEntry();
                            }
                            tarArchiveOutput.Close();
                        }
                    }

                    // Delete old file and rename new file
                    File.Delete(path + ArchiveFileName);
                    File.Move(path + ArchiveFileName + ".tmp", path + ArchiveFileName);

                    // Check if targz file is empty
                    bool isEmpty = false;
                    using (Stream stream = File.Open(path + ArchiveFileName, FileMode.Open))
                    using (Stream gzipStream = new GZipInputStream(stream))
                    using (TarInputStream tarArchive = new TarInputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        // Read all TarArchive and pass it to output stream
                        TarEntry? entry = tarArchive.GetNextEntry();
                        if (entry == null)
                            isEmpty = true;
                    }

                    if (isEmpty)
                        File.Delete(path + ArchiveFileName);
                }
            }
            catch
            {
                // If file in use, wait and try again
                await Task.Delay(100);
                await removeFileFromGZPrivate(JsonFileName, ArchiveFileName);
            }

            isRunning = false;
        }
        private async Task updateFileFromGZPrivate(string data, string JsonFileName, string ArchiveFileName)
        {
            try
            {
                if (JsonFileName.Contains(".json") == false)
                    JsonFileName += ".json";
                if (ArchiveFileName.Contains("."+ extension) == false)
                    ArchiveFileName += "."+ extension;

                if (File.Exists(path + ArchiveFileName) == true)
                {
                    // Add file to existing targz file
                    using (Stream stream = File.Open(path + ArchiveFileName, FileMode.Open))
                    using (Stream gzipStream = new GZipInputStream(stream))
                    using (TarInputStream tarArchive = new TarInputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        // Read all TarArchive and pass it to output stream
                        TarEntry? entry = tarArchive.GetNextEntry();
                        using (Stream outputStream = File.Create(path + ArchiveFileName + ".tmp"))
                        using (Stream gzipOutputStream = new GZipOutputStream(outputStream))
                        using (TarOutputStream tarArchiveOutput = new TarOutputStream(gzipOutputStream, System.Text.Encoding.UTF8))
                        {
                            while (entry != null)
                            {
                                if (entry.Name != JsonFileName)
                                {
                                    tarArchiveOutput.PutNextEntry(entry);
                                    tarArchiveOutput.Write(tarArchive.ReadBytes((int)entry.Size));
                                    tarArchiveOutput.CloseEntry();
                                }
                                else
                                {
                                    TarEntry newEntry = TarEntry.CreateTarEntry(JsonFileName);
                                    newEntry = fixHash(newEntry);
                                    newEntry.TarHeader.Size = data.Length;
                                    newEntry.TarHeader.Name = JsonFileName;
                                    tarArchiveOutput.PutNextEntry(newEntry);
                                    tarArchiveOutput.Write(System.Text.Encoding.UTF8.GetBytes(data));
                                    tarArchiveOutput.CloseEntry();
                                }
                                entry = tarArchive.GetNextEntry();
                            }
                            tarArchiveOutput.Close();
                        }
                    }

                    // Delete old file and rename new file
                    File.Delete(path + ArchiveFileName);
                    File.Move(path + ArchiveFileName + ".tmp", path + ArchiveFileName);
                }
            }
            catch
            {
                // If file in use, wait and try again
                await Task.Delay(100);
                await updateFileFromGZPrivate(data, JsonFileName, ArchiveFileName);
            }

            isRunning = false;
        }
        public async Task<List<string>?> getFileListFromGZ(string ArchiveFileName)
        {
            if (ArchiveFileName.Contains("."+ extension) == false)
                ArchiveFileName += "."+ extension;

            try
            {
                List<string> result = new List<string>();
                // Check if targz file exists
                if (File.Exists(path + ArchiveFileName) == true)
                {
                    // Add file to existing targz file
                    using (Stream stream = File.Open(path + ArchiveFileName, FileMode.Open))
                    using (Stream gzipStream = new GZipInputStream(stream))
                    using (TarInputStream tarArchive = new TarInputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        // Read all TarArchive and pass it to output stream
                        TarEntry? entry = tarArchive.GetNextEntry();
                        while (entry != null)
                        {
                            result.Add(entry.Name);
                            entry = tarArchive.GetNextEntry();
                        }
                    }

                    if (result.Count == 0)
                        return null;
                    else
                        return result;
                }

                return null;
            }
            catch
            {
                if (!isRunning)
                    return null;

                while (isRunning)
                {
                    await Task.Delay(100);
                }

                return await getFileListFromGZ(ArchiveFileName);
            }
        }
        public async Task<string> getFileFromGZ(string blockUid)
        {
            try
            {
                (string JsonFileName, string ArchiveFileName) = getFileName(blockUid);
                // Check if targz file exists
                string result = "";
                if (File.Exists(path + ArchiveFileName) == true)
                {
                    // Add file to existing targz file
                    using (Stream stream = File.Open(path + ArchiveFileName, FileMode.Open))
                    using (Stream gzipStream = new GZipInputStream(stream))
                    using (TarInputStream tarArchive = new TarInputStream(gzipStream, System.Text.Encoding.UTF8))
                    {
                        // Read all TarArchive and pass it to output stream
                        TarEntry? entry = tarArchive.GetNextEntry();
                        bool exitInnerWhile = false;
                        while (entry != null && exitInnerWhile==false)
                        {
                            if (entry.Name == JsonFileName)
                            {
                                result = System.Text.Encoding.UTF8.GetString(tarArchive.ReadBytes((int)entry.Size));
                                exitInnerWhile = true;
                            }
                            else
                            {
                                entry = tarArchive.GetNextEntry();
                            }
                        }
                    }
                }

                return result;
            }
            catch
            {
                while (isRunning)
                {
                    await Task.Delay(100);
                }

                return await getFileFromGZ(blockUid);
            }
        }
        public async Task<string?> GetHash(string ArchiveFileName)
        {
            if (ArchiveFileName.Contains("."+ extension) == false)
                ArchiveFileName += "."+ extension;

            Notus.Hash hashObj = new Notus.Hash();
            try
            {
                // Check if targz file exists
                if (File.Exists(path + ArchiveFileName) == true)
                {
                    using (FileStream fs = File.OpenRead(path + ArchiveFileName))
                    {
                        FileInfo fi = new FileInfo(path + ArchiveFileName);
                        byte[] buffer = new byte[fi.Length];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int read;
                            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, read);
                            }
                            return hashObj.CommonHash("sasha", ms.ToArray());
                        }
                    }
                }

                return string.Empty;
            }
            catch
            {
                while (isRunning)
                {
                    await Task.Delay(100);
                }

                return await GetHash(ArchiveFileName);
            }
        }
        private TarEntry fixHash(TarEntry entry)
        {
            entry.TarHeader.Mode = 420;
            entry.TarHeader.UserId = 0;
            entry.TarHeader.GroupId = 0;
            entry.TarHeader.ModTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return entry;
        }
        private (string JsonFileName, string ArchiveFileName) getFileName(string blockUid)
        {
            return (blockUid + ".json",
                Notus.Block.Key.GetBlockStorageFileName(blockUid, true) + "."+ extension);
        }
    }
}

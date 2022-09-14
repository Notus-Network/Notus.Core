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
        private Notus.Variable.Common.ClassSetting settings = null;
        private string path = "";

        public TGZArchiver(Notus.Variable.Common.ClassSetting settings)
        {
            this.settings = settings;
            path = Notus.IO.GetFolderName(
                settings.Network,
                settings.Layer,
                Notus.Variable.Constant.StorageFolderName.Block
            );

            Notus.Threads.Timer timer = new Notus.Threads.Timer(1000);
            timer.Start(() =>
            {
                while (TaskList.Count > 0)
                {
                    if (isRunning == false)
                    {
                        isRunning = true;
                        KeyValuePair<Guid, (TaskType, object)> task = TaskList.First();
                        TaskList.TryRemove(task.Key, out _);
                        switch (task.Value.Item1)
                        {
                            case TaskType.AddFile:
                                {
                                    (string data, string JsonFileName, string ArchiveFileName) = ((string, string, string))task.Value.Item2;
                                    addFileViaTextToGZPrivate(data, JsonFileName, ArchiveFileName).GetAwaiter().GetResult();
                                }
                                break;
                            case TaskType.RemoveFile:
                                {
                                    (string JsonFileName, string ArchiveFileName) = ((string, string))task.Value.Item2;
                                    removeFileFromGZPrivate(JsonFileName, ArchiveFileName).GetAwaiter().GetResult();
                                }
                                break;
                            case TaskType.UpdateFile:
                                {
                                    (string data, string JsonFileName, string ArchiveFileName) = ((string, string, string))task.Value.Item2;
                                    updateFileFromGZPrivate(data, JsonFileName, ArchiveFileName).GetAwaiter().GetResult();
                                }
                                break;
                        }
                    }
                }
            });
        }

        public void addFileToGZ(string data, string JsonFileName, string ArchiveFileName)
        {
            TaskList.TryAdd(
                Guid.NewGuid(),
                (TaskType.AddFile, (data, JsonFileName, ArchiveFileName))
            );
        }

        public void addFileToGZ(Notus.Variable.Class.BlockData data)
        {
            TaskList.TryAdd(
                Guid.NewGuid(),
                (TaskType.AddFile, (JsonSerializer.Serialize(data), getFileName(data.info.uID).JsonFileName, getFileName(data.info.uID).ArchiveFileName))
            );
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
                if (ArchiveFileName.Contains(".tar.gz") == false)
                    ArchiveFileName += ".tar.gz";

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
                if (ArchiveFileName.Contains(".tar.gz") == false)
                    ArchiveFileName += ".tar.gz";

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
                if (ArchiveFileName.Contains(".tar.gz") == false)
                    ArchiveFileName += ".tar.gz";

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
            if (ArchiveFileName.Contains(".tar.gz") == false)
                ArchiveFileName += ".tar.gz";

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

        public async Task<string?> getFileFromGZ(string blockUid)
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
                        while (entry != null)
                        {
                            if (entry.Name == JsonFileName)
                            {
                                result = System.Text.Encoding.UTF8.GetString(tarArchive.ReadBytes((int)entry.Size));
                            }
                            entry = tarArchive.GetNextEntry();
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
            if (ArchiveFileName.Contains(".tar.gz") == false)
                ArchiveFileName += ".tar.gz";

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
                Notus.Block.Key.GetBlockStorageFileName(blockUid, true) + ".tar.gz");
        }
    }
}

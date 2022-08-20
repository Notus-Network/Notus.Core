// FileDB is a free, fast, lightweight C# Library project to store,
// retrieve and delete files using a single archive file as a container on disk. 
// It's ideal for storing files (all kind, all sizes) without databases and
// keeping them organized on a single disk file.
// The code is provided in mbdavid's repo and the repo can be found here:
// https://github.com/mbdavid/FileDB

using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Notus
{
    /// <summary>
    /// FileDB main class.
    /// </summary>
    public partial class FileDB : IDisposable
    {
        private FileStream? _fileStream = null;
        private Engine? _engine = null;
        private DebugFile? _debug = null;

        /// <summary>
        /// Open a database file
        /// </summary>
        /// <param name="fileName">Database filename (eg: C:\Data\MyDB.dat)</param>
        /// <param name="fileAccess">Acces mode (Read|ReadWrite|Write)</param>
        public FileDB(string fileName, FileAccess fileAccess)
        {
            Connect(fileName, fileAccess);
        }

        private void Connect(string fileName, FileAccess fileAccess)
        {
            if (!File.Exists(fileName))
                FileDB.CreateEmptyFile(fileName);

            // Não permite acesso somente gravação (transforma em leitura/gravação)
            FileAccess fa = fileAccess == FileAccess.Write || fileAccess == FileAccess.ReadWrite ? FileAccess.ReadWrite : FileAccess.Read;

            _fileStream = new FileStream(fileName, FileMode.Open, fa, FileShare.ReadWrite, (int)BasePage.PAGE_SIZE, FileOptions.None);

            _engine = new Engine(_fileStream);
        }

        /// <summary>
        /// Store a disk file inside database
        /// </summary>
        /// <param name="fileName">Full path to file (eg: C:\Temp\MyPhoto.jpg)</param>
        /// <returns>EntryInfo with information store</returns>
        public EntryInfo Store(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return Store(fileName, stream);
            }
        }

        /// <summary>
        /// Store a stream inside database
        /// </summary>
        /// <param name="fileName">Just a name of file, to get future reference (eg: MyPhoto.jpg)</param>
        /// <param name="input">Stream thats contains the file</param>
        /// <returns>EntryInfo with information store</returns>
        public EntryInfo Store(string fileName, Stream input)
        {
            var entry = new EntryInfo(fileName, input);
            _engine.Write(entry, input);
            return entry;
        }

        internal void Store(EntryInfo entry, Stream input)
        {
            _engine.Write(entry, input);
        }

        /// <summary>
        /// Retrieve a file inside a database
        /// </summary>
        /// <param name="id">A Guid that references to file</param>
        /// <param name="fileName">Path to save the file</param>
        /// <returns>EntryInfo with information about the file</returns>
        public EntryInfo Read(Guid id, string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                return Read(id, stream);
            }
        }

        /// <summary>
        /// Retrieve a file inside a database
        /// </summary>
        /// <param name="id">A Guid that references to file</param>
        /// <param name="output">Output strem to save the file</param>
        /// <returns>EntryInfo with information about the file</returns>
        public EntryInfo Read(Guid id, Stream output)
        {
            return _engine.Read(id, output);
        }

        /// <summary>
        /// Retrieve a file inside a database returning a FileDBStream to read
        /// </summary>
        /// <param name="id">A Guid that references to file</param>
        /// <returns>A FileDBStream ready to be readed or null if ID was not found</returns>
        public FileDBStream OpenRead(Guid id)
        {
            return _engine.OpenRead(id);
        }

        /// <summary>
        /// Search for a file inside database BUT get only EntryInfo information (don't copy the file)
        /// </summary>
        /// <param name="id">File ID</param>
        /// <returns>EntryInfo with file information or null with not found</returns>
        public EntryInfo Search(Guid id)
        {
            IndexNode? indexNode = _engine.Search(id);

            if (indexNode == null)
                return null;
            else
                return new EntryInfo(indexNode);
        }

        /// <summary>
        /// Delete a file inside database
        /// </summary>
        /// <param name="id">Guid ID from a file</param>
        /// <returns>True when the file was deleted or False when not found</returns>
        public bool Delete(Guid id)
        {
            return _engine.Delete(id);
        }

        /// <summary>
        /// List all files inside a FileDB
        /// </summary>
        /// <returns>Array with all files</returns>
        public EntryInfo[] ListFiles()
        {
            return _engine.ListAllFiles();
        }

        /// <summary>
        /// Export all files inside FileDB database to a directory
        /// </summary>
        /// <param name="directory">Directory name</param>
        public void Export(string directory)
        {
            this.Export(directory, "{filename}.{id}.{extension}");
        }

        /// <summary>
        /// Export all files inside FileDB database to a directory
        /// </summary>
        /// <param name="directory">Directory name</param>
        /// <param name="filePattern">File Pattern. Use keys: {id} {extension} {filename}. Eg: "{filename}.{id}.{extension}"</param>
        public void Export(string directory, string filePattern)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var files = ListFiles();

            foreach (var file in files)
            {
                var fileName = filePattern.Replace("{id}", file.ID.ToString())
                    .Replace("{filename}", Path.GetFileNameWithoutExtension(file.FileName))
                    .Replace("{extension}", Path.GetExtension(file.FileName).Replace(".", ""));

                Read(file.ID, Path.Combine(directory, fileName));
            }
        }

        /// <summary>
        /// Shrink datafile
        /// </summary>
        public void Shrink()
        {
            var dbFileName = _fileStream.Name;
            var fileAccess = _fileStream.CanWrite ? FileAccess.ReadWrite : FileAccess.Read;
            var tempFile = Path.GetDirectoryName(dbFileName) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(dbFileName) + ".temp" + Path.GetExtension(dbFileName);

            if (File.Exists(tempFile))
                File.Delete(tempFile);

            var entries = ListFiles();

            FileDB.CreateEmptyFile(tempFile, false);

            using (var tempDb = new FileDB(tempFile, FileAccess.ReadWrite))
            {
                foreach (var entry in entries)
                {
                    using (var stream = new MemoryStream())
                    {
                        Read(entry.ID, stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        tempDb.Store(entry, stream);
                    }
                }
            }

            Dispose();

            File.Delete(dbFileName);
            File.Move(tempFile, dbFileName);

            Connect(dbFileName, fileAccess);
        }

        public void Dispose()
        {
            if (_engine != null)
            {
                _engine.PersistPages(); // Persiste as paginas/header que ficaram em memória

                if (_fileStream.CanWrite)
                    _fileStream.Flush();

                _engine.Dispose();

                _fileStream.Dispose();
            }
        }

        /// <summary>
        /// Print debug information about FileDB Structure
        /// </summary>
        public DebugFile Debug
        {
            get
            {
                if (_debug == null)
                    _debug = new DebugFile(_engine);

                return _debug;
            }
        }
    }
    internal enum PageType
    {
        /// <summary>
        /// Data = 1
        /// </summary>
        Data = 1,

        /// <summary>
        /// Index = 2
        /// </summary>
        Index = 2
    }

    internal abstract class BasePage
    {
        public const long PAGE_SIZE = 4096;

        public uint PageID { get; set; }
        public abstract PageType Type { get; }
        public uint NextPageID { get; set; }
    }

    internal class DataPage : BasePage
    {
        public const long HEADER_SIZE = 8;
        public const long DATA_PER_PAGE = 4088;

        public override PageType Type { get { return PageType.Data; } }  //  1 byte

        public bool IsEmpty { get; set; }                                //  1 byte
        public short DataBlockLength { get; set; }                       //  2 bytes

        public byte[] DataBlock { get; set; }

        public DataPage(uint pageID)
        {
            PageID = pageID;
            IsEmpty = true;
            DataBlockLength = 0;
            NextPageID = uint.MaxValue;
            DataBlock = new byte[DataPage.DATA_PER_PAGE];
        }
    }

    public class EntryInfo
    {
        private Guid _id;
        private string _fileName;
        private uint _fileLength;
        private string _mimeType;

        public Guid ID { get { return _id; } }
        public string FileName { get { return _fileName; } }
        public uint FileLength { get { return _fileLength; } internal set { _fileLength = value; } }
        public string MimeType { get { return _mimeType; } }

        internal EntryInfo(string fileName, Stream input)
        {
            SHA256 encryptor = SHA256.Create();
            var hash = StringToHex(System.Convert.ToBase64String(encryptor.ComputeHash(input)) + System.Convert.ToBase64String(encryptor.ComputeHash(Encoding.UTF8.GetBytes(fileName))) + System.Convert.ToBase64String(encryptor.ComputeHash(Encoding.UTF8.GetBytes(input.Length.ToString()))));
            _id = Guid.Parse(
                hash.Substring(0, 8) + "-" +
                hash.Substring(8, 4) + "-" +
                hash.Substring(12, 4) + "-" +
                hash.Substring(16, 4) + "-" +
                hash.Substring(20, 12));
            _fileName = Path.GetFileName(fileName);
            _mimeType = MimeTypeConverter.Convert(Path.GetExtension(_fileName));
            _fileLength = 0;
        }

        internal EntryInfo(IndexNode node)
        {
            _id = node.ID;
            _fileName = node.FileName + "." + node.FileExtension;
            _mimeType = MimeTypeConverter.Convert(node.FileExtension);
            _fileLength = node.FileLength;
        }

        private string StringToHex(string hexstring)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char t in hexstring)
            {
                //Note: X for upper, x for lower case letters
                sb.Append(System.Convert.ToInt32(t).ToString("x2"));
            }
            return sb.ToString();
        }
    }

    internal class Header
    {
        public const long LOCKER_POS = 98;
        public const long HEADER_SIZE = 100;

        public const string FileID = "FileDB";        // 6 bytes
        public const short FileVersion = 1;           // 2 bytes

        /// <summary>
        /// Armazena a primeira página que contem o inicio do indice. Valor sempre fixo = 0. Utilizado o inicio da busca binária
        /// Storage the fist index page (root page). It's fixed on 0 (zero)
        /// </summary>
        public uint IndexRootPageID { get; set; }      // 4 bytes

        /// <summary>
        /// Contem a página que possui espaço disponível para novas inclusões de indices
        /// This last has free nodes to be used
        /// </summary>
        public uint FreeIndexPageID { get; set; }      // 4 bytes

        /// <summary>
        /// Quando há exclusão de dados, a primeira pagina a ficar vazia infora a esse ponteiro que depois vai aproveitar numa proxima inclusão
        /// When a deleted data, this variable point to first page emtpy. I will use to insert the next data page
        /// </summary>
        public uint FreeDataPageID { get; set; }       // 4 bytes

        /// <summary>
        /// Define, numa exclusão de dados, a ultima pagina excluida. Será utilizado para fazer segmentos continuos de exclusão, ou seja, assim que um segundo arquivo for apagado, o ponteiro inicial dele deve apontar para o ponteiro final do outro
        /// Define, in a deleted data, the last deleted page. It's used to make continuos statments of empty page data
        /// </summary>
        public uint LastFreeDataPageID { get; set; }   // 4 bytes

        /// <summary>
        /// Ultima página utilizada pelo FileDB (seja para Indice/Data). É utilizado para quando o arquivo precisa crescer (criar nova pagina)
        /// Last used page on FileDB disk (even index or data page). It's used to grow the file db (create new pages)
        /// </summary>
        public uint LastPageID { get; set; }           // 4 bytes

        public Header()
        {
            IndexRootPageID = uint.MaxValue;
            FreeIndexPageID = uint.MaxValue;
            FreeDataPageID = uint.MaxValue;
            LastFreeDataPageID = uint.MaxValue;
            LastPageID = uint.MaxValue;
            IsDirty = false;
        }

        public bool IsDirty { get; set; }
    }

    internal class IndexLink
    {
        public byte Index { get; set; }
        public uint PageID { get; set; }

        public IndexLink()
        {
            Index = 0;
            PageID = uint.MaxValue;
        }

        public bool IsEmpty
        {
            get
            {
                return PageID == uint.MaxValue;
            }
        }
    }

    internal class IndexNode
    {
        public const int FILENAME_SIZE = 41;       // Size of file name string
        public const int FILE_EXTENSION_SIZE = 5;  // Size of file extension string
        public const int INDEX_NODE_SIZE = 81;     // Node Index size

        public Guid ID { get; set; }               // 16 bytes

        public bool IsDeleted { get; set; }        //  1 byte

        public IndexLink Right { get; set; }       //  5 bytes 
        public IndexLink Left { get; set; }        //  5 bytes

        public uint DataPageID { get; set; }       //  4 bytes

        // Info
        public string FileName { get; set; }       // 41 bytes (file name + extension)
        public string FileExtension { get; set; }  //  5 bytes (only extension without dot ".")
        public uint FileLength { get; set; }       //  4 bytes

        public IndexPage IndexPage { get; set; }

        public IndexNode(IndexPage indexPage)
        {
            ID = Guid.Empty;
            IsDeleted = true; // Start with index node mark as deleted. Update this after save all stream on disk
            Right = new IndexLink();
            Left = new IndexLink();
            DataPageID = uint.MaxValue;
            IndexPage = indexPage;
        }

        public void UpdateFromEntry(EntryInfo entity)
        {
            ID = entity.ID;
            FileName = Path.GetFileNameWithoutExtension(entity.FileName);
            FileExtension = Path.GetExtension(entity.FileName).Replace(".", "");
            FileLength = entity.FileLength;
        }
    }

    internal class IndexPage : BasePage
    {
        public const long HEADER_SIZE = 46;
        public const int NODES_PER_PAGE = 50;

        public override PageType Type { get { return PageType.Index; } }  //  1 byte
        public byte NodeIndex { get; set; }                               //  1 byte

        public IndexNode[] Nodes { get; set; }

        public bool IsDirty { get; set; }

        public IndexPage(uint pageID)
        {
            PageID = pageID;
            NextPageID = uint.MaxValue;
            NodeIndex = 0;
            Nodes = new IndexNode[IndexPage.NODES_PER_PAGE];
            IsDirty = false;

            for (int i = 0; i < IndexPage.NODES_PER_PAGE; i++)
            {
                var node = Nodes[i] = new IndexNode(this);
            }
        }

    }

    public sealed class FileDBStream : Stream
    {
        private Engine _engine = null;
        private readonly long _streamLength = 0;

        private long _streamPosition = 0;
        private DataPage _currentPage = null;
        private int _positionInPage = 0;
        private EntryInfo _info = null;

        internal FileDBStream(Engine engine, Guid id)
        {
            _engine = engine;

            var indexNode = _engine.Search(id);
            if (indexNode != null)
            {
                _streamLength = indexNode.FileLength;
                _currentPage = PageFactory.GetDataPage(indexNode.DataPageID, engine.Reader, false);
                _info = new EntryInfo(indexNode);
            }
        }

        /// <summary>
        /// Get file information
        /// </summary>
        public EntryInfo FileInfo
        {
            get
            {
                return _info;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { return _streamLength; }
        }

        public override long Position
        {
            get
            {
                return _streamPosition;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesLeft = count;

            while (_currentPage != null && bytesLeft > 0)
            {
                int bytesToCopy = Math.Min(bytesLeft, _currentPage.DataBlockLength - _positionInPage);
                Buffer.BlockCopy(_currentPage.DataBlock, _positionInPage, buffer, offset, bytesToCopy);

                _positionInPage += bytesToCopy;
                bytesLeft -= bytesToCopy;
                offset += bytesToCopy;
                _streamPosition += bytesToCopy;

                if (_positionInPage >= _currentPage.DataBlockLength)
                {
                    _positionInPage = 0;

                    if (_currentPage.NextPageID == uint.MaxValue)
                        _currentPage = null;
                    else
                        _currentPage = PageFactory.GetDataPage(_currentPage.NextPageID, _engine.Reader, false);
                }
            }

            return count - bytesLeft;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    public class FileDBException : ApplicationException
    {
        public FileDBException(string message)
            : base(message)
        {
        }

        public FileDBException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }
    }

    internal class Engine : IDisposable
    {
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }
        public CacheIndexPage CacheIndexPage { get; private set; } // Used for cache index pages.
        public Header Header { get; private set; }

        public Engine(FileStream stream)
        {
            Reader = new BinaryReader(stream);

            if (stream.CanWrite)
            {
                Writer = new BinaryWriter(stream);
            }

            Header = new Header();
            HeaderFactory.ReadFromFile(Header, this.Reader);

            CacheIndexPage = new CacheIndexPage(Reader, Writer, Header.IndexRootPageID);
        }

        public IndexPage GetFreeIndexPage()
        {
            var freeIndexPage = CacheIndexPage.GetPage(Header.FreeIndexPageID);

            // Check if "free page" has no more index to be used
            if (freeIndexPage.NodeIndex >= IndexPage.NODES_PER_PAGE - 1)
            {
                Header.LastPageID++; // Take last page and increase
                Header.IsDirty = true;

                var newIndexPage = new IndexPage(Header.LastPageID); // Create a new index page
                freeIndexPage.NextPageID = newIndexPage.PageID; // Point older page to the new page
                Header.FreeIndexPageID = Header.LastPageID; // Update last index page

                CacheIndexPage.AddPage(freeIndexPage, true);

                return newIndexPage;
            }
            else
            {
                // Has more free index on same index page? return them
                freeIndexPage.NodeIndex++; // Reserve space
                return freeIndexPage;
            }
        }

        public DataPage GetPageData(uint pageID)
        {
            if (pageID == Header.LastPageID) // Page does not exists in disk
            {
                var dataPage = new DataPage(pageID);
                return dataPage;
            }
            else
            {
                return PageFactory.GetDataPage(pageID, Reader, false);
            }
        }

        // Implement file physic storage
        public void Write(EntryInfo entry, Stream stream)
        {
            // Take the first index page
            IndexNode rootIndexNode = IndexFactory.GetRootIndexNode(this);

            // Search and insert the index
            var indexNode = IndexFactory.BinaryInsert(entry, rootIndexNode, this);

            // In this moment, the index are ready and saved. I use to add the file
            DataFactory.InsertFile(indexNode, stream, this);

            // Update entry information with file length (I know file length only after read all)
            entry.FileLength = indexNode.FileLength;

            // Only after insert all stream file I confirm that index node is valid
            indexNode.IsDeleted = false;

            // Mask header as dirty for save on dispose
            Header.IsDirty = true;
        }

        public IndexNode Search(Guid id)
        {
            // Take the root node from inital index page
            IndexNode rootIndexNode = IndexFactory.GetRootIndexNode(this);

            var indexNode = IndexFactory.BinarySearch(id, rootIndexNode, this);

            // Returns null with not found the record, return false
            if (indexNode == null || indexNode.IsDeleted)
                return null;

            return indexNode;
        }

        public EntryInfo Read(Guid id, Stream stream)
        {
            // Search from index node
            var indexNode = Search(id);

            // If index node is null, not found the guid
            if (indexNode == null)
                return null;

            // Create a entry based on index node
            EntryInfo entry = new EntryInfo(indexNode);

            // Read data from the index pointer to stream
            DataFactory.ReadFile(indexNode, stream, this);

            return entry;
        }

        public FileDBStream? OpenRead(Guid id)
        {
            // Open a FileDBStream and return to user
            var file = new FileDBStream(this, id);

            // If FileInfo is null, ID was not found
            return file.FileInfo == null ? null : file;
        }

        public bool Delete(Guid id)
        {
            // Search index node from guid
            var indexNode = Search(id);

            // If null, not found (return false)
            if (indexNode == null)
                return false;

            // Delete the index node logicaly
            indexNode.IsDeleted = true;

            // Add page (from index node) to cache and set as dirty
            CacheIndexPage.AddPage(indexNode.IndexPage, true);

            // Mark all data blocks (from data pages) as IsEmpty = true
            DataFactory.MarkAsEmpty(indexNode.DataPageID, this);

            // Set header as Dirty to be saved on dispose
            Header.IsDirty = true;

            return true; // Confirma a exclusão
        }

        public EntryInfo[] ListAllFiles()
        {
            // Get root index page from cache
            var pageIndex = CacheIndexPage.GetPage(Header.IndexRootPageID);
            bool cont = true;

            List<EntryInfo> list = new List<EntryInfo>();

            while (cont)
            {
                for (int i = 0; i <= pageIndex.NodeIndex; i++)
                {
                    // Convert node (if is not logicaly deleted) to Entry
                    var node = pageIndex.Nodes[i];
                    if (!node.IsDeleted)
                        list.Add(new EntryInfo(node));
                }

                // Go to the next page
                if (pageIndex.NextPageID != uint.MaxValue)
                    pageIndex = CacheIndexPage.GetPage(pageIndex.NextPageID);
                else
                    cont = false;
            }

            return list.ToArray();
        }

        public void PersistPages()
        {
            // Check if header is dirty and save to disk
            if (Header.IsDirty)
            {
                HeaderFactory.WriteToFile(Header, Writer);
                Header.IsDirty = false;
            }

            // Persist all index pages that are dirty
            CacheIndexPage.PersistPages();
        }

        public void Dispose()
        {
            if (Writer != null)
            {
                Writer.Close();
            }

            Reader.Close();
        }
    }

    internal class DataFactory
    {
        public static uint GetStartDataPageID(Engine engine)
        {
            if (engine.Header.FreeDataPageID != uint.MaxValue) // I have free page inside the disk file. Use it
            {
                // Take the first free data page
                var startPage = PageFactory.GetDataPage(engine.Header.FreeDataPageID, engine.Reader, true);

                engine.Header.FreeDataPageID = startPage.NextPageID; // and point the free page to new free one

                // If the next page is MAX, fix too LastFreeData

                if (engine.Header.FreeDataPageID == uint.MaxValue)
                    engine.Header.LastFreeDataPageID = uint.MaxValue;

                return startPage.PageID;
            }
            else // Don't have free data pages, create new one.
            {
                engine.Header.LastPageID++;
                return engine.Header.LastPageID;
            }
        }

        // Take a new data page on sequence and update the last
        public static DataPage GetNewDataPage(DataPage basePage, Engine engine)
        {
            if (basePage.NextPageID != uint.MaxValue)
            {
                PageFactory.WriteToFile(basePage, engine.Writer); // Write last page on disk

                var dataPage = PageFactory.GetDataPage(basePage.NextPageID, engine.Reader, false);

                engine.Header.FreeDataPageID = dataPage.NextPageID;

                if (engine.Header.FreeDataPageID == uint.MaxValue)
                    engine.Header.LastFreeDataPageID = uint.MaxValue;

                return dataPage;
            }
            else
            {
                var pageID = ++engine.Header.LastPageID;
                DataPage newPage = new DataPage(pageID);
                basePage.NextPageID = newPage.PageID;
                PageFactory.WriteToFile(basePage, engine.Writer); // Write last page on disk
                return newPage;
            }
        }

        public static void InsertFile(IndexNode node, Stream stream, Engine engine)
        {
            DataPage dataPage = null;
            var buffer = new byte[DataPage.DATA_PER_PAGE];
            uint totalBytes = 0;

            int read = 0;
            int dataPerPage = (int)DataPage.DATA_PER_PAGE;

            while ((read = stream.Read(buffer, 0, dataPerPage)) > 0)
            {
                totalBytes += (uint)read;

                if (dataPage == null) // First read
                    dataPage = engine.GetPageData(node.DataPageID);
                else
                    dataPage = GetNewDataPage(dataPage, engine);

                if (!dataPage.IsEmpty) // This is never to happend!!
                    throw new FileDBException("Page {0} is not empty", dataPage.PageID);

                Array.Copy(buffer, dataPage.DataBlock, read);
                dataPage.IsEmpty = false;
                dataPage.DataBlockLength = (short)read;
            }

            if (dataPage == null)
                dataPage = engine.GetPageData(node.DataPageID);

            // If the last page point to another one, i need to fix that
            if (dataPage.NextPageID != uint.MaxValue)
            {
                engine.Header.FreeDataPageID = dataPage.NextPageID;
                dataPage.NextPageID = uint.MaxValue;
            }

            // Salve the last page on disk
            PageFactory.WriteToFile(dataPage, engine.Writer);

            // Save on node index that file length
            node.FileLength = totalBytes;

        }

        public static void ReadFile(IndexNode node, Stream stream, Engine engine)
        {
            var dataPage = PageFactory.GetDataPage(node.DataPageID, engine.Reader, false);

            while (dataPage != null)
            {
                stream.Write(dataPage.DataBlock, 0, dataPage.DataBlockLength);

                if (dataPage.NextPageID == uint.MaxValue)
                    dataPage = null;
                else
                    dataPage = PageFactory.GetDataPage(dataPage.NextPageID, engine.Reader, false);
            }

        }

        public static void MarkAsEmpty(uint firstPageID, Engine engine)
        {
            var dataPage = PageFactory.GetDataPage(firstPageID, engine.Reader, true);
            uint lastPageID = uint.MaxValue;
            var cont = true;

            while (cont)
            {
                dataPage.IsEmpty = true;

                PageFactory.WriteToFile(dataPage, engine.Writer);

                if (dataPage.NextPageID != uint.MaxValue)
                {
                    lastPageID = dataPage.NextPageID;
                    dataPage = PageFactory.GetDataPage(lastPageID, engine.Reader, true);
                }
                else
                {
                    cont = false;
                }
            }

            // Fix header to correct pointer
            if (engine.Header.FreeDataPageID == uint.MaxValue) // No free pages
            {
                engine.Header.FreeDataPageID = firstPageID;
                engine.Header.LastFreeDataPageID = lastPageID == uint.MaxValue ? firstPageID : lastPageID;
            }
            else
            {
                // Take the last statment available
                var lastPage = PageFactory.GetDataPage(engine.Header.LastFreeDataPageID, engine.Reader, true);

                // Point this last statent to first of next one
                if (lastPage.NextPageID != uint.MaxValue || !lastPage.IsEmpty) // This is never to happend!!
                    throw new FileDBException("The page is not empty");

                // Update this last page to first new empty page
                lastPage.NextPageID = firstPageID;

                // Save on disk this update
                PageFactory.WriteToFile(lastPage, engine.Writer);

                // Point header to the new empty page
                engine.Header.LastFreeDataPageID = lastPageID == uint.MaxValue ? firstPageID : lastPageID;
            }
        }

    }

    internal class FileFactory
    {
        public static void CreateEmptyFile(BinaryWriter writer)
        {
            // Create new header instance
            var header = new Header();

            header.IndexRootPageID = 0;
            header.FreeIndexPageID = 0;
            header.FreeDataPageID = uint.MaxValue;
            header.LastFreeDataPageID = uint.MaxValue;
            header.LastPageID = 0;

            HeaderFactory.WriteToFile(header, writer);

            // Create a first fixed index page
            var pageIndex = new IndexPage(0);
            pageIndex.NodeIndex = 0;
            pageIndex.NextPageID = uint.MaxValue;

            // Create first fixed index node, with fixed middle guid
            var indexNode = pageIndex.Nodes[0];
            indexNode.ID = new Guid(new byte[] { 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127, 127 });
            indexNode.IsDeleted = true;
            indexNode.Right = new IndexLink();
            indexNode.Left = new IndexLink();
            indexNode.DataPageID = uint.MaxValue;
            indexNode.FileName = string.Empty;
            indexNode.FileExtension = string.Empty;

            PageFactory.WriteToFile(pageIndex, writer);

        }
    }

    internal class HeaderFactory
    {
        public static void ReadFromFile(Header header, BinaryReader reader)
        {
            // Seek the stream on 0 position to read header
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            // Make same validation on header file
            if (reader.ReadString(Header.FileID.Length) != Header.FileID)
                throw new FileDBException("The file is not a valid storage archive");

            if (reader.ReadInt16() != Header.FileVersion)
                throw new FileDBException("The archive version is not valid");

            header.IndexRootPageID = reader.ReadUInt32();
            header.FreeIndexPageID = reader.ReadUInt32();
            header.FreeDataPageID = reader.ReadUInt32();
            header.LastFreeDataPageID = reader.ReadUInt32();
            header.LastPageID = reader.ReadUInt32();
            header.IsDirty = false;
        }

        public static void WriteToFile(Header header, BinaryWriter writer)
        {
            // Seek the stream on 0 position to save header
            writer.BaseStream.Seek(0, SeekOrigin.Begin);

            writer.Write(Header.FileID.ToBytes(Header.FileID.Length));
            writer.Write(Header.FileVersion);

            writer.Write(header.IndexRootPageID);
            writer.Write(header.FreeIndexPageID);
            writer.Write(header.FreeDataPageID);
            writer.Write(header.LastFreeDataPageID);
            writer.Write(header.LastPageID);
        }

    }

    internal class IndexFactory
    {
        public static IndexNode GetRootIndexNode(Engine engine)
        {
            IndexPage rootIndexPage = engine.CacheIndexPage.GetPage(engine.Header.IndexRootPageID);
            return rootIndexPage.Nodes[0];
        }

        public static IndexNode BinaryInsert(EntryInfo target, IndexNode baseNode, Engine engine)
        {
            var dif = baseNode.ID.CompareTo(target.ID);

            if (dif == 1) // > Maior (Right)
            {
                if (baseNode.Right.IsEmpty)
                    return BinaryInsertNode(baseNode.Right, baseNode, target, engine);
                else
                    return BinaryInsert(target, GetChildIndexNode(baseNode.Right, engine), engine);
            }
            else if (dif == -1) // < Menor (Left)
            {
                if (baseNode.Left.IsEmpty)
                    return BinaryInsertNode(baseNode.Left, baseNode, target, engine);
                else
                    return BinaryInsert(target, GetChildIndexNode(baseNode.Left, engine), engine);
            }
            else
            {
                throw new FileDBException("Same GUID?!?");
            }
        }

        private static IndexNode GetChildIndexNode(IndexLink link, Engine engine)
        {
            var pageIndex = engine.CacheIndexPage.GetPage(link.PageID);
            return pageIndex.Nodes[link.Index];
        }

        private static IndexNode BinaryInsertNode(IndexLink baseLink, IndexNode baseNode, EntryInfo entry, Engine engine)
        {
            // Must insert my new nodo
            var pageIndex = engine.GetFreeIndexPage();
            var newNode = pageIndex.Nodes[pageIndex.NodeIndex];

            baseLink.PageID = pageIndex.PageID;
            baseLink.Index = pageIndex.NodeIndex;

            newNode.UpdateFromEntry(entry);
            newNode.DataPageID = DataFactory.GetStartDataPageID(engine);

            if (pageIndex.PageID != baseNode.IndexPage.PageID)
                engine.CacheIndexPage.AddPage(baseNode.IndexPage, true);

            engine.CacheIndexPage.AddPage(pageIndex, true);

            return newNode;
        }

        public static IndexNode BinarySearch(Guid target, IndexNode baseNode, Engine engine)
        {
            var dif = baseNode.ID.CompareTo(target);

            if (dif == 1) // > Maior (Right)
            {
                if (baseNode.Right.IsEmpty) // If there no ones on right, GUID not found
                    return null;
                else
                    return BinarySearch(target, GetChildIndexNode(baseNode.Right, engine), engine); // Recursive call on right node
            }
            else if (dif == -1) // < Menor (Left)
            {
                if (baseNode.Left.IsEmpty) // If there no ones on left, GUID not found
                    return null;
                else
                    return BinarySearch(target, GetChildIndexNode(baseNode.Left, engine), engine); // Recursive call on left node
            }
            else
            {
                // Found it
                return baseNode;
            }
        }


    }

    internal class PageFactory
    {
        #region Read/Write Index Page

        public static void ReadFromFile(IndexPage indexPage, BinaryReader reader)
        {
            // Seek the stream to the fist byte on page
            long initPos = reader.Seek(Header.HEADER_SIZE + ((long)indexPage.PageID * BasePage.PAGE_SIZE));

            if (reader.ReadByte() != (byte)PageType.Index)
                throw new FileDBException("PageID {0} is not a Index Page", indexPage.PageID);

            indexPage.NextPageID = reader.ReadUInt32();
            indexPage.NodeIndex = reader.ReadByte();

            // Seek the stream to end of header data page
            reader.Seek(initPos + IndexPage.HEADER_SIZE);

            for (int i = 0; i <= indexPage.NodeIndex; i++)
            {
                var node = indexPage.Nodes[i];

                node.ID = reader.ReadGuid();

                node.IsDeleted = reader.ReadBoolean();

                node.Right.Index = reader.ReadByte();
                node.Right.PageID = reader.ReadUInt32();
                node.Left.Index = reader.ReadByte();
                node.Left.PageID = reader.ReadUInt32();

                node.DataPageID = reader.ReadUInt32();

                node.FileName = reader.ReadString(IndexNode.FILENAME_SIZE);
                node.FileExtension = reader.ReadString(IndexNode.FILE_EXTENSION_SIZE);
                node.FileLength = reader.ReadUInt32();
            }
        }

        public static void WriteToFile(IndexPage indexPage, BinaryWriter writer)
        {
            // Seek the stream to the fist byte on page
            long initPos = writer.Seek(Header.HEADER_SIZE + ((long)indexPage.PageID * BasePage.PAGE_SIZE));

            // Write page header 
            writer.Write((byte)indexPage.Type);
            writer.Write(indexPage.NextPageID);
            writer.Write(indexPage.NodeIndex);

            // Seek the stream to end of header index page
            writer.Seek(initPos + IndexPage.HEADER_SIZE);

            for (int i = 0; i <= indexPage.NodeIndex; i++)
            {
                var node = indexPage.Nodes[i];

                writer.Write(node.ID);

                writer.Write(node.IsDeleted);

                writer.Write(node.Right.Index);
                writer.Write(node.Right.PageID);
                writer.Write(node.Left.Index);
                writer.Write(node.Left.PageID);

                writer.Write(node.DataPageID);

                writer.Write(node.FileName.ToBytes(IndexNode.FILENAME_SIZE));
                writer.Write(node.FileExtension.ToBytes(IndexNode.FILE_EXTENSION_SIZE));
                writer.Write(node.FileLength);
            }

        }

        #endregion

        #region Read/Write Data Page

        public static void ReadFromFile(DataPage dataPage, BinaryReader reader, bool onlyHeader)
        {
            // Seek the stream on first byte from data page
            long initPos = reader.Seek(Header.HEADER_SIZE + ((long)dataPage.PageID * BasePage.PAGE_SIZE));

            if (reader.ReadByte() != (byte)PageType.Data)
                throw new FileDBException("PageID {0} is not a Data Page", dataPage.PageID);

            dataPage.NextPageID = reader.ReadUInt32();
            dataPage.IsEmpty = reader.ReadBoolean();
            dataPage.DataBlockLength = reader.ReadInt16();

            // If page is empty or onlyHeader parameter, I don't read data content
            if (!dataPage.IsEmpty && !onlyHeader)
            {
                // Seek the stream at the end of page header
                reader.Seek(initPos + DataPage.HEADER_SIZE);

                // Read all bytes from page
                dataPage.DataBlock = reader.ReadBytes(dataPage.DataBlockLength);
            }
        }

        public static void WriteToFile(DataPage dataPage, BinaryWriter writer)
        {
            // Seek the stream on first byte from data page
            long initPos = writer.Seek(Header.HEADER_SIZE + ((long)dataPage.PageID * BasePage.PAGE_SIZE));

            // Write data page header
            writer.Write((byte)dataPage.Type);
            writer.Write(dataPage.NextPageID);
            writer.Write(dataPage.IsEmpty);
            writer.Write(dataPage.DataBlockLength);

            // I will only save data content if the page is not empty
            if (!dataPage.IsEmpty)
            {
                // Seek the stream at the end of page header
                writer.Seek(initPos + DataPage.HEADER_SIZE);

                writer.Write(dataPage.DataBlock, 0, (int)dataPage.DataBlockLength);
            }
        }

        #endregion

        #region Get Pages from File

        public static IndexPage GetIndexPage(uint pageID, BinaryReader reader)
        {
            var indexPage = new IndexPage(pageID);
            ReadFromFile(indexPage, reader);
            return indexPage;
        }

        public static DataPage GetDataPage(uint pageID, BinaryReader reader, bool onlyHeader)
        {
            var dataPage = new DataPage(pageID);
            ReadFromFile(dataPage, reader, onlyHeader);
            return dataPage;
        }

        public static BasePage GetBasePage(uint pageID, BinaryReader reader)
        {
            // Seek the stream at begin of page
            long initPos = reader.Seek(Header.HEADER_SIZE + ((long)pageID * BasePage.PAGE_SIZE));

            if (reader.ReadByte() == (byte)PageType.Index)
                return GetIndexPage(pageID, reader);
            else
                return GetDataPage(pageID, reader, true);
        }

        #endregion

    }

    internal static class StringExtensions
    {
        public static byte[] ToBytes(this string str, int size)
        {
            if (string.IsNullOrEmpty(str))
                return new byte[size];

            var buffer = new byte[size];
            var strbytes = Encoding.UTF8.GetBytes(str);

            Array.Copy(strbytes, buffer, size > strbytes.Length ? strbytes.Length : size);

            return buffer;
        }
    }

    internal class Range<TStart, TEnd>
    {
        public TStart Start { get; set; }
        public TEnd End { get; set; }

        public Range()
        {
        }

        public Range(TStart start, TEnd end)
        {
            this.Start = start;
            this.End = end;
        }
    }

    internal class MimeTypeConverter
    {
        public static string Convert(string extension)
        {
            var ext = extension.Replace(".", "").ToLower();
            var r = string.Empty;

            switch (ext)
            {
                case "3dm": r = "x-world/x-3dmf"; break;
                case "3dmf": r = "x-world/x-3dmf"; break;
                case "a": r = "application/octet-stream"; break;
                case "aab": r = "application/x-authorware-bin"; break;
                case "aam": r = "application/x-authorware-map"; break;
                case "aas": r = "application/x-authorware-seg"; break;
                case "abc": r = "text/vnd.abc"; break;
                case "acgi": r = "text/html"; break;
                case "afl": r = "video/animaflex"; break;
                case "ai": r = "application/postscript"; break;
                case "aif": r = "audio/aiff"; break;
                case "aifc": r = "audio/aiff"; break;
                case "aiff": r = "audio/aiff"; break;
                case "aim": r = "application/x-aim"; break;
                case "aip": r = "text/x-audiosoft-intra"; break;
                case "ani": r = "application/x-navi-animation"; break;
                case "aos": r = "application/x-nokia-9000-communicator-add-on-software"; break;
                case "aps": r = "application/mime"; break;
                case "arc": r = "application/octet-stream"; break;
                case "arj": r = "application/arj"; break;
                case "art": r = "image/x-jg"; break;
                case "asf": r = "video/x-ms-asf"; break;
                case "asm": r = "text/x-asm"; break;
                case "asp": r = "text/asp"; break;
                case "asx": r = "video/x-ms-asf"; break;
                case "au": r = "audio/basic"; break;
                case "avi": r = "video/avi"; break;
                case "avs": r = "video/avs-video"; break;
                case "bcpio": r = "application/x-bcpio"; break;
                case "bin": r = "application/octet-stream"; break;
                case "bm": r = "image/bmp"; break;
                case "bmp": r = "image/bmp"; break;
                case "boo": r = "application/book"; break;
                case "book": r = "application/book"; break;
                case "boz": r = "application/x-bzip2"; break;
                case "bsh": r = "application/x-bsh"; break;
                case "bz": r = "application/x-bzip"; break;
                case "bz2": r = "application/x-bzip2"; break;
                case "c": r = "text/plain"; break;
                case "c++": r = "text/plain"; break;
                case "cat": r = "application/vnd.ms-pki.seccat"; break;
                case "cc": r = "text/plain"; break;
                case "ccad": r = "application/clariscad"; break;
                case "cco": r = "application/x-cocoa"; break;
                case "cdf": r = "application/cdf"; break;
                case "cer": r = "application/pkix-cert"; break;
                case "cha": r = "application/x-chat"; break;
                case "chat": r = "application/x-chat"; break;
                case "class": r = "application/java"; break;
                case "com": r = "application/octet-stream"; break;
                case "conf": r = "text/plain"; break;
                case "cpio": r = "application/x-cpio"; break;
                case "cpp": r = "text/x-c"; break;
                case "cpt": r = "application/x-cpt"; break;
                case "crl": r = "application/pkcs-crl"; break;
                case "crt": r = "application/pkix-cert"; break;
                case "csh": r = "application/x-csh"; break;
                case "css": r = "text/css"; break;
                case "cxx": r = "text/plain"; break;
                case "dcr": r = "application/x-director"; break;
                case "deepv": r = "application/x-deepv"; break;
                case "def": r = "text/plain"; break;
                case "der": r = "application/x-x509-ca-cert"; break;
                case "dif": r = "video/x-dv"; break;
                case "dir": r = "application/x-director"; break;
                case "dl": r = "video/dl"; break;
                case "doc": r = "application/msword"; break;
                case "dot": r = "application/msword"; break;
                case "dp": r = "application/commonground"; break;
                case "drw": r = "application/drafting"; break;
                case "dump": r = "application/octet-stream"; break;
                case "dv": r = "video/x-dv"; break;
                case "dvi": r = "application/x-dvi"; break;
                case "dwf": r = "model/vnd.dwf"; break;
                case "dwg": r = "image/vnd.dwg"; break;
                case "dxf": r = "image/vnd.dwg"; break;
                case "dxr": r = "application/x-director"; break;
                case "el": r = "text/x-script.elisp"; break;
                case "elc": r = "application/x-elc"; break;
                case "env": r = "application/x-envoy"; break;
                case "eps": r = "application/postscript"; break;
                case "es": r = "application/x-esrehber"; break;
                case "etx": r = "text/x-setext"; break;
                case "evy": r = "application/envoy"; break;
                case "exe": r = "application/octet-stream"; break;
                case "f": r = "text/plain"; break;
                case "f77": r = "text/x-fortran"; break;
                case "f90": r = "text/plain"; break;
                case "fdf": r = "application/vnd.fdf"; break;
                case "fif": r = "image/fif"; break;
                case "fli": r = "video/fli"; break;
                case "flo": r = "image/florian"; break;
                case "flx": r = "text/vnd.fmi.flexstor"; break;
                case "fmf": r = "video/x-atomic3d-feature"; break;
                case "for": r = "text/x-fortran"; break;
                case "fpx": r = "image/vnd.fpx"; break;
                case "frl": r = "application/freeloader"; break;
                case "funk": r = "audio/make"; break;
                case "g": r = "text/plain"; break;
                case "g3": r = "image/g3fax"; break;
                case "gif": r = "image/gif"; break;
                case "gl": r = "video/gl"; break;
                case "gsd": r = "audio/x-gsm"; break;
                case "gsm": r = "audio/x-gsm"; break;
                case "gsp": r = "application/x-gsp"; break;
                case "gss": r = "application/x-gss"; break;
                case "gtar": r = "application/x-gtar"; break;
                case "gz": r = "application/x-gzip"; break;
                case "gzip": r = "application/x-gzip"; break;
                case "h": r = "text/plain"; break;
                case "hdf": r = "application/x-hdf"; break;
                case "help": r = "application/x-helpfile"; break;
                case "hgl": r = "application/vnd.hp-hpgl"; break;
                case "hh": r = "text/plain"; break;
                case "hlb": r = "text/x-script"; break;
                case "hlp": r = "application/hlp"; break;
                case "hpg": r = "application/vnd.hp-hpgl"; break;
                case "hpgl": r = "application/vnd.hp-hpgl"; break;
                case "hqx": r = "application/binhex"; break;
                case "hta": r = "application/hta"; break;
                case "htc": r = "text/x-component"; break;
                case "htm": r = "text/html"; break;
                case "html": r = "text/html"; break;
                case "htmls": r = "text/html"; break;
                case "htt": r = "text/webviewhtml"; break;
                case "htx": r = "text/html"; break;
                case "ice": r = "x-conference/x-cooltalk"; break;
                case "ico": r = "image/x-icon"; break;
                case "idc": r = "text/plain"; break;
                case "ief": r = "image/ief"; break;
                case "iefs": r = "image/ief"; break;
                case "iges": r = "application/iges"; break;
                case "igs": r = "application/iges"; break;
                case "ima": r = "application/x-ima"; break;
                case "imap": r = "application/x-httpd-imap"; break;
                case "inf": r = "application/inf"; break;
                case "ins": r = "application/x-internett-signup"; break;
                case "ip": r = "application/x-ip2"; break;
                case "isu": r = "video/x-isvideo"; break;
                case "it": r = "audio/it"; break;
                case "iv": r = "application/x-inventor"; break;
                case "ivr": r = "i-world/i-vrml"; break;
                case "ivy": r = "application/x-livescreen"; break;
                case "jam": r = "audio/x-jam"; break;
                case "jav": r = "text/plain"; break;
                case "java": r = "text/plain"; break;
                case "jcm": r = "application/x-java-commerce"; break;
                case "jfif": r = "image/jpeg"; break;
                case "jfif-tbnl": r = "image/jpeg"; break;
                case "jpe": r = "image/jpeg"; break;
                case "jpeg": r = "image/jpeg"; break;
                case "jpg": r = "image/jpeg"; break;
                case "jps": r = "image/x-jps"; break;
                case "js": r = "application/x-javascript"; break;
                case "jut": r = "image/jutvision"; break;
                case "kar": r = "audio/midi"; break;
                case "ksh": r = "application/x-ksh"; break;
                case "la": r = "audio/nspaudio"; break;
                case "lam": r = "audio/x-liveaudio"; break;
                case "latex": r = "application/x-latex"; break;
                case "lha": r = "application/octet-stream"; break;
                case "lhx": r = "application/octet-stream"; break;
                case "list": r = "text/plain"; break;
                case "lma": r = "audio/nspaudio"; break;
                case "log": r = "text/plain"; break;
                case "lsp": r = "application/x-lisp"; break;
                case "lst": r = "text/plain"; break;
                case "lsx": r = "text/x-la-asf"; break;
                case "ltx": r = "application/x-latex"; break;
                case "lzh": r = "application/octet-stream"; break;
                case "lzx": r = "application/octet-stream"; break;
                case "m": r = "text/plain"; break;
                case "m1v": r = "video/mpeg"; break;
                case "m2a": r = "audio/mpeg"; break;
                case "m2v": r = "video/mpeg"; break;
                case "m3u": r = "audio/x-mpequrl"; break;
                case "man": r = "application/x-troff-man"; break;
                case "map": r = "application/x-navimap"; break;
                case "mar": r = "text/plain"; break;
                case "mbd": r = "application/mbedlet"; break;
                case "mc$": r = "application/x-magic-cap-package-1.0"; break;
                case "mcd": r = "application/mcad"; break;
                case "mcf": r = "text/mcf"; break;
                case "mcp": r = "application/netmc"; break;
                case "me": r = "application/x-troff-me"; break;
                case "mht": r = "message/rfc822"; break;
                case "mhtml": r = "message/rfc822"; break;
                case "mid": r = "audio/midi"; break;
                case "midi": r = "audio/midi"; break;
                case "mif": r = "application/x-mif"; break;
                case "mime": r = "message/rfc822"; break;
                case "mjf": r = "audio/x-vnd.audioexplosion.mjuicemediafile"; break;
                case "mjpg": r = "video/x-motion-jpeg"; break;
                case "mm": r = "application/base64"; break;
                case "mme": r = "application/base64"; break;
                case "mod": r = "audio/mod"; break;
                case "moov": r = "video/quicktime"; break;
                case "mov": r = "video/quicktime"; break;
                case "movie": r = "video/x-sgi-movie"; break;
                case "mp2": r = "audio/mpeg"; break;
                case "mp3": r = "audio/mpeg"; break;
                case "mpa": r = "audio/mpeg"; break;
                case "mpc": r = "application/x-project"; break;
                case "mpe": r = "video/mpeg"; break;
                case "mpeg": r = "video/mpeg"; break;
                case "mpg": r = "video/mpeg"; break;
                case "mpga": r = "audio/mpeg"; break;
                case "mpp": r = "application/vnd.ms-project"; break;
                case "mpt": r = "application/vnd.ms-project"; break;
                case "mpv": r = "application/vnd.ms-project"; break;
                case "mpx": r = "application/vnd.ms-project"; break;
                case "mrc": r = "application/marc"; break;
                case "ms": r = "application/x-troff-ms"; break;
                case "mv": r = "video/x-sgi-movie"; break;
                case "my": r = "audio/make"; break;
                case "mzz": r = "application/x-vnd.audioexplosion.mzz"; break;
                case "nap": r = "image/naplps"; break;
                case "naplps": r = "image/naplps"; break;
                case "nc": r = "application/x-netcdf"; break;
                case "ncm": r = "application/vnd.nokia.configuration-message"; break;
                case "nif": r = "image/x-niff"; break;
                case "niff": r = "image/x-niff"; break;
                case "nix": r = "application/x-mix-transfer"; break;
                case "nsc": r = "application/x-conference"; break;
                case "nvd": r = "application/x-navidoc"; break;
                case "o": r = "application/octet-stream"; break;
                case "oda": r = "application/oda"; break;
                case "omc": r = "application/x-omc"; break;
                case "omcd": r = "application/x-omcdatamaker"; break;
                case "omcr": r = "application/x-omcregerator"; break;
                case "p": r = "text/x-pascal"; break;
                case "p10": r = "application/pkcs10"; break;
                case "p12": r = "application/pkcs-12"; break;
                case "p7a": r = "application/x-pkcs7-signature"; break;
                case "p7c": r = "application/pkcs7-mime"; break;
                case "p7m": r = "application/pkcs7-mime"; break;
                case "p7r": r = "application/x-pkcs7-certreqresp"; break;
                case "p7s": r = "application/pkcs7-signature"; break;
                case "part": r = "application/pro_eng"; break;
                case "pas": r = "text/pascal"; break;
                case "pbm": r = "image/x-portable-bitmap"; break;
                case "pcl": r = "application/vnd.hp-pcl"; break;
                case "pct": r = "image/x-pict"; break;
                case "pcx": r = "image/x-pcx"; break;
                case "pdb": r = "chemical/x-pdb"; break;
                case "pdf": r = "application/pdf"; break;
                case "pfunk": r = "audio/make"; break;
                case "pgm": r = "image/x-portable-greymap"; break;
                case "pic": r = "image/pict"; break;
                case "pict": r = "image/pict"; break;
                case "pkg": r = "application/x-newton-compatible-pkg"; break;
                case "pko": r = "application/vnd.ms-pki.pko"; break;
                case "pl": r = "text/plain"; break;
                case "plx": r = "application/x-pixclscript"; break;
                case "pm": r = "image/x-xpixmap"; break;
                case "pm4": r = "application/x-pagemaker"; break;
                case "pm5": r = "application/x-pagemaker"; break;
                case "png": r = "image/png"; break;
                case "pnm": r = "application/x-portable-anymap"; break;
                case "pot": r = "application/vnd.ms-powerpoint"; break;
                case "pov": r = "model/x-pov"; break;
                case "ppa": r = "application/vnd.ms-powerpoint"; break;
                case "ppm": r = "image/x-portable-pixmap"; break;
                case "pps": r = "application/vnd.ms-powerpoint"; break;
                case "ppt": r = "application/vnd.ms-powerpoint"; break;
                case "ppz": r = "application/vnd.ms-powerpoint"; break;
                case "pre": r = "application/x-freelance"; break;
                case "prt": r = "application/pro_eng"; break;
                case "ps": r = "application/postscript"; break;
                case "psd": r = "application/octet-stream"; break;
                case "pvu": r = "paleovu/x-pv"; break;
                case "pwz": r = "application/vnd.ms-powerpoint"; break;
                case "py": r = "text/x-script.phyton"; break;
                case "pyc": r = "applicaiton/x-bytecode.python"; break;
                case "qcp": r = "audio/vnd.qcelp"; break;
                case "qd3": r = "x-world/x-3dmf"; break;
                case "qd3d": r = "x-world/x-3dmf"; break;
                case "qif": r = "image/x-quicktime"; break;
                case "qt": r = "video/quicktime"; break;
                case "qtc": r = "video/x-qtc"; break;
                case "qti": r = "image/x-quicktime"; break;
                case "qtif": r = "image/x-quicktime"; break;
                case "ra": r = "audio/x-pn-realaudio"; break;
                case "ram": r = "audio/x-pn-realaudio"; break;
                case "ras": r = "application/x-cmu-raster"; break;
                case "rast": r = "image/cmu-raster"; break;
                case "rexx": r = "text/x-script.rexx"; break;
                case "rf": r = "image/vnd.rn-realflash"; break;
                case "rgb": r = "image/x-rgb"; break;
                case "rm": r = "application/vnd.rn-realmedia"; break;
                case "rmi": r = "audio/mid"; break;
                case "rmm": r = "audio/x-pn-realaudio"; break;
                case "rmp": r = "audio/x-pn-realaudio"; break;
                case "rng": r = "application/ringing-tones"; break;
                case "rnx": r = "application/vnd.rn-realplayer"; break;
                case "roff": r = "application/x-troff"; break;
                case "rp": r = "image/vnd.rn-realpix"; break;
                case "rpm": r = "audio/x-pn-realaudio-plugin"; break;
                case "rt": r = "text/richtext"; break;
                case "rtf": r = "text/richtext"; break;
                case "rtx": r = "text/richtext"; break;
                case "rv": r = "video/vnd.rn-realvideo"; break;
                case "s": r = "text/x-asm"; break;
                case "s3m": r = "audio/s3m"; break;
                case "saveme": r = "application/octet-stream"; break;
                case "sbk": r = "application/x-tbook"; break;
                case "scm": r = "application/x-lotusscreencam"; break;
                case "sdml": r = "text/plain"; break;
                case "sdp": r = "application/sdp"; break;
                case "sdr": r = "application/sounder"; break;
                case "sea": r = "application/sea"; break;
                case "set": r = "application/set"; break;
                case "sgm": r = "text/sgml"; break;
                case "sgml": r = "text/sgml"; break;
                case "sh": r = "application/x-sh"; break;
                case "shar": r = "application/x-shar"; break;
                case "shtml": r = "text/html"; break;
                case "sid": r = "audio/x-psid"; break;
                case "sit": r = "application/x-sit"; break;
                case "skd": r = "application/x-koan"; break;
                case "skm": r = "application/x-koan"; break;
                case "skp": r = "application/x-koan"; break;
                case "skt": r = "application/x-koan"; break;
                case "sl": r = "application/x-seelogo"; break;
                case "smi": r = "application/smil"; break;
                case "smil": r = "application/smil"; break;
                case "snd": r = "audio/basic"; break;
                case "sol": r = "application/solids"; break;
                case "spc": r = "text/x-speech"; break;
                case "spl": r = "application/futuresplash"; break;
                case "spr": r = "application/x-sprite"; break;
                case "sprite": r = "application/x-sprite"; break;
                case "src": r = "application/x-wais-source"; break;
                case "ssi": r = "text/x-server-parsed-html"; break;
                case "ssm": r = "application/streamingmedia"; break;
                case "sst": r = "application/vnd.ms-pki.certstore"; break;
                case "step": r = "application/step"; break;
                case "stl": r = "application/sla"; break;
                case "stp": r = "application/step"; break;
                case "sv4cpio": r = "application/x-sv4cpio"; break;
                case "sv4crc": r = "application/x-sv4crc"; break;
                case "svf": r = "image/vnd.dwg"; break;
                case "svr": r = "application/x-world"; break;
                case "swf": r = "application/x-shockwave-flash"; break;
                case "t": r = "application/x-troff"; break;
                case "talk": r = "text/x-speech"; break;
                case "tar": r = "application/x-tar"; break;
                case "tbk": r = "application/toolbook"; break;
                case "tcl": r = "application/x-tcl"; break;
                case "tcsh": r = "text/x-script.tcsh"; break;
                case "tex": r = "application/x-tex"; break;
                case "texi": r = "application/x-texinfo"; break;
                case "texinfo": r = "application/x-texinfo"; break;
                case "text": r = "text/plain"; break;
                case "tgz": r = "application/x-compressed"; break;
                case "tif": r = "image/tiff"; break;
                case "tiff": r = "image/tiff"; break;
                case "tr": r = "application/x-troff"; break;
                case "tsi": r = "audio/tsp-audio"; break;
                case "tsp": r = "application/dsptype"; break;
                case "tsv": r = "text/tab-separated-values"; break;
                case "turbot": r = "image/florian"; break;
                case "txt": r = "text/plain"; break;
                case "uil": r = "text/x-uil"; break;
                case "uni": r = "text/uri-list"; break;
                case "unis": r = "text/uri-list"; break;
                case "unv": r = "application/i-deas"; break;
                case "uri": r = "text/uri-list"; break;
                case "uris": r = "text/uri-list"; break;
                case "ustar": r = "application/x-ustar"; break;
                case "uu": r = "application/octet-stream"; break;
                case "uue": r = "text/x-uuencode"; break;
                case "vcd": r = "application/x-cdlink"; break;
                case "vcs": r = "text/x-vcalendar"; break;
                case "vda": r = "application/vda"; break;
                case "vdo": r = "video/vdo"; break;
                case "vew": r = "application/groupwise"; break;
                case "viv": r = "video/vivo"; break;
                case "vivo": r = "video/vivo"; break;
                case "vmd": r = "application/vocaltec-media-desc"; break;
                case "vmf": r = "application/vocaltec-media-file"; break;
                case "voc": r = "audio/voc"; break;
                case "vos": r = "video/vosaic"; break;
                case "vox": r = "audio/voxware"; break;
                case "vqe": r = "audio/x-twinvq-plugin"; break;
                case "vqf": r = "audio/x-twinvq"; break;
                case "vql": r = "audio/x-twinvq-plugin"; break;
                case "vrml": r = "application/x-vrml"; break;
                case "vrt": r = "x-world/x-vrt"; break;
                case "vsd": r = "application/x-visio"; break;
                case "vst": r = "application/x-visio"; break;
                case "vsw": r = "application/x-visio"; break;
                case "w60": r = "application/wordperfect6.0"; break;
                case "w61": r = "application/wordperfect6.1"; break;
                case "w6w": r = "application/msword"; break;
                case "wav": r = "audio/wav"; break;
                case "wb1": r = "application/x-qpro"; break;
                case "wbmp": r = "image/vnd.wap.wbmp"; break;
                case "web": r = "application/vnd.xara"; break;
                case "wiz": r = "application/msword"; break;
                case "wk1": r = "application/x-123"; break;
                case "wmf": r = "windows/metafile"; break;
                case "wml": r = "text/vnd.wap.wml"; break;
                case "wmlc": r = "application/vnd.wap.wmlc"; break;
                case "wmls": r = "text/vnd.wap.wmlscript"; break;
                case "wmlsc": r = "application/vnd.wap.wmlscriptc"; break;
                case "word": r = "application/msword"; break;
                case "wp": r = "application/wordperfect"; break;
                case "wp5": r = "application/wordperfect"; break;
                case "wp6": r = "application/wordperfect"; break;
                case "wpd": r = "application/wordperfect"; break;
                case "wq1": r = "application/x-lotus"; break;
                case "wri": r = "application/mswrite"; break;
                case "wrl": r = "application/x-world"; break;
                case "wrz": r = "x-world/x-vrml"; break;
                case "wsc": r = "text/scriplet"; break;
                case "wsrc": r = "application/x-wais-source"; break;
                case "wtk": r = "application/x-wintalk"; break;
                case "xbm": r = "image/x-xbitmap"; break;
                case "xdr": r = "video/x-amt-demorun"; break;
                case "xgz": r = "xgl/drawing"; break;
                case "xif": r = "image/vnd.xiff"; break;
                case "xl": r = "application/excel"; break;
                case "xla": r = "application/vnd.ms-excel"; break;
                case "xlb": r = "application/vnd.ms-excel"; break;
                case "xlc": r = "application/vnd.ms-excel"; break;
                case "xld": r = "application/vnd.ms-excel"; break;
                case "xlk": r = "application/vnd.ms-excel"; break;
                case "xll": r = "application/vnd.ms-excel"; break;
                case "xlm": r = "application/vnd.ms-excel"; break;
                case "xls": r = "application/vnd.ms-excel"; break;
                case "xlt": r = "application/vnd.ms-excel"; break;
                case "xlv": r = "application/vnd.ms-excel"; break;
                case "xlw": r = "application/vnd.ms-excel"; break;
                case "xm": r = "audio/xm"; break;
                case "xml": r = "application/xml"; break;
                case "xmz": r = "xgl/movie"; break;
                case "xpix": r = "application/x-vnd.ls-xpix"; break;
                case "xpm": r = "image/xpm"; break;
                case "x-png": r = "image/png"; break;
                case "xsr": r = "video/x-amt-showrun"; break;
                case "xwd": r = "image/x-xwd"; break;
                case "xyz": r = "chemical/x-pdb"; break;
                case "z": r = "application/x-compressed"; break;
                case "zip": r = "application/zip"; break;
                case "zoo": r = "application/octet-stream"; break;
                case "zsh": r = "text/x-script.zsh"; break;
                default: r = "application/octet-stream"; break;
            }
            return r;
        }
    }

    internal static class IOExceptionExtensions
    {
        public static bool IsLockException(this IOException exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }
    }

    public partial class FileDB
    {
        /// <summary>
        /// Store a file inside the database
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <param name="fileName">Filename/Path to read file (eg: C:\Temp\MyPhoto.jpg)</param>
        /// <returns>EntryInfo with </returns>
        public static EntryInfo Store(string dbFileName, string fileName)
        {
            using (FileStream input = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return Store(dbFileName, fileName, input);
            }
        }

        /// <summary>
        /// Store a file inside the database
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <param name="fileName">Filename to associate with file (eg: MyPhoto.jpg)</param>
        /// <param name="input">Stream with a file content</param>
        /// <returns>EntryInfo with file information</returns>
        public static EntryInfo Store(string dbFileName, string fileName, Stream input)
        {
            using (var db = new FileDB(dbFileName, FileAccess.ReadWrite))
            {
                return db.Store(fileName, input);
            }
        }

        /// <summary>
        /// Read a file inside the database file
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <param name="id">File ID</param>
        /// <param name="fileName">Filename/Path to save the file (eg: C:\Temp\MyPhoto.jpg)</param>
        /// <returns>EntryInfo with file information</returns>
        public static EntryInfo Read(string dbFileName, Guid id, string fileName)
        {
            using (var db = new FileDB(dbFileName, FileAccess.Read))
            {
                return db.Read(id, fileName);
            }
        }

        /// <summary>
        /// Read a file inside the database file
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <param name="id">File ID</param>
        /// <param name="output">Stream to save the file</param>
        /// <returns>EntryInfo with file information</returns>
        public static EntryInfo Read(string dbFileName, Guid id, Stream output)
        {
            using (var db = new FileDB(dbFileName, FileAccess.Read))
            {
                return db.Read(id, output);
            }
        }

        /// <summary>
        /// Delete a file inside a database
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <returns>Array with all files identities</returns>
        public static EntryInfo[] ListFiles(string dbFileName)
        {
            using (var db = new FileDB(dbFileName, FileAccess.Read))
            {
                return db.ListFiles();
            }
        }

        /// <summary>
        /// Delete a file inside a database
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <param name="id">Guid of file</param>
        /// <returns>True with found and delete the file, otherwise false</returns>
        public static bool Delete(string dbFileName, Guid id)
        {
            using (var db = new FileDB(dbFileName, FileAccess.ReadWrite))
            {
                return db.Delete(id);
            }
        }

        /// <summary>
        /// Create a new database file
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        public static void CreateEmptyFile(string dbFileName)
        {
            CreateEmptyFile(dbFileName, true);
        }

        /// <summary>
        /// Create a new database file
        /// </summary>
        /// <param name="dbFileName">Database path/filname (eg: C:\Temp\MyDB.dat)</param>
        /// <param name="ignoreIfExists">True to ignore the file if already exists, otherise, throw a exception</param>
        public static void CreateEmptyFile(string dbFileName, bool ignoreIfExists)
        {
            if (File.Exists(dbFileName))
            {
                if (ignoreIfExists)
                    return;
                else
                    throw new FileDBException("Database file {0} already exists", dbFileName);
            }

            using (FileStream fileStream = new FileStream(dbFileName, FileMode.CreateNew, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    FileFactory.CreateEmptyFile(writer);
                }
            }
        }

        /// <summary>
        /// Shrink database file
        /// </summary>
        /// <param name="dbFileName">Path to database file (eg: C:\Temp\MyDB.dat)</param>
        public static void Shrink(string dbFileName)
        {
            using (var db = new FileDB(dbFileName, FileAccess.Read))
            {
                db.Shrink();
            }
        }

        /// <summary>
        /// Export all file inside a database to a directory
        /// </summary>
        /// <param name="dbFileName">FileDB database file</param>
        /// <param name="directory">Directory to export files</param>
        /// <param name="filePattern">File Pattern. Use keys: {id} {extension} {filename}. Eg: "{filename}.{id}.{extension}"</param>
        public static void Export(string dbFileName, string directory, string filePattern)
        {
            using (var db = new FileDB(dbFileName, FileAccess.Read))
            {
                db.Export(directory, filePattern);
            }
        }

    }

    public class DebugFile
    {
        private Engine _engine;

        internal DebugFile(Engine engine)
        {
            _engine = engine;
        }

        public string DisplayPages()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Constants:");
            sb.AppendLine("=============");
            sb.AppendLine("BasePage.PAGE_SIZE       : " + BasePage.PAGE_SIZE);
            sb.AppendLine("IndexPage.HEADER_SIZE    : " + IndexPage.HEADER_SIZE);
            sb.AppendLine("IndexPage.NODES_PER_PAGE : " + IndexPage.NODES_PER_PAGE);
            sb.AppendLine("DataPage.HEADER_SIZE     : " + DataPage.HEADER_SIZE);
            sb.AppendLine("DataPage.DATA_PER_PAGE   : " + DataPage.DATA_PER_PAGE);

            sb.AppendLine();
            sb.AppendLine("Header:");
            sb.AppendLine("=============");
            sb.AppendLine("IndexRootPageID    : " + _engine.Header.IndexRootPageID.Fmt());
            sb.AppendLine("FreeIndexPageID    : " + _engine.Header.FreeIndexPageID.Fmt());
            sb.AppendLine("FreeDataPageID     : " + _engine.Header.FreeDataPageID.Fmt());
            sb.AppendLine("LastFreeDataPageID : " + _engine.Header.LastFreeDataPageID.Fmt());
            sb.AppendLine("LastPageID         : " + _engine.Header.LastPageID.Fmt());

            sb.AppendLine();
            sb.AppendLine("Pages:");
            sb.AppendLine("=============");

            for (uint i = 0; i <= _engine.Header.LastPageID; i++)
            {
                BasePage page = PageFactory.GetBasePage(i, _engine.Reader);

                sb.AppendFormat("[{0}] >> [{1}] ({2}) ",
                    page.PageID.Fmt(), page.NextPageID.Fmt(), page.Type == PageType.Data ? "D" : "I");

                if (page.Type == PageType.Data)
                {
                    var dataPage = (DataPage)page;

                    if (dataPage.IsEmpty)
                        sb.Append("Empty");
                    else
                        sb.AppendFormat("Bytes: {0}", dataPage.DataBlockLength);
                }
                else
                {
                    var indexPage = (IndexPage)page;

                    sb.AppendFormat("Keys: {0}", indexPage.NodeIndex + 1);
                }


                sb.AppendLine();
            }


            return sb.ToString();
        }
    }

    internal static class Display
    {
        public static string Fmt(this uint val)
        {
            if (val == uint.MaxValue)
                return "----";
            else
                return val.ToString("0000");
        }
    }

    internal delegate void ReleasePageIndexFromCache(IndexPage page);

    internal class CacheIndexPage
    {
        public const int CACHE_SIZE = 200;

        private BinaryReader _reader;
        private BinaryWriter _writer;
        private Dictionary<uint, IndexPage> _cache;
        private uint _rootPageID;

        public CacheIndexPage(BinaryReader reader, BinaryWriter writer, uint rootPageID)
        {
            _reader = reader;
            _writer = writer;
            _cache = new Dictionary<uint, IndexPage>();
            _rootPageID = rootPageID;
        }

        public IndexPage GetPage(uint pageID)
        {
            if (_cache.ContainsKey(pageID))
                return _cache[pageID];

            var indexPage = PageFactory.GetIndexPage(pageID, _reader);

            AddPage(indexPage, false);

            return indexPage;
        }

        public void AddPage(IndexPage indexPage)
        {
            AddPage(indexPage, false);
        }

        public void AddPage(IndexPage indexPage, bool markAsDirty)
        {
            if (!_cache.ContainsKey(indexPage.PageID))
            {
                if (_cache.Count >= CACHE_SIZE)
                {
                    // Remove fist page that are not the root page (because I use too much)
                    var pageToRemove = _cache.First(x => x.Key != _rootPageID);

                    if (pageToRemove.Value.IsDirty)
                    {
                        PageFactory.WriteToFile(pageToRemove.Value, _writer);
                        pageToRemove.Value.IsDirty = false;
                    }

                    _cache.Remove(pageToRemove.Key);
                }

                _cache.Add(indexPage.PageID, indexPage);
            }

            if (markAsDirty)
                indexPage.IsDirty = true;
        }

        public void PersistPages()
        {
            // Check which pages is dirty and need to saved on disk 
            var pagesToPersist = _cache.Values.Where(x => x.IsDirty).ToArray();

            if (pagesToPersist.Length > 0)
            {
                foreach (var indexPage in pagesToPersist)
                {
                    PageFactory.WriteToFile(indexPage, _writer);
                    indexPage.IsDirty = false;
                }
            }
        }
    }

    internal static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        public static void Write(this BinaryWriter writer, DateTime dateTime)
        {
            writer.Write(dateTime.Ticks);
        }

        public static long Seek(this BinaryWriter writer, long position)
        {
            return writer.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }

    internal static class BinaryReaderExtensions
    {
        public static string ReadString(this BinaryReader reader, int size)
        {
            var bytes = reader.ReadBytes(size);
            string str = Encoding.UTF8.GetString(bytes);
            return str.Replace((char)0, ' ').Trim();
        }

        public static Guid ReadGuid(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(16);
            return new Guid(bytes);
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            var ticks = reader.ReadInt64();
            return new DateTime(ticks);
        }

        public static long Seek(this BinaryReader reader, long position)
        {
            return reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }
}
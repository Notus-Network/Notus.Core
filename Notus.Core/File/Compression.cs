// SharpZipLib(#ziplib, formerly NZipLib) is a compression library that supports Zip files
// using both stored and deflate compression methods, PKZIP 2.0 style and AES encryption,
// tar with GNU long filename extensions, GZip, zlib and raw deflate, as well as BZip2.
// The code is provided in icsharpcode's repo and the repo can be found here:
// https://github.com/icsharpcode/SharpZipLib

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Text;
using CT = System.Threading.CancellationToken;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Buffers;
using System.Text.RegularExpressions;
using System.Linq;

namespace Notus.Compression.TGZ
{
    /// <summary>
	/// Defines the contents of the general bit flags field for an archive entry.
	/// </summary>
	[Flags]
    public enum GeneralBitFlags
    {
        /// <summary>
        /// Bit 0 if set indicates that the file is encrypted
        /// </summary>
        Encrypted = 0x0001,

        /// <summary>
        /// Bits 1 and 2 - Two bits defining the compression method (only for Method 6 Imploding and 8,9 Deflating)
        /// </summary>
        Method = 0x0006,

        /// <summary>
        /// Bit 3 if set indicates a trailing data descriptor is appended to the entry data
        /// </summary>
        Descriptor = 0x0008,

        /// <summary>
        /// Bit 4 is reserved for use with method 8 for enhanced deflation
        /// </summary>
        ReservedPKware4 = 0x0010,

        /// <summary>
        /// Bit 5 if set indicates the file contains Pkzip compressed patched data.
        /// Requires version 2.7 or greater.
        /// </summary>
        Patched = 0x0020,

        /// <summary>
        /// Bit 6 if set indicates strong encryption has been used for this entry.
        /// </summary>
        StrongEncryption = 0x0040,

        /// <summary>
        /// Bit 7 is currently unused
        /// </summary>
        Unused7 = 0x0080,

        /// <summary>
        /// Bit 8 is currently unused
        /// </summary>
        Unused8 = 0x0100,

        /// <summary>
        /// Bit 9 is currently unused
        /// </summary>
        Unused9 = 0x0200,

        /// <summary>
        /// Bit 10 is currently unused
        /// </summary>
        Unused10 = 0x0400,

        /// <summary>
        /// Bit 11 if set indicates the filename and
        /// comment fields for this file must be encoded using UTF-8.
        /// </summary>
        UnicodeText = 0x0800,

        /// <summary>
        /// Bit 12 is documented as being reserved by PKware for enhanced compression.
        /// </summary>
        EnhancedCompress = 0x1000,

        /// <summary>
        /// Bit 13 if set indicates that values in the local header are masked to hide
        /// their actual values, and the central directory is encrypted.
        /// </summary>
        /// <remarks>
        /// Used when encrypting the central directory contents.
        /// </remarks>
        HeaderMasked = 0x2000,

        /// <summary>
        /// Bit 14 is documented as being reserved for use by PKware
        /// </summary>
        ReservedPkware14 = 0x4000,

        /// <summary>
        /// Bit 15 is documented as being reserved for use by PKware
        /// </summary>
        ReservedPkware15 = 0x8000
    }
    /// <summary>
	/// Utility class for resolving the encoding used for reading and writing strings
	/// </summary>
	public class StringCodec
    {
        static StringCodec()
        {
            try
            {
                var platformCodepage = Encoding.Default.CodePage;
                SystemDefaultCodePage = (platformCodepage == 1 || platformCodepage == 2 || platformCodepage == 3 || platformCodepage == 42) ? FallbackCodePage : platformCodepage;
            }
            catch
            {
                SystemDefaultCodePage = FallbackCodePage;
            }

            SystemDefaultEncoding = Encoding.GetEncoding(SystemDefaultCodePage);
        }

        /// <summary>
        /// If set, use the encoding set by <see cref="CodePage"/> for zip entries instead of the defaults
        /// </summary>
        public bool ForceZipLegacyEncoding { get; set; }

        /// <summary>
        /// The default encoding used for ZipCrypto passwords in zip files, set to <see cref="SystemDefaultEncoding"/>
        /// for greatest compability.
        /// </summary>
        public static Encoding DefaultZipCryptoEncoding => SystemDefaultEncoding;

        /// <summary>
        /// Returns the encoding for an output <see cref="ZipEntry"/>.
        /// Unless overriden by <see cref="ForceZipLegacyEncoding"/> it returns <see cref="UnicodeZipEncoding"/>.
        /// </summary>
        public Encoding ZipOutputEncoding => ZipEncoding(!ForceZipLegacyEncoding);

        /// <summary>
        /// Returns <see cref="UnicodeZipEncoding"/> if <paramref name="unicode"/> is set, otherwise it returns the encoding indicated by <see cref="CodePage"/>
        /// </summary>
        public Encoding ZipEncoding(bool unicode) => unicode ? UnicodeZipEncoding : _legacyEncoding;

        /// <summary>
        /// Returns the appropriate encoding for an input <see cref="ZipEntry"/> according to <paramref name="flags"/>.
        /// If overridden by <see cref="ForceZipLegacyEncoding"/>, it always returns the encoding indicated by <see cref="CodePage"/>.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public Encoding ZipInputEncoding(GeneralBitFlags flags) => ZipInputEncoding((int)flags);

        /// <inheritdoc cref="ZipInputEncoding(GeneralBitFlags)"/>
        public Encoding ZipInputEncoding(int flags) => ZipEncoding(!ForceZipLegacyEncoding && (flags & (int)GeneralBitFlags.UnicodeText) != 0);

        /// <summary>Code page encoding, used for non-unicode strings</summary>
        /// <remarks>
        /// The original Zip specification (https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT) states
        /// that file names should only be encoded with IBM Code Page 437 or UTF-8.
        /// In practice, most zip apps use OEM or system encoding (typically cp437 on Windows).
        /// Let's be good citizens and default to UTF-8 http://utf8everywhere.org/
        /// </remarks>
        private Encoding _legacyEncoding = SystemDefaultEncoding;

        private Encoding _zipArchiveCommentEncoding;
        private Encoding _zipCryptoEncoding;

        /// <summary>
        /// Returns the UTF-8 code page (65001) used for zip entries with unicode flag set
        /// </summary>
        public static readonly Encoding UnicodeZipEncoding = Encoding.UTF8;

        /// <summary>
        /// Code page used for non-unicode strings and legacy zip encoding (if <see cref="ForceZipLegacyEncoding"/> is set).
        /// Default value is <see cref="SystemDefaultCodePage"/>
        /// </summary>
        public int CodePage
        {
            get => _legacyEncoding.CodePage;
            set => _legacyEncoding = (value < 4 || value > 65535 || value == 42)
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : Encoding.GetEncoding(value);
        }

        private const int FallbackCodePage = 437;

        /// <summary>
        /// Operating system default codepage, or if it could not be retrieved, the fallback code page IBM 437.
        /// </summary>
        public static int SystemDefaultCodePage { get; }

        /// <summary>
        /// The system default encoding, based on <see cref="SystemDefaultCodePage"/>
        /// </summary>
        public static Encoding SystemDefaultEncoding { get; }

        /// <summary>
        /// The encoding used for the zip archive comment. Defaults to the encoding for <see cref="CodePage"/>, since
        /// no unicode flag can be set for it in the files.
        /// </summary>
        public Encoding ZipArchiveCommentEncoding
        {
            get => _zipArchiveCommentEncoding ?? _legacyEncoding;
            set => _zipArchiveCommentEncoding = value;
        }

        /// <summary>
        /// The encoding used for the ZipCrypto passwords. Defaults to <see cref="DefaultZipCryptoEncoding"/>.
        /// </summary>
        public Encoding ZipCryptoEncoding
        {
            get => _zipCryptoEncoding ?? DefaultZipCryptoEncoding;
            set => _zipCryptoEncoding = value;
        }
    }
    /// <summary>
	/// Transforms stream using AES in CTR mode
	/// </summary>
	internal class ZipAESTransform : ICryptoTransform
    {
#if NET45
		class IncrementalHash : HMACSHA1
		{
			bool _finalised;
			public IncrementalHash(byte[] key) : base(key) { }
			public static IncrementalHash CreateHMAC(string n, byte[] key) => new IncrementalHash(key);
			public void AppendData(byte[] buffer, int offset, int count) => TransformBlock(buffer, offset, count, buffer, offset);
			public byte[] GetHashAndReset()
			{
				if (!_finalised)
				{
					byte[] dummy = new byte[0];
					TransformFinalBlock(dummy, 0, 0);
					_finalised = true;
				}
				return Hash;
			}
		}

		static class HashAlgorithmName
		{
			public static string SHA1 = null;
		}
#endif

        private const int PWD_VER_LENGTH = 2;

        // WinZip use iteration count of 1000 for PBKDF2 key generation
        private const int KEY_ROUNDS = 1000;

        // For 128-bit AES (16 bytes) the encryption is implemented as expected.
        // For 256-bit AES (32 bytes) WinZip do full 256 bit AES of the nonce to create the encryption
        // block but use only the first 16 bytes of it, and discard the second half.
        private const int ENCRYPT_BLOCK = 16;

        private int _blockSize;
        private readonly ICryptoTransform _encryptor;
        private readonly byte[] _counterNonce;
        private byte[] _encryptBuffer;
        private int _encrPos;
        private byte[] _pwdVerifier;
        private IncrementalHash _hmacsha1;
        private byte[] _authCode = null;

        private bool _writeMode;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="key">Password string</param>
        /// <param name="saltBytes">Random bytes, length depends on encryption strength.
        /// 128 bits = 8 bytes, 192 bits = 12 bytes, 256 bits = 16 bytes.</param>
        /// <param name="blockSize">The encryption strength, in bytes eg 16 for 128 bits.</param>
        /// <param name="writeMode">True when creating a zip, false when reading. For the AuthCode.</param>
        ///
        public ZipAESTransform(string key, byte[] saltBytes, int blockSize, bool writeMode)
        {
            if (blockSize != 16 && blockSize != 32) // 24 valid for AES but not supported by Winzip
                throw new Exception("Invalid blocksize " + blockSize + ". Must be 16 or 32.");
            if (saltBytes.Length != blockSize / 2)
                throw new Exception("Invalid salt len. Must be " + blockSize / 2 + " for blocksize " + blockSize);
            // initialise the encryption buffer and buffer pos
            _blockSize = blockSize;
            _encryptBuffer = new byte[_blockSize];
            _encrPos = ENCRYPT_BLOCK;

            // Performs the equivalent of derive_key in Dr Brian Gladman's pwd2key.c
#if NET472_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            var pdb = new Rfc2898DeriveBytes(key, saltBytes, KEY_ROUNDS, HashAlgorithmName.SHA1);
#else
			var pdb = new Rfc2898DeriveBytes(key, saltBytes, KEY_ROUNDS);
#endif
            var rm = Aes.Create();
            rm.Mode = CipherMode.ECB;           // No feedback from cipher for CTR mode
            _counterNonce = new byte[_blockSize];
            byte[] key1bytes = pdb.GetBytes(_blockSize);
            byte[] key2bytes = pdb.GetBytes(_blockSize);

            // Use empty IV for AES
            _encryptor = rm.CreateEncryptor(key1bytes, new byte[16]);
            _pwdVerifier = pdb.GetBytes(PWD_VER_LENGTH);
            //
            _hmacsha1 = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, key2bytes);
            _writeMode = writeMode;
        }

        /// <summary>
        /// Implement the ICryptoTransform method.
        /// </summary>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            // Pass the data stream to the hash algorithm for generating the Auth Code.
            // This does not change the inputBuffer. Do this before decryption for read mode.
            if (!_writeMode)
            {
                _hmacsha1.AppendData(inputBuffer, inputOffset, inputCount);
            }
            // Encrypt with AES in CTR mode. Regards to Dr Brian Gladman for this.
            int ix = 0;
            while (ix < inputCount)
            {
                if (_encrPos == ENCRYPT_BLOCK)
                {
                    /* increment encryption nonce   */
                    int j = 0;
                    while (++_counterNonce[j] == 0)
                    {
                        ++j;
                    }
                    /* encrypt the nonce to form next xor buffer    */
                    _encryptor.TransformBlock(_counterNonce, 0, _blockSize, _encryptBuffer, 0);
                    _encrPos = 0;
                }
                outputBuffer[ix + outputOffset] = (byte)(inputBuffer[ix + inputOffset] ^ _encryptBuffer[_encrPos++]);
                //
                ix++;
            }
            if (_writeMode)
            {
                // This does not change the buffer.
                _hmacsha1.AppendData(outputBuffer, outputOffset, inputCount);
            }
            return inputCount;
        }

        /// <summary>
        /// Returns the 2 byte password verifier
        /// </summary>
        public byte[] PwdVerifier
        {
            get
            {
                return _pwdVerifier;
            }
        }

        /// <summary>
        /// Returns the 10 byte AUTH CODE to be checked or appended immediately following the AES data stream.
        /// </summary>
        public byte[] GetAuthCode()
        {
            if (_authCode == null)
            {
                _authCode = _hmacsha1.GetHashAndReset();
            }
            return _authCode;
        }

        #region ICryptoTransform Members

        /// <summary>
        /// Not implemented.
        /// </summary>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputCount > 0)
            {
                throw new NotImplementedException("TransformFinalBlock is not implemented and inputCount is greater than 0");
            }
            return Empty.Array<byte>();
        }

        /// <summary>
        /// Gets the size of the input data blocks in bytes.
        /// </summary>
        public int InputBlockSize
        {
            get
            {
                return _blockSize;
            }
        }

        /// <summary>
        /// Gets the size of the output data blocks in bytes.
        /// </summary>
        public int OutputBlockSize
        {
            get
            {
                return _blockSize;
            }
        }

        /// <summary>
        /// Gets a value indicating whether multiple blocks can be transformed.
        /// </summary>
        public bool CanTransformMultipleBlocks
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current transform can be reused.
        /// </summary>
        public bool CanReuseTransform
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Cleanup internal state.
        /// </summary>
        public void Dispose()
        {
            _encryptor.Dispose();
        }

        #endregion ICryptoTransform Members
    }
    /// <summary>
	/// ZipException represents exceptions specific to Zip classes and code.
	/// </summary>
	[Serializable]
    public class ZipException : SharpZipBaseException
    {
        /// <summary>
        /// Initialise a new instance of <see cref="ZipException" />.
        /// </summary>
        public ZipException()
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="ZipException" /> with its message string.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that describes the error.</param>
        public ZipException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="ZipException" />.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that describes the error.</param>
        /// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
        public ZipException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ZipException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected ZipException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    // <summary>
    /// Strategies for deflater
    /// </summary>
    public enum DeflateStrategy
    {
        /// <summary>
        /// The default strategy
        /// </summary>
        Default = 0,

        /// <summary>
        /// This strategy will only allow longer string repetitions.  It is
        /// useful for random data with a small character set.
        /// </summary>
        Filtered = 1,

        /// <summary>
        /// This strategy will not look for string repetitions at all.  It
        /// only encodes with Huffman trees (which means, that more common
        /// characters get a smaller encoding.
        /// </summary>
        HuffmanOnly = 2
    }

    // DEFLATE ALGORITHM:
    //
    // The uncompressed stream is inserted into the window array.  When
    // the window array is full the first half is thrown away and the
    // second half is copied to the beginning.
    //
    // The head array is a hash table.  Three characters build a hash value
    // and they the value points to the corresponding index in window of
    // the last string with this hash.  The prev array implements a
    // linked list of matches with the same hash: prev[index & WMASK] points
    // to the previous index with the same hash.
    //

    /// <summary>
    /// Low level compression engine for deflate algorithm which uses a 32K sliding window
    /// with secondary compression from Huffman/Shannon-Fano codes.
    /// </summary>
    public class DeflaterEngine
    {
        #region Constants

        private const int TooFar = 4096;

        #endregion Constants

        #region Constructors

        /// <summary>
        /// Construct instance with pending buffer
        /// Adler calculation will be performed
        /// </summary>
        /// <param name="pending">
        /// Pending buffer to use
        /// </param>
        public DeflaterEngine(DeflaterPending pending)
            : this(pending, false)
        {
        }



        /// <summary>
        /// Construct instance with pending buffer
        /// </summary>
        /// <param name="pending">
        /// Pending buffer to use
        /// </param>
        /// <param name="noAdlerCalculation">
        /// If no adler calculation should be performed
        /// </param>
        public DeflaterEngine(DeflaterPending pending, bool noAdlerCalculation)
        {
            this.pending = pending;
            huffman = new DeflaterHuffman(pending);
            if (!noAdlerCalculation)
                adler = new Adler32();

            window = new byte[2 * DeflaterConstants.WSIZE];
            head = new short[DeflaterConstants.HASH_SIZE];
            prev = new short[DeflaterConstants.WSIZE];

            // We start at index 1, to avoid an implementation deficiency, that
            // we cannot build a repeat pattern at index 0.
            blockStart = strstart = 1;
        }

        #endregion Constructors

        /// <summary>
        /// Deflate drives actual compression of data
        /// </summary>
        /// <param name="flush">True to flush input buffers</param>
        /// <param name="finish">Finish deflation with the current input.</param>
        /// <returns>Returns true if progress has been made.</returns>
        public bool Deflate(bool flush, bool finish)
        {
            bool progress;
            do
            {
                FillWindow();
                bool canFlush = flush && (inputOff == inputEnd);

#if DebugDeflation
				if (DeflaterConstants.DEBUGGING) {
					Console.WriteLine("window: [" + blockStart + "," + strstart + ","
								+ lookahead + "], " + compressionFunction + "," + canFlush);
				}
#endif
                switch (compressionFunction)
                {
                    case DeflaterConstants.DEFLATE_STORED:
                        progress = DeflateStored(canFlush, finish);
                        break;

                    case DeflaterConstants.DEFLATE_FAST:
                        progress = DeflateFast(canFlush, finish);
                        break;

                    case DeflaterConstants.DEFLATE_SLOW:
                        progress = DeflateSlow(canFlush, finish);
                        break;

                    default:
                        throw new InvalidOperationException("unknown compressionFunction");
                }
            } while (pending.IsFlushed && progress); // repeat while we have no pending output and progress was made
            return progress;
        }

        /// <summary>
        /// Sets input data to be deflated.  Should only be called when <code>NeedsInput()</code>
        /// returns true
        /// </summary>
        /// <param name="buffer">The buffer containing input data.</param>
        /// <param name="offset">The offset of the first byte of data.</param>
        /// <param name="count">The number of bytes of data to use as input.</param>
        public void SetInput(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (inputOff < inputEnd)
            {
                throw new InvalidOperationException("Old input was not completely processed");
            }

            int end = offset + count;

            /* We want to throw an ArrayIndexOutOfBoundsException early.  The
			* check is very tricky: it also handles integer wrap around.
			*/
            if ((offset > end) || (end > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            inputBuf = buffer;
            inputOff = offset;
            inputEnd = end;
        }

        /// <summary>
        /// Determines if more <see cref="SetInput">input</see> is needed.
        /// </summary>
        /// <returns>Return true if input is needed via <see cref="SetInput">SetInput</see></returns>
        public bool NeedsInput()
        {
            return (inputEnd == inputOff);
        }

        /// <summary>
        /// Set compression dictionary
        /// </summary>
        /// <param name="buffer">The buffer containing the dictionary data</param>
        /// <param name="offset">The offset in the buffer for the first byte of data</param>
        /// <param name="length">The length of the dictionary data.</param>
        public void SetDictionary(byte[] buffer, int offset, int length)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (strstart != 1) )
			{
				throw new InvalidOperationException("strstart not 1");
			}
#endif
            adler?.Update(new ArraySegment<byte>(buffer, offset, length));
            if (length < DeflaterConstants.MIN_MATCH)
            {
                return;
            }

            if (length > DeflaterConstants.MAX_DIST)
            {
                offset += length - DeflaterConstants.MAX_DIST;
                length = DeflaterConstants.MAX_DIST;
            }

            System.Array.Copy(buffer, offset, window, strstart, length);

            UpdateHash();
            --length;
            while (--length > 0)
            {
                InsertString();
                strstart++;
            }
            strstart += 2;
            blockStart = strstart;
        }

        /// <summary>
        /// Reset internal state
        /// </summary>
        public void Reset()
        {
            huffman.Reset();
            adler?.Reset();
            blockStart = strstart = 1;
            lookahead = 0;
            totalIn = 0;
            prevAvailable = false;
            matchLen = DeflaterConstants.MIN_MATCH - 1;

            for (int i = 0; i < DeflaterConstants.HASH_SIZE; i++)
            {
                head[i] = 0;
            }

            for (int i = 0; i < DeflaterConstants.WSIZE; i++)
            {
                prev[i] = 0;
            }
        }

        /// <summary>
        /// Reset Adler checksum
        /// </summary>
        public void ResetAdler()
        {
            adler?.Reset();
        }

        /// <summary>
        /// Get current value of Adler checksum
        /// </summary>
        public int Adler
        {
            get
            {
                return (adler != null) ? unchecked((int)adler.Value) : 0;
            }
        }

        /// <summary>
        /// Total data processed
        /// </summary>
        public long TotalIn
        {
            get
            {
                return totalIn;
            }
        }

        /// <summary>
        /// Get/set the <see cref="DeflateStrategy">deflate strategy</see>
        /// </summary>
        public DeflateStrategy Strategy
        {
            get
            {
                return strategy;
            }
            set
            {
                strategy = value;
            }
        }

        /// <summary>
        /// Set the deflate level (0-9)
        /// </summary>
        /// <param name="level">The value to set the level to.</param>
        public void SetLevel(int level)
        {
            if ((level < 0) || (level > 9))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            goodLength = DeflaterConstants.GOOD_LENGTH[level];
            max_lazy = DeflaterConstants.MAX_LAZY[level];
            niceLength = DeflaterConstants.NICE_LENGTH[level];
            max_chain = DeflaterConstants.MAX_CHAIN[level];

            if (DeflaterConstants.COMPR_FUNC[level] != compressionFunction)
            {
#if DebugDeflation
				if (DeflaterConstants.DEBUGGING) {
				   Console.WriteLine("Change from " + compressionFunction + " to "
										  + DeflaterConstants.COMPR_FUNC[level]);
				}
#endif
                switch (compressionFunction)
                {
                    case DeflaterConstants.DEFLATE_STORED:
                        if (strstart > blockStart)
                        {
                            huffman.FlushStoredBlock(window, blockStart,
                                strstart - blockStart, false);
                            blockStart = strstart;
                        }
                        UpdateHash();
                        break;

                    case DeflaterConstants.DEFLATE_FAST:
                        if (strstart > blockStart)
                        {
                            huffman.FlushBlock(window, blockStart, strstart - blockStart,
                                false);
                            blockStart = strstart;
                        }
                        break;

                    case DeflaterConstants.DEFLATE_SLOW:
                        if (prevAvailable)
                        {
                            huffman.TallyLit(window[strstart - 1] & 0xff);
                        }
                        if (strstart > blockStart)
                        {
                            huffman.FlushBlock(window, blockStart, strstart - blockStart, false);
                            blockStart = strstart;
                        }
                        prevAvailable = false;
                        matchLen = DeflaterConstants.MIN_MATCH - 1;
                        break;
                }
                compressionFunction = DeflaterConstants.COMPR_FUNC[level];
            }
        }

        /// <summary>
        /// Fill the window
        /// </summary>
        public void FillWindow()
        {
            /* If the window is almost full and there is insufficient lookahead,
			 * move the upper half to the lower one to make room in the upper half.
			 */
            if (strstart >= DeflaterConstants.WSIZE + DeflaterConstants.MAX_DIST)
            {
                SlideWindow();
            }

            /* If there is not enough lookahead, but still some input left,
			 * read in the input
			 */
            if (lookahead < DeflaterConstants.MIN_LOOKAHEAD && inputOff < inputEnd)
            {
                int more = 2 * DeflaterConstants.WSIZE - lookahead - strstart;

                if (more > inputEnd - inputOff)
                {
                    more = inputEnd - inputOff;
                }

                System.Array.Copy(inputBuf, inputOff, window, strstart + lookahead, more);
                adler?.Update(new ArraySegment<byte>(inputBuf, inputOff, more));

                inputOff += more;
                totalIn += more;
                lookahead += more;
            }

            if (lookahead >= DeflaterConstants.MIN_MATCH)
            {
                UpdateHash();
            }
        }

        private void UpdateHash()
        {
            /*
						if (DEBUGGING) {
							Console.WriteLine("updateHash: "+strstart);
						}
			*/
            ins_h = (window[strstart] << DeflaterConstants.HASH_SHIFT) ^ window[strstart + 1];
        }

        /// <summary>
        /// Inserts the current string in the head hash and returns the previous
        /// value for this hash.
        /// </summary>
        /// <returns>The previous hash value</returns>
        private int InsertString()
        {
            short match;
            int hash = ((ins_h << DeflaterConstants.HASH_SHIFT) ^ window[strstart + (DeflaterConstants.MIN_MATCH - 1)]) & DeflaterConstants.HASH_MASK;

#if DebugDeflation
			if (DeflaterConstants.DEBUGGING)
			{
				if (hash != (((window[strstart] << (2*HASH_SHIFT)) ^
								  (window[strstart + 1] << HASH_SHIFT) ^
								  (window[strstart + 2])) & HASH_MASK)) {
						throw new SharpZipBaseException("hash inconsistent: " + hash + "/"
												+window[strstart] + ","
												+window[strstart + 1] + ","
												+window[strstart + 2] + "," + HASH_SHIFT);
					}
			}
#endif
            prev[strstart & DeflaterConstants.WMASK] = match = head[hash];
            head[hash] = unchecked((short)strstart);
            ins_h = hash;
            return match & 0xffff;
        }

        private void SlideWindow()
        {
            Array.Copy(window, DeflaterConstants.WSIZE, window, 0, DeflaterConstants.WSIZE);
            matchStart -= DeflaterConstants.WSIZE;
            strstart -= DeflaterConstants.WSIZE;
            blockStart -= DeflaterConstants.WSIZE;

            // Slide the hash table (could be avoided with 32 bit values
            // at the expense of memory usage).
            for (int i = 0; i < DeflaterConstants.HASH_SIZE; ++i)
            {
                int m = head[i] & 0xffff;
                head[i] = (short)(m >= DeflaterConstants.WSIZE ? (m - DeflaterConstants.WSIZE) : 0);
            }

            // Slide the prev table.
            for (int i = 0; i < DeflaterConstants.WSIZE; i++)
            {
                int m = prev[i] & 0xffff;
                prev[i] = (short)(m >= DeflaterConstants.WSIZE ? (m - DeflaterConstants.WSIZE) : 0);
            }
        }

        /// <summary>
        /// Find the best (longest) string in the window matching the
        /// string starting at strstart.
        ///
        /// Preconditions:
        /// <code>
        /// strstart + DeflaterConstants.MAX_MATCH &lt;= window.length.</code>
        /// </summary>
        /// <param name="curMatch"></param>
        /// <returns>True if a match greater than the minimum length is found</returns>
        private bool FindLongestMatch(int curMatch)
        {
            int match;
            int scan = strstart;
            // scanMax is the highest position that we can look at
            int scanMax = scan + Math.Min(DeflaterConstants.MAX_MATCH, lookahead) - 1;
            int limit = Math.Max(scan - DeflaterConstants.MAX_DIST, 0);

            byte[] window = this.window;
            short[] prev = this.prev;
            int chainLength = this.max_chain;
            int niceLength = Math.Min(this.niceLength, lookahead);

            matchLen = Math.Max(matchLen, DeflaterConstants.MIN_MATCH - 1);

            if (scan + matchLen > scanMax) return false;

            byte scan_end1 = window[scan + matchLen - 1];
            byte scan_end = window[scan + matchLen];

            // Do not waste too much time if we already have a good match:
            if (matchLen >= this.goodLength) chainLength >>= 2;

            do
            {
                match = curMatch;
                scan = strstart;

                if (window[match + matchLen] != scan_end
                 || window[match + matchLen - 1] != scan_end1
                 || window[match] != window[scan]
                 || window[++match] != window[++scan])
                {
                    continue;
                }

                // scan is set to strstart+1 and the comparison passed, so
                // scanMax - scan is the maximum number of bytes we can compare.
                // below we compare 8 bytes at a time, so first we compare
                // (scanMax - scan) % 8 bytes, so the remainder is a multiple of 8

                switch ((scanMax - scan) % 8)
                {
                    case 1:
                        if (window[++scan] == window[++match]) break;
                        break;

                    case 2:
                        if (window[++scan] == window[++match]
                  && window[++scan] == window[++match]) break;
                        break;

                    case 3:
                        if (window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]) break;
                        break;

                    case 4:
                        if (window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]) break;
                        break;

                    case 5:
                        if (window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]) break;
                        break;

                    case 6:
                        if (window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]) break;
                        break;

                    case 7:
                        if (window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]
                  && window[++scan] == window[++match]) break;
                        break;
                }

                if (window[scan] == window[match])
                {
                    /* We check for insufficient lookahead only every 8th comparison;
					 * the 256th check will be made at strstart + 258 unless lookahead is
					 * exhausted first.
					 */
                    do
                    {
                        if (scan == scanMax)
                        {
                            ++scan;     // advance to first position not matched
                            ++match;

                            break;
                        }
                    }
                    while (window[++scan] == window[++match]
                        && window[++scan] == window[++match]
                        && window[++scan] == window[++match]
                        && window[++scan] == window[++match]
                        && window[++scan] == window[++match]
                        && window[++scan] == window[++match]
                        && window[++scan] == window[++match]
                        && window[++scan] == window[++match]);
                }

                if (scan - strstart > matchLen)
                {
#if DebugDeflation
              if (DeflaterConstants.DEBUGGING && (ins_h == 0) )
              Console.Error.WriteLine("Found match: " + curMatch + "-" + (scan - strstart));
#endif

                    matchStart = curMatch;
                    matchLen = scan - strstart;

                    if (matchLen >= niceLength)
                        break;

                    scan_end1 = window[scan - 1];
                    scan_end = window[scan];
                }
            } while ((curMatch = (prev[curMatch & DeflaterConstants.WMASK] & 0xffff)) > limit && 0 != --chainLength);

            return matchLen >= DeflaterConstants.MIN_MATCH;
        }

        private bool DeflateStored(bool flush, bool finish)
        {
            if (!flush && (lookahead == 0))
            {
                return false;
            }

            strstart += lookahead;
            lookahead = 0;

            int storedLength = strstart - blockStart;

            if ((storedLength >= DeflaterConstants.MAX_BLOCK_SIZE) || // Block is full
                (blockStart < DeflaterConstants.WSIZE && storedLength >= DeflaterConstants.MAX_DIST) ||   // Block may move out of window
                flush)
            {
                bool lastBlock = finish;
                if (storedLength > DeflaterConstants.MAX_BLOCK_SIZE)
                {
                    storedLength = DeflaterConstants.MAX_BLOCK_SIZE;
                    lastBlock = false;
                }

#if DebugDeflation
				if (DeflaterConstants.DEBUGGING)
				{
				   Console.WriteLine("storedBlock[" + storedLength + "," + lastBlock + "]");
				}
#endif

                huffman.FlushStoredBlock(window, blockStart, storedLength, lastBlock);
                blockStart += storedLength;
                return !(lastBlock || storedLength == 0);
            }
            return true;
        }

        private bool DeflateFast(bool flush, bool finish)
        {
            if (lookahead < DeflaterConstants.MIN_LOOKAHEAD && !flush)
            {
                return false;
            }

            while (lookahead >= DeflaterConstants.MIN_LOOKAHEAD || flush)
            {
                if (lookahead == 0)
                {
                    // We are flushing everything
                    huffman.FlushBlock(window, blockStart, strstart - blockStart, finish);
                    blockStart = strstart;
                    return false;
                }

                if (strstart > 2 * DeflaterConstants.WSIZE - DeflaterConstants.MIN_LOOKAHEAD)
                {
                    /* slide window, as FindLongestMatch needs this.
					 * This should only happen when flushing and the window
					 * is almost full.
					 */
                    SlideWindow();
                }

                int hashHead;
                if (lookahead >= DeflaterConstants.MIN_MATCH &&
                    (hashHead = InsertString()) != 0 &&
                    strategy != DeflateStrategy.HuffmanOnly &&
                    strstart - hashHead <= DeflaterConstants.MAX_DIST &&
                    FindLongestMatch(hashHead))
                {
                    // longestMatch sets matchStart and matchLen
#if DebugDeflation
					if (DeflaterConstants.DEBUGGING)
					{
						for (int i = 0 ; i < matchLen; i++) {
							if (window[strstart + i] != window[matchStart + i]) {
								throw new SharpZipBaseException("Match failure");
							}
						}
					}
#endif

                    bool full = huffman.TallyDist(strstart - matchStart, matchLen);

                    lookahead -= matchLen;
                    if (matchLen <= max_lazy && lookahead >= DeflaterConstants.MIN_MATCH)
                    {
                        while (--matchLen > 0)
                        {
                            ++strstart;
                            InsertString();
                        }
                        ++strstart;
                    }
                    else
                    {
                        strstart += matchLen;
                        if (lookahead >= DeflaterConstants.MIN_MATCH - 1)
                        {
                            UpdateHash();
                        }
                    }
                    matchLen = DeflaterConstants.MIN_MATCH - 1;
                    if (!full)
                    {
                        continue;
                    }
                }
                else
                {
                    // No match found
                    huffman.TallyLit(window[strstart] & 0xff);
                    ++strstart;
                    --lookahead;
                }

                if (huffman.IsFull())
                {
                    bool lastBlock = finish && (lookahead == 0);
                    huffman.FlushBlock(window, blockStart, strstart - blockStart, lastBlock);
                    blockStart = strstart;
                    return !lastBlock;
                }
            }
            return true;
        }

        private bool DeflateSlow(bool flush, bool finish)
        {
            if (lookahead < DeflaterConstants.MIN_LOOKAHEAD && !flush)
            {
                return false;
            }

            while (lookahead >= DeflaterConstants.MIN_LOOKAHEAD || flush)
            {
                if (lookahead == 0)
                {
                    if (prevAvailable)
                    {
                        huffman.TallyLit(window[strstart - 1] & 0xff);
                    }
                    prevAvailable = false;

                    // We are flushing everything
#if DebugDeflation
					if (DeflaterConstants.DEBUGGING && !flush)
					{
						throw new SharpZipBaseException("Not flushing, but no lookahead");
					}
#endif
                    huffman.FlushBlock(window, blockStart, strstart - blockStart,
                        finish);
                    blockStart = strstart;
                    return false;
                }

                if (strstart >= 2 * DeflaterConstants.WSIZE - DeflaterConstants.MIN_LOOKAHEAD)
                {
                    /* slide window, as FindLongestMatch needs this.
					 * This should only happen when flushing and the window
					 * is almost full.
					 */
                    SlideWindow();
                }

                int prevMatch = matchStart;
                int prevLen = matchLen;
                if (lookahead >= DeflaterConstants.MIN_MATCH)
                {
                    int hashHead = InsertString();

                    if (strategy != DeflateStrategy.HuffmanOnly &&
                        hashHead != 0 &&
                        strstart - hashHead <= DeflaterConstants.MAX_DIST &&
                        FindLongestMatch(hashHead))
                    {
                        // longestMatch sets matchStart and matchLen

                        // Discard match if too small and too far away
                        if (matchLen <= 5 && (strategy == DeflateStrategy.Filtered || (matchLen == DeflaterConstants.MIN_MATCH && strstart - matchStart > TooFar)))
                        {
                            matchLen = DeflaterConstants.MIN_MATCH - 1;
                        }
                    }
                }

                // previous match was better
                if ((prevLen >= DeflaterConstants.MIN_MATCH) && (matchLen <= prevLen))
                {
#if DebugDeflation
					if (DeflaterConstants.DEBUGGING)
					{
					   for (int i = 0 ; i < matchLen; i++) {
						  if (window[strstart-1+i] != window[prevMatch + i])
							 throw new SharpZipBaseException();
						}
					}
#endif
                    huffman.TallyDist(strstart - 1 - prevMatch, prevLen);
                    prevLen -= 2;
                    do
                    {
                        strstart++;
                        lookahead--;
                        if (lookahead >= DeflaterConstants.MIN_MATCH)
                        {
                            InsertString();
                        }
                    } while (--prevLen > 0);

                    strstart++;
                    lookahead--;
                    prevAvailable = false;
                    matchLen = DeflaterConstants.MIN_MATCH - 1;
                }
                else
                {
                    if (prevAvailable)
                    {
                        huffman.TallyLit(window[strstart - 1] & 0xff);
                    }
                    prevAvailable = true;
                    strstart++;
                    lookahead--;
                }

                if (huffman.IsFull())
                {
                    int len = strstart - blockStart;
                    if (prevAvailable)
                    {
                        len--;
                    }
                    bool lastBlock = (finish && (lookahead == 0) && !prevAvailable);
                    huffman.FlushBlock(window, blockStart, len, lastBlock);
                    blockStart += len;
                    return !lastBlock;
                }
            }
            return true;
        }

        #region Instance Fields

        // Hash index of string to be inserted
        private int ins_h;

        /// <summary>
        /// Hashtable, hashing three characters to an index for window, so
        /// that window[index]..window[index+2] have this hash code.
        /// Note that the array should really be unsigned short, so you need
        /// to and the values with 0xffff.
        /// </summary>
        private short[] head;

        /// <summary>
        /// <code>prev[index &amp; WMASK]</code> points to the previous index that has the
        /// same hash code as the string starting at index.  This way
        /// entries with the same hash code are in a linked list.
        /// Note that the array should really be unsigned short, so you need
        /// to and the values with 0xffff.
        /// </summary>
        private short[] prev;

        private int matchStart;

        // Length of best match
        private int matchLen;

        // Set if previous match exists
        private bool prevAvailable;

        private int blockStart;

        /// <summary>
        /// Points to the current character in the window.
        /// </summary>
        private int strstart;

        /// <summary>
        /// lookahead is the number of characters starting at strstart in
        /// window that are valid.
        /// So window[strstart] until window[strstart+lookahead-1] are valid
        /// characters.
        /// </summary>
        private int lookahead;

        /// <summary>
        /// This array contains the part of the uncompressed stream that
        /// is of relevance.  The current character is indexed by strstart.
        /// </summary>
        private byte[] window;

        private DeflateStrategy strategy;
        private int max_chain, max_lazy, niceLength, goodLength;

        /// <summary>
        /// The current compression function.
        /// </summary>
        private int compressionFunction;

        /// <summary>
        /// The input data for compression.
        /// </summary>
        private byte[] inputBuf;

        /// <summary>
        /// The total bytes of input read.
        /// </summary>
        private long totalIn;

        /// <summary>
        /// The offset into inputBuf, where input data starts.
        /// </summary>
        private int inputOff;

        /// <summary>
        /// The end offset of the input data.
        /// </summary>
        private int inputEnd;

        private DeflaterPending pending;
        private DeflaterHuffman huffman;

        /// <summary>
        /// The adler checksum
        /// </summary>
        private Adler32 adler;

        #endregion Instance Fields
    }
    /// <summary>
	/// This class is general purpose class for writing data to a buffer.
	///
	/// It allows you to write bits as well as bytes
	/// Based on DeflaterPending.java
	///
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class PendingBuffer
    {
        #region Instance Fields

        /// <summary>
        /// Internal work buffer
        /// </summary>
        private readonly byte[] buffer;

        private int start;
        private int end;

        private uint bits;
        private int bitCount;

        #endregion Instance Fields

        #region Constructors

        /// <summary>
        /// construct instance using default buffer size of 4096
        /// </summary>
        public PendingBuffer() : this(4096)
        {
        }

        /// <summary>
        /// construct instance using specified buffer size
        /// </summary>
        /// <param name="bufferSize">
        /// size to use for internal buffer
        /// </param>
        public PendingBuffer(int bufferSize)
        {
            buffer = new byte[bufferSize];
        }

        #endregion Constructors

        /// <summary>
        /// Clear internal state/buffers
        /// </summary>
        public void Reset()
        {
            start = end = bitCount = 0;
        }

        /// <summary>
        /// Write a byte to buffer
        /// </summary>
        /// <param name="value">
        /// The value to write
        /// </param>
        public void WriteByte(int value)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}
#endif
            buffer[end++] = unchecked((byte)value);
        }

        /// <summary>
        /// Write a short value to buffer LSB first
        /// </summary>
        /// <param name="value">
        /// The value to write.
        /// </param>
        public void WriteShort(int value)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}
#endif
            buffer[end++] = unchecked((byte)value);
            buffer[end++] = unchecked((byte)(value >> 8));
        }

        /// <summary>
        /// write an integer LSB first
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt(int value)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}
#endif
            buffer[end++] = unchecked((byte)value);
            buffer[end++] = unchecked((byte)(value >> 8));
            buffer[end++] = unchecked((byte)(value >> 16));
            buffer[end++] = unchecked((byte)(value >> 24));
        }

        /// <summary>
        /// Write a block of data to buffer
        /// </summary>
        /// <param name="block">data to write</param>
        /// <param name="offset">offset of first byte to write</param>
        /// <param name="length">number of bytes to write</param>
        public void WriteBlock(byte[] block, int offset, int length)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}
#endif
            System.Array.Copy(block, offset, buffer, end, length);
            end += length;
        }

        /// <summary>
        /// The number of bits written to the buffer
        /// </summary>
        public int BitCount
        {
            get
            {
                return bitCount;
            }
        }

        /// <summary>
        /// Align internal buffer on a byte boundary
        /// </summary>
        public void AlignToByte()
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}
#endif
            if (bitCount > 0)
            {
                buffer[end++] = unchecked((byte)bits);
                if (bitCount > 8)
                {
                    buffer[end++] = unchecked((byte)(bits >> 8));
                }
            }
            bits = 0;
            bitCount = 0;
        }

        /// <summary>
        /// Write bits to internal buffer
        /// </summary>
        /// <param name="b">source of bits</param>
        /// <param name="count">number of bits to write</param>
        public void WriteBits(int b, int count)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}

			//			if (DeflaterConstants.DEBUGGING) {
			//				//Console.WriteLine("writeBits("+b+","+count+")");
			//			}
#endif
            bits |= (uint)(b << bitCount);
            bitCount += count;
            if (bitCount >= 16)
            {
                buffer[end++] = unchecked((byte)bits);
                buffer[end++] = unchecked((byte)(bits >> 8));
                bits >>= 16;
                bitCount -= 16;
            }
        }

        /// <summary>
        /// Write a short value to internal buffer most significant byte first
        /// </summary>
        /// <param name="s">value to write</param>
        public void WriteShortMSB(int s)
        {
#if DebugDeflation
			if (DeflaterConstants.DEBUGGING && (start != 0) )
			{
				throw new SharpZipBaseException("Debug check: start != 0");
			}
#endif
            buffer[end++] = unchecked((byte)(s >> 8));
            buffer[end++] = unchecked((byte)s);
        }

        /// <summary>
        /// Indicates if buffer has been flushed
        /// </summary>
        public bool IsFlushed
        {
            get
            {
                return end == 0;
            }
        }

        /// <summary>
        /// Flushes the pending buffer into the given output array.  If the
        /// output array is to small, only a partial flush is done.
        /// </summary>
        /// <param name="output">The output array.</param>
        /// <param name="offset">The offset into output array.</param>
        /// <param name="length">The maximum number of bytes to store.</param>
        /// <returns>The number of bytes flushed.</returns>
        public int Flush(byte[] output, int offset, int length)
        {
            if (bitCount >= 8)
            {
                buffer[end++] = unchecked((byte)bits);
                bits >>= 8;
                bitCount -= 8;
            }

            if (length > end - start)
            {
                length = end - start;
                System.Array.Copy(buffer, start, output, offset, length);
                start = 0;
                end = 0;
            }
            else
            {
                System.Array.Copy(buffer, start, output, offset, length);
                start += length;
            }
            return length;
        }

        /// <summary>
        /// Convert internal buffer to byte array.
        /// Buffer is empty on completion
        /// </summary>
        /// <returns>
        /// The internal buffer contents converted to a byte array.
        /// </returns>
        public byte[] ToByteArray()
        {
            AlignToByte();

            byte[] result = new byte[end - start];
            System.Array.Copy(buffer, start, result, 0, result.Length);
            start = 0;
            end = 0;
            return result;
        }
    }
    /// <summary>
	/// This class stores the pending output of the Deflater.
	///
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class DeflaterPending : PendingBuffer
    {
        /// <summary>
        /// Construct instance with default buffer size
        /// </summary>
        public DeflaterPending() : base(DeflaterConstants.PENDING_BUF_SIZE)
        {
        }
    }
    /// <summary>
	/// This class contains constants used for deflation.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "kept for backwards compatibility")]
    public static class DeflaterConstants
    {
        /// <summary>
        /// Set to true to enable debugging
        /// </summary>
        public const bool DEBUGGING = false;

        /// <summary>
        /// Written to Zip file to identify a stored block
        /// </summary>
        public const int STORED_BLOCK = 0;

        /// <summary>
        /// Identifies static tree in Zip file
        /// </summary>
        public const int STATIC_TREES = 1;

        /// <summary>
        /// Identifies dynamic tree in Zip file
        /// </summary>
        public const int DYN_TREES = 2;

        /// <summary>
        /// Header flag indicating a preset dictionary for deflation
        /// </summary>
        public const int PRESET_DICT = 0x20;

        /// <summary>
        /// Sets internal buffer sizes for Huffman encoding
        /// </summary>
        public const int DEFAULT_MEM_LEVEL = 8;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int MAX_MATCH = 258;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int MIN_MATCH = 3;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int MAX_WBITS = 15;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int WSIZE = 1 << MAX_WBITS;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int WMASK = WSIZE - 1;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int HASH_BITS = DEFAULT_MEM_LEVEL + 7;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int HASH_SIZE = 1 << HASH_BITS;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int HASH_MASK = HASH_SIZE - 1;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int HASH_SHIFT = (HASH_BITS + MIN_MATCH - 1) / MIN_MATCH;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int MIN_LOOKAHEAD = MAX_MATCH + MIN_MATCH + 1;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int MAX_DIST = WSIZE - MIN_LOOKAHEAD;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int PENDING_BUF_SIZE = 1 << (DEFAULT_MEM_LEVEL + 8);

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public static int MAX_BLOCK_SIZE = Math.Min(65535, PENDING_BUF_SIZE - 5);

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int DEFLATE_STORED = 0;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int DEFLATE_FAST = 1;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public const int DEFLATE_SLOW = 2;

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public static int[] GOOD_LENGTH = { 0, 4, 4, 4, 4, 8, 8, 8, 32, 32 };

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public static int[] MAX_LAZY = { 0, 4, 5, 6, 4, 16, 16, 32, 128, 258 };

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public static int[] NICE_LENGTH = { 0, 8, 16, 32, 16, 32, 128, 128, 258, 258 };

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public static int[] MAX_CHAIN = { 0, 4, 8, 32, 16, 32, 128, 256, 1024, 4096 };

        /// <summary>
        /// Internal compression engine constant
        /// </summary>
        public static int[] COMPR_FUNC = { 0, 1, 1, 1, 1, 2, 2, 2, 2, 2 };
    }
    /// <summary>
	/// This is the DeflaterHuffman class.
	///
	/// This class is <i>not</i> thread safe.  This is inherent in the API, due
	/// to the split of Deflate and SetInput.
	///
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class DeflaterHuffman
    {
        private const int BUFSIZE = 1 << (DeflaterConstants.DEFAULT_MEM_LEVEL + 6);
        private const int LITERAL_NUM = 286;

        // Number of distance codes
        private const int DIST_NUM = 30;

        // Number of codes used to transfer bit lengths
        private const int BITLEN_NUM = 19;

        // repeat previous bit length 3-6 times (2 bits of repeat count)
        private const int REP_3_6 = 16;

        // repeat a zero length 3-10 times  (3 bits of repeat count)
        private const int REP_3_10 = 17;

        // repeat a zero length 11-138 times  (7 bits of repeat count)
        private const int REP_11_138 = 18;

        private const int EOF_SYMBOL = 256;

        // The lengths of the bit length codes are sent in order of decreasing
        // probability, to avoid transmitting the lengths for unused bit length codes.
        private static readonly int[] BL_ORDER = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        private static readonly byte[] bit4Reverse = {
            0,
            8,
            4,
            12,
            2,
            10,
            6,
            14,
            1,
            9,
            5,
            13,
            3,
            11,
            7,
            15
        };

        private static short[] staticLCodes;
        private static byte[] staticLLength;
        private static short[] staticDCodes;
        private static byte[] staticDLength;

        private class Tree
        {
            #region Instance Fields

            public short[] freqs;

            public byte[] length;

            public int minNumCodes;

            public int numCodes;

            private short[] codes;
            private readonly int[] bl_counts;
            private readonly int maxLength;
            private DeflaterHuffman dh;

            #endregion Instance Fields

            #region Constructors

            public Tree(DeflaterHuffman dh, int elems, int minCodes, int maxLength)
            {
                this.dh = dh;
                this.minNumCodes = minCodes;
                this.maxLength = maxLength;
                freqs = new short[elems];
                bl_counts = new int[maxLength];
            }

            #endregion Constructors

            /// <summary>
            /// Resets the internal state of the tree
            /// </summary>
            public void Reset()
            {
                for (int i = 0; i < freqs.Length; i++)
                {
                    freqs[i] = 0;
                }
                codes = null;
                length = null;
            }

            public void WriteSymbol(int code)
            {
                //				if (DeflaterConstants.DEBUGGING) {
                //					freqs[code]--;
                //					//  	  Console.Write("writeSymbol("+freqs.length+","+code+"): ");
                //				}
                dh.pending.WriteBits(codes[code] & 0xffff, length[code]);
            }

            /// <summary>
            /// Check that all frequencies are zero
            /// </summary>
            /// <exception cref="SharpZipBaseException">
            /// At least one frequency is non-zero
            /// </exception>
            public void CheckEmpty()
            {
                bool empty = true;
                for (int i = 0; i < freqs.Length; i++)
                {
                    empty &= freqs[i] == 0;
                }

                if (!empty)
                {
                    throw new SharpZipBaseException("!Empty");
                }
            }

            /// <summary>
            /// Set static codes and length
            /// </summary>
            /// <param name="staticCodes">new codes</param>
            /// <param name="staticLengths">length for new codes</param>
            public void SetStaticCodes(short[] staticCodes, byte[] staticLengths)
            {
                codes = staticCodes;
                length = staticLengths;
            }

            /// <summary>
            /// Build dynamic codes and lengths
            /// </summary>
            public void BuildCodes()
            {
                int numSymbols = freqs.Length;
                int[] nextCode = new int[maxLength];
                int code = 0;

                codes = new short[freqs.Length];

                //				if (DeflaterConstants.DEBUGGING) {
                //					//Console.WriteLine("buildCodes: "+freqs.Length);
                //				}

                for (int bits = 0; bits < maxLength; bits++)
                {
                    nextCode[bits] = code;
                    code += bl_counts[bits] << (15 - bits);

                    //					if (DeflaterConstants.DEBUGGING) {
                    //						//Console.WriteLine("bits: " + ( bits + 1) + " count: " + bl_counts[bits]
                    //						                  +" nextCode: "+code);
                    //					}
                }

#if DebugDeflation
				if ( DeflaterConstants.DEBUGGING && (code != 65536) )
				{
					throw new SharpZipBaseException("Inconsistent bl_counts!");
				}
#endif
                for (int i = 0; i < numCodes; i++)
                {
                    int bits = length[i];
                    if (bits > 0)
                    {
                        //						if (DeflaterConstants.DEBUGGING) {
                        //								//Console.WriteLine("codes["+i+"] = rev(" + nextCode[bits-1]+"),
                        //								                  +bits);
                        //						}

                        codes[i] = BitReverse(nextCode[bits - 1]);
                        nextCode[bits - 1] += 1 << (16 - bits);
                    }
                }
            }

            public void BuildTree()
            {
                int numSymbols = freqs.Length;

                /* heap is a priority queue, sorted by frequency, least frequent
				* nodes first.  The heap is a binary tree, with the property, that
				* the parent node is smaller than both child nodes.  This assures
				* that the smallest node is the first parent.
				*
				* The binary tree is encoded in an array:  0 is root node and
				* the nodes 2*n+1, 2*n+2 are the child nodes of node n.
				*/
                int[] heap = new int[numSymbols];
                int heapLen = 0;
                int maxCode = 0;
                for (int n = 0; n < numSymbols; n++)
                {
                    int freq = freqs[n];
                    if (freq != 0)
                    {
                        // Insert n into heap
                        int pos = heapLen++;
                        int ppos;
                        while (pos > 0 && freqs[heap[ppos = (pos - 1) / 2]] > freq)
                        {
                            heap[pos] = heap[ppos];
                            pos = ppos;
                        }
                        heap[pos] = n;

                        maxCode = n;
                    }
                }

                /* We could encode a single literal with 0 bits but then we
				* don't see the literals.  Therefore we force at least two
				* literals to avoid this case.  We don't care about order in
				* this case, both literals get a 1 bit code.
				*/
                while (heapLen < 2)
                {
                    int node = maxCode < 2 ? ++maxCode : 0;
                    heap[heapLen++] = node;
                }

                numCodes = Math.Max(maxCode + 1, minNumCodes);

                int numLeafs = heapLen;
                int[] childs = new int[4 * heapLen - 2];
                int[] values = new int[2 * heapLen - 1];
                int numNodes = numLeafs;
                for (int i = 0; i < heapLen; i++)
                {
                    int node = heap[i];
                    childs[2 * i] = node;
                    childs[2 * i + 1] = -1;
                    values[i] = freqs[node] << 8;
                    heap[i] = i;
                }

                /* Construct the Huffman tree by repeatedly combining the least two
				* frequent nodes.
				*/
                do
                {
                    int first = heap[0];
                    int last = heap[--heapLen];

                    // Propagate the hole to the leafs of the heap
                    int ppos = 0;
                    int path = 1;

                    while (path < heapLen)
                    {
                        if (path + 1 < heapLen && values[heap[path]] > values[heap[path + 1]])
                        {
                            path++;
                        }

                        heap[ppos] = heap[path];
                        ppos = path;
                        path = path * 2 + 1;
                    }

                    /* Now propagate the last element down along path.  Normally
					* it shouldn't go too deep.
					*/
                    int lastVal = values[last];
                    while ((path = ppos) > 0 && values[heap[ppos = (path - 1) / 2]] > lastVal)
                    {
                        heap[path] = heap[ppos];
                    }
                    heap[path] = last;

                    int second = heap[0];

                    // Create a new node father of first and second
                    last = numNodes++;
                    childs[2 * last] = first;
                    childs[2 * last + 1] = second;
                    int mindepth = Math.Min(values[first] & 0xff, values[second] & 0xff);
                    values[last] = lastVal = values[first] + values[second] - mindepth + 1;

                    // Again, propagate the hole to the leafs
                    ppos = 0;
                    path = 1;

                    while (path < heapLen)
                    {
                        if (path + 1 < heapLen && values[heap[path]] > values[heap[path + 1]])
                        {
                            path++;
                        }

                        heap[ppos] = heap[path];
                        ppos = path;
                        path = ppos * 2 + 1;
                    }

                    // Now propagate the new element down along path
                    while ((path = ppos) > 0 && values[heap[ppos = (path - 1) / 2]] > lastVal)
                    {
                        heap[path] = heap[ppos];
                    }
                    heap[path] = last;
                } while (heapLen > 1);

                if (heap[0] != childs.Length / 2 - 1)
                {
                    throw new SharpZipBaseException("Heap invariant violated");
                }

                BuildLength(childs);
            }

            /// <summary>
            /// Get encoded length
            /// </summary>
            /// <returns>Encoded length, the sum of frequencies * lengths</returns>
            public int GetEncodedLength()
            {
                int len = 0;
                for (int i = 0; i < freqs.Length; i++)
                {
                    len += freqs[i] * length[i];
                }
                return len;
            }

            /// <summary>
            /// Scan a literal or distance tree to determine the frequencies of the codes
            /// in the bit length tree.
            /// </summary>
            public void CalcBLFreq(Tree blTree)
            {
                int max_count;               /* max repeat count */
                int min_count;               /* min repeat count */
                int count;                   /* repeat count of the current code */
                int curlen = -1;             /* length of current code */

                int i = 0;
                while (i < numCodes)
                {
                    count = 1;
                    int nextlen = length[i];
                    if (nextlen == 0)
                    {
                        max_count = 138;
                        min_count = 3;
                    }
                    else
                    {
                        max_count = 6;
                        min_count = 3;
                        if (curlen != nextlen)
                        {
                            blTree.freqs[nextlen]++;
                            count = 0;
                        }
                    }
                    curlen = nextlen;
                    i++;

                    while (i < numCodes && curlen == length[i])
                    {
                        i++;
                        if (++count >= max_count)
                        {
                            break;
                        }
                    }

                    if (count < min_count)
                    {
                        blTree.freqs[curlen] += (short)count;
                    }
                    else if (curlen != 0)
                    {
                        blTree.freqs[REP_3_6]++;
                    }
                    else if (count <= 10)
                    {
                        blTree.freqs[REP_3_10]++;
                    }
                    else
                    {
                        blTree.freqs[REP_11_138]++;
                    }
                }
            }

            /// <summary>
            /// Write tree values
            /// </summary>
            /// <param name="blTree">Tree to write</param>
            public void WriteTree(Tree blTree)
            {
                int max_count;               // max repeat count
                int min_count;               // min repeat count
                int count;                   // repeat count of the current code
                int curlen = -1;             // length of current code

                int i = 0;
                while (i < numCodes)
                {
                    count = 1;
                    int nextlen = length[i];
                    if (nextlen == 0)
                    {
                        max_count = 138;
                        min_count = 3;
                    }
                    else
                    {
                        max_count = 6;
                        min_count = 3;
                        if (curlen != nextlen)
                        {
                            blTree.WriteSymbol(nextlen);
                            count = 0;
                        }
                    }
                    curlen = nextlen;
                    i++;

                    while (i < numCodes && curlen == length[i])
                    {
                        i++;
                        if (++count >= max_count)
                        {
                            break;
                        }
                    }

                    if (count < min_count)
                    {
                        while (count-- > 0)
                        {
                            blTree.WriteSymbol(curlen);
                        }
                    }
                    else if (curlen != 0)
                    {
                        blTree.WriteSymbol(REP_3_6);
                        dh.pending.WriteBits(count - 3, 2);
                    }
                    else if (count <= 10)
                    {
                        blTree.WriteSymbol(REP_3_10);
                        dh.pending.WriteBits(count - 3, 3);
                    }
                    else
                    {
                        blTree.WriteSymbol(REP_11_138);
                        dh.pending.WriteBits(count - 11, 7);
                    }
                }
            }

            private void BuildLength(int[] childs)
            {
                this.length = new byte[freqs.Length];
                int numNodes = childs.Length / 2;
                int numLeafs = (numNodes + 1) / 2;
                int overflow = 0;

                for (int i = 0; i < maxLength; i++)
                {
                    bl_counts[i] = 0;
                }

                // First calculate optimal bit lengths
                int[] lengths = new int[numNodes];
                lengths[numNodes - 1] = 0;

                for (int i = numNodes - 1; i >= 0; i--)
                {
                    if (childs[2 * i + 1] != -1)
                    {
                        int bitLength = lengths[i] + 1;
                        if (bitLength > maxLength)
                        {
                            bitLength = maxLength;
                            overflow++;
                        }
                        lengths[childs[2 * i]] = lengths[childs[2 * i + 1]] = bitLength;
                    }
                    else
                    {
                        // A leaf node
                        int bitLength = lengths[i];
                        bl_counts[bitLength - 1]++;
                        this.length[childs[2 * i]] = (byte)lengths[i];
                    }
                }

                //				if (DeflaterConstants.DEBUGGING) {
                //					//Console.WriteLine("Tree "+freqs.Length+" lengths:");
                //					for (int i=0; i < numLeafs; i++) {
                //						//Console.WriteLine("Node "+childs[2*i]+" freq: "+freqs[childs[2*i]]
                //						                  + " len: "+length[childs[2*i]]);
                //					}
                //				}

                if (overflow == 0)
                {
                    return;
                }

                int incrBitLen = maxLength - 1;
                do
                {
                    // Find the first bit length which could increase:
                    while (bl_counts[--incrBitLen] == 0)
                    {
                    }

                    // Move this node one down and remove a corresponding
                    // number of overflow nodes.
                    do
                    {
                        bl_counts[incrBitLen]--;
                        bl_counts[++incrBitLen]++;
                        overflow -= 1 << (maxLength - 1 - incrBitLen);
                    } while (overflow > 0 && incrBitLen < maxLength - 1);
                } while (overflow > 0);

                /* We may have overshot above.  Move some nodes from maxLength to
				* maxLength-1 in that case.
				*/
                bl_counts[maxLength - 1] += overflow;
                bl_counts[maxLength - 2] -= overflow;

                /* Now recompute all bit lengths, scanning in increasing
				* frequency.  It is simpler to reconstruct all lengths instead of
				* fixing only the wrong ones. This idea is taken from 'ar'
				* written by Haruhiko Okumura.
				*
				* The nodes were inserted with decreasing frequency into the childs
				* array.
				*/
                int nodePtr = 2 * numLeafs;
                for (int bits = maxLength; bits != 0; bits--)
                {
                    int n = bl_counts[bits - 1];
                    while (n > 0)
                    {
                        int childPtr = 2 * childs[nodePtr++];
                        if (childs[childPtr + 1] == -1)
                        {
                            // We found another leaf
                            length[childs[childPtr]] = (byte)bits;
                            n--;
                        }
                    }
                }
                //				if (DeflaterConstants.DEBUGGING) {
                //					//Console.WriteLine("*** After overflow elimination. ***");
                //					for (int i=0; i < numLeafs; i++) {
                //						//Console.WriteLine("Node "+childs[2*i]+" freq: "+freqs[childs[2*i]]
                //						                  + " len: "+length[childs[2*i]]);
                //					}
                //				}
            }
        }

        #region Instance Fields

        /// <summary>
        /// Pending buffer to use
        /// </summary>
        public DeflaterPending pending;

        private Tree literalTree;
        private Tree distTree;
        private Tree blTree;

        // Buffer for distances
        private short[] d_buf;

        private byte[] l_buf;
        private int last_lit;
        private int extra_bits;

        #endregion Instance Fields

        static DeflaterHuffman()
        {
            // See RFC 1951 3.2.6
            // Literal codes
            staticLCodes = new short[LITERAL_NUM];
            staticLLength = new byte[LITERAL_NUM];

            int i = 0;
            while (i < 144)
            {
                staticLCodes[i] = BitReverse((0x030 + i) << 8);
                staticLLength[i++] = 8;
            }

            while (i < 256)
            {
                staticLCodes[i] = BitReverse((0x190 - 144 + i) << 7);
                staticLLength[i++] = 9;
            }

            while (i < 280)
            {
                staticLCodes[i] = BitReverse((0x000 - 256 + i) << 9);
                staticLLength[i++] = 7;
            }

            while (i < LITERAL_NUM)
            {
                staticLCodes[i] = BitReverse((0x0c0 - 280 + i) << 8);
                staticLLength[i++] = 8;
            }

            // Distance codes
            staticDCodes = new short[DIST_NUM];
            staticDLength = new byte[DIST_NUM];
            for (i = 0; i < DIST_NUM; i++)
            {
                staticDCodes[i] = BitReverse(i << 11);
                staticDLength[i] = 5;
            }
        }

        /// <summary>
        /// Construct instance with pending buffer
        /// </summary>
        /// <param name="pending">Pending buffer to use</param>
        public DeflaterHuffman(DeflaterPending pending)
        {
            this.pending = pending;

            literalTree = new Tree(this, LITERAL_NUM, 257, 15);
            distTree = new Tree(this, DIST_NUM, 1, 15);
            blTree = new Tree(this, BITLEN_NUM, 4, 7);

            d_buf = new short[BUFSIZE];
            l_buf = new byte[BUFSIZE];
        }

        /// <summary>
        /// Reset internal state
        /// </summary>
        public void Reset()
        {
            last_lit = 0;
            extra_bits = 0;
            literalTree.Reset();
            distTree.Reset();
            blTree.Reset();
        }

        /// <summary>
        /// Write all trees to pending buffer
        /// </summary>
        /// <param name="blTreeCodes">The number/rank of treecodes to send.</param>
        public void SendAllTrees(int blTreeCodes)
        {
            blTree.BuildCodes();
            literalTree.BuildCodes();
            distTree.BuildCodes();
            pending.WriteBits(literalTree.numCodes - 257, 5);
            pending.WriteBits(distTree.numCodes - 1, 5);
            pending.WriteBits(blTreeCodes - 4, 4);
            for (int rank = 0; rank < blTreeCodes; rank++)
            {
                pending.WriteBits(blTree.length[BL_ORDER[rank]], 3);
            }
            literalTree.WriteTree(blTree);
            distTree.WriteTree(blTree);

#if DebugDeflation
			if (DeflaterConstants.DEBUGGING) {
				blTree.CheckEmpty();
			}
#endif
        }

        /// <summary>
        /// Compress current buffer writing data to pending buffer
        /// </summary>
        public void CompressBlock()
        {
            for (int i = 0; i < last_lit; i++)
            {
                int litlen = l_buf[i] & 0xff;
                int dist = d_buf[i];
                if (dist-- != 0)
                {
                    //					if (DeflaterConstants.DEBUGGING) {
                    //						Console.Write("["+(dist+1)+","+(litlen+3)+"]: ");
                    //					}

                    int lc = Lcode(litlen);
                    literalTree.WriteSymbol(lc);

                    int bits = (lc - 261) / 4;
                    if (bits > 0 && bits <= 5)
                    {
                        pending.WriteBits(litlen & ((1 << bits) - 1), bits);
                    }

                    int dc = Dcode(dist);
                    distTree.WriteSymbol(dc);

                    bits = dc / 2 - 1;
                    if (bits > 0)
                    {
                        pending.WriteBits(dist & ((1 << bits) - 1), bits);
                    }
                }
                else
                {
                    //					if (DeflaterConstants.DEBUGGING) {
                    //						if (litlen > 32 && litlen < 127) {
                    //							Console.Write("("+(char)litlen+"): ");
                    //						} else {
                    //							Console.Write("{"+litlen+"}: ");
                    //						}
                    //					}
                    literalTree.WriteSymbol(litlen);
                }
            }

#if DebugDeflation
			if (DeflaterConstants.DEBUGGING) {
				Console.Write("EOF: ");
			}
#endif
            literalTree.WriteSymbol(EOF_SYMBOL);

#if DebugDeflation
			if (DeflaterConstants.DEBUGGING) {
				literalTree.CheckEmpty();
				distTree.CheckEmpty();
			}
#endif
        }

        /// <summary>
        /// Flush block to output with no compression
        /// </summary>
        /// <param name="stored">Data to write</param>
        /// <param name="storedOffset">Index of first byte to write</param>
        /// <param name="storedLength">Count of bytes to write</param>
        /// <param name="lastBlock">True if this is the last block</param>
        public void FlushStoredBlock(byte[] stored, int storedOffset, int storedLength, bool lastBlock)
        {
#if DebugDeflation
			//			if (DeflaterConstants.DEBUGGING) {
			//				//Console.WriteLine("Flushing stored block "+ storedLength);
			//			}
#endif
            pending.WriteBits((DeflaterConstants.STORED_BLOCK << 1) + (lastBlock ? 1 : 0), 3);
            pending.AlignToByte();
            pending.WriteShort(storedLength);
            pending.WriteShort(~storedLength);
            pending.WriteBlock(stored, storedOffset, storedLength);
            Reset();
        }

        /// <summary>
        /// Flush block to output with compression
        /// </summary>
        /// <param name="stored">Data to flush</param>
        /// <param name="storedOffset">Index of first byte to flush</param>
        /// <param name="storedLength">Count of bytes to flush</param>
        /// <param name="lastBlock">True if this is the last block</param>
        public void FlushBlock(byte[] stored, int storedOffset, int storedLength, bool lastBlock)
        {
            literalTree.freqs[EOF_SYMBOL]++;

            // Build trees
            literalTree.BuildTree();
            distTree.BuildTree();

            // Calculate bitlen frequency
            literalTree.CalcBLFreq(blTree);
            distTree.CalcBLFreq(blTree);

            // Build bitlen tree
            blTree.BuildTree();

            int blTreeCodes = 4;
            for (int i = 18; i > blTreeCodes; i--)
            {
                if (blTree.length[BL_ORDER[i]] > 0)
                {
                    blTreeCodes = i + 1;
                }
            }
            int opt_len = 14 + blTreeCodes * 3 + blTree.GetEncodedLength() +
                literalTree.GetEncodedLength() + distTree.GetEncodedLength() +
                extra_bits;

            int static_len = extra_bits;
            for (int i = 0; i < LITERAL_NUM; i++)
            {
                static_len += literalTree.freqs[i] * staticLLength[i];
            }
            for (int i = 0; i < DIST_NUM; i++)
            {
                static_len += distTree.freqs[i] * staticDLength[i];
            }
            if (opt_len >= static_len)
            {
                // Force static trees
                opt_len = static_len;
            }

            if (storedOffset >= 0 && storedLength + 4 < opt_len >> 3)
            {
                // Store Block

                //				if (DeflaterConstants.DEBUGGING) {
                //					//Console.WriteLine("Storing, since " + storedLength + " < " + opt_len
                //					                  + " <= " + static_len);
                //				}
                FlushStoredBlock(stored, storedOffset, storedLength, lastBlock);
            }
            else if (opt_len == static_len)
            {
                // Encode with static tree
                pending.WriteBits((DeflaterConstants.STATIC_TREES << 1) + (lastBlock ? 1 : 0), 3);
                literalTree.SetStaticCodes(staticLCodes, staticLLength);
                distTree.SetStaticCodes(staticDCodes, staticDLength);
                CompressBlock();
                Reset();
            }
            else
            {
                // Encode with dynamic tree
                pending.WriteBits((DeflaterConstants.DYN_TREES << 1) + (lastBlock ? 1 : 0), 3);
                SendAllTrees(blTreeCodes);
                CompressBlock();
                Reset();
            }
        }

        /// <summary>
        /// Get value indicating if internal buffer is full
        /// </summary>
        /// <returns>true if buffer is full</returns>
        public bool IsFull()
        {
            return last_lit >= BUFSIZE;
        }

        /// <summary>
        /// Add literal to buffer
        /// </summary>
        /// <param name="literal">Literal value to add to buffer.</param>
        /// <returns>Value indicating internal buffer is full</returns>
        public bool TallyLit(int literal)
        {
            //			if (DeflaterConstants.DEBUGGING) {
            //				if (lit > 32 && lit < 127) {
            //					//Console.WriteLine("("+(char)lit+")");
            //				} else {
            //					//Console.WriteLine("{"+lit+"}");
            //				}
            //			}
            d_buf[last_lit] = 0;
            l_buf[last_lit++] = (byte)literal;
            literalTree.freqs[literal]++;
            return IsFull();
        }

        /// <summary>
        /// Add distance code and length to literal and distance trees
        /// </summary>
        /// <param name="distance">Distance code</param>
        /// <param name="length">Length</param>
        /// <returns>Value indicating if internal buffer is full</returns>
        public bool TallyDist(int distance, int length)
        {
            //			if (DeflaterConstants.DEBUGGING) {
            //				//Console.WriteLine("[" + distance + "," + length + "]");
            //			}

            d_buf[last_lit] = (short)distance;
            l_buf[last_lit++] = (byte)(length - 3);

            int lc = Lcode(length - 3);
            literalTree.freqs[lc]++;
            if (lc >= 265 && lc < 285)
            {
                extra_bits += (lc - 261) / 4;
            }

            int dc = Dcode(distance - 1);
            distTree.freqs[dc]++;
            if (dc >= 4)
            {
                extra_bits += dc / 2 - 1;
            }
            return IsFull();
        }

        /// <summary>
        /// Reverse the bits of a 16 bit value.
        /// </summary>
        /// <param name="toReverse">Value to reverse bits</param>
        /// <returns>Value with bits reversed</returns>
        public static short BitReverse(int toReverse)
        {
            return (short)(bit4Reverse[toReverse & 0xF] << 12 |
                            bit4Reverse[(toReverse >> 4) & 0xF] << 8 |
                            bit4Reverse[(toReverse >> 8) & 0xF] << 4 |
                            bit4Reverse[toReverse >> 12]);
        }

        private static int Lcode(int length)
        {
            if (length == 255)
            {
                return 285;
            }

            int code = 257;
            while (length >= 8)
            {
                code += 4;
                length >>= 1;
            }
            return code + length;
        }

        private static int Dcode(int distance)
        {
            int code = 0;
            while (distance >= 4)
            {
                code += 2;
                distance >>= 1;
            }
            return code + distance;
        }
    }

    /// <summary>
	/// Computes Adler32 checksum for a stream of data. An Adler32
	/// checksum is not as reliable as a CRC32 checksum, but a lot faster to
	/// compute.
	///
	/// The specification for Adler32 may be found in RFC 1950.
	/// ZLIB Compressed Data Format Specification version 3.3)
	///
	///
	/// From that document:
	///
	///      "ADLER32 (Adler-32 checksum)
	///       This contains a checksum value of the uncompressed data
	///       (excluding any dictionary data) computed according to Adler-32
	///       algorithm. This algorithm is a 32-bit extension and improvement
	///       of the Fletcher algorithm, used in the ITU-T X.224 / ISO 8073
	///       standard.
	///
	///       Adler-32 is composed of two sums accumulated per byte: s1 is
	///       the sum of all bytes, s2 is the sum of all s1 values. Both sums
	///       are done modulo 65521. s1 is initialized to 1, s2 to zero.  The
	///       Adler-32 checksum is stored as s2*65536 + s1 in most-
	///       significant-byte first (network) order."
	///
	///  "8.2. The Adler-32 algorithm
	///
	///    The Adler-32 algorithm is much faster than the CRC32 algorithm yet
	///    still provides an extremely low probability of undetected errors.
	///
	///    The modulo on unsigned long accumulators can be delayed for 5552
	///    bytes, so the modulo operation time is negligible.  If the bytes
	///    are a, b, c, the second sum is 3a + 2b + c + 3, and so is position
	///    and order sensitive, unlike the first sum, which is just a
	///    checksum.  That 65521 is prime is important to avoid a possible
	///    large class of two-byte errors that leave the check unchanged.
	///    (The Fletcher checksum uses 255, which is not prime and which also
	///    makes the Fletcher check insensitive to single byte changes 0 -
	///    255.)
	///
	///    The sum s1 is initialized to 1 instead of zero to make the length
	///    of the sequence part of s2, so that the length does not have to be
	///    checked separately. (Any sequence of zeroes has a Fletcher
	///    checksum of zero.)"
	/// </summary>
	/// <see cref="ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream"/>
	/// <see cref="ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream"/>
	public sealed class Adler32 : IChecksum
    {
        #region Instance Fields

        /// <summary>
        /// largest prime smaller than 65536
        /// </summary>
        private static readonly uint BASE = 65521;

        /// <summary>
        /// The CRC data checksum so far.
        /// </summary>
        private uint checkValue;

        #endregion Instance Fields

        /// <summary>
        /// Initialise a default instance of <see cref="Adler32"></see>
        /// </summary>
        public Adler32()
        {
            Reset();
        }

        /// <summary>
        /// Resets the Adler32 data checksum as if no update was ever called.
        /// </summary>
        public void Reset()
        {
            checkValue = 1;
        }

        /// <summary>
        /// Returns the Adler32 data checksum computed so far.
        /// </summary>
        public long Value
        {
            get
            {
                return checkValue;
            }
        }

        /// <summary>
        /// Updates the checksum with the byte b.
        /// </summary>
        /// <param name="bval">
        /// The data value to add. The high byte of the int is ignored.
        /// </param>
        public void Update(int bval)
        {
            // We could make a length 1 byte array and call update again, but I
            // would rather not have that overhead
            uint s1 = checkValue & 0xFFFF;
            uint s2 = checkValue >> 16;

            s1 = (s1 + ((uint)bval & 0xFF)) % BASE;
            s2 = (s1 + s2) % BASE;

            checkValue = (s2 << 16) + s1;
        }

        /// <summary>
        /// Updates the Adler32 data checksum with the bytes taken from
        /// a block of data.
        /// </summary>
        /// <param name="buffer">Contains the data to update the checksum with.</param>
        public void Update(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Update(new ArraySegment<byte>(buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Update Adler32 data checksum based on a portion of a block of data
        /// </summary>
        /// <param name = "segment">
        /// The chunk of data to add
        /// </param>
        public void Update(ArraySegment<byte> segment)
        {
            //(By Per Bothner)
            uint s1 = checkValue & 0xFFFF;
            uint s2 = checkValue >> 16;
            var count = segment.Count;
            var offset = segment.Offset;
            while (count > 0)
            {
                // We can defer the modulo operation:
                // s1 maximally grows from 65521 to 65521 + 255 * 3800
                // s2 maximally grows by 3800 * median(s1) = 2090079800 < 2^31
                int n = 3800;
                if (n > count)
                {
                    n = count;
                }
                count -= n;
                while (--n >= 0)
                {
                    s1 = s1 + (uint)(segment.Array[offset++] & 0xff);
                    s2 = s2 + s1;
                }
                s1 %= BASE;
                s2 %= BASE;
            }
            checkValue = (s2 << 16) | s1;
        }
    }
    /// <summary>
	/// Huffman tree used for inflation
	/// </summary>
	public class InflaterHuffmanTree
    {
        #region Constants

        private const int MAX_BITLEN = 15;

        #endregion Constants

        #region Instance Fields

        private short[] tree;

        #endregion Instance Fields

        /// <summary>
        /// Literal length tree
        /// </summary>
        public static InflaterHuffmanTree defLitLenTree;

        /// <summary>
        /// Distance tree
        /// </summary>
        public static InflaterHuffmanTree defDistTree;

        static InflaterHuffmanTree()
        {
            try
            {
                byte[] codeLengths = new byte[288];
                int i = 0;
                while (i < 144)
                {
                    codeLengths[i++] = 8;
                }
                while (i < 256)
                {
                    codeLengths[i++] = 9;
                }
                while (i < 280)
                {
                    codeLengths[i++] = 7;
                }
                while (i < 288)
                {
                    codeLengths[i++] = 8;
                }
                defLitLenTree = new InflaterHuffmanTree(codeLengths);

                codeLengths = new byte[32];
                i = 0;
                while (i < 32)
                {
                    codeLengths[i++] = 5;
                }
                defDistTree = new InflaterHuffmanTree(codeLengths);
            }
            catch (Exception)
            {
                throw new SharpZipBaseException("InflaterHuffmanTree: static tree length illegal");
            }
        }

        #region Constructors

        /// <summary>
        /// Constructs a Huffman tree from the array of code lengths.
        /// </summary>
        /// <param name = "codeLengths">
        /// the array of code lengths
        /// </param>
        public InflaterHuffmanTree(IList<byte> codeLengths)
        {
            BuildTree(codeLengths);
        }

        #endregion Constructors

        private void BuildTree(IList<byte> codeLengths)
        {
            int[] blCount = new int[MAX_BITLEN + 1];
            int[] nextCode = new int[MAX_BITLEN + 1];

            for (int i = 0; i < codeLengths.Count; i++)
            {
                int bits = codeLengths[i];
                if (bits > 0)
                {
                    blCount[bits]++;
                }
            }

            int code = 0;
            int treeSize = 512;
            for (int bits = 1; bits <= MAX_BITLEN; bits++)
            {
                nextCode[bits] = code;
                code += blCount[bits] << (16 - bits);
                if (bits >= 10)
                {
                    /* We need an extra table for bit lengths >= 10. */
                    int start = nextCode[bits] & 0x1ff80;
                    int end = code & 0x1ff80;
                    treeSize += (end - start) >> (16 - bits);
                }
            }

            /* -jr comment this out! doesnt work for dynamic trees and pkzip 2.04g
						if (code != 65536)
						{
							throw new SharpZipBaseException("Code lengths don't add up properly.");
						}
			*/
            /* Now create and fill the extra tables from longest to shortest
			* bit len.  This way the sub trees will be aligned.
			*/
            tree = new short[treeSize];
            int treePtr = 512;
            for (int bits = MAX_BITLEN; bits >= 10; bits--)
            {
                int end = code & 0x1ff80;
                code -= blCount[bits] << (16 - bits);
                int start = code & 0x1ff80;
                for (int i = start; i < end; i += 1 << 7)
                {
                    tree[DeflaterHuffman.BitReverse(i)] = (short)((-treePtr << 4) | bits);
                    treePtr += 1 << (bits - 9);
                }
            }

            for (int i = 0; i < codeLengths.Count; i++)
            {
                int bits = codeLengths[i];
                if (bits == 0)
                {
                    continue;
                }
                code = nextCode[bits];
                int revcode = DeflaterHuffman.BitReverse(code);
                if (bits <= 9)
                {
                    do
                    {
                        tree[revcode] = (short)((i << 4) | bits);
                        revcode += 1 << bits;
                    } while (revcode < 512);
                }
                else
                {
                    int subTree = tree[revcode & 511];
                    int treeLen = 1 << (subTree & 15);
                    subTree = -(subTree >> 4);
                    do
                    {
                        tree[subTree | (revcode >> 9)] = (short)((i << 4) | bits);
                        revcode += 1 << bits;
                    } while (revcode < treeLen);
                }
                nextCode[bits] = code + (1 << (16 - bits));
            }
        }

        /// <summary>
        /// Reads the next symbol from input.  The symbol is encoded using the
        /// huffman tree.
        /// </summary>
        /// <param name="input">
        /// input the input source.
        /// </param>
        /// <returns>
        /// the next symbol, or -1 if not enough input is available.
        /// </returns>
        public int GetSymbol(StreamManipulator input)
        {
            int lookahead, symbol;
            if ((lookahead = input.PeekBits(9)) >= 0)
            {
                symbol = tree[lookahead];
                int bitlen = symbol & 15;

                if (symbol >= 0)
                {
                    if (bitlen == 0)
                    {
                        throw new SharpZipBaseException("Encountered invalid codelength 0");
                    }
                    input.DropBits(bitlen);
                    return symbol >> 4;
                }
                int subtree = -(symbol >> 4);
                if ((lookahead = input.PeekBits(bitlen)) >= 0)
                {
                    symbol = tree[subtree | (lookahead >> 9)];
                    input.DropBits(symbol & 15);
                    return symbol >> 4;
                }
                else
                {
                    int bits = input.AvailableBits;
                    lookahead = input.PeekBits(bits);
                    symbol = tree[subtree | (lookahead >> 9)];
                    if ((symbol & 15) <= bits)
                    {
                        input.DropBits(symbol & 15);
                        return symbol >> 4;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            else // Less than 9 bits
            {
                int bits = input.AvailableBits;
                lookahead = input.PeekBits(bits);
                symbol = tree[lookahead];
                if (symbol >= 0 && (symbol & 15) <= bits)
                {
                    input.DropBits(symbol & 15);
                    return symbol >> 4;
                }
                else
                {
                    return -1;
                }
            }
        }
    }
    internal class InflaterDynHeader
    {
        #region Constants

        // maximum number of literal/length codes
        private const int LITLEN_MAX = 286;

        // maximum number of distance codes
        private const int DIST_MAX = 30;

        // maximum data code lengths to read
        private const int CODELEN_MAX = LITLEN_MAX + DIST_MAX;

        // maximum meta code length codes to read
        private const int META_MAX = 19;

        private static readonly int[] MetaCodeLengthIndex =
            { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        #endregion Constants

        /// <summary>
        /// Continue decoding header from <see cref="input"/> until more bits are needed or decoding has been completed
        /// </summary>
        /// <returns>Returns whether decoding could be completed</returns>
        public bool AttemptRead()
            => !state.MoveNext() || state.Current;

        public InflaterDynHeader(StreamManipulator input)
        {
            this.input = input;
            stateMachine = CreateStateMachine();
            state = stateMachine.GetEnumerator();
        }

        private IEnumerable<bool> CreateStateMachine()
        {
            // Read initial code length counts from header
            while (!input.TryGetBits(5, ref litLenCodeCount, 257)) yield return false;
            while (!input.TryGetBits(5, ref distanceCodeCount, 1)) yield return false;
            while (!input.TryGetBits(4, ref metaCodeCount, 4)) yield return false;
            var dataCodeCount = litLenCodeCount + distanceCodeCount;

            if (litLenCodeCount > LITLEN_MAX) throw new ValueOutOfRangeException(nameof(litLenCodeCount));
            if (distanceCodeCount > DIST_MAX) throw new ValueOutOfRangeException(nameof(distanceCodeCount));
            if (metaCodeCount > META_MAX) throw new ValueOutOfRangeException(nameof(metaCodeCount));

            // Load code lengths for the meta tree from the header bits
            for (int i = 0; i < metaCodeCount; i++)
            {
                while (!input.TryGetBits(3, ref codeLengths, MetaCodeLengthIndex[i])) yield return false;
            }

            var metaCodeTree = new InflaterHuffmanTree(codeLengths);

            // Decompress the meta tree symbols into the data table code lengths
            int index = 0;
            while (index < dataCodeCount)
            {
                byte codeLength;
                int symbol;

                while ((symbol = metaCodeTree.GetSymbol(input)) < 0) yield return false;

                if (symbol < 16)
                {
                    // append literal code length
                    codeLengths[index++] = (byte)symbol;
                }
                else
                {
                    int repeatCount = 0;

                    if (symbol == 16) // Repeat last code length 3..6 times
                    {
                        if (index == 0)
                            throw new StreamDecodingException("Cannot repeat previous code length when no other code length has been read");

                        codeLength = codeLengths[index - 1];

                        // 2 bits + 3, [3..6]
                        while (!input.TryGetBits(2, ref repeatCount, 3)) yield return false;
                    }
                    else if (symbol == 17) // Repeat zero 3..10 times
                    {
                        codeLength = 0;

                        // 3 bits + 3, [3..10]
                        while (!input.TryGetBits(3, ref repeatCount, 3)) yield return false;
                    }
                    else // (symbol == 18), Repeat zero 11..138 times
                    {
                        codeLength = 0;

                        // 7 bits + 11, [11..138]
                        while (!input.TryGetBits(7, ref repeatCount, 11)) yield return false;
                    }

                    if (index + repeatCount > dataCodeCount)
                        throw new StreamDecodingException("Cannot repeat code lengths past total number of data code lengths");

                    while (repeatCount-- > 0)
                        codeLengths[index++] = codeLength;
                }
            }

            if (codeLengths[256] == 0)
                throw new StreamDecodingException("Inflater dynamic header end-of-block code missing");

            litLenTree = new InflaterHuffmanTree(new ArraySegment<byte>(codeLengths, 0, litLenCodeCount));
            distTree = new InflaterHuffmanTree(new ArraySegment<byte>(codeLengths, litLenCodeCount, distanceCodeCount));

            yield return true;
        }

        /// <summary>
        /// Get literal/length huffman tree, must not be used before <see cref="AttemptRead"/> has returned true
        /// </summary>
        /// <exception cref="StreamDecodingException">If hader has not been successfully read by the state machine</exception>
        public InflaterHuffmanTree LiteralLengthTree
            => litLenTree ?? throw new StreamDecodingException("Header properties were accessed before header had been successfully read");

        /// <summary>
        /// Get distance huffman tree, must not be used before <see cref="AttemptRead"/> has returned true
        /// </summary>
        /// <exception cref="StreamDecodingException">If hader has not been successfully read by the state machine</exception>
        public InflaterHuffmanTree DistanceTree
            => distTree ?? throw new StreamDecodingException("Header properties were accessed before header had been successfully read");

        #region Instance Fields

        private readonly StreamManipulator input;
        private readonly IEnumerator<bool> state;
        private readonly IEnumerable<bool> stateMachine;

        private byte[] codeLengths = new byte[CODELEN_MAX];

        private InflaterHuffmanTree litLenTree;
        private InflaterHuffmanTree distTree;

        private int litLenCodeCount, distanceCodeCount, metaCodeCount;

        #endregion Instance Fields
    }
    /// <summary>
	/// Contains the output from the Inflation process.
	/// We need to have a window so that we can refer backwards into the output stream
	/// to repeat stuff.<br/>
	/// Author of the original java version : John Leuner
	/// </summary>
	public class OutputWindow
    {
        #region Constants

        private const int WindowSize = 1 << 15;
        private const int WindowMask = WindowSize - 1;

        #endregion Constants

        #region Instance Fields

        private byte[] window = new byte[WindowSize]; //The window is 2^15 bytes
        private int windowEnd;
        private int windowFilled;

        #endregion Instance Fields

        /// <summary>
        /// Write a byte to this output window
        /// </summary>
        /// <param name="value">value to write</param>
        /// <exception cref="InvalidOperationException">
        /// if window is full
        /// </exception>
        public void Write(int value)
        {
            if (windowFilled++ == WindowSize)
            {
                throw new InvalidOperationException("Window full");
            }
            window[windowEnd++] = (byte)value;
            windowEnd &= WindowMask;
        }

        private void SlowRepeat(int repStart, int length, int distance)
        {
            while (length-- > 0)
            {
                window[windowEnd++] = window[repStart++];
                windowEnd &= WindowMask;
                repStart &= WindowMask;
            }
        }

        /// <summary>
        /// Append a byte pattern already in the window itself
        /// </summary>
        /// <param name="length">length of pattern to copy</param>
        /// <param name="distance">distance from end of window pattern occurs</param>
        /// <exception cref="InvalidOperationException">
        /// If the repeated data overflows the window
        /// </exception>
        public void Repeat(int length, int distance)
        {
            if ((windowFilled += length) > WindowSize)
            {
                throw new InvalidOperationException("Window full");
            }

            int repStart = (windowEnd - distance) & WindowMask;
            int border = WindowSize - length;
            if ((repStart <= border) && (windowEnd < border))
            {
                if (length <= distance)
                {
                    System.Array.Copy(window, repStart, window, windowEnd, length);
                    windowEnd += length;
                }
                else
                {
                    // We have to copy manually, since the repeat pattern overlaps.
                    while (length-- > 0)
                    {
                        window[windowEnd++] = window[repStart++];
                    }
                }
            }
            else
            {
                SlowRepeat(repStart, length, distance);
            }
        }

        /// <summary>
        /// Copy from input manipulator to internal window
        /// </summary>
        /// <param name="input">source of data</param>
        /// <param name="length">length of data to copy</param>
        /// <returns>the number of bytes copied</returns>
        public int CopyStored(StreamManipulator input, int length)
        {
            length = Math.Min(Math.Min(length, WindowSize - windowFilled), input.AvailableBytes);
            int copied;

            int tailLen = WindowSize - windowEnd;
            if (length > tailLen)
            {
                copied = input.CopyBytes(window, windowEnd, tailLen);
                if (copied == tailLen)
                {
                    copied += input.CopyBytes(window, 0, length - tailLen);
                }
            }
            else
            {
                copied = input.CopyBytes(window, windowEnd, length);
            }

            windowEnd = (windowEnd + copied) & WindowMask;
            windowFilled += copied;
            return copied;
        }

        /// <summary>
        /// Copy dictionary to window
        /// </summary>
        /// <param name="dictionary">source dictionary</param>
        /// <param name="offset">offset of start in source dictionary</param>
        /// <param name="length">length of dictionary</param>
        /// <exception cref="InvalidOperationException">
        /// If window isnt empty
        /// </exception>
        public void CopyDict(byte[] dictionary, int offset, int length)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (windowFilled > 0)
            {
                throw new InvalidOperationException();
            }

            if (length > WindowSize)
            {
                offset += length - WindowSize;
                length = WindowSize;
            }
            System.Array.Copy(dictionary, offset, window, 0, length);
            windowEnd = length & WindowMask;
        }

        /// <summary>
        /// Get remaining unfilled space in window
        /// </summary>
        /// <returns>Number of bytes left in window</returns>
        public int GetFreeSpace()
        {
            return WindowSize - windowFilled;
        }

        /// <summary>
        /// Get bytes available for output in window
        /// </summary>
        /// <returns>Number of bytes filled</returns>
        public int GetAvailable()
        {
            return windowFilled;
        }

        /// <summary>
        /// Copy contents of window to output
        /// </summary>
        /// <param name="output">buffer to copy to</param>
        /// <param name="offset">offset to start at</param>
        /// <param name="len">number of bytes to count</param>
        /// <returns>The number of bytes copied</returns>
        /// <exception cref="InvalidOperationException">
        /// If a window underflow occurs
        /// </exception>
        public int CopyOutput(byte[] output, int offset, int len)
        {
            int copyEnd = windowEnd;
            if (len > windowFilled)
            {
                len = windowFilled;
            }
            else
            {
                copyEnd = (windowEnd - windowFilled + len) & WindowMask;
            }

            int copied = len;
            int tailLen = len - copyEnd;

            if (tailLen > 0)
            {
                System.Array.Copy(window, WindowSize - tailLen, output, offset, tailLen);
                offset += tailLen;
                len = copyEnd;
            }
            System.Array.Copy(window, copyEnd - len, output, offset, len);
            windowFilled -= copied;
            if (windowFilled < 0)
            {
                throw new InvalidOperationException();
            }
            return copied;
        }

        /// <summary>
        /// Reset by clearing window so <see cref="GetAvailable">GetAvailable</see> returns 0
        /// </summary>
        public void Reset()
        {
            windowFilled = windowEnd = 0;
        }
    }
    /// <summary>
	/// This class allows us to retrieve a specified number of bits from
	/// the input buffer, as well as copy big byte blocks.
	///
	/// It uses an int buffer to store up to 31 bits for direct
	/// manipulation.  This guarantees that we can get at least 16 bits,
	/// but we only need at most 15, so this is all safe.
	///
	/// There are some optimizations in this class, for example, you must
	/// never peek more than 8 bits more than needed, and you must first
	/// peek bits before you may drop them.  This is not a general purpose
	/// class but optimized for the behaviour of the Inflater.
	///
	/// authors of the original java version : John Leuner, Jochen Hoenicke
	/// </summary>
	public class StreamManipulator
    {
        /// <summary>
        /// Get the next sequence of bits but don't increase input pointer.  bitCount must be
        /// less or equal 16 and if this call succeeds, you must drop
        /// at least n - 8 bits in the next call.
        /// </summary>
        /// <param name="bitCount">The number of bits to peek.</param>
        /// <returns>
        /// the value of the bits, or -1 if not enough bits available.  */
        /// </returns>
        public int PeekBits(int bitCount)
        {
            if (bitsInBuffer_ < bitCount)
            {
                if (windowStart_ == windowEnd_)
                {
                    return -1; // ok
                }
                buffer_ |= (uint)((window_[windowStart_++] & 0xff |
                                 (window_[windowStart_++] & 0xff) << 8) << bitsInBuffer_);
                bitsInBuffer_ += 16;
            }
            return (int)(buffer_ & ((1 << bitCount) - 1));
        }

        /// <summary>
        /// Tries to grab the next <paramref name="bitCount"/> bits from the input and
        /// sets <paramref name="output"/> to the value, adding <paramref name="outputOffset"/>.
        /// </summary>
        /// <returns>true if enough bits could be read, otherwise false</returns>
        public bool TryGetBits(int bitCount, ref int output, int outputOffset = 0)
        {
            var bits = PeekBits(bitCount);
            if (bits < 0)
            {
                return false;
            }
            output = bits + outputOffset;
            DropBits(bitCount);
            return true;
        }

        /// <summary>
        /// Tries to grab the next <paramref name="bitCount"/> bits from the input and
        /// sets <paramref name="index"/> of <paramref name="array"/> to the value.
        /// </summary>
        /// <returns>true if enough bits could be read, otherwise false</returns>
        public bool TryGetBits(int bitCount, ref byte[] array, int index)
        {
            var bits = PeekBits(bitCount);
            if (bits < 0)
            {
                return false;
            }
            array[index] = (byte)bits;
            DropBits(bitCount);
            return true;
        }

        /// <summary>
        /// Drops the next n bits from the input.  You should have called PeekBits
        /// with a bigger or equal n before, to make sure that enough bits are in
        /// the bit buffer.
        /// </summary>
        /// <param name="bitCount">The number of bits to drop.</param>
        public void DropBits(int bitCount)
        {
            buffer_ >>= bitCount;
            bitsInBuffer_ -= bitCount;
        }

        /// <summary>
        /// Gets the next n bits and increases input pointer.  This is equivalent
        /// to <see cref="PeekBits"/> followed by <see cref="DropBits"/>, except for correct error handling.
        /// </summary>
        /// <param name="bitCount">The number of bits to retrieve.</param>
        /// <returns>
        /// the value of the bits, or -1 if not enough bits available.
        /// </returns>
        public int GetBits(int bitCount)
        {
            int bits = PeekBits(bitCount);
            if (bits >= 0)
            {
                DropBits(bitCount);
            }
            return bits;
        }

        /// <summary>
        /// Gets the number of bits available in the bit buffer.  This must be
        /// only called when a previous PeekBits() returned -1.
        /// </summary>
        /// <returns>
        /// the number of bits available.
        /// </returns>
        public int AvailableBits
        {
            get
            {
                return bitsInBuffer_;
            }
        }

        /// <summary>
        /// Gets the number of bytes available.
        /// </summary>
        /// <returns>
        /// The number of bytes available.
        /// </returns>
        public int AvailableBytes
        {
            get
            {
                return windowEnd_ - windowStart_ + (bitsInBuffer_ >> 3);
            }
        }

        /// <summary>
        /// Skips to the next byte boundary.
        /// </summary>
        public void SkipToByteBoundary()
        {
            buffer_ >>= (bitsInBuffer_ & 7);
            bitsInBuffer_ &= ~7;
        }

        /// <summary>
        /// Returns true when SetInput can be called
        /// </summary>
        public bool IsNeedingInput
        {
            get
            {
                return windowStart_ == windowEnd_;
            }
        }

        /// <summary>
        /// Copies bytes from input buffer to output buffer starting
        /// at output[offset].  You have to make sure, that the buffer is
        /// byte aligned.  If not enough bytes are available, copies fewer
        /// bytes.
        /// </summary>
        /// <param name="output">
        /// The buffer to copy bytes to.
        /// </param>
        /// <param name="offset">
        /// The offset in the buffer at which copying starts
        /// </param>
        /// <param name="length">
        /// The length to copy, 0 is allowed.
        /// </param>
        /// <returns>
        /// The number of bytes copied, 0 if no bytes were available.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Length is less than zero
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Bit buffer isnt byte aligned
        /// </exception>
        public int CopyBytes(byte[] output, int offset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if ((bitsInBuffer_ & 7) != 0)
            {
                // bits_in_buffer may only be 0 or a multiple of 8
                throw new InvalidOperationException("Bit buffer is not byte aligned!");
            }

            int count = 0;
            while ((bitsInBuffer_ > 0) && (length > 0))
            {
                output[offset++] = (byte)buffer_;
                buffer_ >>= 8;
                bitsInBuffer_ -= 8;
                length--;
                count++;
            }

            if (length == 0)
            {
                return count;
            }

            int avail = windowEnd_ - windowStart_;
            if (length > avail)
            {
                length = avail;
            }
            System.Array.Copy(window_, windowStart_, output, offset, length);
            windowStart_ += length;

            if (((windowStart_ - windowEnd_) & 1) != 0)
            {
                // We always want an even number of bytes in input, see peekBits
                buffer_ = (uint)(window_[windowStart_++] & 0xff);
                bitsInBuffer_ = 8;
            }
            return count + length;
        }

        /// <summary>
        /// Resets state and empties internal buffers
        /// </summary>
        public void Reset()
        {
            buffer_ = 0;
            windowStart_ = windowEnd_ = bitsInBuffer_ = 0;
        }

        /// <summary>
        /// Add more input for consumption.
        /// Only call when IsNeedingInput returns true
        /// </summary>
        /// <param name="buffer">data to be input</param>
        /// <param name="offset">offset of first byte of input</param>
        /// <param name="count">number of bytes of input to add.</param>
        public void SetInput(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Cannot be negative");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot be negative");
            }

            if (windowStart_ < windowEnd_)
            {
                throw new InvalidOperationException("Old input was not completely processed");
            }

            int end = offset + count;

            // We want to throw an ArrayIndexOutOfBoundsException early.
            // Note the check also handles integer wrap around.
            if ((offset > end) || (end > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if ((count & 1) != 0)
            {
                // We always want an even number of bytes in input, see PeekBits
                buffer_ |= (uint)((buffer[offset++] & 0xff) << bitsInBuffer_);
                bitsInBuffer_ += 8;
            }

            window_ = buffer;
            windowStart_ = offset;
            windowEnd_ = end;
        }

        #region Instance Fields

        private byte[] window_;
        private int windowStart_;
        private int windowEnd_;

        private uint buffer_;
        private int bitsInBuffer_;

        #endregion Instance Fields
    }
    /// <summary>
	/// Provides simple <see cref="Stream"/>" utilities.
	/// </summary>
	public static class StreamUtils
    {
        /// <summary>
        /// Read from a <see cref="Stream"/> ensuring all the required data is read.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="buffer">The buffer to fill.</param>
        /// <seealso cref="ReadFully(Stream,byte[],int,int)"/>
        public static void ReadFully(Stream stream, byte[] buffer)
        {
            ReadFully(stream, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Read from a <see cref="Stream"/>" ensuring all the required data is read.
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <param name="buffer">The buffer to store data in.</param>
        /// <param name="offset">The offset at which to begin storing data.</param>
        /// <param name="count">The number of bytes of data to store.</param>
        /// <exception cref="ArgumentNullException">Required parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> and or <paramref name="count"/> are invalid.</exception>
        /// <exception cref="EndOfStreamException">End of stream is encountered before all the data has been read.</exception>
        public static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Offset can equal length when buffer and count are 0.
            if ((offset < 0) || (offset > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if ((count < 0) || (offset + count > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (count > 0)
            {
                int readCount = stream.Read(buffer, offset, count);
                if (readCount <= 0)
                {
                    throw new EndOfStreamException();
                }
                offset += readCount;
                count -= readCount;
            }
        }

        /// <summary>
        /// Read as much data as possible from a <see cref="Stream"/>", up to the requested number of bytes
        /// </summary>
        /// <param name="stream">The stream to read data from.</param>
        /// <param name="buffer">The buffer to store data in.</param>
        /// <param name="offset">The offset at which to begin storing data.</param>
        /// <param name="count">The number of bytes of data to store.</param>
        /// <exception cref="ArgumentNullException">Required parameter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> and or <paramref name="count"/> are invalid.</exception>
        public static int ReadRequestedBytes(Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Offset can equal length when buffer and count are 0.
            if ((offset < 0) || (offset > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if ((count < 0) || (offset + count > buffer.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int totalReadCount = 0;
            while (count > 0)
            {
                int readCount = stream.Read(buffer, offset, count);
                if (readCount <= 0)
                {
                    break;
                }
                offset += readCount;
                count -= readCount;
                totalReadCount += readCount;
            }

            return totalReadCount;
        }

        /// <summary>
        /// Copy the contents of one <see cref="Stream"/> to another.
        /// </summary>
        /// <param name="source">The stream to source data from.</param>
        /// <param name="destination">The stream to write data to.</param>
        /// <param name="buffer">The buffer to use during copying.</param>
        public static void Copy(Stream source, Stream destination, byte[] buffer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Ensure a reasonable size of buffer is used without being prohibitive.
            if (buffer.Length < 128)
            {
                throw new ArgumentException("Buffer is too small", nameof(buffer));
            }

            bool copying = true;

            while (copying)
            {
                int bytesRead = source.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                }
                else
                {
                    destination.Flush();
                    copying = false;
                }
            }
        }

        /// <summary>
        /// Copy the contents of one <see cref="Stream"/> to another.
        /// </summary>
        /// <param name="source">The stream to source data from.</param>
        /// <param name="destination">The stream to write data to.</param>
        /// <param name="buffer">The buffer to use during copying.</param>
        /// <param name="progressHandler">The <see cref="ProgressHandler">progress handler delegate</see> to use.</param>
        /// <param name="updateInterval">The minimum <see cref="TimeSpan"/> between progress updates.</param>
        /// <param name="sender">The source for this event.</param>
        /// <param name="name">The name to use with the event.</param>
        /// <remarks>This form is specialised for use within #Zip to support events during archive operations.</remarks>
        public static void Copy(Stream source, Stream destination,
            byte[] buffer, ProgressHandler progressHandler, TimeSpan updateInterval, object sender, string name)
        {
            Copy(source, destination, buffer, progressHandler, updateInterval, sender, name, -1);
        }

        /// <summary>
        /// Copy the contents of one <see cref="Stream"/> to another.
        /// </summary>
        /// <param name="source">The stream to source data from.</param>
        /// <param name="destination">The stream to write data to.</param>
        /// <param name="buffer">The buffer to use during copying.</param>
        /// <param name="progressHandler">The <see cref="ProgressHandler">progress handler delegate</see> to use.</param>
        /// <param name="updateInterval">The minimum <see cref="TimeSpan"/> between progress updates.</param>
        /// <param name="sender">The source for this event.</param>
        /// <param name="name">The name to use with the event.</param>
        /// <param name="fixedTarget">A predetermined fixed target value to use with progress updates.
        /// If the value is negative the target is calculated by looking at the stream.</param>
        /// <remarks>This form is specialised for use within #Zip to support events during archive operations.</remarks>
        public static void Copy(Stream source, Stream destination,
            byte[] buffer,
            ProgressHandler progressHandler, TimeSpan updateInterval,
            object sender, string name, long fixedTarget)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Ensure a reasonable size of buffer is used without being prohibitive.
            if (buffer.Length < 128)
            {
                throw new ArgumentException("Buffer is too small", nameof(buffer));
            }

            if (progressHandler == null)
            {
                throw new ArgumentNullException(nameof(progressHandler));
            }

            bool copying = true;

            DateTime marker = DateTime.Now;
            long processed = 0;
            long target = 0;

            if (fixedTarget >= 0)
            {
                target = fixedTarget;
            }
            else if (source.CanSeek)
            {
                target = source.Length - source.Position;
            }

            // Always fire 0% progress..
            var args = new ProgressEventArgs(name, processed, target);
            progressHandler(sender, args);

            bool progressFired = true;

            while (copying)
            {
                int bytesRead = source.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    processed += bytesRead;
                    progressFired = false;
                    destination.Write(buffer, 0, bytesRead);
                }
                else
                {
                    destination.Flush();
                    copying = false;
                }

                if (DateTime.Now - marker > updateInterval)
                {
                    progressFired = true;
                    marker = DateTime.Now;
                    args = new ProgressEventArgs(name, processed, target);
                    progressHandler(sender, args);

                    copying = args.ContinueRunning;
                }
            }

            if (!progressFired)
            {
                args = new ProgressEventArgs(name, processed, target);
                progressHandler(sender, args);
            }
        }

        internal static async Task WriteProcToStreamAsync(this Stream targetStream, MemoryStream bufferStream, Action<Stream> writeProc, CancellationToken ct)
        {
            bufferStream.SetLength(0);
            writeProc(bufferStream);
            bufferStream.Position = 0;
            await bufferStream.CopyToAsync(targetStream, 81920, ct);
            bufferStream.SetLength(0);
        }

        internal static async Task WriteProcToStreamAsync(this Stream targetStream, Action<Stream> writeProc, CancellationToken ct)
        {
            using (var ms = new MemoryStream())
            {
                await WriteProcToStreamAsync(targetStream, ms, writeProc, ct);
            }
        }
    }
    /// <summary>
	/// Inflater is used to decompress data that has been compressed according
	/// to the "deflate" standard described in rfc1951.
	///
	/// By default Zlib (rfc1950) headers and footers are expected in the input.
	/// You can use constructor <code> public Inflater(bool noHeader)</code> passing true
	/// if there is no Zlib header information
	///
	/// The usage is as following.  First you have to set some input with
	/// <code>SetInput()</code>, then Inflate() it.  If inflate doesn't
	/// inflate any bytes there may be three reasons:
	/// <ul>
	/// <li>IsNeedingInput() returns true because the input buffer is empty.
	/// You have to provide more input with <code>SetInput()</code>.
	/// NOTE: IsNeedingInput() also returns true when, the stream is finished.
	/// </li>
	/// <li>IsNeedingDictionary() returns true, you have to provide a preset
	///    dictionary with <code>SetDictionary()</code>.</li>
	/// <li>IsFinished returns true, the inflater has finished.</li>
	/// </ul>
	/// Once the first output byte is produced, a dictionary will not be
	/// needed at a later stage.
	///
	/// author of the original java version : John Leuner, Jochen Hoenicke
	/// </summary>
	public class Inflater
    {
        #region Constants/Readonly

        /// <summary>
        /// Copy lengths for literal codes 257..285
        /// </summary>
        private static readonly int[] CPLENS = {
                                  3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
                                  35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
                              };

        /// <summary>
        /// Extra bits for literal codes 257..285
        /// </summary>
        private static readonly int[] CPLEXT = {
                                  0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
                                  3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
                              };

        /// <summary>
        /// Copy offsets for distance codes 0..29
        /// </summary>
        private static readonly int[] CPDIST = {
                                1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
                                257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
                                8193, 12289, 16385, 24577
                              };

        /// <summary>
        /// Extra bits for distance codes
        /// </summary>
        private static readonly int[] CPDEXT = {
                                0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
                                7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
                                12, 12, 13, 13
                              };

        /// <summary>
        /// These are the possible states for an inflater
        /// </summary>
        private const int DECODE_HEADER = 0;

        private const int DECODE_DICT = 1;
        private const int DECODE_BLOCKS = 2;
        private const int DECODE_STORED_LEN1 = 3;
        private const int DECODE_STORED_LEN2 = 4;
        private const int DECODE_STORED = 5;
        private const int DECODE_DYN_HEADER = 6;
        private const int DECODE_HUFFMAN = 7;
        private const int DECODE_HUFFMAN_LENBITS = 8;
        private const int DECODE_HUFFMAN_DIST = 9;
        private const int DECODE_HUFFMAN_DISTBITS = 10;
        private const int DECODE_CHKSUM = 11;
        private const int FINISHED = 12;

        #endregion Constants/Readonly

        #region Instance Fields

        /// <summary>
        /// This variable contains the current state.
        /// </summary>
        private int mode;

        /// <summary>
        /// The adler checksum of the dictionary or of the decompressed
        /// stream, as it is written in the header resp. footer of the
        /// compressed stream.
        /// Only valid if mode is DECODE_DICT or DECODE_CHKSUM.
        /// </summary>
        private int readAdler;

        /// <summary>
        /// The number of bits needed to complete the current state.  This
        /// is valid, if mode is DECODE_DICT, DECODE_CHKSUM,
        /// DECODE_HUFFMAN_LENBITS or DECODE_HUFFMAN_DISTBITS.
        /// </summary>
        private int neededBits;

        private int repLength;
        private int repDist;
        private int uncomprLen;

        /// <summary>
        /// True, if the last block flag was set in the last block of the
        /// inflated stream.  This means that the stream ends after the
        /// current block.
        /// </summary>
        private bool isLastBlock;

        /// <summary>
        /// The total number of inflated bytes.
        /// </summary>
        private long totalOut;

        /// <summary>
        /// The total number of bytes set with setInput().  This is not the
        /// value returned by the TotalIn property, since this also includes the
        /// unprocessed input.
        /// </summary>
        private long totalIn;

        /// <summary>
        /// This variable stores the noHeader flag that was given to the constructor.
        /// True means, that the inflated stream doesn't contain a Zlib header or
        /// footer.
        /// </summary>
        private bool noHeader;

        private readonly StreamManipulator input;
        private OutputWindow outputWindow;
        private InflaterDynHeader dynHeader;
        private InflaterHuffmanTree litlenTree, distTree;
        private Adler32 adler;

        #endregion Instance Fields

        #region Constructors

        /// <summary>
        /// Creates a new inflater or RFC1951 decompressor
        /// RFC1950/Zlib headers and footers will be expected in the input data
        /// </summary>
        public Inflater() : this(false)
        {
        }

        /// <summary>
        /// Creates a new inflater.
        /// </summary>
        /// <param name="noHeader">
        /// True if no RFC1950/Zlib header and footer fields are expected in the input data
        ///
        /// This is used for GZIPed/Zipped input.
        ///
        /// For compatibility with
        /// Sun JDK you should provide one byte of input more than needed in
        /// this case.
        /// </param>
        public Inflater(bool noHeader)
        {
            this.noHeader = noHeader;
            if (!noHeader)
                this.adler = new Adler32();
            input = new StreamManipulator();
            outputWindow = new OutputWindow();
            mode = noHeader ? DECODE_BLOCKS : DECODE_HEADER;
        }

        #endregion Constructors

        /// <summary>
        /// Resets the inflater so that a new stream can be decompressed.  All
        /// pending input and output will be discarded.
        /// </summary>
        public void Reset()
        {
            mode = noHeader ? DECODE_BLOCKS : DECODE_HEADER;
            totalIn = 0;
            totalOut = 0;
            input.Reset();
            outputWindow.Reset();
            dynHeader = null;
            litlenTree = null;
            distTree = null;
            isLastBlock = false;
            adler?.Reset();
        }

        /// <summary>
        /// Decodes a zlib/RFC1950 header.
        /// </summary>
        /// <returns>
        /// False if more input is needed.
        /// </returns>
        /// <exception cref="SharpZipBaseException">
        /// The header is invalid.
        /// </exception>
        private bool DecodeHeader()
        {
            int header = input.PeekBits(16);
            if (header < 0)
            {
                return false;
            }
            input.DropBits(16);

            // The header is written in "wrong" byte order
            header = ((header << 8) | (header >> 8)) & 0xffff;
            if (header % 31 != 0)
            {
                throw new SharpZipBaseException("Header checksum illegal");
            }

            if ((header & 0x0f00) != (Deflater.DEFLATED << 8))
            {
                throw new SharpZipBaseException("Compression Method unknown");
            }

            /* Maximum size of the backwards window in bits.
			* We currently ignore this, but we could use it to make the
			* inflater window more space efficient. On the other hand the
			* full window (15 bits) is needed most times, anyway.
			int max_wbits = ((header & 0x7000) >> 12) + 8;
			*/

            if ((header & 0x0020) == 0)
            { // Dictionary flag?
                mode = DECODE_BLOCKS;
            }
            else
            {
                mode = DECODE_DICT;
                neededBits = 32;
            }
            return true;
        }

        /// <summary>
        /// Decodes the dictionary checksum after the deflate header.
        /// </summary>
        /// <returns>
        /// False if more input is needed.
        /// </returns>
        private bool DecodeDict()
        {
            while (neededBits > 0)
            {
                int dictByte = input.PeekBits(8);
                if (dictByte < 0)
                {
                    return false;
                }
                input.DropBits(8);
                readAdler = (readAdler << 8) | dictByte;
                neededBits -= 8;
            }
            return false;
        }

        /// <summary>
        /// Decodes the huffman encoded symbols in the input stream.
        /// </summary>
        /// <returns>
        /// false if more input is needed, true if output window is
        /// full or the current block ends.
        /// </returns>
        /// <exception cref="SharpZipBaseException">
        /// if deflated stream is invalid.
        /// </exception>
        private bool DecodeHuffman()
        {
            int free = outputWindow.GetFreeSpace();
            while (free >= 258)
            {
                int symbol;
                switch (mode)
                {
                    case DECODE_HUFFMAN:
                        // This is the inner loop so it is optimized a bit
                        while (((symbol = litlenTree.GetSymbol(input)) & ~0xff) == 0)
                        {
                            outputWindow.Write(symbol);
                            if (--free < 258)
                            {
                                return true;
                            }
                        }

                        if (symbol < 257)
                        {
                            if (symbol < 0)
                            {
                                return false;
                            }
                            else
                            {
                                // symbol == 256: end of block
                                distTree = null;
                                litlenTree = null;
                                mode = DECODE_BLOCKS;
                                return true;
                            }
                        }

                        try
                        {
                            repLength = CPLENS[symbol - 257];
                            neededBits = CPLEXT[symbol - 257];
                        }
                        catch (Exception)
                        {
                            throw new SharpZipBaseException("Illegal rep length code");
                        }
                        goto case DECODE_HUFFMAN_LENBITS; // fall through

                    case DECODE_HUFFMAN_LENBITS:
                        if (neededBits > 0)
                        {
                            mode = DECODE_HUFFMAN_LENBITS;
                            int i = input.PeekBits(neededBits);
                            if (i < 0)
                            {
                                return false;
                            }
                            input.DropBits(neededBits);
                            repLength += i;
                        }
                        mode = DECODE_HUFFMAN_DIST;
                        goto case DECODE_HUFFMAN_DIST; // fall through

                    case DECODE_HUFFMAN_DIST:
                        symbol = distTree.GetSymbol(input);
                        if (symbol < 0)
                        {
                            return false;
                        }

                        try
                        {
                            repDist = CPDIST[symbol];
                            neededBits = CPDEXT[symbol];
                        }
                        catch (Exception)
                        {
                            throw new SharpZipBaseException("Illegal rep dist code");
                        }

                        goto case DECODE_HUFFMAN_DISTBITS; // fall through

                    case DECODE_HUFFMAN_DISTBITS:
                        if (neededBits > 0)
                        {
                            mode = DECODE_HUFFMAN_DISTBITS;
                            int i = input.PeekBits(neededBits);
                            if (i < 0)
                            {
                                return false;
                            }
                            input.DropBits(neededBits);
                            repDist += i;
                        }

                        outputWindow.Repeat(repLength, repDist);
                        free -= repLength;
                        mode = DECODE_HUFFMAN;
                        break;

                    default:
                        throw new SharpZipBaseException("Inflater unknown mode");
                }
            }
            return true;
        }

        /// <summary>
        /// Decodes the adler checksum after the deflate stream.
        /// </summary>
        /// <returns>
        /// false if more input is needed.
        /// </returns>
        /// <exception cref="SharpZipBaseException">
        /// If checksum doesn't match.
        /// </exception>
        private bool DecodeChksum()
        {
            while (neededBits > 0)
            {
                int chkByte = input.PeekBits(8);
                if (chkByte < 0)
                {
                    return false;
                }
                input.DropBits(8);
                readAdler = (readAdler << 8) | chkByte;
                neededBits -= 8;
            }

            if ((int)adler?.Value != readAdler)
            {
                throw new SharpZipBaseException("Adler chksum doesn't match: " + (int)adler?.Value + " vs. " + readAdler);
            }

            mode = FINISHED;
            return false;
        }

        /// <summary>
        /// Decodes the deflated stream.
        /// </summary>
        /// <returns>
        /// false if more input is needed, or if finished.
        /// </returns>
        /// <exception cref="SharpZipBaseException">
        /// if deflated stream is invalid.
        /// </exception>
        private bool Decode()
        {
            switch (mode)
            {
                case DECODE_HEADER:
                    return DecodeHeader();

                case DECODE_DICT:
                    return DecodeDict();

                case DECODE_CHKSUM:
                    return DecodeChksum();

                case DECODE_BLOCKS:
                    if (isLastBlock)
                    {
                        if (noHeader)
                        {
                            mode = FINISHED;
                            return false;
                        }
                        else
                        {
                            input.SkipToByteBoundary();
                            neededBits = 32;
                            mode = DECODE_CHKSUM;
                            return true;
                        }
                    }

                    int type = input.PeekBits(3);
                    if (type < 0)
                    {
                        return false;
                    }
                    input.DropBits(3);

                    isLastBlock |= (type & 1) != 0;
                    switch (type >> 1)
                    {
                        case DeflaterConstants.STORED_BLOCK:
                            input.SkipToByteBoundary();
                            mode = DECODE_STORED_LEN1;
                            break;

                        case DeflaterConstants.STATIC_TREES:
                            litlenTree = InflaterHuffmanTree.defLitLenTree;
                            distTree = InflaterHuffmanTree.defDistTree;
                            mode = DECODE_HUFFMAN;
                            break;

                        case DeflaterConstants.DYN_TREES:
                            dynHeader = new InflaterDynHeader(input);
                            mode = DECODE_DYN_HEADER;
                            break;

                        default:
                            throw new SharpZipBaseException("Unknown block type " + type);
                    }
                    return true;

                case DECODE_STORED_LEN1:
                    {
                        if ((uncomprLen = input.PeekBits(16)) < 0)
                        {
                            return false;
                        }
                        input.DropBits(16);
                        mode = DECODE_STORED_LEN2;
                    }
                    goto case DECODE_STORED_LEN2; // fall through

                case DECODE_STORED_LEN2:
                    {
                        int nlen = input.PeekBits(16);
                        if (nlen < 0)
                        {
                            return false;
                        }
                        input.DropBits(16);
                        if (nlen != (uncomprLen ^ 0xffff))
                        {
                            throw new SharpZipBaseException("broken uncompressed block");
                        }
                        mode = DECODE_STORED;
                    }
                    goto case DECODE_STORED; // fall through

                case DECODE_STORED:
                    {
                        int more = outputWindow.CopyStored(input, uncomprLen);
                        uncomprLen -= more;
                        if (uncomprLen == 0)
                        {
                            mode = DECODE_BLOCKS;
                            return true;
                        }
                        return !input.IsNeedingInput;
                    }

                case DECODE_DYN_HEADER:
                    if (!dynHeader.AttemptRead())
                    {
                        return false;
                    }

                    litlenTree = dynHeader.LiteralLengthTree;
                    distTree = dynHeader.DistanceTree;
                    mode = DECODE_HUFFMAN;
                    goto case DECODE_HUFFMAN; // fall through

                case DECODE_HUFFMAN:
                case DECODE_HUFFMAN_LENBITS:
                case DECODE_HUFFMAN_DIST:
                case DECODE_HUFFMAN_DISTBITS:
                    return DecodeHuffman();

                case FINISHED:
                    return false;

                default:
                    throw new SharpZipBaseException("Inflater.Decode unknown mode");
            }
        }

        /// <summary>
        /// Sets the preset dictionary.  This should only be called, if
        /// needsDictionary() returns true and it should set the same
        /// dictionary, that was used for deflating.  The getAdler()
        /// function returns the checksum of the dictionary needed.
        /// </summary>
        /// <param name="buffer">
        /// The dictionary.
        /// </param>
        public void SetDictionary(byte[] buffer)
        {
            SetDictionary(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Sets the preset dictionary.  This should only be called, if
        /// needsDictionary() returns true and it should set the same
        /// dictionary, that was used for deflating.  The getAdler()
        /// function returns the checksum of the dictionary needed.
        /// </summary>
        /// <param name="buffer">
        /// The dictionary.
        /// </param>
        /// <param name="index">
        /// The index into buffer where the dictionary starts.
        /// </param>
        /// <param name="count">
        /// The number of bytes in the dictionary.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// No dictionary is needed.
        /// </exception>
        /// <exception cref="SharpZipBaseException">
        /// The adler checksum for the buffer is invalid
        /// </exception>
        public void SetDictionary(byte[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (!IsNeedingDictionary)
            {
                throw new InvalidOperationException("Dictionary is not needed");
            }

            adler?.Update(new ArraySegment<byte>(buffer, index, count));

            if (adler != null && (int)adler.Value != readAdler)
            {
                throw new SharpZipBaseException("Wrong adler checksum");
            }
            adler?.Reset();
            outputWindow.CopyDict(buffer, index, count);
            mode = DECODE_BLOCKS;
        }

        /// <summary>
        /// Sets the input.  This should only be called, if needsInput()
        /// returns true.
        /// </summary>
        /// <param name="buffer">
        /// the input.
        /// </param>
        public void SetInput(byte[] buffer)
        {
            SetInput(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Sets the input.  This should only be called, if needsInput()
        /// returns true.
        /// </summary>
        /// <param name="buffer">
        /// The source of input data
        /// </param>
        /// <param name="index">
        /// The index into buffer where the input starts.
        /// </param>
        /// <param name="count">
        /// The number of bytes of input to use.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// No input is needed.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The index and/or count are wrong.
        /// </exception>
        public void SetInput(byte[] buffer, int index, int count)
        {
            input.SetInput(buffer, index, count);
            totalIn += (long)count;
        }

        /// <summary>
        /// Inflates the compressed stream to the output buffer.  If this
        /// returns 0, you should check, whether IsNeedingDictionary(),
        /// IsNeedingInput() or IsFinished() returns true, to determine why no
        /// further output is produced.
        /// </summary>
        /// <param name="buffer">
        /// the output buffer.
        /// </param>
        /// <returns>
        /// The number of bytes written to the buffer, 0 if no further
        /// output can be produced.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// if buffer has length 0.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// if deflated stream is invalid.
        /// </exception>
        public int Inflate(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            return Inflate(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Inflates the compressed stream to the output buffer.  If this
        /// returns 0, you should check, whether needsDictionary(),
        /// needsInput() or finished() returns true, to determine why no
        /// further output is produced.
        /// </summary>
        /// <param name="buffer">
        /// the output buffer.
        /// </param>
        /// <param name="offset">
        /// the offset in buffer where storing starts.
        /// </param>
        /// <param name="count">
        /// the maximum number of bytes to output.
        /// </param>
        /// <returns>
        /// the number of bytes written to the buffer, 0 if no further output can be produced.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// if count is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// if the index and / or count are wrong.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// if deflated stream is invalid.
        /// </exception>
        public int Inflate(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be negative");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("count exceeds buffer bounds");
            }

            // Special case: count may be zero
            if (count == 0)
            {
                if (!IsFinished)
                { // -jr- 08-Nov-2003 INFLATE_BUG fix..
                    Decode();
                }
                return 0;
            }

            int bytesCopied = 0;

            do
            {
                if (mode != DECODE_CHKSUM)
                {
                    /* Don't give away any output, if we are waiting for the
					* checksum in the input stream.
					*
					* With this trick we have always:
					*   IsNeedingInput() and not IsFinished()
					*   implies more output can be produced.
					*/
                    int more = outputWindow.CopyOutput(buffer, offset, count);
                    if (more > 0)
                    {
                        adler?.Update(new ArraySegment<byte>(buffer, offset, more));
                        offset += more;
                        bytesCopied += more;
                        totalOut += (long)more;
                        count -= more;
                        if (count == 0)
                        {
                            return bytesCopied;
                        }
                    }
                }
            } while (Decode() || ((outputWindow.GetAvailable() > 0) && (mode != DECODE_CHKSUM)));
            return bytesCopied;
        }

        /// <summary>
        /// Returns true, if the input buffer is empty.
        /// You should then call setInput().
        /// NOTE: This method also returns true when the stream is finished.
        /// </summary>
        public bool IsNeedingInput
        {
            get
            {
                return input.IsNeedingInput;
            }
        }

        /// <summary>
        /// Returns true, if a preset dictionary is needed to inflate the input.
        /// </summary>
        public bool IsNeedingDictionary
        {
            get
            {
                return mode == DECODE_DICT && neededBits == 0;
            }
        }

        /// <summary>
        /// Returns true, if the inflater has finished.  This means, that no
        /// input is needed and no output can be produced.
        /// </summary>
        public bool IsFinished
        {
            get
            {
                return mode == FINISHED && outputWindow.GetAvailable() == 0;
            }
        }

        /// <summary>
        /// Gets the adler checksum.  This is either the checksum of all
        /// uncompressed bytes returned by inflate(), or if needsDictionary()
        /// returns true (and thus no output was yet produced) this is the
        /// adler checksum of the expected dictionary.
        /// </summary>
        /// <returns>
        /// the adler checksum.
        /// </returns>
        public int Adler
        {
            get
            {
                if (IsNeedingDictionary)
                {
                    return readAdler;
                }
                else if (adler != null)
                {
                    return (int)adler.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the total number of output bytes returned by Inflate().
        /// </summary>
        /// <returns>
        /// the total number of output bytes.
        /// </returns>
        public long TotalOut
        {
            get
            {
                return totalOut;
            }
        }

        /// <summary>
        /// Gets the total number of processed compressed input bytes.
        /// </summary>
        /// <returns>
        /// The total number of bytes of processed input bytes.
        /// </returns>
        public long TotalIn
        {
            get
            {
                return totalIn - (long)RemainingInput;
            }
        }

        /// <summary>
        /// Gets the number of unprocessed input bytes.  Useful, if the end of the
        /// stream is reached and you want to further process the bytes after
        /// the deflate stream.
        /// </summary>
        /// <returns>
        /// The number of bytes of the input which have not been processed.
        /// </returns>
        public int RemainingInput
        {
            // TODO: This should be a long?
            get
            {
                return input.AvailableBytes;
            }
        }
    }
    /// <summary>
    /// An input buffer customised for use by <see cref="InflaterInputStream"/>
    /// </summary>
    /// <remarks>
    /// The buffer supports decryption of incoming data.
    /// </remarks>
    public class InflaterInputBuffer
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of <see cref="InflaterInputBuffer"/> with a default buffer size
        /// </summary>
        /// <param name="stream">The stream to buffer.</param>
        public InflaterInputBuffer(Stream stream) : this(stream, 4096)
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="InflaterInputBuffer"/>
        /// </summary>
        /// <param name="stream">The stream to buffer.</param>
        /// <param name="bufferSize">The size to use for the buffer</param>
        /// <remarks>A minimum buffer size of 1KB is permitted.  Lower sizes are treated as 1KB.</remarks>
        public InflaterInputBuffer(Stream stream, int bufferSize)
        {
            inputStream = stream;
            if (bufferSize < 1024)
            {
                bufferSize = 1024;
            }
            rawData = new byte[bufferSize];
            clearText = rawData;
        }

        #endregion Constructors

        /// <summary>
        /// Get the length of bytes in the <see cref="RawData"/>
        /// </summary>
        public int RawLength
        {
            get
            {
                return rawLength;
            }
        }

        /// <summary>
        /// Get the contents of the raw data buffer.
        /// </summary>
        /// <remarks>This may contain encrypted data.</remarks>
        public byte[] RawData
        {
            get
            {
                return rawData;
            }
        }

        /// <summary>
        /// Get the number of useable bytes in <see cref="ClearText"/>
        /// </summary>
        public int ClearTextLength
        {
            get
            {
                return clearTextLength;
            }
        }

        /// <summary>
        /// Get the contents of the clear text buffer.
        /// </summary>
        public byte[] ClearText
        {
            get
            {
                return clearText;
            }
        }

        /// <summary>
        /// Get/set the number of bytes available
        /// </summary>
        public int Available
        {
            get { return available; }
            set { available = value; }
        }

        /// <summary>
        /// Call <see cref="Inflater.SetInput(byte[], int, int)"/> passing the current clear text buffer contents.
        /// </summary>
        /// <param name="inflater">The inflater to set input for.</param>
        public void SetInflaterInput(Inflater inflater)
        {
            if (available > 0)
            {
                inflater.SetInput(clearText, clearTextLength - available, available);
                available = 0;
            }
        }

        /// <summary>
        /// Fill the buffer from the underlying input stream.
        /// </summary>
        public void Fill()
        {
            rawLength = 0;
            int toRead = rawData.Length;

            while (toRead > 0 && inputStream.CanRead)
            {
                int count = inputStream.Read(rawData, rawLength, toRead);
                if (count <= 0)
                {
                    break;
                }
                rawLength += count;
                toRead -= count;
            }

            if (cryptoTransform != null)
            {
                clearTextLength = cryptoTransform.TransformBlock(rawData, 0, rawLength, clearText, 0);
            }
            else
            {
                clearTextLength = rawLength;
            }

            available = clearTextLength;
        }

        /// <summary>
        /// Read a buffer directly from the input stream
        /// </summary>
        /// <param name="buffer">The buffer to fill</param>
        /// <returns>Returns the number of bytes read.</returns>
        public int ReadRawBuffer(byte[] buffer)
        {
            return ReadRawBuffer(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Read a buffer directly from the input stream
        /// </summary>
        /// <param name="outBuffer">The buffer to read into</param>
        /// <param name="offset">The offset to start reading data into.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>Returns the number of bytes read.</returns>
        public int ReadRawBuffer(byte[] outBuffer, int offset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int currentOffset = offset;
            int currentLength = length;

            while (currentLength > 0)
            {
                if (available <= 0)
                {
                    Fill();
                    if (available <= 0)
                    {
                        return 0;
                    }
                }
                int toCopy = Math.Min(currentLength, available);
                System.Array.Copy(rawData, rawLength - (int)available, outBuffer, currentOffset, toCopy);
                currentOffset += toCopy;
                currentLength -= toCopy;
                available -= toCopy;
            }
            return length;
        }

        /// <summary>
        /// Read clear text data from the input stream.
        /// </summary>
        /// <param name="outBuffer">The buffer to add data to.</param>
        /// <param name="offset">The offset to start adding data at.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>Returns the number of bytes actually read.</returns>
        public int ReadClearTextBuffer(byte[] outBuffer, int offset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int currentOffset = offset;
            int currentLength = length;

            while (currentLength > 0)
            {
                if (available <= 0)
                {
                    Fill();
                    if (available <= 0)
                    {
                        return 0;
                    }
                }

                int toCopy = Math.Min(currentLength, available);
                Array.Copy(clearText, clearTextLength - (int)available, outBuffer, currentOffset, toCopy);
                currentOffset += toCopy;
                currentLength -= toCopy;
                available -= toCopy;
            }
            return length;
        }

        /// <summary>
        /// Read a <see cref="byte"/> from the input stream.
        /// </summary>
        /// <returns>Returns the byte read.</returns>
        public byte ReadLeByte()
        {
            if (available <= 0)
            {
                Fill();
                if (available <= 0)
                {
                    throw new ZipException("EOF in header");
                }
            }
            byte result = rawData[rawLength - available];
            available -= 1;
            return result;
        }

        /// <summary>
        /// Read an <see cref="short"/> in little endian byte order.
        /// </summary>
        /// <returns>The short value read case to an int.</returns>
        public int ReadLeShort()
        {
            return ReadLeByte() | (ReadLeByte() << 8);
        }

        /// <summary>
        /// Read an <see cref="int"/> in little endian byte order.
        /// </summary>
        /// <returns>The int value read.</returns>
        public int ReadLeInt()
        {
            return ReadLeShort() | (ReadLeShort() << 16);
        }

        /// <summary>
        /// Read a <see cref="long"/> in little endian byte order.
        /// </summary>
        /// <returns>The long value read.</returns>
        public long ReadLeLong()
        {
            return (uint)ReadLeInt() | ((long)ReadLeInt() << 32);
        }

        /// <summary>
        /// Get/set the <see cref="ICryptoTransform"/> to apply to any data.
        /// </summary>
        /// <remarks>Set this value to null to have no transform applied.</remarks>
        public ICryptoTransform CryptoTransform
        {
            set
            {
                cryptoTransform = value;
                if (cryptoTransform != null)
                {
                    if (rawData == clearText)
                    {
                        if (internalClearText == null)
                        {
                            internalClearText = new byte[rawData.Length];
                        }
                        clearText = internalClearText;
                    }
                    clearTextLength = rawLength;
                    if (available > 0)
                    {
                        cryptoTransform.TransformBlock(rawData, rawLength - available, available, clearText, rawLength - available);
                    }
                }
                else
                {
                    clearText = rawData;
                    clearTextLength = rawLength;
                }
            }
        }

        #region Instance Fields

        private int rawLength;
        private byte[] rawData;

        private int clearTextLength;
        private byte[] clearText;
        private byte[] internalClearText;

        private int available;

        private ICryptoTransform cryptoTransform;
        private Stream inputStream;

        #endregion Instance Fields
    }

    /// <summary>
    /// This filter stream is used to decompress data compressed using the "deflate"
    /// format. The "deflate" format is described in RFC 1951.
    ///
    /// This stream may form the basis for other decompression filters, such
    /// as the <see cref="ICSharpCode.SharpZipLib.GZip.GZipInputStream">GZipInputStream</see>.
    ///
    /// Author of the original java version : John Leuner.
    /// </summary>
    public class InflaterInputStream : Stream
    {
        #region Constructors

        /// <summary>
        /// Create an InflaterInputStream with the default decompressor
        /// and a default buffer size of 4KB.
        /// </summary>
        /// <param name = "baseInputStream">
        /// The InputStream to read bytes from
        /// </param>
        public InflaterInputStream(Stream baseInputStream)
            : this(baseInputStream, new Inflater(), 4096)
        {
        }

        /// <summary>
        /// Create an InflaterInputStream with the specified decompressor
        /// and a default buffer size of 4KB.
        /// </summary>
        /// <param name = "baseInputStream">
        /// The source of input data
        /// </param>
        /// <param name = "inf">
        /// The decompressor used to decompress data read from baseInputStream
        /// </param>
        public InflaterInputStream(Stream baseInputStream, Inflater inf)
            : this(baseInputStream, inf, 4096)
        {
        }

        /// <summary>
        /// Create an InflaterInputStream with the specified decompressor
        /// and the specified buffer size.
        /// </summary>
        /// <param name = "baseInputStream">
        /// The InputStream to read bytes from
        /// </param>
        /// <param name = "inflater">
        /// The decompressor to use
        /// </param>
        /// <param name = "bufferSize">
        /// Size of the buffer to use
        /// </param>
        public InflaterInputStream(Stream baseInputStream, Inflater inflater, int bufferSize)
        {
            if (baseInputStream == null)
            {
                throw new ArgumentNullException(nameof(baseInputStream));
            }

            if (inflater == null)
            {
                throw new ArgumentNullException(nameof(inflater));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            this.baseInputStream = baseInputStream;
            this.inf = inflater;

            inputBuffer = new InflaterInputBuffer(baseInputStream, bufferSize);
        }

        #endregion Constructors

        /// <summary>
        /// Gets or sets a flag indicating ownership of underlying stream.
        /// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        public bool IsStreamOwner { get; set; } = true;

        /// <summary>
        /// Skip specified number of bytes of uncompressed data
        /// </summary>
        /// <param name ="count">
        /// Number of bytes to skip
        /// </param>
        /// <returns>
        /// The number of bytes skipped, zero if the end of
        /// stream has been reached
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count">The number of bytes</paramref> to skip is less than or equal to zero.
        /// </exception>
        public long Skip(long count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // v0.80 Skip by seeking if underlying stream supports it...
            if (baseInputStream.CanSeek)
            {
                baseInputStream.Seek(count, SeekOrigin.Current);
                return count;
            }
            else
            {
                int length = 2048;
                if (count < length)
                {
                    length = (int)count;
                }

                byte[] tmp = new byte[length];
                int readCount = 1;
                long toSkip = count;

                while ((toSkip > 0) && (readCount > 0))
                {
                    if (toSkip < length)
                    {
                        length = (int)toSkip;
                    }

                    readCount = baseInputStream.Read(tmp, 0, length);
                    toSkip -= readCount;
                }

                return count - toSkip;
            }
        }

        /// <summary>
        /// Clear any cryptographic state.
        /// </summary>
        protected void StopDecrypting()
        {
            inputBuffer.CryptoTransform = null;
        }

        /// <summary>
        /// Returns 0 once the end of the stream (EOF) has been reached.
        /// Otherwise returns 1.
        /// </summary>
        public virtual int Available
        {
            get
            {
                return inf.IsFinished ? 0 : 1;
            }
        }

        /// <summary>
        /// Fills the buffer with more data to decompress.
        /// </summary>
        /// <exception cref="SharpZipBaseException">
        /// Stream ends early
        /// </exception>
        protected void Fill()
        {
            // Protect against redundant calls
            if (inputBuffer.Available <= 0)
            {
                inputBuffer.Fill();
                if (inputBuffer.Available <= 0)
                {
                    throw new SharpZipBaseException("Unexpected EOF");
                }
            }
            inputBuffer.SetInflaterInput(inf);
        }

        #region Stream Overrides

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return baseInputStream.CanRead;
            }
        }

        /// <summary>
        /// Gets a value of false indicating seeking is not supported for this stream.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value of false indicating that this stream is not writeable.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// A value representing the length of the stream in bytes.
        /// </summary>
        public override long Length
        {
            get
            {
                //return inputBuffer.RawLength;
                throw new NotSupportedException("InflaterInputStream Length is not supported");
            }
        }

        /// <summary>
        /// The current position within the stream.
        /// Throws a NotSupportedException when attempting to set the position
        /// </summary>
        /// <exception cref="NotSupportedException">Attempting to set the position</exception>
        public override long Position
        {
            get
            {
                return baseInputStream.Position;
            }
            set
            {
                throw new NotSupportedException("InflaterInputStream Position not supported");
            }
        }

        /// <summary>
        /// Flushes the baseInputStream
        /// </summary>
        public override void Flush()
        {
            baseInputStream.Flush();
        }

        /// <summary>
        /// Sets the position within the current stream
        /// Always throws a NotSupportedException
        /// </summary>
        /// <param name="offset">The relative offset to seek to.</param>
        /// <param name="origin">The <see cref="SeekOrigin"/> defining where to seek from.</param>
        /// <returns>The new position in the stream.</returns>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek not supported");
        }

        /// <summary>
        /// Set the length of the current stream
        /// Always throws a NotSupportedException
        /// </summary>
        /// <param name="value">The new length value for the stream.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("InflaterInputStream SetLength not supported");
        }

        /// <summary>
        /// Writes a sequence of bytes to stream and advances the current position
        /// This method always throws a NotSupportedException
        /// </summary>
        /// <param name="buffer">The buffer containing data to write.</param>
        /// <param name="offset">The offset of the first byte to write.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("InflaterInputStream Write not supported");
        }

        /// <summary>
        /// Writes one byte to the current stream and advances the current position
        /// Always throws a NotSupportedException
        /// </summary>
        /// <param name="value">The byte to write.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void WriteByte(byte value)
        {
            throw new NotSupportedException("InflaterInputStream WriteByte not supported");
        }

        /// <summary>
        /// Closes the input stream.  When <see cref="IsStreamOwner"></see>
        /// is true the underlying stream is also closed.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!isClosed)
            {
                isClosed = true;
                if (IsStreamOwner)
                {
                    baseInputStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Reads decompressed data into the provided buffer byte array
        /// </summary>
        /// <param name ="buffer">
        /// The array to read and decompress data into
        /// </param>
        /// <param name ="offset">
        /// The offset indicating where the data should be placed
        /// </param>
        /// <param name ="count">
        /// The number of bytes to decompress
        /// </param>
        /// <returns>The number of bytes read.  Zero signals the end of stream</returns>
        /// <exception cref="SharpZipBaseException">
        /// Inflater needs a dictionary
        /// </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (inf.IsNeedingDictionary)
            {
                throw new SharpZipBaseException("Need a dictionary");
            }

            int remainingBytes = count;
            while (true)
            {
                int bytesRead = inf.Inflate(buffer, offset, remainingBytes);
                offset += bytesRead;
                remainingBytes -= bytesRead;

                if (remainingBytes == 0 || inf.IsFinished)
                {
                    break;
                }

                if (inf.IsNeedingInput)
                {
                    Fill();
                }
                else if (bytesRead == 0)
                {
                    throw new ZipException("Invalid input data");
                }
            }
            return count - remainingBytes;
        }

        #endregion Stream Overrides

        #region Instance Fields

        /// <summary>
        /// Decompressor for this stream
        /// </summary>
        protected Inflater inf;

        /// <summary>
        /// <see cref="InflaterInputBuffer">Input buffer</see> for this stream.
        /// </summary>
        protected InflaterInputBuffer inputBuffer;

        /// <summary>
        /// Base stream the inflater reads from.
        /// </summary>
        private Stream baseInputStream;

        /// <summary>
        /// The compressed size
        /// </summary>
        protected long csize;

        /// <summary>
        /// Flag indicating whether this instance has been closed or not.
        /// </summary>
        private bool isClosed;

        #endregion Instance Fields
    }
    /// <summary>
	/// This is the Deflater class.  The deflater class compresses input
	/// with the deflate algorithm described in RFC 1951.  It has several
	/// compression levels and three different strategies described below.
	///
	/// This class is <i>not</i> thread safe.  This is inherent in the API, due
	/// to the split of deflate and setInput.
	///
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class Deflater
    {
        #region Deflater Documentation

        /*
		* The Deflater can do the following state transitions:
		*
		* (1) -> INIT_STATE   ----> INIT_FINISHING_STATE ---.
		*        /  | (2)      (5)                          |
		*       /   v          (5)                          |
		*   (3)| SETDICT_STATE ---> SETDICT_FINISHING_STATE |(3)
		*       \   | (3)                 |        ,--------'
		*        |  |                     | (3)   /
		*        v  v          (5)        v      v
		* (1) -> BUSY_STATE   ----> FINISHING_STATE
		*                                | (6)
		*                                v
		*                           FINISHED_STATE
		*    \_____________________________________/
		*                    | (7)
		*                    v
		*               CLOSED_STATE
		*
		* (1) If we should produce a header we start in INIT_STATE, otherwise
		*     we start in BUSY_STATE.
		* (2) A dictionary may be set only when we are in INIT_STATE, then
		*     we change the state as indicated.
		* (3) Whether a dictionary is set or not, on the first call of deflate
		*     we change to BUSY_STATE.
		* (4) -- intentionally left blank -- :)
		* (5) FINISHING_STATE is entered, when flush() is called to indicate that
		*     there is no more INPUT.  There are also states indicating, that
		*     the header wasn't written yet.
		* (6) FINISHED_STATE is entered, when everything has been flushed to the
		*     internal pending output buffer.
		* (7) At any time (7)
		*
		*/

        #endregion Deflater Documentation

        #region Public Constants

        /// <summary>
        /// The best and slowest compression level.  This tries to find very
        /// long and distant string repetitions.
        /// </summary>
        public const int BEST_COMPRESSION = 9;

        /// <summary>
        /// The worst but fastest compression level.
        /// </summary>
        public const int BEST_SPEED = 1;

        /// <summary>
        /// The default compression level.
        /// </summary>
        public const int DEFAULT_COMPRESSION = -1;

        /// <summary>
        /// This level won't compress at all but output uncompressed blocks.
        /// </summary>
        public const int NO_COMPRESSION = 0;

        /// <summary>
        /// The compression method.  This is the only method supported so far.
        /// There is no need to use this constant at all.
        /// </summary>
        public const int DEFLATED = 8;

        #endregion Public Constants

        #region Public Enum

        /// <summary>
        /// Compression Level as an enum for safer use
        /// </summary>
        public enum CompressionLevel
        {
            /// <summary>
            /// The best and slowest compression level.  This tries to find very
            /// long and distant string repetitions.
            /// </summary>
            BEST_COMPRESSION = Deflater.BEST_COMPRESSION,

            /// <summary>
            /// The worst but fastest compression level.
            /// </summary>
            BEST_SPEED = Deflater.BEST_SPEED,

            /// <summary>
            /// The default compression level.
            /// </summary>
            DEFAULT_COMPRESSION = Deflater.DEFAULT_COMPRESSION,

            /// <summary>
            /// This level won't compress at all but output uncompressed blocks.
            /// </summary>
            NO_COMPRESSION = Deflater.NO_COMPRESSION,

            /// <summary>
            /// The compression method.  This is the only method supported so far.
            /// There is no need to use this constant at all.
            /// </summary>
            DEFLATED = Deflater.DEFLATED
        }

        #endregion Public Enum

        #region Local Constants

        private const int IS_SETDICT = 0x01;
        private const int IS_FLUSHING = 0x04;
        private const int IS_FINISHING = 0x08;

        private const int INIT_STATE = 0x00;
        private const int SETDICT_STATE = 0x01;

        //		private static  int INIT_FINISHING_STATE    = 0x08;
        //		private static  int SETDICT_FINISHING_STATE = 0x09;
        private const int BUSY_STATE = 0x10;

        private const int FLUSHING_STATE = 0x14;
        private const int FINISHING_STATE = 0x1c;
        private const int FINISHED_STATE = 0x1e;
        private const int CLOSED_STATE = 0x7f;

        #endregion Local Constants

        #region Constructors

        /// <summary>
        /// Creates a new deflater with default compression level.
        /// </summary>
        public Deflater() : this(DEFAULT_COMPRESSION, false)
        {
        }

        /// <summary>
        /// Creates a new deflater with given compression level.
        /// </summary>
        /// <param name="level">
        /// the compression level, a value between NO_COMPRESSION
        /// and BEST_COMPRESSION, or DEFAULT_COMPRESSION.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">if lvl is out of range.</exception>
        public Deflater(int level) : this(level, false)
        {
        }

        /// <summary>
        /// Creates a new deflater with given compression level.
        /// </summary>
        /// <param name="level">
        /// the compression level, a value between NO_COMPRESSION
        /// and BEST_COMPRESSION.
        /// </param>
        /// <param name="noZlibHeaderOrFooter">
        /// true, if we should suppress the Zlib/RFC1950 header at the
        /// beginning and the adler checksum at the end of the output.  This is
        /// useful for the GZIP/PKZIP formats.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">if lvl is out of range.</exception>
        public Deflater(int level, bool noZlibHeaderOrFooter)
        {
            if (level == DEFAULT_COMPRESSION)
            {
                level = 6;
            }
            else if (level < NO_COMPRESSION || level > BEST_COMPRESSION)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            pending = new DeflaterPending();
            engine = new DeflaterEngine(pending, noZlibHeaderOrFooter);
            this.noZlibHeaderOrFooter = noZlibHeaderOrFooter;
            SetStrategy(DeflateStrategy.Default);
            SetLevel(level);
            Reset();
        }

        #endregion Constructors

        /// <summary>
        /// Resets the deflater.  The deflater acts afterwards as if it was
        /// just created with the same compression level and strategy as it
        /// had before.
        /// </summary>
        public void Reset()
        {
            state = (noZlibHeaderOrFooter ? BUSY_STATE : INIT_STATE);
            totalOut = 0;
            pending.Reset();
            engine.Reset();
        }

        /// <summary>
        /// Gets the current adler checksum of the data that was processed so far.
        /// </summary>
        public int Adler
        {
            get
            {
                return engine.Adler;
            }
        }

        /// <summary>
        /// Gets the number of input bytes processed so far.
        /// </summary>
        public long TotalIn
        {
            get
            {
                return engine.TotalIn;
            }
        }

        /// <summary>
        /// Gets the number of output bytes so far.
        /// </summary>
        public long TotalOut
        {
            get
            {
                return totalOut;
            }
        }

        /// <summary>
        /// Flushes the current input block.  Further calls to deflate() will
        /// produce enough output to inflate everything in the current input
        /// block.  This is not part of Sun's JDK so I have made it package
        /// private.  It is used by DeflaterOutputStream to implement
        /// flush().
        /// </summary>
        public void Flush()
        {
            state |= IS_FLUSHING;
        }

        /// <summary>
        /// Finishes the deflater with the current input block.  It is an error
        /// to give more input after this method was called.  This method must
        /// be called to force all bytes to be flushed.
        /// </summary>
        public void Finish()
        {
            state |= (IS_FLUSHING | IS_FINISHING);
        }

        /// <summary>
        /// Returns true if the stream was finished and no more output bytes
        /// are available.
        /// </summary>
        public bool IsFinished
        {
            get
            {
                return (state == FINISHED_STATE) && pending.IsFlushed;
            }
        }

        /// <summary>
        /// Returns true, if the input buffer is empty.
        /// You should then call setInput().
        /// NOTE: This method can also return true when the stream
        /// was finished.
        /// </summary>
        public bool IsNeedingInput
        {
            get
            {
                return engine.NeedsInput();
            }
        }

        /// <summary>
        /// Sets the data which should be compressed next.  This should be only
        /// called when needsInput indicates that more input is needed.
        /// If you call setInput when needsInput() returns false, the
        /// previous input that is still pending will be thrown away.
        /// The given byte array should not be changed, before needsInput() returns
        /// true again.
        /// This call is equivalent to <code>setInput(input, 0, input.length)</code>.
        /// </summary>
        /// <param name="input">
        /// the buffer containing the input data.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// if the buffer was finished() or ended().
        /// </exception>
        public void SetInput(byte[] input)
        {
            SetInput(input, 0, input.Length);
        }

        /// <summary>
        /// Sets the data which should be compressed next.  This should be
        /// only called when needsInput indicates that more input is needed.
        /// The given byte array should not be changed, before needsInput() returns
        /// true again.
        /// </summary>
        /// <param name="input">
        /// the buffer containing the input data.
        /// </param>
        /// <param name="offset">
        /// the start of the data.
        /// </param>
        /// <param name="count">
        /// the number of data bytes of input.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// if the buffer was Finish()ed or if previous input is still pending.
        /// </exception>
        public void SetInput(byte[] input, int offset, int count)
        {
            if ((state & IS_FINISHING) != 0)
            {
                throw new InvalidOperationException("Finish() already called");
            }
            engine.SetInput(input, offset, count);
        }

        /// <summary>
        /// Sets the compression level.  There is no guarantee of the exact
        /// position of the change, but if you call this when needsInput is
        /// true the change of compression level will occur somewhere near
        /// before the end of the so far given input.
        /// </summary>
        /// <param name="level">
        /// the new compression level.
        /// </param>
        public void SetLevel(int level)
        {
            if (level == DEFAULT_COMPRESSION)
            {
                level = 6;
            }
            else if (level < NO_COMPRESSION || level > BEST_COMPRESSION)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (this.level != level)
            {
                this.level = level;
                engine.SetLevel(level);
            }
        }

        /// <summary>
        /// Get current compression level
        /// </summary>
        /// <returns>Returns the current compression level</returns>
        public int GetLevel()
        {
            return level;
        }

        /// <summary>
        /// Sets the compression strategy. Strategy is one of
        /// DEFAULT_STRATEGY, HUFFMAN_ONLY and FILTERED.  For the exact
        /// position where the strategy is changed, the same as for
        /// SetLevel() applies.
        /// </summary>
        /// <param name="strategy">
        /// The new compression strategy.
        /// </param>
        public void SetStrategy(DeflateStrategy strategy)
        {
            engine.Strategy = strategy;
        }

        /// <summary>
        /// Deflates the current input block with to the given array.
        /// </summary>
        /// <param name="output">
        /// The buffer where compressed data is stored
        /// </param>
        /// <returns>
        /// The number of compressed bytes added to the output, or 0 if either
        /// IsNeedingInput() or IsFinished returns true or length is zero.
        /// </returns>
        public int Deflate(byte[] output)
        {
            return Deflate(output, 0, output.Length);
        }

        /// <summary>
        /// Deflates the current input block to the given array.
        /// </summary>
        /// <param name="output">
        /// Buffer to store the compressed data.
        /// </param>
        /// <param name="offset">
        /// Offset into the output array.
        /// </param>
        /// <param name="length">
        /// The maximum number of bytes that may be stored.
        /// </param>
        /// <returns>
        /// The number of compressed bytes added to the output, or 0 if either
        /// needsInput() or finished() returns true or length is zero.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// If Finish() was previously called.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// If offset or length don't match the array length.
        /// </exception>
        public int Deflate(byte[] output, int offset, int length)
        {
            int origLength = length;

            if (state == CLOSED_STATE)
            {
                throw new InvalidOperationException("Deflater closed");
            }

            if (state < BUSY_STATE)
            {
                // output header
                int header = (DEFLATED +
                    ((DeflaterConstants.MAX_WBITS - 8) << 4)) << 8;
                int level_flags = (level - 1) >> 1;
                if (level_flags < 0 || level_flags > 3)
                {
                    level_flags = 3;
                }
                header |= level_flags << 6;
                if ((state & IS_SETDICT) != 0)
                {
                    // Dictionary was set
                    header |= DeflaterConstants.PRESET_DICT;
                }
                header += 31 - (header % 31);

                pending.WriteShortMSB(header);
                if ((state & IS_SETDICT) != 0)
                {
                    int chksum = engine.Adler;
                    engine.ResetAdler();
                    pending.WriteShortMSB(chksum >> 16);
                    pending.WriteShortMSB(chksum & 0xffff);
                }

                state = BUSY_STATE | (state & (IS_FLUSHING | IS_FINISHING));
            }

            for (; ; )
            {
                int count = pending.Flush(output, offset, length);
                offset += count;
                totalOut += count;
                length -= count;

                if (length == 0 || state == FINISHED_STATE)
                {
                    break;
                }

                if (!engine.Deflate((state & IS_FLUSHING) != 0, (state & IS_FINISHING) != 0))
                {
                    switch (state)
                    {
                        case BUSY_STATE:
                            // We need more input now
                            return origLength - length;

                        case FLUSHING_STATE:
                            if (level != NO_COMPRESSION)
                            {
                                /* We have to supply some lookahead.  8 bit lookahead
								 * is needed by the zlib inflater, and we must fill
								 * the next byte, so that all bits are flushed.
								 */
                                int neededbits = 8 + ((-pending.BitCount) & 7);
                                while (neededbits > 0)
                                {
                                    /* write a static tree block consisting solely of
									 * an EOF:
									 */
                                    pending.WriteBits(2, 10);
                                    neededbits -= 10;
                                }
                            }
                            state = BUSY_STATE;
                            break;

                        case FINISHING_STATE:
                            pending.AlignToByte();

                            // Compressed data is complete.  Write footer information if required.
                            if (!noZlibHeaderOrFooter)
                            {
                                int adler = engine.Adler;
                                pending.WriteShortMSB(adler >> 16);
                                pending.WriteShortMSB(adler & 0xffff);
                            }
                            state = FINISHED_STATE;
                            break;
                    }
                }
            }
            return origLength - length;
        }

        /// <summary>
        /// Sets the dictionary which should be used in the deflate process.
        /// This call is equivalent to <code>setDictionary(dict, 0, dict.Length)</code>.
        /// </summary>
        /// <param name="dictionary">
        /// the dictionary.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// if SetInput () or Deflate () were already called or another dictionary was already set.
        /// </exception>
        public void SetDictionary(byte[] dictionary)
        {
            SetDictionary(dictionary, 0, dictionary.Length);
        }

        /// <summary>
        /// Sets the dictionary which should be used in the deflate process.
        /// The dictionary is a byte array containing strings that are
        /// likely to occur in the data which should be compressed.  The
        /// dictionary is not stored in the compressed output, only a
        /// checksum.  To decompress the output you need to supply the same
        /// dictionary again.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary data
        /// </param>
        /// <param name="index">
        /// The index where dictionary information commences.
        /// </param>
        /// <param name="count">
        /// The number of bytes in the dictionary.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        /// If SetInput () or Deflate() were already called or another dictionary was already set.
        /// </exception>
        public void SetDictionary(byte[] dictionary, int index, int count)
        {
            if (state != INIT_STATE)
            {
                throw new InvalidOperationException();
            }

            state = SETDICT_STATE;
            engine.SetDictionary(dictionary, index, count);
        }

        #region Instance Fields

        /// <summary>
        /// Compression level.
        /// </summary>
        private int level;

        /// <summary>
        /// If true no Zlib/RFC1950 headers or footers are generated
        /// </summary>
        private bool noZlibHeaderOrFooter;

        /// <summary>
        /// The current state.
        /// </summary>
        private int state;

        /// <summary>
        /// The total bytes of output written.
        /// </summary>
        private long totalOut;

        /// <summary>
        /// The pending output.
        /// </summary>
        private DeflaterPending pending;

        /// <summary>
        /// The deflater engine.
        /// </summary>
        private DeflaterEngine engine;

        #endregion Instance Fields
    }
    /// <summary>
    /// A special stream deflating or compressing the bytes that are
    /// written to it.  It uses a Deflater to perform actual deflating.<br/>
    /// Authors of the original java version : Tom Tromey, Jochen Hoenicke
    /// </summary>
    public class DeflaterOutputStream : Stream
    {
        #region Constructors

        /// <summary>
        /// Creates a new DeflaterOutputStream with a default Deflater and default buffer size.
        /// </summary>
        /// <param name="baseOutputStream">
        /// the output stream where deflated output should be written.
        /// </param>
        public DeflaterOutputStream(Stream baseOutputStream)
            : this(baseOutputStream, new Deflater(), 512)
        {
        }

        /// <summary>
        /// Creates a new DeflaterOutputStream with the given Deflater and
        /// default buffer size.
        /// </summary>
        /// <param name="baseOutputStream">
        /// the output stream where deflated output should be written.
        /// </param>
        /// <param name="deflater">
        /// the underlying deflater.
        /// </param>
        public DeflaterOutputStream(Stream baseOutputStream, Deflater deflater)
            : this(baseOutputStream, deflater, 512)
        {
        }

        /// <summary>
        /// Creates a new DeflaterOutputStream with the given Deflater and
        /// buffer size.
        /// </summary>
        /// <param name="baseOutputStream">
        /// The output stream where deflated output is written.
        /// </param>
        /// <param name="deflater">
        /// The underlying deflater to use
        /// </param>
        /// <param name="bufferSize">
        /// The buffer size in bytes to use when deflating (minimum value 512)
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// bufsize is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// baseOutputStream does not support writing
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// deflater instance is null
        /// </exception>
        public DeflaterOutputStream(Stream baseOutputStream, Deflater deflater, int bufferSize)
        {
            if (baseOutputStream == null)
            {
                throw new ArgumentNullException(nameof(baseOutputStream));
            }

            if (baseOutputStream.CanWrite == false)
            {
                throw new ArgumentException("Must support writing", nameof(baseOutputStream));
            }

            if (bufferSize < 512)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            baseOutputStream_ = baseOutputStream;
            buffer_ = new byte[bufferSize];
            deflater_ = deflater ?? throw new ArgumentNullException(nameof(deflater));
        }

        #endregion Constructors

        #region Public API

        /// <summary>
        /// Finishes the stream by calling finish() on the deflater.
        /// </summary>
        /// <exception cref="SharpZipBaseException">
        /// Not all input is deflated
        /// </exception>
        public virtual void Finish()
        {
            deflater_.Finish();
            while (!deflater_.IsFinished)
            {
                int len = deflater_.Deflate(buffer_, 0, buffer_.Length);
                if (len <= 0)
                {
                    break;
                }

                EncryptBlock(buffer_, 0, len);

                baseOutputStream_.Write(buffer_, 0, len);
            }

            if (!deflater_.IsFinished)
            {
                throw new SharpZipBaseException("Can't deflate all input?");
            }

            baseOutputStream_.Flush();

            if (cryptoTransform_ != null)
            {
                if (cryptoTransform_ is ZipAESTransform)
                {
                    AESAuthCode = ((ZipAESTransform)cryptoTransform_).GetAuthCode();
                }
                cryptoTransform_.Dispose();
                cryptoTransform_ = null;
            }
        }

        /// <summary>
        /// Finishes the stream by calling finish() on the deflater.
        /// </summary>
        /// <param name="ct">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <exception cref="SharpZipBaseException">
        /// Not all input is deflated
        /// </exception>
        public virtual async Task FinishAsync(CancellationToken ct)
        {
            deflater_.Finish();
            while (!deflater_.IsFinished)
            {
                int len = deflater_.Deflate(buffer_, 0, buffer_.Length);
                if (len <= 0)
                {
                    break;
                }

                EncryptBlock(buffer_, 0, len);

                await baseOutputStream_.WriteAsync(buffer_, 0, len, ct);
            }

            if (!deflater_.IsFinished)
            {
                throw new SharpZipBaseException("Can't deflate all input?");
            }

            await baseOutputStream_.FlushAsync(ct);

            if (cryptoTransform_ != null)
            {
                if (cryptoTransform_ is ZipAESTransform)
                {
                    AESAuthCode = ((ZipAESTransform)cryptoTransform_).GetAuthCode();
                }
                cryptoTransform_.Dispose();
                cryptoTransform_ = null;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating ownership of underlying stream.
        /// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        public bool IsStreamOwner { get; set; } = true;

        ///	<summary>
        /// Allows client to determine if an entry can be patched after its added
        /// </summary>
        public bool CanPatchEntries
        {
            get
            {
                return baseOutputStream_.CanSeek;
            }
        }

        #endregion Public API

        #region Encryption

        /// <summary>
        /// The CryptoTransform currently being used to encrypt the compressed data.
        /// </summary>
        protected ICryptoTransform cryptoTransform_;

        /// <summary>
        /// Returns the 10 byte AUTH CODE to be appended immediately following the AES data stream.
        /// </summary>
        protected byte[] AESAuthCode;

        /// <inheritdoc cref="StringCodec.ZipCryptoEncoding"/>
        public Encoding ZipCryptoEncoding { get; set; } = StringCodec.DefaultZipCryptoEncoding;

        /// <summary>
        /// Encrypt a block of data
        /// </summary>
        /// <param name="buffer">
        /// Data to encrypt.  NOTE the original contents of the buffer are lost
        /// </param>
        /// <param name="offset">
        /// Offset of first byte in buffer to encrypt
        /// </param>
        /// <param name="length">
        /// Number of bytes in buffer to encrypt
        /// </param>
        protected void EncryptBlock(byte[] buffer, int offset, int length)
        {
            if (cryptoTransform_ is null) return;
            cryptoTransform_.TransformBlock(buffer, 0, length, buffer, 0);
        }

        #endregion Encryption

        #region Deflation Support

        /// <summary>
        /// Deflates everything in the input buffers.  This will call
        /// <code>def.deflate()</code> until all bytes from the input buffers
        /// are processed.
        /// </summary>
        protected void Deflate()
        {
            Deflate(false);
        }

        private void Deflate(bool flushing)
        {
            while (flushing || !deflater_.IsNeedingInput)
            {
                int deflateCount = deflater_.Deflate(buffer_, 0, buffer_.Length);

                if (deflateCount <= 0)
                {
                    break;
                }

                EncryptBlock(buffer_, 0, deflateCount);

                baseOutputStream_.Write(buffer_, 0, deflateCount);
            }

            if (!deflater_.IsNeedingInput)
            {
                throw new SharpZipBaseException("DeflaterOutputStream can't deflate all input?");
            }
        }

        #endregion Deflation Support

        #region Stream Overrides

        /// <summary>
        /// Gets value indicating stream can be read from
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating if seeking is supported for this stream
        /// This property always returns false
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Get value indicating if this stream supports writing
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return baseOutputStream_.CanWrite;
            }
        }

        /// <summary>
        /// Get current length of stream
        /// </summary>
        public override long Length
        {
            get
            {
                return baseOutputStream_.Length;
            }
        }

        /// <summary>
        /// Gets the current position within the stream.
        /// </summary>
        /// <exception cref="NotSupportedException">Any attempt to set position</exception>
        public override long Position
        {
            get
            {
                return baseOutputStream_.Position;
            }
            set
            {
                throw new NotSupportedException("Position property not supported");
            }
        }

        /// <summary>
        /// Sets the current position of this stream to the given value. Not supported by this class!
        /// </summary>
        /// <param name="offset">The offset relative to the <paramref name="origin"/> to seek.</param>
        /// <param name="origin">The <see cref="SeekOrigin"/> to seek from.</param>
        /// <returns>The new position in the stream.</returns>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("DeflaterOutputStream Seek not supported");
        }

        /// <summary>
        /// Sets the length of this stream to the given value. Not supported by this class!
        /// </summary>
        /// <param name="value">The new stream length.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("DeflaterOutputStream SetLength not supported");
        }

        /// <summary>
        /// Read a byte from stream advancing position by one
        /// </summary>
        /// <returns>The byte read cast to an int.  THe value is -1 if at the end of the stream.</returns>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override int ReadByte()
        {
            throw new NotSupportedException("DeflaterOutputStream ReadByte not supported");
        }

        /// <summary>
        /// Read a block of bytes from stream
        /// </summary>
        /// <param name="buffer">The buffer to store read data in.</param>
        /// <param name="offset">The offset to start storing at.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The actual number of bytes read.  Zero if end of stream is detected.</returns>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("DeflaterOutputStream Read not supported");
        }

        /// <summary>
        /// Flushes the stream by calling <see cref="Flush">Flush</see> on the deflater and then
        /// on the underlying stream.  This ensures that all bytes are flushed.
        /// </summary>
        public override void Flush()
        {
            deflater_.Flush();
            Deflate(true);
            baseOutputStream_.Flush();
        }

        /// <summary>
        /// Calls <see cref="Finish"/> and closes the underlying
        /// stream when <see cref="IsStreamOwner"></see> is true.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!isClosed_)
            {
                isClosed_ = true;

                try
                {
                    Finish();
                    if (cryptoTransform_ != null)
                    {
                        GetAuthCodeIfAES();
                        cryptoTransform_.Dispose();
                        cryptoTransform_ = null;
                    }
                }
                finally
                {
                    if (IsStreamOwner)
                    {
                        baseOutputStream_.Dispose();
                    }
                }
            }
        }

#if NETSTANDARD2_1
		/// <summary>
		/// Calls <see cref="FinishAsync"/> and closes the underlying
		/// stream when <see cref="IsStreamOwner"></see> is true.
		/// </summary>
		public override async ValueTask DisposeAsync()
		{
			if (!isClosed_)
			{
				isClosed_ = true;

				try
				{
					await FinishAsync(CancellationToken.None);
					if (cryptoTransform_ != null)
					{
						GetAuthCodeIfAES();
						cryptoTransform_.Dispose();
						cryptoTransform_ = null;
					}
				}
				finally
				{
					if (IsStreamOwner)
					{
						await baseOutputStream_.DisposeAsync();
					}
				}
			}
		}
#endif

        /// <summary>
        /// Get the Auth code for AES encrypted entries
        /// </summary>
        protected void GetAuthCodeIfAES()
        {
            if (cryptoTransform_ is ZipAESTransform)
            {
                AESAuthCode = ((ZipAESTransform)cryptoTransform_).GetAuthCode();
            }
        }

        /// <summary>
        /// Writes a single byte to the compressed output stream.
        /// </summary>
        /// <param name="value">
        /// The byte value.
        /// </param>
        public override void WriteByte(byte value)
        {
            byte[] b = new byte[1];
            b[0] = value;
            Write(b, 0, 1);
        }

        /// <summary>
        /// Writes bytes from an array to the compressed stream.
        /// </summary>
        /// <param name="buffer">
        /// The byte array
        /// </param>
        /// <param name="offset">
        /// The offset into the byte array where to start.
        /// </param>
        /// <param name="count">
        /// The number of bytes to write.
        /// </param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            deflater_.SetInput(buffer, offset, count);
            Deflate();
        }

        #endregion Stream Overrides

        #region Instance Fields

        /// <summary>
        /// This buffer is used temporarily to retrieve the bytes from the
        /// deflater and write them to the underlying output stream.
        /// </summary>
        private byte[] buffer_;

        /// <summary>
        /// The deflater which is used to deflate the stream.
        /// </summary>
        protected Deflater deflater_;

        /// <summary>
        /// Base stream the deflater depends on.
        /// </summary>
        protected Stream baseOutputStream_;

        private bool isClosed_;

        #endregion Instance Fields
    }
    /// <summary>
    /// An example class to demonstrate compression and decompression of GZip streams.
    /// </summary>
    public static class GZip
    {
        /// <summary>
        /// Decompress the <paramref name="inStream">input</paramref> writing
        /// uncompressed data to the <paramref name="outStream">output stream</paramref>
        /// </summary>
        /// <param name="inStream">The readable stream containing data to decompress.</param>
        /// <param name="outStream">The output stream to receive the decompressed data.</param>
        /// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
        /// <exception cref="ArgumentNullException">Input or output stream is null</exception>
        public static void Decompress(Stream inStream, Stream outStream, bool isStreamOwner)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream), "Input stream is null");

            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream), "Output stream is null");

            try
            {
                using (GZipInputStream gzipInput = new GZipInputStream(inStream))
                {
                    gzipInput.IsStreamOwner = isStreamOwner;
                    StreamUtils.Copy(gzipInput, outStream, new byte[4096]);
                }
            }
            finally
            {
                if (isStreamOwner)
                {
                    // inStream is closed by the GZipInputStream if stream owner
                    outStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Compress the <paramref name="inStream">input stream</paramref> sending
        /// result data to <paramref name="outStream">output stream</paramref>
        /// </summary>
        /// <param name="inStream">The readable stream to compress.</param>
        /// <param name="outStream">The output stream to receive the compressed data.</param>
        /// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
        /// <param name="bufferSize">Deflate buffer size, minimum 512</param>
        /// <param name="level">Deflate compression level, 0-9</param>
        /// <exception cref="ArgumentNullException">Input or output stream is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Buffer Size is smaller than 512</exception>
        /// <exception cref="ArgumentOutOfRangeException">Compression level outside 0-9</exception>
        public static void Compress(Stream inStream, Stream outStream, bool isStreamOwner, int bufferSize = 512, int level = 6)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream), "Input stream is null");

            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream), "Output stream is null");

            if (bufferSize < 512)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Deflate buffer size must be >= 512");

            if (level < 0 || level > 9)
                throw new ArgumentOutOfRangeException(nameof(level), "Compression level must be 0-9");

            try
            {
                using (GZipOutputStream gzipOutput = new GZipOutputStream(outStream, bufferSize))
                {
                    gzipOutput.SetLevel(level);
                    gzipOutput.IsStreamOwner = isStreamOwner;
                    StreamUtils.Copy(inStream, gzipOutput, new byte[bufferSize]);
                }
            }
            finally
            {
                if (isStreamOwner)
                {
                    // outStream is closed by the GZipOutputStream if stream owner
                    inStream.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// This class contains constants used for gzip.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "kept for backwards compatibility")]
    sealed public class GZipConstants
    {
        /// <summary>
        /// First GZip identification byte
        /// </summary>
        public const byte ID1 = 0x1F;

        /// <summary>
        /// Second GZip identification byte
        /// </summary>
        public const byte ID2 = 0x8B;

        /// <summary>
        /// Deflate compression method
        /// </summary>
        public const byte CompressionMethodDeflate = 0x8;

        /// <summary>
        /// Get the GZip specified encoding (CP-1252 if supported, otherwise ASCII)
        /// </summary>
        public static Encoding Encoding
        {
            get
            {
                try
                {
                    return Encoding.GetEncoding(1252);
                }
                catch
                {
                    return Encoding.ASCII;
                }
            }
        }

    }

    /// <summary>
    /// GZip header flags
    /// </summary>
    [Flags]
    public enum GZipFlags : byte
    {
        /// <summary>
        /// Text flag hinting that the file is in ASCII
        /// </summary>
        FTEXT = 0x1 << 0,

        /// <summary>
        /// CRC flag indicating that a CRC16 preceeds the data
        /// </summary>
        FHCRC = 0x1 << 1,

        /// <summary>
        /// Extra flag indicating that extra fields are present
        /// </summary>
        FEXTRA = 0x1 << 2,

        /// <summary>
        /// Filename flag indicating that the original filename is present
        /// </summary>
        FNAME = 0x1 << 3,

        /// <summary>
        /// Flag bit mask indicating that a comment is present
        /// </summary>
        FCOMMENT = 0x1 << 4,
    }

    /// <summary>
    /// GZipException represents exceptions specific to GZip classes and code.
    /// </summary>
    [Serializable]
    public class GZipException : SharpZipBaseException
    {
        /// <summary>
        /// Initialise a new instance of <see cref="GZipException" />.
        /// </summary>
        public GZipException()
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="GZipException" /> with its message string.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that describes the error.</param>
        public GZipException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="GZipException" />.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that describes the error.</param>
        /// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
        public GZipException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the GZipException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected GZipException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// This filter stream is used to decompress a "GZIP" format stream.
    /// The "GZIP" format is described baseInputStream RFC 1952.
    ///
    /// author of the original java version : John Leuner
    /// </summary>
    /// <example> This sample shows how to unzip a gzipped file
    /// <code>
    /// using System;
    /// using System.IO;
    ///
    /// using ICSharpCode.SharpZipLib.Core;
    /// using ICSharpCode.SharpZipLib.GZip;
    ///
    /// class MainClass
    /// {
    /// 	public static void Main(string[] args)
    /// 	{
    ///			using (Stream inStream = new GZipInputStream(File.OpenRead(args[0])))
    ///			using (FileStream outStream = File.Create(Path.GetFileNameWithoutExtension(args[0]))) {
    ///				byte[] buffer = new byte[4096];
    ///				StreamUtils.Copy(inStream, outStream, buffer);
    /// 		}
    /// 	}
    /// }
    /// </code>
    /// </example>
    public class GZipInputStream : InflaterInputStream
    {
        #region Instance Fields

        /// <summary>
        /// CRC-32 value for uncompressed data
        /// </summary>
        protected Crc32 crc;

        /// <summary>
        /// Flag to indicate if we've read the GZIP header yet for the current member (block of compressed data).
        /// This is tracked per-block as the file is parsed.
        /// </summary>
        private bool readGZIPHeader;

        /// <summary>
        /// Flag to indicate if at least one block in a stream with concatenated blocks was read successfully.
        /// This allows us to exit gracefully if downstream data is not in gzip format.
        /// </summary>
        private bool completedLastBlock;

        private string fileName;

        #endregion Instance Fields

        #region Constructors

        /// <summary>
        /// Creates a GZipInputStream with the default buffer size
        /// </summary>
        /// <param name="baseInputStream">
        /// The stream to read compressed data from (baseInputStream GZIP format)
        /// </param>
        public GZipInputStream(Stream baseInputStream)
            : this(baseInputStream, 4096)
        {
        }

        /// <summary>
        /// Creates a GZIPInputStream with the specified buffer size
        /// </summary>
        /// <param name="baseInputStream">
        /// The stream to read compressed data from (baseInputStream GZIP format)
        /// </param>
        /// <param name="size">
        /// Size of the buffer to use
        /// </param>
        public GZipInputStream(Stream baseInputStream, int size)
            : base(baseInputStream, new Inflater(true), size)
        {
        }

        #endregion Constructors

        #region Stream overrides

        /// <summary>
        /// Reads uncompressed data into an array of bytes
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read uncompressed data into
        /// </param>
        /// <param name="offset">
        /// The offset indicating where the data should be placed
        /// </param>
        /// <param name="count">
        /// The number of uncompressed bytes to be read
        /// </param>
        /// <returns>Returns the number of bytes actually read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // A GZIP file can contain multiple blocks of compressed data, although this is quite rare.
            // A compressed block could potentially be empty, so we need to loop until we reach EOF or
            // we find data.
            while (true)
            {
                // If we haven't read the header for this block, read it
                if (!readGZIPHeader)
                {
                    // Try to read header. If there is no header (0 bytes available), this is EOF. If there is
                    // an incomplete header, this will throw an exception.
                    try
                    {
                        if (!ReadHeader())
                        {
                            return 0;
                        }
                    }
                    catch (Exception ex) when (completedLastBlock && (ex is GZipException || ex is EndOfStreamException))
                    {
                        // if we completed the last block (i.e. we're in a stream that has multiple blocks concatenated
                        // we want to return gracefully from any header parsing exceptions since sometimes there may
                        // be trailing garbage on a stream
                        return 0;
                    }
                }

                // Try to read compressed data
                int bytesRead = base.Read(buffer, offset, count);
                if (bytesRead > 0)
                {
                    crc.Update(new ArraySegment<byte>(buffer, offset, bytesRead));
                }

                // If this is the end of stream, read the footer
                if (inf.IsFinished)
                {
                    ReadFooter();
                }

                // Attempting to read 0 bytes will never yield any bytesRead, so we return instead of looping forever
                if (bytesRead > 0 || count == 0)
                {
                    return bytesRead;
                }
            }
        }

        /// <summary>
        /// Retrieves the filename header field for the block last read
        /// </summary>
        /// <returns></returns>
        public string GetFilename()
        {
            return fileName;
        }

        #endregion Stream overrides

        #region Support routines

        private bool ReadHeader()
        {
            // Initialize CRC for this block
            crc = new Crc32();

            // Make sure there is data in file. We can't rely on ReadLeByte() to fill the buffer, as this could be EOF,
            // which is fine, but ReadLeByte() throws an exception if it doesn't find data, so we do this part ourselves.
            if (inputBuffer.Available <= 0)
            {
                inputBuffer.Fill();
                if (inputBuffer.Available <= 0)
                {
                    // No header, EOF.
                    return false;
                }
            }

            var headCRC = new Crc32();

            // 1. Check the two magic bytes

            var magic = inputBuffer.ReadLeByte();
            headCRC.Update(magic);
            if (magic != GZipConstants.ID1)
            {
                throw new GZipException("Error GZIP header, first magic byte doesn't match");
            }

            magic = inputBuffer.ReadLeByte();
            if (magic != GZipConstants.ID2)
            {
                throw new GZipException("Error GZIP header,  second magic byte doesn't match");
            }
            headCRC.Update(magic);

            // 2. Check the compression type (must be 8)
            var compressionType = inputBuffer.ReadLeByte();

            if (compressionType != GZipConstants.CompressionMethodDeflate)
            {
                throw new GZipException("Error GZIP header, data not in deflate format");
            }
            headCRC.Update(compressionType);

            // 3. Check the flags
            var flagsByte = inputBuffer.ReadLeByte();

            headCRC.Update(flagsByte);

            // 3.1 Check the reserved bits are zero

            if ((flagsByte & 0xE0) != 0)
            {
                throw new GZipException("Reserved flag bits in GZIP header != 0");
            }

            var flags = (GZipFlags)flagsByte;

            // 4.-6. Skip the modification time, extra flags, and OS type
            for (int i = 0; i < 6; i++)
            {
                headCRC.Update(inputBuffer.ReadLeByte());
            }

            // 7. Read extra field
            if (flags.HasFlag(GZipFlags.FEXTRA))
            {
                // XLEN is total length of extra subfields, we will skip them all
                var len1 = inputBuffer.ReadLeByte();
                var len2 = inputBuffer.ReadLeByte();

                headCRC.Update(len1);
                headCRC.Update(len2);

                int extraLen = (len2 << 8) | len1;      // gzip is LSB first
                for (int i = 0; i < extraLen; i++)
                {
                    headCRC.Update(inputBuffer.ReadLeByte());
                }
            }

            // 8. Read file name
            if (flags.HasFlag(GZipFlags.FNAME))
            {
                var fname = new byte[1024];
                var fnamePos = 0;
                int readByte;
                while ((readByte = inputBuffer.ReadLeByte()) > 0)
                {
                    if (fnamePos < 1024)
                    {
                        fname[fnamePos++] = (byte)readByte;
                    }
                    headCRC.Update(readByte);
                }

                headCRC.Update(readByte);

                fileName = GZipConstants.Encoding.GetString(fname, 0, fnamePos);
            }
            else
            {
                fileName = null;
            }

            // 9. Read comment
            if (flags.HasFlag(GZipFlags.FCOMMENT))
            {
                int readByte;
                while ((readByte = inputBuffer.ReadLeByte()) > 0)
                {
                    headCRC.Update(readByte);
                }

                headCRC.Update(readByte);
            }

            // 10. Read header CRC
            if (flags.HasFlag(GZipFlags.FHCRC))
            {
                int tempByte;
                int crcval = inputBuffer.ReadLeByte();
                if (crcval < 0)
                {
                    throw new EndOfStreamException("EOS reading GZIP header");
                }

                tempByte = inputBuffer.ReadLeByte();
                if (tempByte < 0)
                {
                    throw new EndOfStreamException("EOS reading GZIP header");
                }

                crcval = (crcval << 8) | tempByte;
                if (crcval != ((int)headCRC.Value & 0xffff))
                {
                    throw new GZipException("Header CRC value mismatch");
                }
            }

            readGZIPHeader = true;
            return true;
        }

        private void ReadFooter()
        {
            byte[] footer = new byte[8];

            // End of stream; reclaim all bytes from inf, read the final byte count, and reset the inflator
            long bytesRead = inf.TotalOut & 0xffffffff;
            inputBuffer.Available += inf.RemainingInput;
            inf.Reset();

            // Read footer from inputBuffer
            int needed = 8;
            while (needed > 0)
            {
                int count = inputBuffer.ReadClearTextBuffer(footer, 8 - needed, needed);
                if (count <= 0)
                {
                    throw new EndOfStreamException("EOS reading GZIP footer");
                }
                needed -= count; // Jewel Jan 16
            }

            // Calculate CRC
            int crcval = (footer[0] & 0xff) | ((footer[1] & 0xff) << 8) | ((footer[2] & 0xff) << 16) | (footer[3] << 24);
            if (crcval != (int)crc.Value)
            {
                throw new GZipException("GZIP crc sum mismatch, theirs \"" + crcval + "\" and ours \"" + (int)crc.Value);
            }

            // NOTE The total here is the original total modulo 2 ^ 32.
            uint total =
                (uint)((uint)footer[4] & 0xff) |
                (uint)(((uint)footer[5] & 0xff) << 8) |
                (uint)(((uint)footer[6] & 0xff) << 16) |
                (uint)((uint)footer[7] << 24);

            if (bytesRead != total)
            {
                throw new GZipException("Number of bytes mismatch in footer");
            }

            // Mark header read as false so if another header exists, we'll continue reading through the file
            readGZIPHeader = false;

            // Indicate that we succeeded on at least one block so we can exit gracefully if there is trailing garbage downstream
            completedLastBlock = true;
        }

        #endregion Support routines
    }

    /// <summary>
    /// This filter stream is used to compress a stream into a "GZIP" stream.
    /// The "GZIP" format is described in RFC 1952.
    ///
    /// author of the original java version : John Leuner
    /// </summary>
    /// <example> This sample shows how to gzip a file
    /// <code>
    /// using System;
    /// using System.IO;
    ///
    /// using ICSharpCode.SharpZipLib.GZip;
    /// using ICSharpCode.SharpZipLib.Core;
    ///
    /// class MainClass
    /// {
    /// 	public static void Main(string[] args)
    /// 	{
    /// 			using (Stream s = new GZipOutputStream(File.Create(args[0] + ".gz")))
    /// 			using (FileStream fs = File.OpenRead(args[0])) {
    /// 				byte[] writeData = new byte[4096];
    /// 				Streamutils.Copy(s, fs, writeData);
    /// 			}
    /// 		}
    /// 	}
    /// }
    /// </code>
    /// </example>
    public class GZipOutputStream : DeflaterOutputStream
    {
        private enum OutputState
        {
            Header,
            Footer,
            Finished,
            Closed,
        };

        #region Instance Fields

        /// <summary>
        /// CRC-32 value for uncompressed data
        /// </summary>
        protected Crc32 crc = new Crc32();

        private OutputState state_ = OutputState.Header;

        private string fileName;

        private GZipFlags flags = 0;

        #endregion Instance Fields

        #region Constructors

        /// <summary>
        /// Creates a GzipOutputStream with the default buffer size
        /// </summary>
        /// <param name="baseOutputStream">
        /// The stream to read data (to be compressed) from
        /// </param>
        public GZipOutputStream(Stream baseOutputStream)
            : this(baseOutputStream, 4096)
        {
        }

        /// <summary>
        /// Creates a GZipOutputStream with the specified buffer size
        /// </summary>
        /// <param name="baseOutputStream">
        /// The stream to read data (to be compressed) from
        /// </param>
        /// <param name="size">
        /// Size of the buffer to use
        /// </param>
        public GZipOutputStream(Stream baseOutputStream, int size) : base(baseOutputStream, new Deflater(Deflater.DEFAULT_COMPRESSION, true), size)
        {
        }

        #endregion Constructors

        #region Public API

        /// <summary>
        /// Sets the active compression level (0-9).  The new level will be activated
        /// immediately.
        /// </summary>
        /// <param name="level">The compression level to set.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Level specified is not supported.
        /// </exception>
        /// <see cref="Deflater"/>
        public void SetLevel(int level)
        {
            if (level < Deflater.NO_COMPRESSION || level > Deflater.BEST_COMPRESSION)
                throw new ArgumentOutOfRangeException(nameof(level), "Compression level must be 0-9");

            deflater_.SetLevel(level);
        }

        /// <summary>
        /// Get the current compression level.
        /// </summary>
        /// <returns>The current compression level.</returns>
        public int GetLevel()
        {
            return deflater_.GetLevel();
        }

        /// <summary>
        /// Original filename
        /// </summary>
        public string FileName
        {
            get => fileName;
            set
            {
                fileName = CleanFilename(value);
                if (string.IsNullOrEmpty(fileName))
                {
                    flags &= ~GZipFlags.FNAME;
                }
                else
                {
                    flags |= GZipFlags.FNAME;
                }
            }
        }

        #endregion Public API

        #region Stream overrides

        /// <summary>
        /// Write given buffer to output updating crc
        /// </summary>
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset of first byte in buf to write</param>
        /// <param name="count">Number of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (state_ == OutputState.Header)
            {
                WriteHeader();
            }

            if (state_ != OutputState.Footer)
            {
                throw new InvalidOperationException("Write not permitted in current state");
            }

            crc.Update(new ArraySegment<byte>(buffer, offset, count));
            base.Write(buffer, offset, count);
        }

        /// <summary>
        /// Writes remaining compressed output data to the output stream
        /// and closes it.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                Finish();
            }
            finally
            {
                if (state_ != OutputState.Closed)
                {
                    state_ = OutputState.Closed;
                    if (IsStreamOwner)
                    {
                        baseOutputStream_.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Flushes the stream by ensuring the header is written, and then calling <see cref="DeflaterOutputStream.Flush">Flush</see>
        /// on the deflater.
        /// </summary>
        public override void Flush()
        {
            if (state_ == OutputState.Header)
            {
                WriteHeader();
            }

            base.Flush();
        }

        #endregion Stream overrides

        #region DeflaterOutputStream overrides

        /// <summary>
        /// Finish compression and write any footer information required to stream
        /// </summary>
        public override void Finish()
        {
            // If no data has been written a header should be added.
            if (state_ == OutputState.Header)
            {
                WriteHeader();
            }

            if (state_ == OutputState.Footer)
            {
                state_ = OutputState.Finished;
                base.Finish();

                var totalin = (uint)(deflater_.TotalIn & 0xffffffff);
                var crcval = (uint)(crc.Value & 0xffffffff);

                byte[] gzipFooter;

                unchecked
                {
                    gzipFooter = new byte[] {
                    (byte) crcval, (byte) (crcval >> 8),
                    (byte) (crcval >> 16), (byte) (crcval >> 24),

                    (byte) totalin, (byte) (totalin >> 8),
                    (byte) (totalin >> 16), (byte) (totalin >> 24)
                };
                }

                baseOutputStream_.Write(gzipFooter, 0, gzipFooter.Length);
            }
        }

        #endregion DeflaterOutputStream overrides

        #region Support Routines

        private static string CleanFilename(string path)
            => path.Substring(path.LastIndexOf('/') + 1);

        private void WriteHeader()
        {
            if (state_ == OutputState.Header)
            {
                state_ = OutputState.Footer;

                var mod_time = (int)((new DateTime(1970, 1, 1).Ticks) / 10000000L);  // Ticks give back 100ns intervals
                byte[] gzipHeader = {
					// The two magic bytes
					GZipConstants.ID1,
                    GZipConstants.ID2,

					// The compression type
					GZipConstants.CompressionMethodDeflate,

					// The flags (not set)
					(byte)flags,

					// The modification time
					(byte) mod_time, (byte) (mod_time >> 8),
                    (byte) (mod_time >> 16), (byte) (mod_time >> 24),

					// The extra flags
					0,

					// The OS type (unknown)
					255
                };

                baseOutputStream_.Write(gzipHeader, 0, gzipHeader.Length);

                if (flags.HasFlag(GZipFlags.FNAME))
                {
                    var fname = GZipConstants.Encoding.GetBytes(fileName);
                    baseOutputStream_.Write(fname, 0, fname.Length);

                    // End filename string with a \0
                    baseOutputStream_.Write(new byte[] { 0 }, 0, 1);
                }
            }
        }

        #endregion Support Routines
    }

    /// <summary>
	/// Interface to compute a data checksum used by checked input/output streams.
	/// A data checksum can be updated by one byte or with a byte array. After each
	/// update the value of the current checksum can be returned by calling
	/// <code>getValue</code>. The complete checksum object can also be reset
	/// so it can be used again with new data.
	/// </summary>
	public interface IChecksum
    {
        /// <summary>
        /// Resets the data checksum as if no update was ever called.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns the data checksum computed so far.
        /// </summary>
        long Value
        {
            get;
        }

        /// <summary>
        /// Adds one byte to the data checksum.
        /// </summary>
        /// <param name = "bval">
        /// the data value to add. The high byte of the int is ignored.
        /// </param>
        void Update(int bval);

        /// <summary>
        /// Updates the data checksum with the bytes taken from the array.
        /// </summary>
        /// <param name="buffer">
        /// buffer an array of bytes
        /// </param>
        void Update(byte[] buffer);

        /// <summary>
        /// Adds the byte array to the data checksum.
        /// </summary>
        /// <param name = "segment">
        /// The chunk of data to add
        /// </param>
        void Update(ArraySegment<byte> segment);
    }

    /// <summary>
    /// CRC-32 with reversed data and unreversed output
    /// </summary>
    /// <remarks>
    /// Generate a table for a byte-wise 32-bit CRC calculation on the polynomial:
    /// x^32+x^26+x^23+x^22+x^16+x^12+x^11+x^10+x^8+x^7+x^5+x^4+x^2+x^1+x^0.
    ///
    /// Polynomials over GF(2) are represented in binary, one bit per coefficient,
    /// with the lowest powers in the most significant bit.  Then adding polynomials
    /// is just exclusive-or, and multiplying a polynomial by x is a right shift by
    /// one.  If we call the above polynomial p, and represent a byte as the
    /// polynomial q, also with the lowest power in the most significant bit (so the
    /// byte 0xb1 is the polynomial x^7+x^3+x+1), then the CRC is (q*x^32) mod p,
    /// where a mod b means the remainder after dividing a by b.
    ///
    /// This calculation is done using the shift-register method of multiplying and
    /// taking the remainder.  The register is initialized to zero, and for each
    /// incoming bit, x^32 is added mod p to the register if the bit is a one (where
    /// x^32 mod p is p+x^32 = x^26+...+1), and the register is multiplied mod p by
    /// x (which is shifting right by one and adding x^32 mod p if the bit shifted
    /// out is a one).  We start with the highest power (least significant bit) of
    /// q and repeat for all eight bits of q.
    ///
    /// This implementation uses sixteen lookup tables stored in one linear array
    /// to implement the slicing-by-16 algorithm, a variant of the slicing-by-8
    /// algorithm described in this Intel white paper:
    ///
    /// https://web.archive.org/web/20120722193753/http://download.intel.com/technology/comms/perfnet/download/slicing-by-8.pdf
    ///
    /// The first lookup table is simply the CRC of all possible eight bit values.
    /// Each successive lookup table is derived from the original table generated
    /// by Sarwate's algorithm. Slicing a 16-bit input and XORing the outputs
    /// together will produce the same output as a byte-by-byte CRC loop with
    /// fewer arithmetic and bit manipulation operations, at the cost of increased
    /// memory consumed by the lookup tables. (Slicing-by-16 requires a 16KB table,
    /// which is still small enough to fit in most processors' L1 cache.)
    /// </remarks>
    public sealed class Crc32 : IChecksum
    {
        #region Instance Fields

        private static readonly uint crcInit = 0xFFFFFFFF;
        private static readonly uint crcXor = 0xFFFFFFFF;

        private static readonly uint[] crcTable = CrcUtilities.GenerateSlicingLookupTable(0xEDB88320, isReversed: true);

        /// <summary>
        /// The CRC data checksum so far.
        /// </summary>
        private uint checkValue;

        #endregion Instance Fields

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ComputeCrc32(uint oldCrc, byte bval)
        {
            return (uint)(Crc32.crcTable[(oldCrc ^ bval) & 0xFF] ^ (oldCrc >> 8));
        }

        /// <summary>
        /// Initialise a default instance of <see cref="Crc32"></see>
        /// </summary>
        public Crc32()
        {
            Reset();
        }

        /// <summary>
        /// Resets the CRC data checksum as if no update was ever called.
        /// </summary>
        public void Reset()
        {
            checkValue = crcInit;
        }

        /// <summary>
        /// Returns the CRC data checksum computed so far.
        /// </summary>
        /// <remarks>Reversed Out = false</remarks>
        public long Value
        {
            get
            {
                return (long)(checkValue ^ crcXor);
            }
        }

        /// <summary>
        /// Updates the checksum with the int bval.
        /// </summary>
        /// <param name = "bval">
        /// the byte is taken as the lower 8 bits of bval
        /// </param>
        /// <remarks>Reversed Data = true</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(int bval)
        {
            checkValue = unchecked(crcTable[(checkValue ^ bval) & 0xFF] ^ (checkValue >> 8));
        }

        /// <summary>
        /// Updates the CRC data checksum with the bytes taken from
        /// a block of data.
        /// </summary>
        /// <param name="buffer">Contains the data to update the CRC with.</param>
        public void Update(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Update(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Update CRC data checksum based on a portion of a block of data
        /// </summary>
        /// <param name = "segment">
        /// The chunk of data to add
        /// </param>
        public void Update(ArraySegment<byte> segment)
        {
            Update(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// Internal helper function for updating a block of data using slicing.
        /// </summary>
        /// <param name="data">The array containing the data to add</param>
        /// <param name="offset">Range start for <paramref name="data"/> (inclusive)</param>
        /// <param name="count">The number of bytes to checksum starting from <paramref name="offset"/></param>
        private void Update(byte[] data, int offset, int count)
        {
            int remainder = count % CrcUtilities.SlicingDegree;
            int end = offset + count - remainder;

            while (offset != end)
            {
                checkValue = CrcUtilities.UpdateDataForReversedPoly(data, offset, crcTable, checkValue);
                offset += CrcUtilities.SlicingDegree;
            }

            if (remainder != 0)
            {
                SlowUpdateLoop(data, offset, end + remainder);
            }
        }

        /// <summary>
        /// A non-inlined function for updating data that doesn't fit in a 16-byte
        /// block. We don't expect to enter this function most of the time, and when
        /// we do we're not here for long, so disabling inlining here improves
        /// performance overall.
        /// </summary>
        /// <param name="data">The array containing the data to add</param>
        /// <param name="offset">Range start for <paramref name="data"/> (inclusive)</param>
        /// <param name="end">Range end for <paramref name="data"/> (exclusive)</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SlowUpdateLoop(byte[] data, int offset, int end)
        {
            while (offset != end)
            {
                Update(data[offset++]);
            }
        }
    }

    internal static class CrcUtilities
    {
        /// <summary>
        /// The number of slicing lookup tables to generate.
        /// </summary>
        internal const int SlicingDegree = 16;

        /// <summary>
        /// Generates multiple CRC lookup tables for a given polynomial, stored
        /// in a linear array of uints. The first block (i.e. the first 256
        /// elements) is the same as the byte-by-byte CRC lookup table. 
        /// </summary>
        /// <param name="polynomial">The generating CRC polynomial</param>
        /// <param name="isReversed">Whether the polynomial is in reversed bit order</param>
        /// <returns>A linear array of 256 * <see cref="SlicingDegree"/> elements</returns>
        /// <remarks>
        /// This table could also be generated as a rectangular array, but the
        /// JIT compiler generates slower code than if we use a linear array.
        /// Known issue, see: https://github.com/dotnet/runtime/issues/30275
        /// </remarks>
        internal static uint[] GenerateSlicingLookupTable(uint polynomial, bool isReversed)
        {
            var table = new uint[256 * SlicingDegree];
            uint one = isReversed ? 1 : (1U << 31);

            for (int i = 0; i < 256; i++)
            {
                uint res = (uint)(isReversed ? i : i << 24);
                for (int j = 0; j < SlicingDegree; j++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        if (isReversed)
                        {
                            res = (res & one) == 1 ? polynomial ^ (res >> 1) : res >> 1;
                        }
                        else
                        {
                            res = (res & one) != 0 ? polynomial ^ (res << 1) : res << 1;
                        }
                    }

                    table[(256 * j) + i] = res;
                }
            }

            return table;
        }

        /// <summary>
        /// Mixes the first four bytes of input with <paramref name="checkValue"/>
        /// using normal ordering before calling <see cref="UpdateDataCommon"/>.
        /// </summary>
        /// <param name="input">Array of data to checksum</param>
        /// <param name="offset">Offset to start reading <paramref name="input"/> from</param>
        /// <param name="crcTable">The table to use for slicing-by-16 lookup</param>
        /// <param name="checkValue">Checksum state before this update call</param>
        /// <returns>A new unfinalized checksum value</returns>
        /// <seealso cref="UpdateDataForReversedPoly"/>
        /// <remarks>
        /// Assumes input[offset]..input[offset + 15] are valid array indexes.
        /// For performance reasons, this must be checked by the caller.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint UpdateDataForNormalPoly(byte[] input, int offset, uint[] crcTable, uint checkValue)
        {
            byte x1 = (byte)((byte)(checkValue >> 24) ^ input[offset]);
            byte x2 = (byte)((byte)(checkValue >> 16) ^ input[offset + 1]);
            byte x3 = (byte)((byte)(checkValue >> 8) ^ input[offset + 2]);
            byte x4 = (byte)((byte)checkValue ^ input[offset + 3]);

            return UpdateDataCommon(input, offset, crcTable, x1, x2, x3, x4);
        }

        /// <summary>
        /// Mixes the first four bytes of input with <paramref name="checkValue"/>
        /// using reflected ordering before calling <see cref="UpdateDataCommon"/>.
        /// </summary>
        /// <param name="input">Array of data to checksum</param>
        /// <param name="offset">Offset to start reading <paramref name="input"/> from</param>
        /// <param name="crcTable">The table to use for slicing-by-16 lookup</param>
        /// <param name="checkValue">Checksum state before this update call</param>
        /// <returns>A new unfinalized checksum value</returns>
        /// <seealso cref="UpdateDataForNormalPoly"/>
        /// <remarks>
        /// Assumes input[offset]..input[offset + 15] are valid array indexes.
        /// For performance reasons, this must be checked by the caller.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint UpdateDataForReversedPoly(byte[] input, int offset, uint[] crcTable, uint checkValue)
        {
            byte x1 = (byte)((byte)checkValue ^ input[offset]);
            byte x2 = (byte)((byte)(checkValue >>= 8) ^ input[offset + 1]);
            byte x3 = (byte)((byte)(checkValue >>= 8) ^ input[offset + 2]);
            byte x4 = (byte)((byte)(checkValue >>= 8) ^ input[offset + 3]);

            return UpdateDataCommon(input, offset, crcTable, x1, x2, x3, x4);
        }

        /// <summary>
        /// A shared method for updating an unfinalized CRC checksum using slicing-by-16.
        /// </summary>
        /// <param name="input">Array of data to checksum</param>
        /// <param name="offset">Offset to start reading <paramref name="input"/> from</param>
        /// <param name="crcTable">The table to use for slicing-by-16 lookup</param>
        /// <param name="x1">First byte of input after mixing with the old CRC</param>
        /// <param name="x2">Second byte of input after mixing with the old CRC</param>
        /// <param name="x3">Third byte of input after mixing with the old CRC</param>
        /// <param name="x4">Fourth byte of input after mixing with the old CRC</param>
        /// <returns>A new unfinalized checksum value</returns>
        /// <remarks>
        /// <para>
        /// Even though the first four bytes of input are fed in as arguments,
        /// <paramref name="offset"/> should be the same value passed to this
        /// function's caller (either <see cref="UpdateDataForNormalPoly"/> or
        /// <see cref="UpdateDataForReversedPoly"/>). This method will get inlined
        /// into both functions, so using the same offset produces faster code.
        /// </para>
        /// <para>
        /// Because most processors running C# have some kind of instruction-level
        /// parallelism, the order of XOR operations can affect performance. This
        /// ordering assumes that the assembly code generated by the just-in-time
        /// compiler will emit a bunch of arithmetic operations for checking array
        /// bounds. Then it opportunistically XORs a1 and a2 to keep the processor
        /// busy while those other parts of the pipeline handle the range check
        /// calculations.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint UpdateDataCommon(byte[] input, int offset, uint[] crcTable, byte x1, byte x2, byte x3, byte x4)
        {
            uint result;
            uint a1 = crcTable[x1 + 3840] ^ crcTable[x2 + 3584];
            uint a2 = crcTable[x3 + 3328] ^ crcTable[x4 + 3072];

            result = crcTable[input[offset + 4] + 2816];
            result ^= crcTable[input[offset + 5] + 2560];
            a1 ^= crcTable[input[offset + 9] + 1536];
            result ^= crcTable[input[offset + 6] + 2304];
            result ^= crcTable[input[offset + 7] + 2048];
            result ^= crcTable[input[offset + 8] + 1792];
            a2 ^= crcTable[input[offset + 13] + 512];
            result ^= crcTable[input[offset + 10] + 1280];
            result ^= crcTable[input[offset + 11] + 1024];
            result ^= crcTable[input[offset + 12] + 768];
            result ^= a1;
            result ^= crcTable[input[offset + 14] + 256];
            result ^= crcTable[input[offset + 15]];
            result ^= a2;

            return result;
        }
    }

    internal static class TarStringExtension
    {
        public static string ToTarArchivePath(this string s)
        {
            return PathUtils.DropPathRoot(s).Replace(Path.DirectorySeparatorChar, '/');
        }
    }
    /// <summary>
    /// The TarOutputStream writes a UNIX tar archive as an OutputStream.
    /// Methods are provided to put entries, and then write their contents
    /// by writing to this stream using write().
    /// </summary>
    /// public
    public class TarOutputStream : Stream
    {
        #region Constructors

        /// <summary>
        /// Construct TarOutputStream using default block factor
        /// </summary>
        /// <param name="outputStream">stream to write to</param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public TarOutputStream(Stream outputStream)
            : this(outputStream, TarBuffer.DefaultBlockFactor)
        {
        }

        /// <summary>
        /// Construct TarOutputStream using default block factor
        /// </summary>
        /// <param name="outputStream">stream to write to</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        public TarOutputStream(Stream outputStream, Encoding nameEncoding)
            : this(outputStream, TarBuffer.DefaultBlockFactor, nameEncoding)
        {
        }

        /// <summary>
        /// Construct TarOutputStream with user specified block factor
        /// </summary>
        /// <param name="outputStream">stream to write to</param>
        /// <param name="blockFactor">blocking factor</param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public TarOutputStream(Stream outputStream, int blockFactor)
        {
            this.outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            buffer = TarBuffer.CreateOutputTarBuffer(outputStream, blockFactor);

            assemblyBuffer = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
            blockBuffer = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
        }

        /// <summary>
        /// Construct TarOutputStream with user specified block factor
        /// </summary>
        /// <param name="outputStream">stream to write to</param>
        /// <param name="blockFactor">blocking factor</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        public TarOutputStream(Stream outputStream, int blockFactor, Encoding nameEncoding)
        {
            this.outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            buffer = TarBuffer.CreateOutputTarBuffer(outputStream, blockFactor);

            assemblyBuffer = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
            blockBuffer = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);

            this.nameEncoding = nameEncoding;
        }

        #endregion Constructors

        /// <summary>
        /// Gets or sets a flag indicating ownership of underlying stream.
        /// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        public bool IsStreamOwner
        {
            get => buffer.IsStreamOwner;
            set => buffer.IsStreamOwner = value;
        }

        /// <summary>
        /// true if the stream supports reading; otherwise, false.
        /// </summary>
        public override bool CanRead => outputStream.CanRead;

        /// <summary>
        /// true if the stream supports seeking; otherwise, false.
        /// </summary>
        public override bool CanSeek => outputStream.CanSeek;

        /// <summary>
        /// true if stream supports writing; otherwise, false.
        /// </summary>
        public override bool CanWrite => outputStream.CanWrite;

        /// <summary>
        /// length of stream in bytes
        /// </summary>
        public override long Length => outputStream.Length;

        /// <summary>
        /// gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get => outputStream.Position;
            set => outputStream.Position = value;
        }

        /// <summary>
        /// set the position within the current stream
        /// </summary>
        /// <param name="offset">The offset relative to the <paramref name="origin"/> to seek to</param>
        /// <param name="origin">The <see cref="SeekOrigin"/> to seek from.</param>
        /// <returns>The new position in the stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return outputStream.Seek(offset, origin);
        }

        /// <summary>
        /// Set the length of the current stream
        /// </summary>
        /// <param name="value">The new stream length.</param>
        public override void SetLength(long value)
        {
            outputStream.SetLength(value);
        }

        /// <summary>
        /// Read a byte from the stream and advance the position within the stream
        /// by one byte or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The byte value or -1 if at end of stream</returns>
        public override int ReadByte()
        {
            return outputStream.ReadByte();
        }

        /// <summary>
        /// read bytes from the current stream and advance the position within the
        /// stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to store read bytes in.</param>
        /// <param name="offset">The index into the buffer to being storing bytes at.</param>
        /// <param name="count">The desired number of bytes to read.</param>
        /// <returns>The total number of bytes read, or zero if at the end of the stream.
        /// The number of bytes may be less than the <paramref name="count">count</paramref>
        /// requested if data is not available.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return outputStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// read bytes from the current stream and advance the position within the
        /// stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to store read bytes in.</param>
        /// <param name="offset">The index into the buffer to being storing bytes at.</param>
        /// <param name="count">The desired number of bytes to read.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The total number of bytes read, or zero if at the end of the stream.
        /// The number of bytes may be less than the <paramref name="count">count</paramref>
        /// requested if data is not available.</returns>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CT cancellationToken)
        {
            return await outputStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>
        /// All buffered data is written to destination
        /// </summary>
        public override void Flush()
        {
            outputStream.Flush();
        }

        /// <summary>
        /// All buffered data is written to destination
        /// </summary>
        public override async Task FlushAsync(CT cancellationToken)
        {
            await outputStream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Ends the TAR archive without closing the underlying OutputStream.
        /// The result is that the EOF block of nulls is written.
        /// </summary>
        public void Finish()
        {
            FinishAsync(CT.None, false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ends the TAR archive without closing the underlying OutputStream.
        /// The result is that the EOF block of nulls is written.
        /// </summary>
        public Task FinishAsync(CT cancellationToken)
        {
            return FinishAsync(cancellationToken, true);
        }

        private async Task FinishAsync(CT cancellationToken, bool isAsync)
        {
            if (IsEntryOpen)
            {
                await CloseEntryAsync(cancellationToken, isAsync);
            }

            await WriteEofBlockAsync(cancellationToken, isAsync);
        }

        /// <summary>
        /// Ends the TAR archive and closes the underlying OutputStream.
        /// </summary>
        /// <remarks>This means that Finish() is called followed by calling the
        /// TarBuffer's Close().</remarks>
        protected override void Dispose(bool disposing)
        {
            if (!isClosed)
            {
                isClosed = true;
                Finish();
                buffer.Close();

                ArrayPool<byte>.Shared.Return(assemblyBuffer);
                ArrayPool<byte>.Shared.Return(blockBuffer);
            }
        }

        /// <summary>
        /// Get the record size being used by this stream's TarBuffer.
        /// </summary>
        public int RecordSize => buffer.RecordSize;

        /// <summary>
        /// Get the record size being used by this stream's TarBuffer.
        /// </summary>
        /// <returns>
        /// The TarBuffer record size.
        /// </returns>
        [Obsolete("Use RecordSize property instead")]
        public int GetRecordSize()
        {
            return buffer.RecordSize;
        }

        /// <summary>
        /// Get a value indicating whether an entry is open, requiring more data to be written.
        /// </summary>
        private bool IsEntryOpen => currBytes < currSize;

        /// <summary>
        /// Put an entry on the output stream. This writes the entry's
        /// header and positions the output stream for writing
        /// the contents of the entry. Once this method is called, the
        /// stream is ready for calls to write() to write the entry's
        /// contents. Once the contents are written, closeEntry()
        /// <B>MUST</B> be called to ensure that all buffered data
        /// is completely written to the output stream.
        /// </summary>
        /// <param name="entry">
        /// The TarEntry to be written to the archive.
        /// </param>
        /// <param name="cancellationToken"></param>
        public Task PutNextEntryAsync(TarEntry entry, CT cancellationToken)
        {
            return PutNextEntryAsync(entry, cancellationToken, true);
        }

        /// <summary>
        /// Put an entry on the output stream. This writes the entry's
        /// header and positions the output stream for writing
        /// the contents of the entry. Once this method is called, the
        /// stream is ready for calls to write() to write the entry's
        /// contents. Once the contents are written, closeEntry()
        /// <B>MUST</B> be called to ensure that all buffered data
        /// is completely written to the output stream.
        /// </summary>
        /// <param name="entry">
        /// The TarEntry to be written to the archive.
        /// </param>
        public void PutNextEntry(TarEntry entry)
        {
            PutNextEntryAsync(entry, CT.None, false).GetAwaiter().GetResult();
        }

        private async Task PutNextEntryAsync(TarEntry entry, CT cancellationToken, bool isAsync)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            int namelen = nameEncoding != null
                ? nameEncoding.GetByteCount(entry.TarHeader.Name)
                : entry.TarHeader.Name.Length;

            if (namelen > TarHeader.NAMELEN)
            {
                TarHeader longHeader = new()
                {
                    TypeFlag = TarHeader.LF_GNU_LONGNAME
                };
                longHeader.Name += "././@LongLink";
                longHeader.Mode = 420; //644 by default
                longHeader.UserId = entry.UserId;
                longHeader.GroupId = entry.GroupId;
                longHeader.GroupName = entry.GroupName;
                longHeader.UserName = entry.UserName;
                longHeader.LinkName = "";
                longHeader.Size = namelen + 1; // Plus one to avoid dropping last char

                longHeader.WriteHeader(blockBuffer, nameEncoding);
                // Add special long filename header block
                await buffer.WriteBlockAsync(blockBuffer, 0, cancellationToken, isAsync);

                int nameCharIndex = 0;

                while
                    (nameCharIndex <
                     namelen + 1 /* we've allocated one for the null char, now we must make sure it gets written out */)
                {
                    Array.Clear(blockBuffer, 0, blockBuffer.Length);
                    _ = TarHeader.GetAsciiBytes(entry.TarHeader.Name, nameCharIndex, blockBuffer, 0,
                        TarBuffer.BlockSize, nameEncoding); // This func handles OK the extra char out of string length
                    nameCharIndex += TarBuffer.BlockSize;

                    await buffer.WriteBlockAsync(blockBuffer, 0, cancellationToken, isAsync);
                }
            }

            entry.WriteEntryHeader(blockBuffer, nameEncoding);
            await buffer.WriteBlockAsync(blockBuffer, 0, cancellationToken, isAsync);

            currBytes = 0;

            currSize = entry.IsDirectory ? 0 : entry.Size;
        }

        /// <summary>
        /// Close an entry. This method MUST be called for all file
        /// entries that contain data. The reason is that we must
        /// buffer data written to the stream in order to satisfy
        /// the buffer's block based writes. Thus, there may be
        /// data fragments still being assembled that must be written
        /// to the output stream before this entry is closed and the
        /// next entry written.
        /// </summary>
        public Task CloseEntryAsync(CT cancellationToken)
        {
            return CloseEntryAsync(cancellationToken, true);
        }

        /// <summary>
        /// Close an entry. This method MUST be called for all file
        /// entries that contain data. The reason is that we must
        /// buffer data written to the stream in order to satisfy
        /// the buffer's block based writes. Thus, there may be
        /// data fragments still being assembled that must be written
        /// to the output stream before this entry is closed and the
        /// next entry written.
        /// </summary>
        public void CloseEntry()
        {
            CloseEntryAsync(CT.None, true).GetAwaiter().GetResult();
        }

        private async Task CloseEntryAsync(CT cancellationToken, bool isAsync)
        {
            if (assemblyBufferLength > 0)
            {
                Array.Clear(assemblyBuffer, assemblyBufferLength, assemblyBuffer.Length - assemblyBufferLength);

                await buffer.WriteBlockAsync(assemblyBuffer, 0, cancellationToken, isAsync);

                currBytes += assemblyBufferLength;
                assemblyBufferLength = 0;
            }

            if (currBytes < currSize)
            {
                string errorText = string.Format(
                    "Entry closed at '{0}' before the '{1}' bytes specified in the header were written",
                    currBytes, currSize);
                throw new TarException(errorText);
            }
        }

        /// <summary>
        /// Writes a byte to the current tar archive entry.
        /// This method simply calls Write(byte[], int, int).
        /// </summary>
        /// <param name="value">
        /// The byte to be written.
        /// </param>
        public override void WriteByte(byte value)
        {
            byte[] oneByteArray = ArrayPool<byte>.Shared.Rent(1);
            oneByteArray[0] = value;
            Write(oneByteArray, 0, 1);
            ArrayPool<byte>.Shared.Return(oneByteArray);
        }

        /// <summary>
        /// Writes bytes to the current tar archive entry. This method
        /// is aware of the current entry and will throw an exception if
        /// you attempt to write bytes past the length specified for the
        /// current entry. The method is also (painfully) aware of the
        /// record buffering required by TarBuffer, and manages buffers
        /// that are not a multiple of recordsize in length, including
        /// assembling records from small buffers.
        /// </summary>
        /// <param name = "buffer">
        /// The buffer to write to the archive.
        /// </param>
        /// <param name = "offset">
        /// The offset in the buffer from which to get bytes.
        /// </param>
        /// <param name = "count">
        /// The number of bytes to write.
        /// </param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, CT.None, false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Writes bytes to the current tar archive entry. This method
        /// is aware of the current entry and will throw an exception if
        /// you attempt to write bytes past the length specified for the
        /// current entry. The method is also (painfully) aware of the
        /// record buffering required by TarBuffer, and manages buffers
        /// that are not a multiple of recordsize in length, including
        /// assembling records from small buffers.
        /// </summary>
        /// <param name = "buffer">
        /// The buffer to write to the archive.
        /// </param>
        /// <param name = "offset">
        /// The offset in the buffer from which to get bytes.
        /// </param>
        /// <param name = "count">
        /// The number of bytes to write.
        /// </param>
        /// <param name="cancellationToken"></param>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CT cancellationToken)
        {
            return WriteAsync(buffer, offset, count, cancellationToken, true);
        }

        private async Task WriteAsync(byte[] buffer, int offset, int count, CT cancellationToken, bool isAsync)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Cannot be negative");
            }

            if (buffer.Length - offset < count)
            {
                throw new ArgumentException("offset and count combination is invalid");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot be negative");
            }

            if (currBytes + count > currSize)
            {
                string errorText = string.Format("request to write '{0}' bytes exceeds size in header of '{1}' bytes",
                    count, currSize);
                throw new ArgumentOutOfRangeException(nameof(count), errorText);
            }

            //
            // We have to deal with assembly!!!
            // The programmer can be writing little 32 byte chunks for all
            // we know, and we must assemble complete blocks for writing.
            // TODO  REVIEW Maybe this should be in TarBuffer? Could that help to
            //        eliminate some of the buffer copying.
            //
            if (assemblyBufferLength > 0)
            {
                if (assemblyBufferLength + count >= blockBuffer.Length)
                {
                    int aLen = blockBuffer.Length - assemblyBufferLength;

                    Array.Copy(assemblyBuffer, 0, blockBuffer, 0, assemblyBufferLength);
                    Array.Copy(buffer, offset, blockBuffer, assemblyBufferLength, aLen);

                    await this.buffer.WriteBlockAsync(blockBuffer, 0, cancellationToken, isAsync);

                    currBytes += blockBuffer.Length;

                    offset += aLen;
                    count -= aLen;

                    assemblyBufferLength = 0;
                }
                else
                {
                    Array.Copy(buffer, offset, assemblyBuffer, assemblyBufferLength, count);
                    offset += count;
                    assemblyBufferLength += count;
                    count -= count;
                }
            }

            //
            // When we get here we have EITHER:
            //   o An empty "assembly" buffer.
            //   o No bytes to write (count == 0)
            //
            while (count > 0)
            {
                if (count < blockBuffer.Length)
                {
                    Array.Copy(buffer, offset, assemblyBuffer, assemblyBufferLength, count);
                    assemblyBufferLength += count;
                    break;
                }

                await this.buffer.WriteBlockAsync(buffer, offset, cancellationToken, isAsync);

                int bufferLength = blockBuffer.Length;
                currBytes += bufferLength;
                count -= bufferLength;
                offset += bufferLength;
            }
        }

        /// <summary>
        /// Write an EOF (end of archive) block to the tar archive.
        /// The	end of the archive is indicated	by two blocks consisting entirely of zero bytes.
        /// </summary>
        private async Task WriteEofBlockAsync(CT cancellationToken, bool isAsync)
        {
            Array.Clear(blockBuffer, 0, blockBuffer.Length);
            await buffer.WriteBlockAsync(blockBuffer, 0, cancellationToken, isAsync);
            await buffer.WriteBlockAsync(blockBuffer, 0, cancellationToken, isAsync);
        }

        #region Instance Fields

        /// <summary>
        /// bytes written for this entry so far
        /// </summary>
        private long currBytes;

        /// <summary>
        /// current 'Assembly' buffer length
        /// </summary>
        private int assemblyBufferLength;

        /// <summary>
        /// Flag indicating whether this instance has been closed or not.
        /// </summary>
        private bool isClosed;

        /// <summary>
        /// Size for the current entry
        /// </summary>
        protected long currSize;

        /// <summary>
        /// single block working buffer
        /// </summary>
        protected byte[] blockBuffer;

        /// <summary>
        /// 'Assembly' buffer used to assemble data before writing
        /// </summary>
        protected byte[] assemblyBuffer;

        /// <summary>
        /// TarBuffer used to provide correct blocking factor
        /// </summary>
        protected TarBuffer buffer;

        /// <summary>
        /// the destination stream for the archive contents
        /// </summary>
        protected Stream outputStream;

        /// <summary>
        /// name encoding
        /// </summary>
        protected Encoding nameEncoding;

        #endregion Instance Fields
    }
    /// <summary>
    /// The TarInputStream reads a UNIX tar archive as an InputStream.
    /// methods are provided to position at each successive entry in
    /// the archive, and the read each entry as a normal input stream
    /// using read().
    /// </summary>
    public class TarInputStream : Stream
    {
        #region Constructors

        /// <summary>
        /// Construct a TarInputStream with default block factor
        /// </summary>
        /// <param name="inputStream">stream to source data from</param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public TarInputStream(Stream inputStream)
            : this(inputStream, TarBuffer.DefaultBlockFactor, null)
        {
        }

        /// <summary>
        /// Construct a TarInputStream with default block factor
        /// </summary>
        /// <param name="inputStream">stream to source data from</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        public TarInputStream(Stream inputStream, Encoding nameEncoding)
            : this(inputStream, TarBuffer.DefaultBlockFactor, nameEncoding)
        {
        }

        /// <summary>
        /// Construct a TarInputStream with user specified block factor
        /// </summary>
        /// <param name="inputStream">stream to source data from</param>
        /// <param name="blockFactor">block factor to apply to archive</param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public TarInputStream(Stream inputStream, int blockFactor)
        {
            this.inputStream = inputStream;
            tarBuffer = TarBuffer.CreateInputTarBuffer(inputStream, blockFactor);
            encoding = null;
        }

        /// <summary>
        /// Construct a TarInputStream with user specified block factor
        /// </summary>
        /// <param name="inputStream">stream to source data from</param>
        /// <param name="blockFactor">block factor to apply to archive</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        public TarInputStream(Stream inputStream, int blockFactor, Encoding nameEncoding)
        {
            this.inputStream = inputStream;
            tarBuffer = TarBuffer.CreateInputTarBuffer(inputStream, blockFactor);
            encoding = nameEncoding;
        }

        #endregion Constructors

        /// <summary>
        /// Gets or sets a flag indicating ownership of underlying stream.
        /// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        public bool IsStreamOwner
        {
            get => tarBuffer.IsStreamOwner;
            set => tarBuffer.IsStreamOwner = value;
        }

        #region Stream Overrides

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading
        /// </summary>
        public override bool CanRead => inputStream.CanRead;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking
        /// This property always returns false.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating if the stream supports writing.
        /// This property always returns false.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// The length in bytes of the stream
        /// </summary>
        public override long Length => inputStream.Length;

        /// <summary>
        /// Gets or sets the position within the stream.
        /// Setting the Position is not supported and throws a NotSupportedExceptionNotSupportedException
        /// </summary>
        /// <exception cref="NotSupportedException">Any attempt to set position</exception>
        public override long Position
        {
            get => inputStream.Position;
            set => throw new NotSupportedException("TarInputStream Seek not supported");
        }

        /// <summary>
        /// Flushes the baseInputStream
        /// </summary>
        public override void Flush()
        {
            inputStream.Flush();
        }

        /// <summary>
        /// Flushes the baseInputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        public override async Task FlushAsync(CT cancellationToken)
        {
            await inputStream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Set the streams position.  This operation is not supported and will throw a NotSupportedException
        /// </summary>
        /// <param name="offset">The offset relative to the origin to seek to.</param>
        /// <param name="origin">The <see cref="SeekOrigin"/> to start seeking from.</param>
        /// <returns>The new position in the stream.</returns>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("TarInputStream Seek not supported");
        }

        /// <summary>
        /// Sets the length of the stream
        /// This operation is not supported and will throw a NotSupportedException
        /// </summary>
        /// <param name="value">The new stream length.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("TarInputStream SetLength not supported");
        }

        /// <summary>
        /// Writes a block of bytes to this stream using data from a buffer.
        /// This operation is not supported and will throw a NotSupportedException
        /// </summary>
        /// <param name="buffer">The buffer containing bytes to write.</param>
        /// <param name="offset">The offset in the buffer of the frist byte to write.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("TarInputStream Write not supported");
        }

        /// <summary>
        /// Writes a byte to the current position in the file stream.
        /// This operation is not supported and will throw a NotSupportedException
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        /// <exception cref="NotSupportedException">Any access</exception>
        public override void WriteByte(byte value)
        {
            throw new NotSupportedException("TarInputStream WriteByte not supported");
        }

        /// <summary>
        /// Reads a byte from the current tar archive entry.
        /// </summary>
        /// <returns>A byte cast to an int; -1 if the at the end of the stream.</returns>
        public override int ReadByte()
        {
            byte[] oneByteBuffer = ArrayPool<byte>.Shared.Rent(1);
            int num = Read(oneByteBuffer, 0, 1);
            if (num <= 0)
            {
                // return -1 to indicate that no byte was read.
                return -1;
            }

            byte result = oneByteBuffer[0];
            ArrayPool<byte>.Shared.Return(oneByteBuffer);
            return result;
        }


        /// <summary>
        /// Reads bytes from the current tar archive entry.
        /// 
        /// This method is aware of the boundaries of the current
        /// entry in the archive and will deal with them appropriately
        /// </summary>
        /// <param name="buffer">
        /// The buffer into which to place bytes read.
        /// </param>
        /// <param name="offset">
        /// The offset at which to place bytes read.
        /// </param>
        /// <param name="count">
        /// The number of bytes to read.
        /// </param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// The number of bytes read, or 0 at end of stream/EOF.
        /// </returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CT cancellationToken)
        {
            return ReadAsync(buffer.AsMemory().Slice(offset, count), cancellationToken, true).AsTask();
        }

#if NETSTANDARD2_1_OR_GREATER
		/// <summary>
		/// Reads bytes from the current tar archive entry.
		/// 
		/// This method is aware of the boundaries of the current
		/// entry in the archive and will deal with them appropriately
		/// </summary>
		/// <param name="buffer">
		/// The buffer into which to place bytes read.
		/// </param>
		/// <param name="cancellationToken"></param>
		/// <returns>
		/// The number of bytes read, or 0 at end of stream/EOF.
		/// </returns>
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken =
			new CancellationToken())
		{
			return ReadAsync(buffer, cancellationToken, true);
		}
#endif

        /// <summary>
        /// Reads bytes from the current tar archive entry.
        ///
        /// This method is aware of the boundaries of the current
        /// entry in the archive and will deal with them appropriately
        /// </summary>
        /// <param name="buffer">
        /// The buffer into which to place bytes read.
        /// </param>
        /// <param name="offset">
        /// The offset at which to place bytes read.
        /// </param>
        /// <param name="count">
        /// The number of bytes to read.
        /// </param>
        /// <returns>
        /// The number of bytes read, or 0 at end of stream/EOF.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return buffer == null
                ? throw new ArgumentNullException(nameof(buffer))
                : ReadAsync(buffer.AsMemory().Slice(offset, count), CT.None, false).GetAwaiter()
                .GetResult();
        }

        private async ValueTask<int> ReadAsync(Memory<byte> buffer, CT ct, bool isAsync)
        {
            int offset = 0;
            int totalRead = 0;

            if (entryOffset >= entrySize)
            {
                return 0;
            }

            long numToRead = buffer.Length;

            if (numToRead + entryOffset > entrySize)
            {
                numToRead = entrySize - entryOffset;
            }

            if (readBuffer != null)
            {
                int sz = numToRead > readBuffer.Memory.Length ? readBuffer.Memory.Length : (int)numToRead;

                readBuffer.Memory[..sz].CopyTo(buffer.Slice(offset, sz));

                if (sz >= readBuffer.Memory.Length)
                {
                    readBuffer.Dispose();
                    readBuffer = null;
                }
                else
                {
                    int newLen = readBuffer.Memory.Length - sz;
                    IMemoryOwner<byte> newBuf = ExactMemoryPool<byte>.Shared.Rent(newLen);
                    readBuffer.Memory.Slice(sz, newLen).CopyTo(newBuf.Memory);
                    readBuffer.Dispose();

                    readBuffer = newBuf;
                }

                totalRead += sz;
                numToRead -= sz;
                offset += sz;
            }

            int recLen = TarBuffer.BlockSize;
            byte[] recBuf = ArrayPool<byte>.Shared.Rent(recLen);

            while (numToRead > 0)
            {
                await tarBuffer.ReadBlockIntAsync(recBuf, ct, isAsync);

                int sz = (int)numToRead;

                if (recLen > sz)
                {
                    recBuf.AsSpan()[..sz].CopyTo(buffer.Slice(offset, sz).Span);
                    readBuffer?.Dispose();

                    readBuffer = ExactMemoryPool<byte>.Shared.Rent(recLen - sz);
                    recBuf.AsSpan()[sz..recLen].CopyTo(readBuffer.Memory.Span);
                }
                else
                {
                    sz = recLen;
                    recBuf.AsSpan().CopyTo(buffer.Slice(offset, recLen).Span);
                }

                totalRead += sz;
                numToRead -= sz;
                offset += sz;
            }

            ArrayPool<byte>.Shared.Return(recBuf);

            entryOffset += totalRead;

            return totalRead;
        }

        /// <summary>
        /// Closes this stream. Calls the TarBuffer's close() method.
        /// The underlying stream is closed by the TarBuffer.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tarBuffer.Close();
            }
        }

#if NETSTANDARD2_1_OR_GREATER
		/// <summary>
		/// Closes this stream. Calls the TarBuffer's close() method.
		/// The underlying stream is closed by the TarBuffer.
		/// </summary>
		public override async ValueTask DisposeAsync()
		{
			await tarBuffer.CloseAsync(CancellationToken.None);
		}
#endif

        #endregion Stream Overrides

        /// <summary>
        /// Set the entry factory for this instance.
        /// </summary>
        /// <param name="factory">The factory for creating new entries</param>
        public void SetEntryFactory(IEntryFactory factory)
        {
            entryFactory = factory;
        }

        /// <summary>
        /// Get the record size being used by this stream's TarBuffer.
        /// </summary>
        public int RecordSize => tarBuffer.RecordSize;

        /// <summary>
        /// Get the record size being used by this stream's TarBuffer.
        /// </summary>
        /// <returns>
        /// TarBuffer record size.
        /// </returns>
        [Obsolete("Use RecordSize property instead")]
        public int GetRecordSize()
        {
            return tarBuffer.RecordSize;
        }

        /// <summary>
        /// Get the available data that can be read from the current
        /// entry in the archive. This does not indicate how much data
        /// is left in the entire archive, only in the current entry.
        /// This value is determined from the entry's size header field
        /// and the amount of data already read from the current entry.
        /// </summary>
        /// <returns>
        /// The number of available bytes for the current entry.
        /// </returns>
        public long Available => entrySize - entryOffset;

        /// <summary>
        /// Skip bytes in the input buffer. This skips bytes in the
        /// current entry's data, not the entire archive, and will
        /// stop at the end of the current entry's data if the number
        /// to skip extends beyond that point.
        /// </summary>
        /// <param name="skipCount">
        /// The number of bytes to skip.
        /// </param>
        /// <param name="ct"></param>
        private Task SkipAsync(long skipCount, CT ct)
        {
            return SkipAsync(skipCount, ct, true).AsTask();
        }

        /// <summary>
        /// Skip bytes in the input buffer. This skips bytes in the
        /// current entry's data, not the entire archive, and will
        /// stop at the end of the current entry's data if the number
        /// to skip extends beyond that point.
        /// </summary>
        /// <param name="skipCount">
        /// The number of bytes to skip.
        /// </param>
        private void Skip(long skipCount)
        {
            SkipAsync(skipCount, CT.None, false).GetAwaiter().GetResult();
        }

        private async ValueTask SkipAsync(long skipCount, CT ct, bool isAsync)
        {
            // TODO: REVIEW efficiency of TarInputStream.Skip
            // This is horribly inefficient, but it ensures that we
            // properly skip over bytes via the TarBuffer...
            //
            int length = 8 * 1024;
            using IMemoryOwner<byte> skipBuf = ExactMemoryPool<byte>.Shared.Rent(length);
            for (long num = skipCount; num > 0;)
            {
                int toRead = num > length ? length : (int)num;
                int numRead = await ReadAsync(skipBuf.Memory[..toRead], ct, isAsync);

                if (numRead == -1)
                {
                    break;
                }

                num -= numRead;
            }
        }

        /// <summary>
        /// Return a value of true if marking is supported; false otherwise.
        /// </summary>
        /// <remarks>Currently marking is not supported, the return value is always false.</remarks>
        public bool IsMarkSupported => false;

        /// <summary>
        /// Since we do not support marking just yet, we do nothing.
        /// </summary>
        /// <param name ="markLimit">
        /// The limit to mark.
        /// </param>
        public void Mark(int markLimit)
        {
        }

        /// <summary>
        /// Since we do not support marking just yet, we do nothing.
        /// </summary>
        public void Reset()
        {
        }

        /// <summary>
        /// Get the next entry in this tar archive. This will skip
        /// over any remaining data in the current entry, if there
        /// is one, and place the input stream at the header of the
        /// next entry, and read the header and instantiate a new
        /// TarEntry from the header bytes and return that entry.
        /// If there are no more entries in the archive, null will
        /// be returned to indicate that the end of the archive has
        /// been reached.
        /// </summary>
        /// <returns>
        /// The next TarEntry in the archive, or null.
        /// </returns>
        public Task<TarEntry> GetNextEntryAsync(CT ct)
        {
            return GetNextEntryAsync(ct, true).AsTask();
        }

        /// <summary>
        /// Get the next entry in this tar archive. This will skip
        /// over any remaining data in the current entry, if there
        /// is one, and place the input stream at the header of the
        /// next entry, and read the header and instantiate a new
        /// TarEntry from the header bytes and return that entry.
        /// If there are no more entries in the archive, null will
        /// be returned to indicate that the end of the archive has
        /// been reached.
        /// </summary>
        /// <returns>
        /// The next TarEntry in the archive, or null.
        /// </returns>
        public TarEntry GetNextEntry()
        {
            return GetNextEntryAsync(CT.None, true).GetAwaiter().GetResult();
        }

        private async ValueTask<TarEntry> GetNextEntryAsync(CT ct, bool isAsync)
        {
            if (hasHitEOF)
            {
                return null;
            }

            if (currentEntry != null)
            {
                await SkipToNextEntryAsync(ct, isAsync);
            }

            byte[] headerBuf = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
            await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);

            if (TarBuffer.IsEndOfArchiveBlock(headerBuf))
            {
                hasHitEOF = true;

                // Read the second zero-filled block
                await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);
            }
            else
            {
                hasHitEOF = false;
            }

            if (hasHitEOF)
            {
                currentEntry = null;
                readBuffer?.Dispose();
            }
            else
            {
                try
                {
                    TarHeader header = new();
                    header.ParseBuffer(headerBuf, encoding);
                    if (!header.IsChecksumValid)
                    {
                        throw new TarException("Header checksum is invalid");
                    }

                    entryOffset = 0;
                    entrySize = header.Size;

                    string longName = null;

                    if (header.TypeFlag == TarHeader.LF_GNU_LONGNAME)
                    {
                        using IMemoryOwner<byte> nameBuffer = ExactMemoryPool<byte>.Shared.Rent(TarBuffer.BlockSize);
                        long numToRead = entrySize;

                        StringBuilder longNameBuilder = StringBuilderPool.Instance.Rent();

                        while (numToRead > 0)
                        {
                            int length = numToRead > TarBuffer.BlockSize ? TarBuffer.BlockSize : (int)numToRead;
                            int numRead = await ReadAsync(nameBuffer.Memory[..length], ct, isAsync);

                            if (numRead == -1)
                            {
                                throw new InvalidHeaderException("Failed to read long name entry");
                            }

                            _ = longNameBuilder.Append(TarHeader.ParseName(nameBuffer.Memory[..numRead].Span,
                                encoding));
                            numToRead -= numRead;
                        }

                        longName = longNameBuilder.ToString();
                        StringBuilderPool.Instance.Return(longNameBuilder);

                        await SkipToNextEntryAsync(ct, isAsync);
                        await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);
                    }
                    else if (header.TypeFlag == TarHeader.LF_GHDR)
                    {
                        // POSIX global extended header
                        // Ignore things we dont understand completely for now
                        await SkipToNextEntryAsync(ct, isAsync);
                        await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);
                    }
                    else if (header.TypeFlag == TarHeader.LF_XHDR)
                    {
                        // POSIX extended header
                        byte[] nameBuffer = ArrayPool<byte>.Shared.Rent(TarBuffer.BlockSize);
                        long numToRead = entrySize;

                        TarExtendedHeaderReader xhr = new();

                        while (numToRead > 0)
                        {
                            int length = numToRead > nameBuffer.Length ? nameBuffer.Length : (int)numToRead;
                            int numRead = await ReadAsync(nameBuffer.AsMemory()[..length], ct, isAsync);

                            if (numRead == -1)
                            {
                                throw new InvalidHeaderException("Failed to read long name entry");
                            }

                            xhr.Read(nameBuffer, numRead);
                            numToRead -= numRead;
                        }

                        ArrayPool<byte>.Shared.Return(nameBuffer);

                        if (xhr.Headers.TryGetValue("path", out string name))
                        {
                            longName = name;
                        }

                        await SkipToNextEntryAsync(ct, isAsync);
                        await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);
                    }
                    else if (header.TypeFlag == TarHeader.LF_GNU_VOLHDR)
                    {
                        // TODO: could show volume name when verbose
                        await SkipToNextEntryAsync(ct, isAsync);
                        await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);
                    }
                    else if (header.TypeFlag is not TarHeader.LF_NORMAL and
                             not TarHeader.LF_OLDNORM and
                             not TarHeader.LF_LINK and
                             not TarHeader.LF_SYMLINK and
                             not TarHeader.LF_DIR)
                    {
                        // Ignore things we dont understand completely for now
                        await SkipToNextEntryAsync(ct, isAsync);
                        await tarBuffer.ReadBlockIntAsync(headerBuf, ct, isAsync);
                    }

                    if (entryFactory == null)
                    {
                        currentEntry = new TarEntry(headerBuf, encoding);
                        readBuffer?.Dispose();

                        if (longName != null)
                        {
                            currentEntry.Name = longName;
                        }
                    }
                    else
                    {
                        currentEntry = entryFactory.CreateEntry(headerBuf);
                        readBuffer?.Dispose();
                    }

                    // Magic was checked here for 'ustar' but there are multiple valid possibilities
                    // so this is not done anymore.

                    entryOffset = 0;

                    // TODO: Review How do we resolve this discrepancy?!
                    entrySize = currentEntry.Size;
                }
                catch (InvalidHeaderException ex)
                {
                    entrySize = 0;
                    entryOffset = 0;
                    currentEntry = null;
                    readBuffer?.Dispose();

                    string errorText = string.Format("Bad header in record {0} block {1} {2}",
                        tarBuffer.CurrentRecord, tarBuffer.CurrentBlock, ex.Message);
                    throw new InvalidHeaderException(errorText);
                }
            }

            ArrayPool<byte>.Shared.Return(headerBuf);

            return currentEntry;
        }

        /// <summary>
        /// Copies the contents of the current tar archive entry directly into
        /// an output stream.
        /// </summary>
        /// <param name="outputStream">
        /// The OutputStream into which to write the entry's data.
        /// </param>
        /// <param name="ct"></param>
        public Task CopyEntryContentsAsync(Stream outputStream, CT ct)
        {
            return CopyEntryContentsAsync(outputStream, ct, true).AsTask();
        }

        /// <summary>
        /// Copies the contents of the current tar archive entry directly into
        /// an output stream.
        /// </summary>
        /// <param name="outputStream">
        /// The OutputStream into which to write the entry's data.
        /// </param>
        public void CopyEntryContents(Stream outputStream)
        {
            CopyEntryContentsAsync(outputStream, CT.None, false).GetAwaiter().GetResult();
        }

        private async ValueTask CopyEntryContentsAsync(Stream outputStream, CT ct, bool isAsync)
        {
            byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(32 * 1024);

            while (true)
            {
                int numRead = await ReadAsync(tempBuffer, ct, isAsync);
                if (numRead <= 0)
                {
                    break;
                }

                if (isAsync)
                {
                    await outputStream.WriteAsync(tempBuffer, 0, numRead, ct);
                }
                else
                {
                    outputStream.Write(tempBuffer, 0, numRead);
                }
            }

            ArrayPool<byte>.Shared.Return(tempBuffer);
        }

        private async ValueTask SkipToNextEntryAsync(CT ct, bool isAsync)
        {
            long numToSkip = entrySize - entryOffset;

            if (numToSkip > 0)
            {
                await SkipAsync(numToSkip, ct, isAsync);
            }

            readBuffer?.Dispose();
            readBuffer = null;
        }

        /// <summary>
        /// This interface is provided, along with the method <see cref="SetEntryFactory"/>, to allow
        /// the programmer to have their own <see cref="TarEntry"/> subclass instantiated for the
        /// entries return from <see cref="GetNextEntry"/>.
        /// </summary>
        public interface IEntryFactory
        {
            // This interface does not considering name encoding.
            // How this interface should be?
            /// <summary>
            /// Create an entry based on name alone
            /// </summary>
            /// <param name="name">
            /// Name of the new EntryPointNotFoundException to create
            /// </param>
            /// <returns>created TarEntry or descendant class</returns>
            TarEntry CreateEntry(string name);

            /// <summary>
            /// Create an instance based on an actual file
            /// </summary>
            /// <param name="fileName">
            /// Name of file to represent in the entry
            /// </param>
            /// <returns>
            /// Created TarEntry or descendant class
            /// </returns>
            TarEntry CreateEntryFromFile(string fileName);

            /// <summary>
            /// Create a tar entry based on the header information passed
            /// </summary>
            /// <param name="headerBuffer">
            /// Buffer containing header information to create an entry from.
            /// </param>
            /// <returns>
            /// Created TarEntry or descendant class
            /// </returns>
            TarEntry CreateEntry(byte[] headerBuffer);
        }

        /// <summary>
        /// Standard entry factory class creating instances of the class TarEntry
        /// </summary>
        public class EntryFactoryAdapter : IEntryFactory
        {
            private readonly Encoding nameEncoding;

            /// <summary>
            /// Construct standard entry factory class with ASCII name encoding
            /// </summary>
            [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
            public EntryFactoryAdapter()
            {
            }

            /// <summary>
            /// Construct standard entry factory with name encoding
            /// </summary>
            /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
            public EntryFactoryAdapter(Encoding nameEncoding)
            {
                this.nameEncoding = nameEncoding;
            }

            /// <summary>
            /// Create a <see cref="TarEntry"/> based on named
            /// </summary>
            /// <param name="name">The name to use for the entry</param>
            /// <returns>A new <see cref="TarEntry"/></returns>
            public TarEntry CreateEntry(string name)
            {
                return TarEntry.CreateTarEntry(name);
            }

            /// <summary>
            /// Create a tar entry with details obtained from <paramref name="fileName">file</paramref>
            /// </summary>
            /// <param name="fileName">The name of the file to retrieve details from.</param>
            /// <returns>A new <see cref="TarEntry"/></returns>
            public TarEntry CreateEntryFromFile(string fileName)
            {
                return TarEntry.CreateEntryFromFile(fileName);
            }

            /// <summary>
            /// Create an entry based on details in <paramref name="headerBuffer">header</paramref>
            /// </summary>
            /// <param name="headerBuffer">The buffer containing entry details.</param>
            /// <returns>A new <see cref="TarEntry"/></returns>
            public TarEntry CreateEntry(byte[] headerBuffer)
            {
                return new TarEntry(headerBuffer, nameEncoding);
            }
        }

        #region Instance Fields

        /// <summary>
        /// Flag set when last block has been read
        /// </summary>
        protected bool hasHitEOF;

        /// <summary>
        /// Size of this entry as recorded in header
        /// </summary>
        protected long entrySize;

        /// <summary>
        /// Number of bytes read for this entry so far
        /// </summary>
        protected long entryOffset;

        /// <summary>
        /// Buffer used with calls to <code>Read()</code>
        /// </summary>
        protected IMemoryOwner<byte> readBuffer;

        /// <summary>
        /// Working buffer
        /// </summary>
        protected TarBuffer tarBuffer;

        /// <summary>
        /// Current entry being read
        /// </summary>
        private TarEntry currentEntry;

        /// <summary>
        /// Factory used to create TarEntry or descendant class instance
        /// </summary>
        protected IEntryFactory entryFactory;

        /// <summary>
        /// Stream used as the source of input data.
        /// </summary>
        private readonly Stream inputStream;

        private readonly Encoding encoding;

        #endregion Instance Fields
    }
    // <summary>
    /// This class encapsulates the Tar Entry Header used in Tar Archives.
    /// The class also holds a number of tar constants, used mostly in headers.
    /// </summary>
    /// <remarks>
    ///    The tar format and its POSIX successor PAX have a long history which makes for compatability
    ///    issues when creating and reading files.
    ///
    ///    This is further complicated by a large number of programs with variations on formats
    ///    One common issue is the handling of names longer than 100 characters.
    ///    GNU style long names are currently supported.
    ///
    /// This is the ustar (Posix 1003.1) header.
    ///
    /// struct header
    /// {
    /// 	char t_name[100];          //   0 Filename
    /// 	char t_mode[8];            // 100 Permissions
    /// 	char t_uid[8];             // 108 Numerical User ID
    /// 	char t_gid[8];             // 116 Numerical Group ID
    /// 	char t_size[12];           // 124 Filesize
    /// 	char t_mtime[12];          // 136 st_mtime
    /// 	char t_chksum[8];          // 148 Checksum
    /// 	char t_typeflag;           // 156 Type of File
    /// 	char t_linkname[100];      // 157 Target of Links
    /// 	char t_magic[6];           // 257 "ustar" or other...
    /// 	char t_version[2];         // 263 Version fixed to 00
    /// 	char t_uname[32];          // 265 User Name
    /// 	char t_gname[32];          // 297 Group Name
    /// 	char t_devmajor[8];        // 329 Major for devices
    /// 	char t_devminor[8];        // 337 Minor for devices
    /// 	char t_prefix[155];        // 345 Prefix for t_name
    /// 	char t_mfill[12];          // 500 Filler up to 512
    /// };
    /// </remarks>
    public class TarHeader
    {
        #region Constants

        /// <summary>
        /// The length of the name field in a header buffer.
        /// </summary>
        public const int NAMELEN = 100;

        /// <summary>
        /// The length of the mode field in a header buffer.
        /// </summary>
        public const int MODELEN = 8;

        /// <summary>
        /// The length of the user id field in a header buffer.
        /// </summary>
        public const int UIDLEN = 8;

        /// <summary>
        /// The length of the group id field in a header buffer.
        /// </summary>
        public const int GIDLEN = 8;

        /// <summary>
        /// The length of the checksum field in a header buffer.
        /// </summary>
        public const int CHKSUMLEN = 8;

        /// <summary>
        /// Offset of checksum in a header buffer.
        /// </summary>
        public const int CHKSUMOFS = 148;

        /// <summary>
        /// The length of the size field in a header buffer.
        /// </summary>
        public const int SIZELEN = 12;

        /// <summary>
        /// The length of the magic field in a header buffer.
        /// </summary>
        public const int MAGICLEN = 6;

        /// <summary>
        /// The length of the version field in a header buffer.
        /// </summary>
        public const int VERSIONLEN = 2;

        /// <summary>
        /// The length of the modification time field in a header buffer.
        /// </summary>
        public const int MODTIMELEN = 12;

        /// <summary>
        /// The length of the user name field in a header buffer.
        /// </summary>
        public const int UNAMELEN = 32;

        /// <summary>
        /// The length of the group name field in a header buffer.
        /// </summary>
        public const int GNAMELEN = 32;

        /// <summary>
        /// The length of the devices field in a header buffer.
        /// </summary>
        public const int DEVLEN = 8;

        /// <summary>
        /// The length of the name prefix field in a header buffer.
        /// </summary>
        public const int PREFIXLEN = 155;

        //
        // LF_ constants represent the "type" of an entry
        //

        /// <summary>
        ///  The "old way" of indicating a normal file.
        /// </summary>
        public const byte LF_OLDNORM = 0;

        /// <summary>
        /// Normal file type.
        /// </summary>
        public const byte LF_NORMAL = (byte)'0';

        /// <summary>
        /// Link file type.
        /// </summary>
        public const byte LF_LINK = (byte)'1';

        /// <summary>
        /// Symbolic link file type.
        /// </summary>
        public const byte LF_SYMLINK = (byte)'2';

        /// <summary>
        /// Character device file type.
        /// </summary>
        public const byte LF_CHR = (byte)'3';

        /// <summary>
        /// Block device file type.
        /// </summary>
        public const byte LF_BLK = (byte)'4';

        /// <summary>
        /// Directory file type.
        /// </summary>
        public const byte LF_DIR = (byte)'5';

        /// <summary>
        /// FIFO (pipe) file type.
        /// </summary>
        public const byte LF_FIFO = (byte)'6';

        /// <summary>
        /// Contiguous file type.
        /// </summary>
        public const byte LF_CONTIG = (byte)'7';

        /// <summary>
        /// Posix.1 2001 global extended header
        /// </summary>
        public const byte LF_GHDR = (byte)'g';

        /// <summary>
        /// Posix.1 2001 extended header
        /// </summary>
        public const byte LF_XHDR = (byte)'x';

        // POSIX allows for upper case ascii type as extensions

        /// <summary>
        /// Solaris access control list file type
        /// </summary>
        public const byte LF_ACL = (byte)'A';

        /// <summary>
        /// GNU dir dump file type
        /// This is a dir entry that contains the names of files that were in the
        /// dir at the time the dump was made
        /// </summary>
        public const byte LF_GNU_DUMPDIR = (byte)'D';

        /// <summary>
        /// Solaris Extended Attribute File
        /// </summary>
        public const byte LF_EXTATTR = (byte)'E';

        /// <summary>
        /// Inode (metadata only) no file content
        /// </summary>
        public const byte LF_META = (byte)'I';

        /// <summary>
        /// Identifies the next file on the tape as having a long link name
        /// </summary>
        public const byte LF_GNU_LONGLINK = (byte)'K';

        /// <summary>
        /// Identifies the next file on the tape as having a long name
        /// </summary>
        public const byte LF_GNU_LONGNAME = (byte)'L';

        /// <summary>
        /// Continuation of a file that began on another volume
        /// </summary>
        public const byte LF_GNU_MULTIVOL = (byte)'M';

        /// <summary>
        /// For storing filenames that dont fit in the main header (old GNU)
        /// </summary>
        public const byte LF_GNU_NAMES = (byte)'N';

        /// <summary>
        /// GNU Sparse file
        /// </summary>
        public const byte LF_GNU_SPARSE = (byte)'S';

        /// <summary>
        /// GNU Tape/volume header ignore on extraction
        /// </summary>
        public const byte LF_GNU_VOLHDR = (byte)'V';

        /// <summary>
        /// The magic tag representing a POSIX tar archive.  (would be written with a trailing NULL)
        /// </summary>
        public const string TMAGIC = "ustar";

        /// <summary>
        /// The magic tag representing an old GNU tar archive where version is included in magic and overwrites it
        /// </summary>
        public const string GNU_TMAGIC = "ustar  ";

        private const long timeConversionFactor = 10000000L; // 1 tick == 100 nanoseconds
        private static readonly DateTime dateTime1970 = new(1970, 1, 1, 0, 0, 0, 0);

        #endregion Constants

        #region Constructors

        /// <summary>
        /// Initialise a default TarHeader instance
        /// </summary>
        public TarHeader()
        {
            Magic = TMAGIC;
            Version = " ";

            Name = "";
            LinkName = "";

            UserId = defaultUserId;
            GroupId = defaultGroupId;
            UserName = defaultUser;
            GroupName = defaultGroupName;
            Size = 0;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Get/set the name for this tar entry.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when attempting to set the property to null.</exception>
        public string Name
        {
            get => name;
            set => name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Get the name of this entry.
        /// </summary>
        /// <returns>The entry's name.</returns>
        [Obsolete("Use the Name property instead", true)]
        public string GetName()
        {
            return name;
        }

        /// <summary>
        /// Get/set the entry's Unix style permission mode.
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// The entry's user id.
        /// </summary>
        /// <remarks>
        /// This is only directly relevant to unix systems.
        /// The default is zero.
        /// </remarks>
        public int UserId { get; set; }

        /// <summary>
        /// Get/set the entry's group id.
        /// </summary>
        /// <remarks>
        /// This is only directly relevant to linux/unix systems.
        /// The default value is zero.
        /// </remarks>
        public int GroupId { get; set; }

        /// <summary>
        /// Get/set the entry's size.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting the size to less than zero.</exception>
        public long Size
        {
            get => size;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Cannot be less than zero");
                }

                size = value;
            }
        }

        /// <summary>
        /// Get/set the entry's modification time.
        /// </summary>
        /// <remarks>
        /// The modification time is only accurate to within a second.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setting the date time to less than 1/1/1970.</exception>
        public DateTime ModTime
        {
            get => modTime;
            set
            {
                if (value < dateTime1970)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "ModTime cannot be before Jan 1st 1970");
                }

                modTime = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
            }
        }

        /// <summary>
        /// Get the entry's checksum.  This is only valid/updated after writing or reading an entry.
        /// </summary>
        public int Checksum { get; private set; }

        /// <summary>
        /// Get value of true if the header checksum is valid, false otherwise.
        /// </summary>
        public bool IsChecksumValid { get; private set; }

        /// <summary>
        /// Get/set the entry's type flag.
        /// </summary>
        public byte TypeFlag { get; set; }

        /// <summary>
        /// The entry's link name.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when attempting to set LinkName to null.</exception>
        public string LinkName
        {
            get => linkName;
            set => linkName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Get/set the entry's magic tag.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when attempting to set Magic to null.</exception>
        public string Magic
        {
            get => magic;
            set => magic = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The entry's version.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when attempting to set Version to null.</exception>
        public string Version
        {
            get => version;

            set => version = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The entry's user name.
        /// </summary>
        public string UserName
        {
            get => userName;
            set
            {
                if (value != null)
                {
                    userName = value[..Math.Min(UNAMELEN, value.Length)];
                }
                else
                {
                    string currentUser = "user";
                    if (currentUser.Length > UNAMELEN)
                    {
                        currentUser = currentUser[..UNAMELEN];
                    }

                    userName = currentUser;
                }
            }
        }

        /// <summary>
        /// Get/set the entry's group name.
        /// </summary>
        /// <remarks>
        /// This is only directly relevant to unix systems.
        /// </remarks>
        public string GroupName
        {
            get => groupName;
            set => groupName = value ?? "None";
        }

        /// <summary>
        /// Get/set the entry's major device number.
        /// </summary>
        public int DevMajor { get; set; }

        /// <summary>
        /// Get/set the entry's minor device number.
        /// </summary>
        public int DevMinor { get; set; }

        #endregion Properties

        #region ICloneable Members

        /// <summary>
        /// Create a new <see cref="TarHeader"/> that is a copy of the current instance.
        /// </summary>
        /// <returns>A new <see cref="object"/> that is a copy of the current instance.</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion ICloneable Members

        /// <summary>
        /// Parse TarHeader information from a header buffer.
        /// </summary>
        /// <param name = "header">
        /// The tar entry header buffer to get information from.
        /// </param>
        /// <param name = "nameEncoding">
        /// The <see cref="Encoding"/> used for the Name field, or null for ASCII only
        /// </param>
        public void ParseBuffer(byte[] header, Encoding nameEncoding)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            int offset = 0;
            Span<byte> headerSpan = header.AsSpan();

            name = ParseName(headerSpan.Slice(offset, NAMELEN), nameEncoding);
            offset += NAMELEN;

            Mode = (int)ParseOctal(header, offset, MODELEN);
            offset += MODELEN;

            UserId = (int)ParseOctal(header, offset, UIDLEN);
            offset += UIDLEN;

            GroupId = (int)ParseOctal(header, offset, GIDLEN);
            offset += GIDLEN;

            Size = ParseBinaryOrOctal(header, offset, SIZELEN);
            offset += SIZELEN;

            ModTime = GetDateTimeFromCTime(ParseOctal(header, offset, MODTIMELEN));
            offset += MODTIMELEN;

            Checksum = (int)ParseOctal(header, offset, CHKSUMLEN);
            offset += CHKSUMLEN;

            TypeFlag = header[offset++];

            LinkName = ParseName(headerSpan.Slice(offset, NAMELEN), nameEncoding);
            offset += NAMELEN;

            Magic = ParseName(headerSpan.Slice(offset, MAGICLEN), nameEncoding);
            offset += MAGICLEN;

            if (Magic == "ustar")
            {
                Version = ParseName(headerSpan.Slice(offset, VERSIONLEN), nameEncoding);
                offset += VERSIONLEN;

                UserName = ParseName(headerSpan.Slice(offset, UNAMELEN), nameEncoding);
                offset += UNAMELEN;

                GroupName = ParseName(headerSpan.Slice(offset, GNAMELEN), nameEncoding);
                offset += GNAMELEN;

                DevMajor = (int)ParseOctal(header, offset, DEVLEN);
                offset += DEVLEN;

                DevMinor = (int)ParseOctal(header, offset, DEVLEN);
                offset += DEVLEN;

                string prefix = ParseName(headerSpan.Slice(offset, PREFIXLEN), nameEncoding);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Name = prefix + '/' + Name;
                }
            }

            IsChecksumValid = Checksum == MakeCheckSum(header);
        }

        /// <summary>
        /// Parse TarHeader information from a header buffer.
        /// </summary>
        /// <param name = "header">
        /// The tar entry header buffer to get information from.
        /// </param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public void ParseBuffer(byte[] header)
        {
            ParseBuffer(header, null);
        }

        /// <summary>
        /// 'Write' header information to buffer provided, updating the <see cref="Checksum">check sum</see>.
        /// </summary>
        /// <param name="outBuffer">output buffer for header information</param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public void WriteHeader(byte[] outBuffer)
        {
            WriteHeader(outBuffer, null);
        }

        /// <summary>
        /// 'Write' header information to buffer provided, updating the <see cref="Checksum">check sum</see>.
        /// </summary>
        /// <param name="outBuffer">output buffer for header information</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name field, or null for ASCII only</param>
        public void WriteHeader(byte[] outBuffer, Encoding nameEncoding)
        {
            if (outBuffer == null)
            {
                throw new ArgumentNullException(nameof(outBuffer));
            }

            int offset = 0;

            offset = GetNameBytes(Name, outBuffer, offset, NAMELEN, nameEncoding);
            offset = GetOctalBytes(Mode, outBuffer, offset, MODELEN);
            offset = GetOctalBytes(UserId, outBuffer, offset, UIDLEN);
            offset = GetOctalBytes(GroupId, outBuffer, offset, GIDLEN);

            offset = GetBinaryOrOctalBytes(Size, outBuffer, offset, SIZELEN);
            offset = GetOctalBytes(GetCTime(ModTime), outBuffer, offset, MODTIMELEN);

            int csOffset = offset;
            for (int c = 0; c < CHKSUMLEN; ++c)
            {
                outBuffer[offset++] = (byte)' ';
            }

            outBuffer[offset++] = TypeFlag;

            offset = GetNameBytes(LinkName, outBuffer, offset, NAMELEN, nameEncoding);
            offset = GetAsciiBytes(Magic, 0, outBuffer, offset, MAGICLEN, nameEncoding);
            offset = GetNameBytes(Version, outBuffer, offset, VERSIONLEN, nameEncoding);
            offset = GetNameBytes(UserName, outBuffer, offset, UNAMELEN, nameEncoding);
            offset = GetNameBytes(GroupName, outBuffer, offset, GNAMELEN, nameEncoding);

            if (TypeFlag is LF_CHR or LF_BLK)
            {
                offset = GetOctalBytes(DevMajor, outBuffer, offset, DEVLEN);
                offset = GetOctalBytes(DevMinor, outBuffer, offset, DEVLEN);
            }

            for (; offset < outBuffer.Length;)
            {
                outBuffer[offset++] = 0;
            }

            Checksum = ComputeCheckSum(outBuffer);

            GetCheckSumOctalBytes(Checksum, outBuffer, csOffset, CHKSUMLEN);
            IsChecksumValid = true;
        }

        /// <summary>
        /// Get a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Determines if this instance is equal to the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns>true if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            bool result = obj is TarHeader localHeader
&& name == localHeader.name
                         && Mode == localHeader.Mode
                         && UserId == localHeader.UserId
                         && GroupId == localHeader.GroupId
                         && Size == localHeader.Size
                         && ModTime == localHeader.ModTime
                         && Checksum == localHeader.Checksum
                         && TypeFlag == localHeader.TypeFlag
                         && LinkName == localHeader.LinkName
                         && Magic == localHeader.Magic
                         && Version == localHeader.Version
                         && UserName == localHeader.UserName
                         && GroupName == localHeader.GroupName
                         && DevMajor == localHeader.DevMajor
                         && DevMinor == localHeader.DevMinor;
            return result;
        }

        /// <summary>
        /// Set defaults for values used when constructing a TarHeader instance.
        /// </summary>
        /// <param name="userId">Value to apply as a default for userId.</param>
        /// <param name="userName">Value to apply as a default for userName.</param>
        /// <param name="groupId">Value to apply as a default for groupId.</param>
        /// <param name="groupName">Value to apply as a default for groupName.</param>
        internal static void SetValueDefaults(int userId, string userName, int groupId, string groupName)
        {
            defaultUserId = userIdAsSet = userId;
            defaultUser = userNameAsSet = userName;
            defaultGroupId = groupIdAsSet = groupId;
            defaultGroupName = groupNameAsSet = groupName;
        }

        internal static void RestoreSetValues()
        {
            defaultUserId = userIdAsSet;
            defaultUser = userNameAsSet;
            defaultGroupId = groupIdAsSet;
            defaultGroupName = groupNameAsSet;
        }

        // Return value that may be stored in octal or binary. Length must exceed 8.
        //
        private static long ParseBinaryOrOctal(byte[] header, int offset, int length)
        {
            if (header[offset] >= 0x80)
            {
                // File sizes over 8GB are stored in 8 right-justified bytes of binary indicated by setting the high-order bit of the leftmost byte of a numeric field.
                long result = 0;
                for (int pos = length - 8; pos < length; pos++)
                {
                    result = (result << 8) | header[offset + pos];
                }

                return result;
            }

            return ParseOctal(header, offset, length);
        }

        /// <summary>
        /// Parse an octal string from a header buffer.
        /// </summary>
        /// <param name = "header">The header buffer from which to parse.</param>
        /// <param name = "offset">The offset into the buffer from which to parse.</param>
        /// <param name = "length">The number of header bytes to parse.</param>
        /// <returns>The long equivalent of the octal string.</returns>
        public static long ParseOctal(byte[] header, int offset, int length)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            long result = 0;
            bool stillPadding = true;

            int end = offset + length;
            for (int i = offset; i < end; ++i)
            {
                if (header[i] == 0)
                {
                    break;
                }

                if (header[i] is ((byte)' ') or (byte)'0')
                {
                    if (stillPadding)
                    {
                        continue;
                    }

                    if (header[i] == (byte)' ')
                    {
                        break;
                    }
                }

                stillPadding = false;

                result = (result << 3) + (header[i] - '0');
            }

            return result;
        }

        /// <summary>
        /// Parse a name from a header buffer.
        /// </summary>
        /// <param name="header">
        /// The header buffer from which to parse.
        /// </param>
        /// <param name="offset">
        /// The offset into the buffer from which to parse.
        /// </param>
        /// <param name="length">
        /// The number of header bytes to parse.
        /// </param>
        /// <returns>
        /// The name parsed.
        /// </returns>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static string ParseName(byte[] header, int offset, int length)
        {
            return ParseName(header.AsSpan().Slice(offset, length), null);
        }

        /// <summary>
        /// Parse a name from a header buffer.
        /// </summary>
        /// <param name="header">
        /// The header buffer from which to parse.
        /// </param>
        /// <param name="encoding">
        /// name encoding, or null for ASCII only
        /// </param>
        /// <returns>
        /// The name parsed.
        /// </returns>
        public static string ParseName(ReadOnlySpan<byte> header, Encoding encoding)
        {
            StringBuilder builder = StringBuilderPool.Instance.Rent();

            int count = 0;
            if (encoding == null)
            {
                for (int i = 0; i < header.Length; ++i)
                {
                    byte b = header[i];
                    if (b == 0)
                    {
                        break;
                    }

                    builder.Append((char)b);
                }
            }
            else
            {
                for (int i = 0; i < header.Length; ++i, ++count)
                {
                    if (header[i] == 0)
                    {
                        break;
                    }
                }

#if NETSTANDARD2_1_OR_GREATER
				var value = encoding.GetString(header.Slice(0, count));
#else
                string value = encoding.GetString(header.ToArray(), 0, count);
#endif
                builder.Append(value);
            }

            string result = builder.ToString();
            StringBuilderPool.Instance.Return(builder);
            return result;
        }

        /// <summary>
        /// Add <paramref name="name">name</paramref> to the buffer as a collection of bytes
        /// </summary>
        /// <param name="name">The name to add</param>
        /// <param name="nameOffset">The offset of the first character</param>
        /// <param name="buffer">The buffer to add to</param>
        /// <param name="bufferOffset">The index of the first byte to add</param>
        /// <param name="length">The number of characters/bytes to add</param>
        /// <returns>The next free index in the <paramref name="buffer"/></returns>
        public static int GetNameBytes(StringBuilder name, int nameOffset, byte[] buffer, int bufferOffset, int length)
        {
            return GetNameBytes(name.ToString(), nameOffset, buffer, bufferOffset, length, null);
        }

        /// <summary>
        /// Add <paramref name="name">name</paramref> to the buffer as a collection of bytes
        /// </summary>
        /// <param name="name">The name to add</param>
        /// <param name="nameOffset">The offset of the first character</param>
        /// <param name="buffer">The buffer to add to</param>
        /// <param name="bufferOffset">The index of the first byte to add</param>
        /// <param name="length">The number of characters/bytes to add</param>
        /// <returns>The next free index in the <paramref name="buffer"/></returns>
        public static int GetNameBytes(string name, int nameOffset, byte[] buffer, int bufferOffset, int length)
        {
            return GetNameBytes(name, nameOffset, buffer, bufferOffset, length, null);
        }

        /// <summary>
        /// Add <paramref name="name">name</paramref> to the buffer as a collection of bytes
        /// </summary>
        /// <param name="name">The name to add</param>
        /// <param name="nameOffset">The offset of the first character</param>
        /// <param name="buffer">The buffer to add to</param>
        /// <param name="bufferOffset">The index of the first byte to add</param>
        /// <param name="length">The number of characters/bytes to add</param>
        /// <param name="encoding">name encoding, or null for ASCII only</param>
        /// <returns>The next free index in the <paramref name="buffer"/></returns>
        public static int GetNameBytes(string name, int nameOffset, byte[] buffer, int bufferOffset, int length,
            Encoding encoding)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int i;
            if (encoding != null)
            {
                // it can be more sufficient if using Span or unsafe
                ReadOnlySpan<char> nameArray =
                    name.AsSpan().Slice(nameOffset, Math.Min(name.Length - nameOffset, length));
                char[] charArray = ArrayPool<char>.Shared.Rent(nameArray.Length);
                nameArray.CopyTo(charArray);

                // it can be more sufficient if using Span(or unsafe?) and ArrayPool for temporary buffer
                int bytesLength = encoding.GetBytes(charArray, 0, nameArray.Length, buffer, bufferOffset);
                ArrayPool<char>.Shared.Return(charArray);
                i = Math.Min(bytesLength, length);
            }
            else
            {
                for (i = 0; i < length && nameOffset + i < name.Length; ++i)
                {
                    buffer[bufferOffset + i] = (byte)name[nameOffset + i];
                }
            }

            for (; i < length; ++i)
            {
                buffer[bufferOffset + i] = 0;
            }

            return bufferOffset + length;
        }

        /// <summary>
        /// Add an entry name to the buffer
        /// </summary>
        /// <param name="name">
        /// The name to add
        /// </param>
        /// <param name="buffer">
        /// The buffer to add to
        /// </param>
        /// <param name="offset">
        /// The offset into the buffer from which to start adding
        /// </param>
        /// <param name="length">
        /// The number of header bytes to add
        /// </param>
        /// <returns>
        /// The index of the next free byte in the buffer
        /// </returns>
        /// TODO: what should be default behavior?(omit upper byte or UTF8?)
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static int GetNameBytes(StringBuilder name, byte[] buffer, int offset, int length)
        {
            return GetNameBytes(name, buffer, offset, length, null);
        }

        /// <summary>
        /// Add an entry name to the buffer
        /// </summary>
        /// <param name="name">
        /// The name to add
        /// </param>
        /// <param name="buffer">
        /// The buffer to add to
        /// </param>
        /// <param name="offset">
        /// The offset into the buffer from which to start adding
        /// </param>
        /// <param name="length">
        /// The number of header bytes to add
        /// </param>
        /// <param name="encoding">
        /// </param>
        /// <returns>
        /// The index of the next free byte in the buffer
        /// </returns>
        public static int GetNameBytes(StringBuilder name, byte[] buffer, int offset, int length, Encoding encoding)
        {
            return name == null
                ? throw new ArgumentNullException(nameof(name))
                : buffer == null
                ? throw new ArgumentNullException(nameof(buffer))
                : GetNameBytes(name.ToString(), 0, buffer, offset, length, encoding);
        }

        /// <summary>
        /// Add an entry name to the buffer
        /// </summary>
        /// <param name="name">The name to add</param>
        /// <param name="buffer">The buffer to add to</param>
        /// <param name="offset">The offset into the buffer from which to start adding</param>
        /// <param name="length">The number of header bytes to add</param>
        /// <returns>The index of the next free byte in the buffer</returns>
        /// TODO: what should be default behavior?(omit upper byte or UTF8?)
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static int GetNameBytes(string name, byte[] buffer, int offset, int length)
        {
            return GetNameBytes(name, buffer, offset, length, null);
        }

        /// <summary>
        /// Add an entry name to the buffer
        /// </summary>
        /// <param name="name">The name to add</param>
        /// <param name="buffer">The buffer to add to</param>
        /// <param name="offset">The offset into the buffer from which to start adding</param>
        /// <param name="length">The number of header bytes to add</param>
        /// <param name="encoding"></param>
        /// <returns>The index of the next free byte in the buffer</returns>
        public static int GetNameBytes(string name, byte[] buffer, int offset, int length, Encoding encoding)
        {
            return name == null
                ? throw new ArgumentNullException(nameof(name))
                : buffer == null ? throw new ArgumentNullException(nameof(buffer)) : GetNameBytes(name, 0, buffer, offset, length, encoding);
        }

        /// <summary>
        /// Add a string to a buffer as a collection of ascii bytes.
        /// </summary>
        /// <param name="toAdd">The string to add</param>
        /// <param name="nameOffset">The offset of the first character to add.</param>
        /// <param name="buffer">The buffer to add to.</param>
        /// <param name="bufferOffset">The offset to start adding at.</param>
        /// <param name="length">The number of ascii characters to add.</param>
        /// <returns>The next free index in the buffer.</returns>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static int GetAsciiBytes(string toAdd, int nameOffset, byte[] buffer, int bufferOffset, int length)
        {
            return GetAsciiBytes(toAdd, nameOffset, buffer, bufferOffset, length, null);
        }

        /// <summary>
        /// Add a string to a buffer as a collection of ascii bytes.
        /// </summary>
        /// <param name="toAdd">The string to add</param>
        /// <param name="nameOffset">The offset of the first character to add.</param>
        /// <param name="buffer">The buffer to add to.</param>
        /// <param name="bufferOffset">The offset to start adding at.</param>
        /// <param name="length">The number of ascii characters to add.</param>
        /// <param name="encoding">String encoding, or null for ASCII only</param>
        /// <returns>The next free index in the buffer.</returns>
        public static int GetAsciiBytes(string toAdd, int nameOffset, byte[] buffer, int bufferOffset, int length,
            Encoding encoding)
        {
            if (toAdd == null)
            {
                throw new ArgumentNullException(nameof(toAdd));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int i;
            if (encoding == null)
            {
                for (i = 0; i < length && nameOffset + i < toAdd.Length; ++i)
                {
                    buffer[bufferOffset + i] = (byte)toAdd[nameOffset + i];
                }
            }
            else
            {
                // It can be more sufficient if using unsafe code or Span(ToCharArray can be omitted)
                char[] chars = toAdd.ToCharArray();
                // It can be more sufficient if using Span(or unsafe?) and ArrayPool for temporary buffer
                byte[] bytes = encoding.GetBytes(chars, nameOffset, Math.Min(toAdd.Length - nameOffset, length));
                i = Math.Min(bytes.Length, length);
                Array.Copy(bytes, 0, buffer, bufferOffset, i);
            }

            // If length is beyond the toAdd string length (which is OK by the prev loop condition), eg if a field has fixed length and the string is shorter, make sure all of the extra chars are written as NULLs, so that the reader func would ignore them and get back the original string
            for (; i < length; ++i)
            {
                buffer[bufferOffset + i] = 0;
            }

            return bufferOffset + length;
        }

        /// <summary>
        /// Put an octal representation of a value into a buffer
        /// </summary>
        /// <param name = "value">
        /// the value to be converted to octal
        /// </param>
        /// <param name = "buffer">
        /// buffer to store the octal string
        /// </param>
        /// <param name = "offset">
        /// The offset into the buffer where the value starts
        /// </param>
        /// <param name = "length">
        /// The length of the octal string to create
        /// </param>
        /// <returns>
        /// The offset of the character next byte after the octal string
        /// </returns>
        public static int GetOctalBytes(long value, byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int localIndex = length - 1;

            // Either a space or null is valid here.  We use NULL as per GNUTar
            buffer[offset + localIndex] = 0;
            --localIndex;

            if (value > 0)
            {
                for (long v = value; localIndex >= 0 && v > 0; --localIndex)
                {
                    buffer[offset + localIndex] = (byte)((byte)'0' + (byte)(v & 7));
                    v >>= 3;
                }
            }

            for (; localIndex >= 0; --localIndex)
            {
                buffer[offset + localIndex] = (byte)'0';
            }

            return offset + length;
        }

        /// <summary>
        /// Put an octal or binary representation of a value into a buffer
        /// </summary>
        /// <param name = "value">Value to be convert to octal</param>
        /// <param name = "buffer">The buffer to update</param>
        /// <param name = "offset">The offset into the buffer to store the value</param>
        /// <param name = "length">The length of the octal string. Must be 12.</param>
        /// <returns>Index of next byte</returns>
        private static int GetBinaryOrOctalBytes(long value, byte[] buffer, int offset, int length)
        {
            if (value > 0x1FFFFFFFF)
            {
                // Octal 77777777777 (11 digits)
                // Put value as binary, right-justified into the buffer. Set high order bit of left-most byte.
                for (int pos = length - 1; pos > 0; pos--)
                {
                    buffer[offset + pos] = (byte)value;
                    value >>= 8;
                }

                buffer[offset] = 0x80;
                return offset + length;
            }

            return GetOctalBytes(value, buffer, offset, length);
        }

        /// <summary>
        /// Add the checksum integer to header buffer.
        /// </summary>
        /// <param name = "value"></param>
        /// <param name = "buffer">The header buffer to set the checksum for</param>
        /// <param name = "offset">The offset into the buffer for the checksum</param>
        /// <param name = "length">The number of header bytes to update.
        /// It's formatted differently from the other fields: it has 6 digits, a
        /// null, then a space -- rather than digits, a space, then a null.
        /// The final space is already there, from checksumming
        /// </param>
        /// <returns>The modified buffer offset</returns>
        private static void GetCheckSumOctalBytes(long value, byte[] buffer, int offset, int length)
        {
            _ = GetOctalBytes(value, buffer, offset, length - 1);
        }

        /// <summary>
        /// Compute the checksum for a tar entry header.
        /// The checksum field must be all spaces prior to this happening
        /// </summary>
        /// <param name = "buffer">The tar entry's header buffer.</param>
        /// <returns>The computed checksum.</returns>
        private static int ComputeCheckSum(byte[] buffer)
        {
            int sum = 0;
            for (int i = 0; i < buffer.Length; ++i)
            {
                sum += buffer[i];
            }

            return sum;
        }

        /// <summary>
        /// Make a checksum for a tar entry ignoring the checksum contents.
        /// </summary>
        /// <param name = "buffer">The tar entry's header buffer.</param>
        /// <returns>The checksum for the buffer</returns>
        private static int MakeCheckSum(byte[] buffer)
        {
            int sum = 0;
            for (int i = 0; i < CHKSUMOFS; ++i)
            {
                sum += buffer[i];
            }

            for (int i = 0; i < CHKSUMLEN; ++i)
            {
                sum += (byte)' ';
            }

            for (int i = CHKSUMOFS + CHKSUMLEN; i < buffer.Length; ++i)
            {
                sum += buffer[i];
            }

            return sum;
        }

        private static int GetCTime(DateTime dateTime)
        {
            return unchecked((int)((dateTime.Ticks - dateTime1970.Ticks) / timeConversionFactor));
        }

        private static DateTime GetDateTimeFromCTime(long ticks)
        {
            DateTime result;

            try
            {
                result = new DateTime(dateTime1970.Ticks + (ticks * timeConversionFactor));
            }
            catch (ArgumentOutOfRangeException)
            {
                result = dateTime1970;
            }

            return result;
        }

        #region Instance Fields

        private string name;
        private long size;
        private DateTime modTime;
        private string linkName;
        private string magic;
        private string version;
        private string userName;
        private string groupName;

        #endregion Instance Fields

        #region Class Fields

        // Values used during recursive operations.
        internal static int userIdAsSet;

        internal static int groupIdAsSet;
        internal static string userNameAsSet;
        internal static string groupNameAsSet = "None";

        internal static int defaultUserId;
        internal static int defaultGroupId;
        internal static string defaultGroupName = "None";
        internal static string defaultUser;

        #endregion Class Fields
    }
    /// <summary>
    /// Reads the extended header of a Tar stream
    /// </summary>
    public class TarExtendedHeaderReader
    {
        private const byte LENGTH = 0;
        private const byte KEY = 1;
        private const byte VALUE = 2;
        private const byte END = 3;
        private string[] headerParts = new string[3];

        private int bbIndex;
        private byte[] byteBuffer;
        private char[] charBuffer;

        private readonly StringBuilder sb = new();
        private readonly Decoder decoder = Encoding.UTF8.GetDecoder();

        private int state = LENGTH;

        private int currHeaderLength;
        private int currHeaderRead;

        private static readonly byte[] StateNext = { (byte)' ', (byte)'=', (byte)'\n' };

        /// <summary>
        /// Creates a new <see cref="TarExtendedHeaderReader"/>.
        /// </summary>
        public TarExtendedHeaderReader()
        {
            ResetBuffers();
        }

        /// <summary>
        /// Read <paramref name="length"/> bytes from <paramref name="buffer"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        public void Read(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                byte next = buffer[i];

                bool foundStateEnd = state == VALUE
                    ? currHeaderRead == currHeaderLength - 1
                    : next == StateNext[state];

                if (foundStateEnd)
                {
                    Flush();
                    headerParts[state] = sb.ToString();
                    _ = sb.Clear();

                    if (++state == END)
                    {
                        if (!Headers.ContainsKey(headerParts[KEY]))
                        {
                            Headers.Add(headerParts[KEY], headerParts[VALUE]);
                        }

                        headerParts = new string[3];
                        currHeaderLength = 0;
                        currHeaderRead = 0;
                        state = LENGTH;
                    }
                    else
                    {
                        currHeaderRead++;
                    }


                    if (state != VALUE)
                    {
                        continue;
                    }

                    if (int.TryParse(headerParts[LENGTH], out int vl))
                    {
                        currHeaderLength = vl;
                    }
                }
                else
                {
                    byteBuffer[bbIndex++] = next;
                    currHeaderRead++;
                    if (bbIndex == 4)
                    {
                        Flush();
                    }
                }
            }
        }

        private void Flush()
        {
            decoder.Convert(byteBuffer, 0, bbIndex, charBuffer, 0, 4, false, out _, out int charsUsed, out _);

            _ = sb.Append(charBuffer, 0, charsUsed);
            ResetBuffers();
        }

        private void ResetBuffers()
        {
            charBuffer = new char[4];
            byteBuffer = new byte[4];
            bbIndex = 0;
        }

        /// <summary>
        /// Returns the parsed headers as key-value strings
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
    }
    /// <summary>
    /// This class represents an entry in a Tar archive. It consists
    /// of the entry's header, as well as the entry's File. Entries
    /// can be instantiated in one of three ways, depending on how
    /// they are to be used.
    /// <p>
    /// TarEntries that are created from the header bytes read from
    /// an archive are instantiated with the TarEntry( byte[] )
    /// constructor. These entries will be used when extracting from
    /// or listing the contents of an archive. These entries have their
    /// header filled in using the header bytes. They also set the File
    /// to null, since they reference an archive entry not a file.</p>
    /// <p>
    /// TarEntries that are created from files that are to be written
    /// into an archive are instantiated with the CreateEntryFromFile(string)
    /// pseudo constructor. These entries have their header filled in using
    /// the File's information. They also keep a reference to the File
    /// for convenience when writing entries.</p>
    /// <p>
    /// Finally, TarEntries can be constructed from nothing but a name.
    /// This allows the programmer to construct the entry by hand, for
    /// instance when only an InputStream is available for writing to
    /// the archive, and the header information is constructed from
    /// other information. In this case the header fields are set to
    /// defaults and the File is set to null.</p>
    /// <see cref="TarHeader"/>
    /// </summary>
    public class TarEntry
    {
        #region Constructors

        /// <summary>
        /// Initialise a default instance of <see cref="TarEntry"/>.
        /// </summary>
        private TarEntry()
        {
            TarHeader = new TarHeader();
        }

        /// <summary>
        /// Construct an entry from an archive's header bytes. File is set
        /// to null.
        /// </summary>
        /// <param name = "headerBuffer">
        /// The header bytes from a tar archive entry.
        /// </param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public TarEntry(byte[] headerBuffer) : this(headerBuffer, null)
        {
        }

        /// <summary>
        /// Construct an entry from an archive's header bytes. File is set
        /// to null.
        /// </summary>
        /// <param name = "headerBuffer">
        /// The header bytes from a tar archive entry.
        /// </param>
        /// <param name = "nameEncoding">
        /// The <see cref="Encoding"/> used for the Name fields, or null for ASCII only
        /// </param>
        public TarEntry(byte[] headerBuffer, Encoding nameEncoding)
        {
            TarHeader = new TarHeader();
            TarHeader.ParseBuffer(headerBuffer, nameEncoding);
        }

        /// <summary>
        /// Construct a TarEntry using the <paramref name="header">header</paramref> provided
        /// </summary>
        /// <param name="header">Header details for entry</param>
        public TarEntry(TarHeader header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            TarHeader = (TarHeader)header.Clone();
        }

        #endregion Constructors

        #region ICloneable Members

        /// <summary>
        /// Clone this tar entry.
        /// </summary>
        /// <returns>Returns a clone of this entry.</returns>
        public object Clone()
        {
            TarEntry entry = new()
            {
                File = File,
                TarHeader = (TarHeader)TarHeader.Clone(),
                Name = Name
            };
            return entry;
        }

        #endregion ICloneable Members

        /// <summary>
        /// Construct an entry with only a <paramref name="name">name</paramref>.
        /// This allows the programmer to construct the entry's header "by hand".
        /// </summary>
        /// <param name="name">The name to use for the entry</param>
        /// <returns>Returns the newly created <see cref="TarEntry"/></returns>
        public static TarEntry CreateTarEntry(string name)
        {
            TarEntry entry = new();

            entry.NameTarHeader(name);
            return entry;
        }

        /// <summary>
        /// Construct an entry for a file. File is set to file, and the
        /// header is constructed from information from the file.
        /// </summary>
        /// <param name = "fileName">The file name that the entry represents.</param>
        /// <returns>Returns the newly created <see cref="TarEntry"/></returns>
        public static TarEntry CreateEntryFromFile(string fileName)
        {
            TarEntry entry = new();
            entry.GetFileTarHeader(entry.TarHeader, fileName);
            return entry;
        }

        /// <summary>
        /// Determine if the two entries are equal. Equality is determined
        /// by the header names being equal.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current Object.</param>
        /// <returns>
        /// True if the entries are equal; false if not.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is TarEntry localEntry && Name.Equals(localEntry.Name);
        }

        /// <summary>
        /// Derive a Hash value for the current <see cref="object"/>
        /// </summary>
        /// <returns>A Hash code for the current <see cref="object"/></returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Determine if the given entry is a descendant of this entry.
        /// Descendancy is determined by the name of the descendant
        /// starting with this entry's name.
        /// </summary>
        /// <param name = "toTest">
        /// Entry to be checked as a descendent of this.
        /// </param>
        /// <returns>
        /// True if entry is a descendant of this.
        /// </returns>
        public bool IsDescendent(TarEntry toTest)
        {
            return toTest == null ? throw new ArgumentNullException(nameof(toTest)) : toTest.Name.StartsWith(Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Get this entry's header.
        /// </summary>
        /// <returns>
        /// This entry's TarHeader.
        /// </returns>
        public TarHeader TarHeader { get; private set; }

        /// <summary>
        /// Get/Set this entry's name.
        /// </summary>
        public string Name
        {
            get => TarHeader.Name;
            set => TarHeader.Name = value;
        }

        /// <summary>
        /// Get/set this entry's user id.
        /// </summary>
        public int UserId
        {
            get => TarHeader.UserId;
            set => TarHeader.UserId = value;
        }

        /// <summary>
        /// Get/set this entry's group id.
        /// </summary>
        public int GroupId
        {
            get => TarHeader.GroupId;
            set => TarHeader.GroupId = value;
        }

        /// <summary>
        /// Get/set this entry's user name.
        /// </summary>
        public string UserName
        {
            get => TarHeader.UserName;
            set => TarHeader.UserName = value;
        }

        /// <summary>
        /// Get/set this entry's group name.
        /// </summary>
        public string GroupName
        {
            get => TarHeader.GroupName;
            set => TarHeader.GroupName = value;
        }

        /// <summary>
        /// Convenience method to set this entry's group and user ids.
        /// </summary>
        /// <param name="userId">
        /// This entry's new user id.
        /// </param>
        /// <param name="groupId">
        /// This entry's new group id.
        /// </param>
        public void SetIds(int userId, int groupId)
        {
            UserId = userId;
            GroupId = groupId;
        }

        /// <summary>
        /// Convenience method to set this entry's group and user names.
        /// </summary>
        /// <param name="userName">
        /// This entry's new user name.
        /// </param>
        /// <param name="groupName">
        /// This entry's new group name.
        /// </param>
        public void SetNames(string userName, string groupName)
        {
            UserName = userName;
            GroupName = groupName;
        }

        /// <summary>
        /// Get/Set the modification time for this entry
        /// </summary>
        public DateTime ModTime
        {
            get => TarHeader.ModTime;
            set => TarHeader.ModTime = value;
        }

        /// <summary>
        /// Get this entry's file.
        /// </summary>
        /// <returns>
        /// This entry's file.
        /// </returns>
        public string File { get; private set; }

        /// <summary>
        /// Get/set this entry's recorded file size.
        /// </summary>
        public long Size
        {
            get => TarHeader.Size;
            set => TarHeader.Size = value;
        }

        /// <summary>
        /// Return true if this entry represents a directory, false otherwise
        /// </summary>
        /// <returns>
        /// True if this entry is a directory.
        /// </returns>
        public bool IsDirectory
        {
            get
            {
                if (File != null)
                {
                    return Directory.Exists(File);
                }

                if (TarHeader != null)
                {
                    if (TarHeader.TypeFlag == TarHeader.LF_DIR || Name.EndsWith("/", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Fill in a TarHeader with information from a File.
        /// </summary>
        /// <param name="header">
        /// The TarHeader to fill in.
        /// </param>
        /// <param name="file">
        /// The file from which to get the header information.
        /// </param>
        public void GetFileTarHeader(TarHeader header, string file)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            File = file ?? throw new ArgumentNullException(nameof(file));

            // bugfix from torhovl from #D forum:
            string name = file;

            // 23-Jan-2004 GnuTar allows device names in path where the name is not local to the current directory
            if (name.IndexOf(Directory.GetCurrentDirectory(), StringComparison.Ordinal) == 0)
            {
                name = name[Directory.GetCurrentDirectory().Length..];
            }

            /*
						if (Path.DirectorySeparatorChar == '\\')
						{
							// check if the OS is Windows
							// Strip off drive letters!
							if (name.Length > 2)
							{
								char ch1 = name[0];
								char ch2 = name[1];
								if (ch2 == ':' && Char.IsLetter(ch1))
								{
									name = name.Substring(2);
								}
							}
						}
			*/

            // No absolute pathnames
            // Windows (and Posix?) paths can start with UNC style "\\NetworkDrive\",
            // so we loop on starting /'s.
            name = name.ToTarArchivePath();

            header.LinkName = string.Empty;
            header.Name = name;

            if (Directory.Exists(file))
            {
                header.Mode = 1003; // Magic number for security access for a UNIX filesystem
                header.TypeFlag = TarHeader.LF_DIR;
                if (header.Name.Length == 0 || header.Name[^1] != '/')
                {
                    header.Name += "/";
                }

                header.Size = 0;
            }
            else
            {
                header.Mode = 33216; // Magic number for security access for a UNIX filesystem
                header.TypeFlag = TarHeader.LF_NORMAL;
                header.Size = new FileInfo(file.Replace('/', Path.DirectorySeparatorChar)).Length;
            }

            header.ModTime = System.IO.File.GetLastWriteTime(file.Replace('/', Path.DirectorySeparatorChar))
                .ToUniversalTime();
            header.DevMajor = 0;
            header.DevMinor = 0;
        }

        /// <summary>
        /// Get entries for all files present in this entries directory.
        /// If this entry doesnt represent a directory zero entries are returned.
        /// </summary>
        /// <returns>
        /// An array of TarEntry's for this entry's children.
        /// </returns>
        public TarEntry[] GetDirectoryEntries()
        {
            if (File == null || !Directory.Exists(File))
            {
                return Empty.Array<TarEntry>();
            }

            string[] list = Directory.GetFileSystemEntries(File);
            TarEntry[] result = new TarEntry[list.Length];

            for (int i = 0; i < list.Length; ++i)
            {
                result[i] = CreateEntryFromFile(list[i]);
            }

            return result;
        }

        /// <summary>
        /// Write an entry's header information to a header buffer.
        /// </summary>
        /// <param name = "outBuffer">
        /// The tar entry header buffer to fill in.
        /// </param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public void WriteEntryHeader(byte[] outBuffer)
        {
            WriteEntryHeader(outBuffer, null);
        }

        /// <summary>
        /// Write an entry's header information to a header buffer.
        /// </summary>
        /// <param name = "outBuffer">
        /// The tar entry header buffer to fill in.
        /// </param>
        /// <param name = "nameEncoding">
        /// The <see cref="Encoding"/> used for the Name fields, or null for ASCII only
        /// </param>
        public void WriteEntryHeader(byte[] outBuffer, Encoding nameEncoding)
        {
            TarHeader.WriteHeader(outBuffer, nameEncoding);
        }

        /// <summary>
        /// Convenience method that will modify an entry's name directly
        /// in place in an entry header buffer byte array.
        /// </summary>
        /// <param name="buffer">
        /// The buffer containing the entry header to modify.
        /// </param>
        /// <param name="newName">
        /// The new name to place into the header buffer.
        /// </param>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static void AdjustEntryName(byte[] buffer, string newName)
        {
            AdjustEntryName(buffer, newName, null);
        }

        /// <summary>
        /// Convenience method that will modify an entry's name directly
        /// in place in an entry header buffer byte array.
        /// </summary>
        /// <param name="buffer">
        /// The buffer containing the entry header to modify.
        /// </param>
        /// <param name="newName">
        /// The new name to place into the header buffer.
        /// </param>
        /// <param name="nameEncoding">
        /// The <see cref="Encoding"/> used for the Name fields, or null for ASCII only
        /// </param>
        public static void AdjustEntryName(byte[] buffer, string newName, Encoding nameEncoding)
        {
            _ = TarHeader.GetNameBytes(newName, buffer, 0, TarHeader.NAMELEN, nameEncoding);
        }

        /// <summary>
        /// Fill in a TarHeader given only the entry's name.
        /// </summary>
        /// <param name="name">
        /// The tar entry name.
        /// </param>
        public void NameTarHeader(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            bool isDir = name.EndsWith("/", StringComparison.Ordinal);

            TarHeader.Name = name;
            TarHeader.Mode = isDir ? 1003 : 33216;
            TarHeader.UserId = 0;
            TarHeader.GroupId = 0;
            TarHeader.Size = 0;

            TarHeader.ModTime = DateTime.UtcNow;

            TarHeader.TypeFlag = isDir ? TarHeader.LF_DIR : TarHeader.LF_NORMAL;

            TarHeader.LinkName = string.Empty;
            TarHeader.UserName = string.Empty;
            TarHeader.GroupName = string.Empty;

            TarHeader.DevMajor = 0;
            TarHeader.DevMinor = 0;
        }

        #region Instance Fields

        /// <summary>
        /// The name of the file this entry represents or null if the entry is not based on a file.
        /// </summary>

        #endregion Instance Fields
    }
    /// <summary>
    /// Used to advise clients of 'events' while processing archives
    /// </summary>
    public delegate void ProgressMessageHandler(TarArchive archive, TarEntry entry, string message);

    /// <summary>
    /// The TarArchive class implements the concept of a
    /// 'Tape Archive'. A tar archive is a series of entries, each of
    /// which represents a file system object. Each entry in
    /// the archive consists of a header block followed by 0 or more data blocks.
    /// Directory entries consist only of the header block, and are followed by entries
    /// for the directory's contents. File entries consist of a
    /// header followed by the number of blocks needed to
    /// contain the file's contents. All entries are written on
    /// block boundaries. Blocks are 512 bytes long.
    ///
    /// TarArchives are instantiated in either read or write mode,
    /// based upon whether they are instantiated with an InputStream
    /// or an OutputStream. Once instantiated TarArchives read/write
    /// mode can not be changed.
    ///
    /// There is currently no support for random access to tar archives.
    /// However, it seems that subclassing TarArchive, and using the
    /// TarBuffer.CurrentRecord and TarBuffer.CurrentBlock
    /// properties, this would be rather trivial.
    /// </summary>
    public class TarArchive : IDisposable
    {
        /// <summary>
        /// Client hook allowing detailed information to be reported during processing
        /// </summary>
        public event ProgressMessageHandler ProgressMessageEvent;

        /// <summary>
        /// Raises the ProgressMessage event
        /// </summary>
        /// <param name="entry">The <see cref="TarEntry">TarEntry</see> for this event</param>
        /// <param name="message">message for this event.  Null is no message</param>
        protected virtual void OnProgressMessageEvent(TarEntry entry, string message)
        {
            ProgressMessageEvent?.Invoke(this, entry, message);
        }

        #region Constructors

        /// <summary>
        /// Constructor for a default <see cref="TarArchive"/>.
        /// </summary>
        protected TarArchive()
        {
        }

        /// <summary>
        /// Initialise a TarArchive for input.
        /// </summary>
        /// <param name="stream">The <see cref="TarInputStream"/> to use for input.</param>
        protected TarArchive(TarInputStream stream)
        {
            tarIn = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        /// Initialise a TarArchive for output.
        /// </summary>
        /// <param name="stream">The <see cref="TarOutputStream"/> to use for output.</param>
        protected TarArchive(TarOutputStream stream)
        {
            tarOut = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        #endregion Constructors

        #region Static factory methods

        /// <summary>
        /// The InputStream based constructors create a TarArchive for the
        /// purposes of extracting or listing a tar archive. Thus, use
        /// these constructors when you wish to extract files from or list
        /// the contents of an existing tar archive.
        /// </summary>
        /// <param name="inputStream">The stream to retrieve archive data from.</param>
        /// <returns>Returns a new <see cref="TarArchive"/> suitable for reading from.</returns>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static TarArchive CreateInputTarArchive(Stream inputStream)
        {
            return CreateInputTarArchive(inputStream, null);
        }

        /// <summary>
        /// The InputStream based constructors create a TarArchive for the
        /// purposes of extracting or listing a tar archive. Thus, use
        /// these constructors when you wish to extract files from or list
        /// the contents of an existing tar archive.
        /// </summary>
        /// <param name="inputStream">The stream to retrieve archive data from.</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        /// <returns>Returns a new <see cref="TarArchive"/> suitable for reading from.</returns>
        public static TarArchive CreateInputTarArchive(Stream inputStream, Encoding nameEncoding)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }


            TarArchive result = inputStream is TarInputStream tarStream ? new TarArchive(tarStream) : CreateInputTarArchive(inputStream, TarBuffer.DefaultBlockFactor, nameEncoding);
            return result;
        }

        /// <summary>
        /// Create TarArchive for reading setting block factor
        /// </summary>
        /// <param name="inputStream">A stream containing the tar archive contents</param>
        /// <param name="blockFactor">The blocking factor to apply</param>
        /// <returns>Returns a <see cref="TarArchive"/> suitable for reading.</returns>
        [Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
        public static TarArchive CreateInputTarArchive(Stream inputStream, int blockFactor)
        {
            return CreateInputTarArchive(inputStream, blockFactor, null);
        }

        /// <summary>
        /// Create TarArchive for reading setting block factor
        /// </summary>
        /// <param name="inputStream">A stream containing the tar archive contents</param>
        /// <param name="blockFactor">The blocking factor to apply</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        /// <returns>Returns a <see cref="TarArchive"/> suitable for reading.</returns>
        public static TarArchive CreateInputTarArchive(Stream inputStream, int blockFactor, Encoding nameEncoding)
        {
            return inputStream == null
                ? throw new ArgumentNullException(nameof(inputStream))
                : inputStream is TarInputStream
                ? throw new ArgumentException("TarInputStream not valid")
                : new TarArchive(new TarInputStream(inputStream, blockFactor, nameEncoding));
        }
        /// <summary>
        /// Create a TarArchive for writing to, using the default blocking factor
        /// </summary>
        /// <param name="outputStream">The <see cref="Stream"/> to write to</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        /// <returns>Returns a <see cref="TarArchive"/> suitable for writing.</returns>
        public static TarArchive CreateOutputTarArchive(Stream outputStream, Encoding nameEncoding)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }


            TarArchive result = outputStream is TarOutputStream tarStream
                ? new TarArchive(tarStream)
                : CreateOutputTarArchive(outputStream, TarBuffer.DefaultBlockFactor, nameEncoding);
            return result;
        }
        /// <summary>
        /// Create a TarArchive for writing to, using the default blocking factor
        /// </summary>
        /// <param name="outputStream">The <see cref="Stream"/> to write to</param>
        /// <returns>Returns a <see cref="TarArchive"/> suitable for writing.</returns>
        public static TarArchive CreateOutputTarArchive(Stream outputStream)
        {
            return CreateOutputTarArchive(outputStream, null);
        }

        /// <summary>
        /// Create a <see cref="TarArchive">tar archive</see> for writing.
        /// </summary>
        /// <param name="outputStream">The stream to write to</param>
        /// <param name="blockFactor">The blocking factor to use for buffering.</param>
        /// <returns>Returns a <see cref="TarArchive"/> suitable for writing.</returns>
        public static TarArchive CreateOutputTarArchive(Stream outputStream, int blockFactor)
        {
            return CreateOutputTarArchive(outputStream, blockFactor, null);
        }
        /// <summary>
        /// Create a <see cref="TarArchive">tar archive</see> for writing.
        /// </summary>
        /// <param name="outputStream">The stream to write to</param>
        /// <param name="blockFactor">The blocking factor to use for buffering.</param>
        /// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
        /// <returns>Returns a <see cref="TarArchive"/> suitable for writing.</returns>
        public static TarArchive CreateOutputTarArchive(Stream outputStream, int blockFactor, Encoding nameEncoding)
        {
            return outputStream == null
                ? throw new ArgumentNullException(nameof(outputStream))
                : outputStream is TarOutputStream
                ? throw new ArgumentException("TarOutputStream is not valid")
                : new TarArchive(new TarOutputStream(outputStream, blockFactor, nameEncoding));
        }

        #endregion Static factory methods

        /// <summary>
        /// Set the flag that determines whether existing files are
        /// kept, or overwritten during extraction.
        /// </summary>
        /// <param name="keepExistingFiles">
        /// If true, do not overwrite existing files.
        /// </param>
        public void SetKeepOldFiles(bool keepExistingFiles)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("TarArchive");
            }

            keepOldFiles = keepExistingFiles;
        }

        /// <summary>
        /// Get/set the ascii file translation flag. If ascii file translation
        /// is true, then the file is checked to see if it a binary file or not.
        /// If the flag is true and the test indicates it is ascii text
        /// file, it will be translated. The translation converts the local
        /// operating system's concept of line ends into the UNIX line end,
        /// '\n', which is the defacto standard for a TAR archive. This makes
        /// text files compatible with UNIX.
        /// </summary>
        public bool AsciiTranslate
        {
            get => isDisposed ? throw new ObjectDisposedException("TarArchive") : asciiTranslate;

            set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("TarArchive");
                }

                asciiTranslate = value;
            }
        }

        /// <summary>
        /// Set the ascii file translation flag.
        /// </summary>
        /// <param name= "translateAsciiFiles">
        /// If true, translate ascii text files.
        /// </param>
        [Obsolete("Use the AsciiTranslate property")]
        public void SetAsciiTranslation(bool translateAsciiFiles)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("TarArchive");
            }

            asciiTranslate = translateAsciiFiles;
        }

        /// <summary>
        /// PathPrefix is added to entry names as they are written if the value is not null.
        /// A slash character is appended after PathPrefix
        /// </summary>
        public string PathPrefix
        {
            get => isDisposed ? throw new ObjectDisposedException("TarArchive") : pathPrefix;

            set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("TarArchive");
                }

                pathPrefix = value;
            }
        }

        /// <summary>
        /// RootPath is removed from entry names if it is found at the
        /// beginning of the name.
        /// </summary>
        public string RootPath
        {
            get => isDisposed ? throw new ObjectDisposedException("TarArchive") : rootPath;

            set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("TarArchive");
                }
                rootPath = value.ToTarArchivePath().TrimEnd('/');
            }
        }

        /// <summary>
        /// Set user and group information that will be used to fill in the
        /// tar archive's entry headers. This information is based on that available
        /// for the linux operating system, which is not always available on other
        /// operating systems.  TarArchive allows the programmer to specify values
        /// to be used in their place.
        /// <see cref="ApplyUserInfoOverrides"/> is set to true by this call.
        /// </summary>
        /// <param name="userId">
        /// The user id to use in the headers.
        /// </param>
        /// <param name="userName">
        /// The user name to use in the headers.
        /// </param>
        /// <param name="groupId">
        /// The group id to use in the headers.
        /// </param>
        /// <param name="groupName">
        /// The group name to use in the headers.
        /// </param>
        public void SetUserInfo(int userId, string userName, int groupId, string groupName)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("TarArchive");
            }

            this.userId = userId;
            this.userName = userName;
            this.groupId = groupId;
            this.groupName = groupName;
            applyUserInfoOverrides = true;
        }

        /// <summary>
        /// Get or set a value indicating if overrides defined by <see cref="SetUserInfo">SetUserInfo</see> should be applied.
        /// </summary>
        /// <remarks>If overrides are not applied then the values as set in each header will be used.</remarks>
        public bool ApplyUserInfoOverrides
        {
            get => isDisposed ? throw new ObjectDisposedException("TarArchive") : applyUserInfoOverrides;

            set
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("TarArchive");
                }

                applyUserInfoOverrides = value;
            }
        }

        /// <summary>
        /// Get the archive user id.
        /// See <see cref="ApplyUserInfoOverrides">ApplyUserInfoOverrides</see> for detail
        /// on how to allow setting values on a per entry basis.
        /// </summary>
        /// <returns>
        /// The current user id.
        /// </returns>
        public int UserId => isDisposed ? throw new ObjectDisposedException("TarArchive") : userId;

        /// <summary>
        /// Get the archive user name.
        /// See <see cref="ApplyUserInfoOverrides">ApplyUserInfoOverrides</see> for detail
        /// on how to allow setting values on a per entry basis.
        /// </summary>
        /// <returns>
        /// The current user name.
        /// </returns>
        public string UserName => isDisposed ? throw new ObjectDisposedException("TarArchive") : userName;

        /// <summary>
        /// Get the archive group id.
        /// See <see cref="ApplyUserInfoOverrides">ApplyUserInfoOverrides</see> for detail
        /// on how to allow setting values on a per entry basis.
        /// </summary>
        /// <returns>
        /// The current group id.
        /// </returns>
        public int GroupId => isDisposed ? throw new ObjectDisposedException("TarArchive") : groupId;

        /// <summary>
        /// Get the archive group name.
        /// See <see cref="ApplyUserInfoOverrides">ApplyUserInfoOverrides</see> for detail
        /// on how to allow setting values on a per entry basis.
        /// </summary>
        /// <returns>
        /// The current group name.
        /// </returns>
        public string GroupName => isDisposed ? throw new ObjectDisposedException("TarArchive") : groupName;

        /// <summary>
        /// Get the archive's record size. Tar archives are composed of
        /// a series of RECORDS each containing a number of BLOCKS.
        /// This allowed tar archives to match the IO characteristics of
        /// the physical device being used. Archives are expected
        /// to be properly "blocked".
        /// </summary>
        /// <returns>
        /// The record size this archive is using.
        /// </returns>
        public int RecordSize
        {
            get
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException("TarArchive");
                }

                if (tarIn != null)
                {
                    return tarIn.RecordSize;
                }
                else if (tarOut != null)
                {
                    return tarOut.RecordSize;
                }
                return TarBuffer.DefaultRecordSize;
            }
        }

        /// <summary>
        /// Sets the IsStreamOwner property on the underlying stream.
        /// Set this to false to prevent the Close of the TarArchive from closing the stream.
        /// </summary>
        public bool IsStreamOwner
        {
            set
            {
                if (tarIn != null)
                {
                    tarIn.IsStreamOwner = value;
                }
                else
                {
                    tarOut.IsStreamOwner = value;
                }
            }
        }

        /// <summary>
        /// Close the archive.
        /// </summary>
        [Obsolete("Use Close instead")]
        public void CloseArchive()
        {
            Close();
        }

        /// <summary>
        /// Perform the "list" command for the archive contents.
        ///
        /// NOTE That this method uses the <see cref="ProgressMessageEvent"> progress event</see> to actually list
        /// the contents. If the progress display event is not set, nothing will be listed!
        /// </summary>
        public void ListContents()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("TarArchive");
            }

            while (true)
            {
                TarEntry entry = tarIn.GetNextEntry();

                if (entry == null)
                {
                    break;
                }
                OnProgressMessageEvent(entry, null);
            }
        }

        /// <summary>
        /// Perform the "extract" command and extract the contents of the archive.
        /// </summary>
        /// <param name="destinationDirectory">
        /// The destination directory into which to extract.
        /// </param>
        public void ExtractContents(string destinationDirectory)
        {
            ExtractContents(destinationDirectory, false);
        }

        /// <summary>
        /// Perform the "extract" command and extract the contents of the archive.
        /// </summary>
        /// <param name="destinationDirectory">
        /// The destination directory into which to extract.
        /// </param>
        /// <param name="allowParentTraversal">Allow parent directory traversal in file paths (e.g. ../file)</param>
        public void ExtractContents(string destinationDirectory, bool allowParentTraversal)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("TarArchive");
            }

            string fullDistDir = Path.GetFullPath(destinationDirectory).TrimEnd('/', '\\');

            while (true)
            {
                TarEntry entry = tarIn.GetNextEntry();

                if (entry == null)
                {
                    break;
                }

                if (entry.TarHeader.TypeFlag is TarHeader.LF_LINK or TarHeader.LF_SYMLINK)
                {
                    continue;
                }

                ExtractEntry(fullDistDir, entry, allowParentTraversal);
            }
        }

        /// <summary>
        /// Extract an entry from the archive. This method assumes that the
        /// tarIn stream has been properly set with a call to GetNextEntry().
        /// </summary>
        /// <param name="destDir">
        /// The destination directory into which to extract.
        /// </param>
        /// <param name="entry">
        /// The TarEntry returned by tarIn.GetNextEntry().
        /// </param>
        /// <param name="allowParentTraversal">Allow parent directory traversal in file paths (e.g. ../file)</param>
        private void ExtractEntry(string destDir, TarEntry entry, bool allowParentTraversal)
        {
            OnProgressMessageEvent(entry, null);

            string name = entry.Name;

            if (Path.IsPathRooted(name))
            {
                // NOTE:
                // for UNC names...  \\machine\share\zoom\beet.txt gives \zoom\beet.txt
                name = name[Path.GetPathRoot(name).Length..];
            }

            name = name.Replace('/', Path.DirectorySeparatorChar);

            string destFile = Path.Combine(destDir, name);
            string destFileDir = Path.GetDirectoryName(Path.GetFullPath(destFile)) ?? "";

            bool isRootDir = entry.IsDirectory && entry.Name == "";

            if (!allowParentTraversal && !isRootDir && !destFileDir.StartsWith(destDir, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidNameException("Parent traversal in paths is not allowed");
            }

            if (entry.IsDirectory)
            {
                EnsureDirectoryExists(destFile);
            }
            else
            {
                string parentDirectory = Path.GetDirectoryName(destFile);
                EnsureDirectoryExists(parentDirectory);

                bool process = true;
                FileInfo fileInfo = new(destFile);
                if (fileInfo.Exists)
                {
                    if (keepOldFiles)
                    {
                        OnProgressMessageEvent(entry, "Destination file already exists");
                        process = false;
                    }
                    else if ((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        OnProgressMessageEvent(entry, "Destination file already exists, and is read-only");
                        process = false;
                    }
                }

                if (process)
                {
                    using FileStream outputStream = File.Create(destFile);
                    if (asciiTranslate)
                    {
                        // May need to translate the file.
                        ExtractAndTranslateEntry(destFile, outputStream);
                    }
                    else
                    {
                        // If translation is disabled, just copy the entry across directly.
                        tarIn.CopyEntryContents(outputStream);
                    }
                }
            }
        }

        // Extract a TAR entry, and perform an ASCII translation if required.
        private void ExtractAndTranslateEntry(string destFile, Stream outputStream)
        {
            bool asciiTrans = !IsBinary(destFile);

            if (asciiTrans)
            {
                using StreamWriter outw = new(outputStream, new UTF8Encoding(false), 1024, true);
                byte[] rdbuf = new byte[32 * 1024];

                while (true)
                {
                    int numRead = tarIn.Read(rdbuf, 0, rdbuf.Length);

                    if (numRead <= 0)
                    {
                        break;
                    }

                    for (int off = 0, b = 0; b < numRead; ++b)
                    {
                        if (rdbuf[b] == 10)
                        {
                            string s = Encoding.ASCII.GetString(rdbuf, off, b - off);
                            outw.WriteLine(s);
                            off = b + 1;
                        }
                    }
                }
            }
            else
            {
                // No translation required.
                tarIn.CopyEntryContents(outputStream);
            }
        }

        /// <summary>
        /// Write an entry to the archive. This method will call the putNextEntry
        /// and then write the contents of the entry, and finally call closeEntry()
        /// for entries that are files. For directories, it will call putNextEntry(),
        /// and then, if the recurse flag is true, process each entry that is a
        /// child of the directory.
        /// </summary>
        /// <param name="sourceEntry">
        /// The TarEntry representing the entry to write to the archive.
        /// </param>
        /// <param name="recurse">
        /// If true, process the children of directory entries.
        /// </param>
        public void WriteEntry(TarEntry sourceEntry, bool recurse)
        {
            if (sourceEntry == null)
            {
                throw new ArgumentNullException(nameof(sourceEntry));
            }

            if (isDisposed)
            {
                throw new ObjectDisposedException("TarArchive");
            }

            try
            {
                if (recurse)
                {
                    TarHeader.SetValueDefaults(sourceEntry.UserId, sourceEntry.UserName,
                                               sourceEntry.GroupId, sourceEntry.GroupName);
                }
                WriteEntryCore(sourceEntry, recurse);
            }
            finally
            {
                if (recurse)
                {
                    TarHeader.RestoreSetValues();
                }
            }
        }

        /// <summary>
        /// Write an entry to the archive. This method will call the putNextEntry
        /// and then write the contents of the entry, and finally call closeEntry()
        /// for entries that are files. For directories, it will call putNextEntry(),
        /// and then, if the recurse flag is true, process each entry that is a
        /// child of the directory.
        /// </summary>
        /// <param name="sourceEntry">
        /// The TarEntry representing the entry to write to the archive.
        /// </param>
        /// <param name="recurse">
        /// If true, process the children of directory entries.
        /// </param>
        private void WriteEntryCore(TarEntry sourceEntry, bool recurse)
        {
            string tempFileName = null;
            string entryFilename = sourceEntry.File;

            TarEntry entry = (TarEntry)sourceEntry.Clone();

            if (applyUserInfoOverrides)
            {
                entry.GroupId = groupId;
                entry.GroupName = groupName;
                entry.UserId = userId;
                entry.UserName = userName;
            }

            OnProgressMessageEvent(entry, null);

            if (asciiTranslate && !entry.IsDirectory)
            {
                if (!IsBinary(entryFilename))
                {
                    tempFileName = PathUtils.GetTempFileName();

                    using (StreamReader inStream = File.OpenText(entryFilename))
                    {
                        using Stream outStream = File.Create(tempFileName);
                        while (true)
                        {
                            string line = inStream.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            byte[] data = Encoding.ASCII.GetBytes(line);
                            outStream.Write(data, 0, data.Length);
                            outStream.WriteByte((byte)'\n');
                        }

                        outStream.Flush();
                    }

                    entry.Size = new FileInfo(tempFileName).Length;
                    entryFilename = tempFileName;
                }
            }

            string newName = null;

            if (!string.IsNullOrEmpty(rootPath))
            {
                if (entry.Name.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    newName = entry.Name[(rootPath.Length + 1)..];
                }
            }

            if (pathPrefix != null)
            {
                newName = newName == null ? pathPrefix + "/" + entry.Name : pathPrefix + "/" + newName;
            }

            if (newName != null)
            {
                entry.Name = newName;
            }

            tarOut.PutNextEntry(entry);

            if (entry.IsDirectory)
            {
                if (recurse)
                {
                    TarEntry[] list = entry.GetDirectoryEntries();
                    for (int i = 0; i < list.Length; ++i)
                    {
                        WriteEntryCore(list[i], recurse);
                    }
                }
            }
            else
            {
                using (Stream inputStream = File.OpenRead(entryFilename))
                {
                    byte[] localBuffer = new byte[32 * 1024];
                    while (true)
                    {
                        int numRead = inputStream.Read(localBuffer, 0, localBuffer.Length);

                        if (numRead <= 0)
                        {
                            break;
                        }

                        tarOut.Write(localBuffer, 0, numRead);
                    }
                }

                if (!string.IsNullOrEmpty(tempFileName))
                {
                    File.Delete(tempFileName);
                }

                tarOut.CloseEntry();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the FileStream and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (disposing)
                {
                    if (tarOut != null)
                    {
                        tarOut.Flush();
                        tarOut.Dispose();
                    }

                    if (tarIn != null)
                    {
                        tarIn.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Closes the archive and releases any associated resources.
        /// </summary>
        public virtual void Close()
        {
            Dispose(true);
        }

        /// <summary>
        /// Ensures that resources are freed and other cleanup operations are performed
        /// when the garbage collector reclaims the <see cref="TarArchive"/>.
        /// </summary>
        ~TarArchive()
        {
            Dispose(false);
        }

        private static void EnsureDirectoryExists(string directoryName)
        {
            if (!Directory.Exists(directoryName))
            {
                try
                {
                    _ = Directory.CreateDirectory(directoryName);
                }
                catch (Exception e)
                {
                    throw new TarException("Exception creating directory '" + directoryName + "', " + e.Message, e);
                }
            }
        }

        // TODO: TarArchive - Is there a better way to test for a text file?
        // It no longer reads entire files into memory but is still a weak test!
        // This assumes that byte values 0-7, 14-31 or 255 are binary
        // and that all non text files contain one of these values
        private static bool IsBinary(string filename)
        {
            using (FileStream fs = File.OpenRead(filename))
            {
                int sampleSize = Math.Min(4096, (int)fs.Length);
                byte[] content = new byte[sampleSize];

                int bytesRead = fs.Read(content, 0, sampleSize);

                for (int i = 0; i < bytesRead; ++i)
                {
                    byte b = content[i];
                    if (b is < 8 or (> 13 and < 32) or 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #region Instance Fields

        private bool keepOldFiles;
        private bool asciiTranslate;

        private int userId;
        private string userName = string.Empty;
        private int groupId;
        private string groupName = string.Empty;

        private string rootPath;
        private string pathPrefix;

        private bool applyUserInfoOverrides;

        private readonly TarInputStream tarIn;
        private readonly TarOutputStream tarOut;
        private bool isDisposed;

        #endregion Instance Fields
    }
    /// <summary>
    /// The TarBuffer class implements the tar archive concept
    /// of a buffered input stream. This concept goes back to the
    /// days of blocked tape drives and special io devices. In the
    /// C# universe, the only real function that this class
    /// performs is to ensure that files have the correct "record"
    /// size, or other tars will complain.
    /// <p>
    /// You should never have a need to access this class directly.
    /// TarBuffers are created by Tar IO Streams.
    /// </p>
    /// </summary>
    public class TarBuffer
    {
        /* A quote from GNU tar man file on blocking and records
		   A `tar' archive file contains a series of blocks.  Each block
		contains `BLOCKSIZE' bytes.  Although this format may be thought of as
		being on magnetic tape, other media are often used.
		   Each file archived is represented by a header block which describes
		the file, followed by zero or more blocks which give the contents of
		the file.  At the end of the archive file there may be a block filled
		with binary zeros as an end-of-file marker.  A reasonable system should
		write a block of zeros at the end, but must not assume that such a
		block exists when reading an archive.
		   The blocks may be "blocked" for physical I/O operations.  Each
		record of N blocks is written with a single 'write ()'
		operation.  On magnetic tapes, the result of such a write is a single
		record.  When writing an archive, the last record of blocks should be
		written at the full size, with blocks after the zero block containing
		all zeros.  When reading an archive, a reasonable system should
		properly handle an archive whose last record is shorter than the rest,
		or which contains garbage records after a zero block.
		*/

        #region Constants

        /// <summary>
        /// The size of a block in a tar archive in bytes.
        /// </summary>
        /// <remarks>This is 512 bytes.</remarks>
        public const int BlockSize = 512;

        /// <summary>
        /// The number of blocks in a default record.
        /// </summary>
        /// <remarks>
        /// The default value is 20 blocks per record.
        /// </remarks>
        public const int DefaultBlockFactor = 20;

        /// <summary>
        /// The size in bytes of a default record.
        /// </summary>
        /// <remarks>
        /// The default size is 10KB.
        /// </remarks>
        public const int DefaultRecordSize = BlockSize * DefaultBlockFactor;

        #endregion Constants

        /// <summary>
        /// Get the record size for this buffer
        /// </summary>
        /// <value>The record size in bytes.
        /// This is equal to the <see cref="BlockFactor"/> multiplied by the <see cref="BlockSize"/></value>
        public int RecordSize { get; private set; } = DefaultRecordSize;

        /// <summary>
        /// Get the TAR Buffer's record size.
        /// </summary>
        /// <returns>The record size in bytes.
        /// This is equal to the <see cref="BlockFactor"/> multiplied by the <see cref="BlockSize"/></returns>
        [Obsolete("Use RecordSize property instead")]
        public int GetRecordSize()
        {
            return RecordSize;
        }

        /// <summary>
        /// Get the Blocking factor for the buffer
        /// </summary>
        /// <value>This is the number of blocks in each record.</value>
        public int BlockFactor { get; private set; } = DefaultBlockFactor;

        /// <summary>
        /// Get the TAR Buffer's block factor
        /// </summary>
        /// <returns>The block factor; the number of blocks per record.</returns>
        [Obsolete("Use BlockFactor property instead")]
        public int GetBlockFactor()
        {
            return BlockFactor;
        }

        /// <summary>
        /// Construct a default TarBuffer
        /// </summary>
        protected TarBuffer()
        {
        }

        /// <summary>
        /// Create TarBuffer for reading with default BlockFactor
        /// </summary>
        /// <param name="inputStream">Stream to buffer</param>
        /// <returns>A new <see cref="TarBuffer"/> suitable for input.</returns>
        public static TarBuffer CreateInputTarBuffer(Stream inputStream)
        {
            return inputStream == null
                ? throw new ArgumentNullException(nameof(inputStream))
                : CreateInputTarBuffer(inputStream, DefaultBlockFactor);
        }

        /// <summary>
        /// Construct TarBuffer for reading inputStream setting BlockFactor
        /// </summary>
        /// <param name="inputStream">Stream to buffer</param>
        /// <param name="blockFactor">Blocking factor to apply</param>
        /// <returns>A new <see cref="TarBuffer"/> suitable for input.</returns>
        public static TarBuffer CreateInputTarBuffer(Stream inputStream, int blockFactor)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (blockFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockFactor), "Factor cannot be negative");
            }

            TarBuffer tarBuffer = new()
            {
                inputStream = inputStream,
                outputStream = null
            };
            tarBuffer.Initialize(blockFactor);

            return tarBuffer;
        }

        /// <summary>
        /// Construct TarBuffer for writing with default BlockFactor
        /// </summary>
        /// <param name="outputStream">output stream for buffer</param>
        /// <returns>A new <see cref="TarBuffer"/> suitable for output.</returns>
        public static TarBuffer CreateOutputTarBuffer(Stream outputStream)
        {
            return outputStream == null
                ? throw new ArgumentNullException(nameof(outputStream))
                : CreateOutputTarBuffer(outputStream, DefaultBlockFactor);
        }

        /// <summary>
        /// Construct TarBuffer for writing Tar output to streams.
        /// </summary>
        /// <param name="outputStream">Output stream to write to.</param>
        /// <param name="blockFactor">Blocking factor to apply</param>
        /// <returns>A new <see cref="TarBuffer"/> suitable for output.</returns>
        public static TarBuffer CreateOutputTarBuffer(Stream outputStream, int blockFactor)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (blockFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockFactor), "Factor cannot be negative");
            }

            TarBuffer tarBuffer = new()
            {
                inputStream = null,
                outputStream = outputStream
            };
            tarBuffer.Initialize(blockFactor);

            return tarBuffer;
        }

        /// <summary>
        /// Initialization common to all constructors.
        /// </summary>
        private void Initialize(int archiveBlockFactor)
        {
            BlockFactor = archiveBlockFactor;
            RecordSize = archiveBlockFactor * BlockSize;
            recordBuffer = ArrayPool<byte>.Shared.Rent(RecordSize);

            if (inputStream != null)
            {
                CurrentRecord = -1;
                CurrentBlock = BlockFactor;
            }
            else
            {
                CurrentRecord = 0;
                CurrentBlock = 0;
            }
        }

        /// <summary>
        /// Determine if an archive block indicates End of Archive. End of
        /// archive is indicated by a block that consists entirely of null bytes.
        /// All remaining blocks for the record should also be null's
        /// However some older tars only do a couple of null blocks (Old GNU tar for one)
        /// and also partial records
        /// </summary>
        /// <param name = "block">The data block to check.</param>
        /// <returns>Returns true if the block is an EOF block; false otherwise.</returns>
        [Obsolete("Use IsEndOfArchiveBlock instead")]
        public bool IsEOFBlock(byte[] block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (block.Length != BlockSize)
            {
                throw new ArgumentException("block length is invalid");
            }

            for (int i = 0; i < BlockSize; ++i)
            {
                if (block[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determine if an archive block indicates the End of an Archive has been reached.
        /// End of archive is indicated by a block that consists entirely of null bytes.
        /// All remaining blocks for the record should also be null's
        /// However some older tars only do a couple of null blocks (Old GNU tar for one)
        /// and also partial records
        /// </summary>
        /// <param name = "block">The data block to check.</param>
        /// <returns>Returns true if the block is an EOF block; false otherwise.</returns>
        public static bool IsEndOfArchiveBlock(byte[] block)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (block.Length != BlockSize)
            {
                throw new ArgumentException("block length is invalid");
            }

            for (int i = 0; i < BlockSize; ++i)
            {
                if (block[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Skip over a block on the input stream.
        /// </summary>
        public void SkipBlock()
        {
            SkipBlockAsync(CT.None, false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Skip over a block on the input stream.
        /// </summary>
        public Task SkipBlockAsync(CT ct)
        {
            return SkipBlockAsync(ct, true).AsTask();
        }

        private async ValueTask SkipBlockAsync(CT ct, bool isAsync)
        {
            if (inputStream == null)
            {
                throw new TarException("no input stream defined");
            }

            if (CurrentBlock >= BlockFactor)
            {
                if (!await ReadRecordAsync(ct, isAsync))
                {
                    throw new TarException("Failed to read a record");
                }
            }

            CurrentBlock++;
        }

        /// <summary>
        /// Read a block from the input stream.
        /// </summary>
        /// <returns>
        /// The block of data read.
        /// </returns>
        public byte[] ReadBlock()
        {
            if (inputStream == null)
            {
                throw new TarException("TarBuffer.ReadBlock - no input stream defined");
            }

            if (CurrentBlock >= BlockFactor)
            {
                if (!ReadRecordAsync(CT.None, false).GetAwaiter().GetResult())
                {
                    throw new TarException("Failed to read a record");
                }
            }

            byte[] result = new byte[BlockSize];

            Array.Copy(recordBuffer, CurrentBlock * BlockSize, result, 0, BlockSize);
            CurrentBlock++;
            return result;
        }

        internal async ValueTask ReadBlockIntAsync(byte[] buffer, CT ct, bool isAsync)
        {
            if (buffer.Length != BlockSize)
            {
                throw new ArgumentException("BUG: buffer must have length BlockSize");
            }

            if (inputStream == null)
            {
                throw new TarException("TarBuffer.ReadBlock - no input stream defined");
            }

            if (CurrentBlock >= BlockFactor)
            {
                if (!await ReadRecordAsync(ct, isAsync))
                {
                    throw new TarException("Failed to read a record");
                }
            }

            recordBuffer.AsSpan().Slice(CurrentBlock * BlockSize, BlockSize).CopyTo(buffer);
            CurrentBlock++;
        }

        /// <summary>
        /// Read a record from data stream.
        /// </summary>
        /// <returns>
        /// false if End-Of-File, else true.
        /// </returns>
        private async ValueTask<bool> ReadRecordAsync(CT ct, bool isAsync)
        {
            if (inputStream == null)
            {
                throw new TarException("no input stream defined");
            }

            CurrentBlock = 0;

            int offset = 0;
            int bytesNeeded = RecordSize;

            while (bytesNeeded > 0)
            {
                long numBytes = isAsync
                    ? await inputStream.ReadAsync(recordBuffer, offset, bytesNeeded, ct)
                    : inputStream.Read(recordBuffer, offset, bytesNeeded);

                //
                // NOTE
                // We have found EOF, and the record is not full!
                //
                // This is a broken archive. It does not follow the standard
                // blocking algorithm. However, because we are generous, and
                // it requires little effort, we will simply ignore the error
                // and continue as if the entire record were read. This does
                // not appear to break anything upstream. We used to return
                // false in this case.
                //
                // Thanks to 'Yohann.Roussel@alcatel.fr' for this fix.
                //
                if (numBytes <= 0)
                {
                    break;
                }

                offset += (int)numBytes;
                bytesNeeded -= (int)numBytes;
            }

            CurrentRecord++;
            return true;
        }

        /// <summary>
        /// Get the current block number, within the current record, zero based.
        /// </summary>
        /// <remarks>Block numbers are zero based values</remarks>
        /// <seealso cref="RecordSize"/>
        public int CurrentBlock { get; private set; }

        /// <summary>
        /// Gets or sets a flag indicating ownership of underlying stream.
        /// When the flag is true <see cref="Close" /> will close the underlying stream also.
        /// </summary>
        /// <remarks>The default value is true.</remarks>
        public bool IsStreamOwner { get; set; } = true;

        /// <summary>
        /// Get the current block number, within the current record, zero based.
        /// </summary>
        /// <returns>
        /// The current zero based block number.
        /// </returns>
        /// <remarks>
        /// The absolute block number = (<see cref="GetCurrentRecordNum">record number</see> * <see cref="BlockFactor">block factor</see>) + <see cref="GetCurrentBlockNum">block number</see>.
        /// </remarks>
        [Obsolete("Use CurrentBlock property instead")]
        public int GetCurrentBlockNum()
        {
            return CurrentBlock;
        }

        /// <summary>
        /// Get the current record number.
        /// </summary>
        /// <returns>
        /// The current zero based record number.
        /// </returns>
        public int CurrentRecord { get; private set; }

        /// <summary>
        /// Get the current record number.
        /// </summary>
        /// <returns>
        /// The current zero based record number.
        /// </returns>
        [Obsolete("Use CurrentRecord property instead")]
        public int GetCurrentRecordNum()
        {
            return CurrentRecord;
        }

        /// <summary>
        /// Write a block of data to the archive.
        /// </summary>
        /// <param name="block">
        /// The data to write to the archive.
        /// </param>
        /// <param name="ct"></param>
        public ValueTask WriteBlockAsync(byte[] block, CT ct)
        {
            return WriteBlockAsync(block, 0, ct);
        }

        /// <summary>
        /// Write a block of data to the archive.
        /// </summary>
        /// <param name="block">
        /// The data to write to the archive.
        /// </param>
        public void WriteBlock(byte[] block)
        {
            WriteBlock(block, 0);
        }

        /// <summary>
        /// Write an archive record to the archive, where the record may be
        /// inside of a larger array buffer. The buffer must be "offset plus
        /// record size" long.
        /// </summary>
        /// <param name="buffer">
        /// The buffer containing the record data to write.
        /// </param>
        /// <param name="offset">
        /// The offset of the record data within buffer.
        /// </param>
        /// <param name="ct"></param>
        public ValueTask WriteBlockAsync(byte[] buffer, int offset, CT ct)
        {
            return WriteBlockAsync(buffer, offset, ct, true);
        }

        /// <summary>
        /// Write an archive record to the archive, where the record may be
        /// inside of a larger array buffer. The buffer must be "offset plus
        /// record size" long.
        /// </summary>
        /// <param name="buffer">
        /// The buffer containing the record data to write.
        /// </param>
        /// <param name="offset">
        /// The offset of the record data within buffer.
        /// </param>
        public void WriteBlock(byte[] buffer, int offset)
        {
            WriteBlockAsync(buffer, offset, CT.None, false).GetAwaiter().GetResult();
        }

        internal async ValueTask WriteBlockAsync(byte[] buffer, int offset, CT ct, bool isAsync)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (outputStream == null)
            {
                throw new TarException("TarBuffer.WriteBlock - no output stream defined");
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (offset + BlockSize > buffer.Length)
            {
                string errorText = string.Format(
                    "TarBuffer.WriteBlock - record has length '{0}' with offset '{1}' which is less than the record size of '{2}'",
                    buffer.Length, offset, RecordSize);
                throw new TarException(errorText);
            }

            if (CurrentBlock >= BlockFactor)
            {
                await WriteRecordAsync(CT.None, isAsync);
            }

            Array.Copy(buffer, offset, recordBuffer, CurrentBlock * BlockSize, BlockSize);

            CurrentBlock++;
        }

        /// <summary>
        /// Write a TarBuffer record to the archive.
        /// </summary>
        private async ValueTask WriteRecordAsync(CT ct, bool isAsync)
        {
            if (outputStream == null)
            {
                throw new TarException("TarBuffer.WriteRecord no output stream defined");
            }

            if (isAsync)
            {
                await outputStream.WriteAsync(recordBuffer, 0, RecordSize, ct);
                await outputStream.FlushAsync(ct);
            }
            else
            {
                outputStream.Write(recordBuffer, 0, RecordSize);
                outputStream.Flush();
            }

            CurrentBlock = 0;
            CurrentRecord++;
        }

        /// <summary>
        /// WriteFinalRecord writes the current record buffer to output any unwritten data is present.
        /// </summary>
        /// <remarks>Any trailing bytes are set to zero which is by definition correct behaviour
        /// for the end of a tar stream.</remarks>
        private async ValueTask WriteFinalRecordAsync(CT ct, bool isAsync)
        {
            if (outputStream == null)
            {
                throw new TarException("TarBuffer.WriteFinalRecord no output stream defined");
            }

            if (CurrentBlock > 0)
            {
                int dataBytes = CurrentBlock * BlockSize;
                Array.Clear(recordBuffer, dataBytes, RecordSize - dataBytes);
                await WriteRecordAsync(ct, isAsync);
            }

            if (isAsync)
            {
                await outputStream.FlushAsync(ct);
            }
            else
            {
                outputStream.Flush();
            }
        }

        /// <summary>
        /// Close the TarBuffer. If this is an output buffer, also flush the
        /// current block before closing.
        /// </summary>
        public void Close()
        {
            CloseAsync(CT.None, false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Close the TarBuffer. If this is an output buffer, also flush the
        /// current block before closing.
        /// </summary>
        public Task CloseAsync(CT ct)
        {
            return CloseAsync(ct, true).AsTask();
        }

        private async ValueTask CloseAsync(CT ct, bool isAsync)
        {
            if (outputStream != null)
            {
                await WriteFinalRecordAsync(ct, isAsync);

                if (IsStreamOwner)
                {
                    if (isAsync)
                    {
#if NETSTANDARD2_1_OR_GREATER
						await outputStream.DisposeAsync();
#else
                        outputStream.Dispose();
#endif
                    }
                    else
                    {
                        outputStream.Dispose();
                    }
                }

                outputStream = null;
            }
            else if (inputStream != null)
            {
                if (IsStreamOwner)
                {
                    if (isAsync)
                    {
#if NETSTANDARD2_1_OR_GREATER
						await inputStream.DisposeAsync();
#else
                        inputStream.Dispose();
#endif
                    }
                    else
                    {
                        inputStream.Dispose();
                    }
                }

                inputStream = null;
            }

            ArrayPool<byte>.Shared.Return(recordBuffer);
        }

        #region Instance Fields

        private Stream inputStream;
        private Stream outputStream;

        private byte[] recordBuffer;

        #endregion Instance Fields
    }
    /// <summary>
    /// This exception is used to indicate that there is a problem
    /// with a TAR archive header.
    /// </summary>
    [Serializable]
    public class InvalidHeaderException : TarException
    {
        /// <summary>
        /// Initialise a new instance of the InvalidHeaderException class.
        /// </summary>
        public InvalidHeaderException()
        {
        }

        /// <summary>
        /// Initialises a new instance of the InvalidHeaderException class with a specified message.
        /// </summary>
        /// <param name="message">Message describing the exception cause.</param>
        public InvalidHeaderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialise a new instance of InvalidHeaderException
        /// </summary>
        /// <param name="message">Message describing the problem.</param>
        /// <param name="exception">The exception that is the cause of the current exception.</param>
        public InvalidHeaderException(string message, Exception exception)
            : base(message, exception)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidHeaderException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected InvalidHeaderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// TarException represents exceptions specific to Tar classes and code.
    /// </summary>
    [Serializable]
    public class TarException : SharpZipBaseException
    {
        /// <summary>
        /// Initialise a new instance of <see cref="TarException" />.
        /// </summary>
        public TarException()
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="TarException" /> with its message string.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that describes the error.</param>
        public TarException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialise a new instance of <see cref="TarException" />.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that describes the error.</param>
        /// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
        public TarException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the TarException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected TarException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// Indicates that a value was outside of the expected range when decoding an input stream
    /// </summary>
    [Serializable]
    public class ValueOutOfRangeException : StreamDecodingException
    {
        /// <summary>
        /// Initializes a new instance of the ValueOutOfRangeException class naming the causing variable
        /// </summary>
        /// <param name="nameOfValue">Name of the variable, use: nameof()</param>
        public ValueOutOfRangeException(string nameOfValue)
            : base($"{nameOfValue} out of range") { }

        /// <summary>
        /// Initializes a new instance of the ValueOutOfRangeException class naming the causing variable,
        /// it's current value and expected range.
        /// </summary>
        /// <param name="nameOfValue">Name of the variable, use: nameof()</param>
        /// <param name="value">The invalid value</param>
        /// <param name="maxValue">Expected maximum value</param>
        /// <param name="minValue">Expected minimum value</param>
        public ValueOutOfRangeException(string nameOfValue, long value, long maxValue, long minValue = 0)
            : this(nameOfValue, value.ToString(), maxValue.ToString(), minValue.ToString()) { }

        /// <summary>
        /// Initializes a new instance of the ValueOutOfRangeException class naming the causing variable,
        /// it's current value and expected range.
        /// </summary>
        /// <param name="nameOfValue">Name of the variable, use: nameof()</param>
        /// <param name="value">The invalid value</param>
        /// <param name="maxValue">Expected maximum value</param>
        /// <param name="minValue">Expected minimum value</param>
        public ValueOutOfRangeException(string nameOfValue, string value, string maxValue, string minValue = "0") :
            base($"{nameOfValue} out of range: {value}, should be {minValue}..{maxValue}")
        { }

        private ValueOutOfRangeException()
        {
        }

        private ValueOutOfRangeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ValueOutOfRangeException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected ValueOutOfRangeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// Indicates that the input stream could not decoded due to the stream ending before enough data had been provided
    /// </summary>
    [Serializable]
    public class UnexpectedEndOfStreamException : StreamDecodingException
    {
        private const string GenericMessage = "Input stream ended unexpectedly";

        /// <summary>
        /// Initializes a new instance of the UnexpectedEndOfStreamException with a generic message
        /// </summary>
        public UnexpectedEndOfStreamException() : base(GenericMessage) { }

        /// <summary>
        /// Initializes a new instance of the UnexpectedEndOfStreamException class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        public UnexpectedEndOfStreamException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the UnexpectedEndOfStreamException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="innerException">The inner exception</param>
        public UnexpectedEndOfStreamException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the UnexpectedEndOfStreamException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected UnexpectedEndOfStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// Indicates that the input stream could not decoded due to known library incompability or missing features
    /// </summary>
    [Serializable]
    public class StreamUnsupportedException : StreamDecodingException
    {
        private const string GenericMessage = "Input stream is in a unsupported format";

        /// <summary>
        /// Initializes a new instance of the StreamUnsupportedException with a generic message
        /// </summary>
        public StreamUnsupportedException() : base(GenericMessage) { }

        /// <summary>
        /// Initializes a new instance of the StreamUnsupportedException class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        public StreamUnsupportedException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the StreamUnsupportedException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="innerException">The inner exception</param>
        public StreamUnsupportedException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the StreamUnsupportedException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected StreamUnsupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// Indicates that an error occurred during decoding of a input stream due to corrupt
    /// data or (unintentional) library incompatibility.
    /// </summary>
    [Serializable]
    public class StreamDecodingException : SharpZipBaseException
    {
        private const string GenericMessage = "Input stream could not be decoded";

        /// <summary>
        /// Initializes a new instance of the StreamDecodingException with a generic message
        /// </summary>
        public StreamDecodingException() : base(GenericMessage) { }

        /// <summary>
        /// Initializes a new instance of the StreamDecodingException class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        public StreamDecodingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the StreamDecodingException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="innerException">The inner exception</param>
        public StreamDecodingException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the StreamDecodingException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected StreamDecodingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// SharpZipBaseException is the base exception class for SharpZipLib.
    /// All library exceptions are derived from this.
    /// </summary>
    /// <remarks>NOTE: Not all exceptions thrown will be derived from this class.
    /// A variety of other exceptions are possible for example <see cref="ArgumentNullException"></see></remarks>
    [Serializable]
    public class SharpZipBaseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the SharpZipBaseException class.
        /// </summary>
        public SharpZipBaseException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SharpZipBaseException class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        public SharpZipBaseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SharpZipBaseException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="innerException">The inner exception</param>
        public SharpZipBaseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SharpZipBaseException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected SharpZipBaseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    internal class StringBuilderPool
    {
        public static StringBuilderPool Instance { get; } = new StringBuilderPool();
        private readonly ConcurrentQueue<StringBuilder> pool = new();

        public StringBuilder Rent()
        {
            return pool.TryDequeue(out StringBuilder builder) ? builder : new StringBuilder();
        }

        public void Return(StringBuilder builder)
        {
            _ = builder.Clear();
            pool.Enqueue(builder);
        }
    }

    /// <summary>
    /// PathUtils provides simple utilities for handling paths.
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Remove any path root present in the path
        /// </summary>
        /// <param name="path">A <see cref="string"/> containing path information.</param>
        /// <returns>The path with the root removed if it was present; path otherwise.</returns>
        public static string DropPathRoot(string path)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            // If the first character after the root is a ':', .NET < 4.6.2 throws
            bool cleanRootSep = path.Length >= 3 && path[1] == ':' && path[2] == ':';

            // Replace any invalid path characters with '_' to prevent Path.GetPathRoot from throwing.
            // Only pass the first 258 (should be 260, but that still throws for some reason) characters
            // as .NET < 4.6.2 throws on longer paths
            string cleanPath = new(path.Take(258)
                .Select((c, i) => invalidChars.Contains(c) || (i == 2 && cleanRootSep) ? '_' : c).ToArray());

            int stripLength = Path.GetPathRoot(cleanPath).Length;
            while (path.Length > stripLength && (path[stripLength] == '/' || path[stripLength] == '\\'))
            {
                stripLength++;
            }

            return path[stripLength..];
        }

        /// <summary>
        /// Returns a random file name in the users temporary directory, or in directory of <paramref name="original"/> if specified
        /// </summary>
        /// <param name="original">If specified, used as the base file name for the temporary file</param>
        /// <returns>Returns a temporary file name</returns>
        public static string GetTempFileName(string original = null)
        {
            string fileName;
            string tempPath = Path.GetTempPath();

            do
            {
                fileName = original == null
                    ? Path.Combine(tempPath, Path.GetRandomFileName())
                    : $"{original}.{Path.GetRandomFileName()}";
            } while (File.Exists(fileName));

            return fileName;
        }
    }
    /// <summary>
    /// PathFilter filters directories and files using a form of <see cref="Regex">regular expressions</see>
    /// by full path name.
    /// See <see cref="NameFilter">NameFilter</see> for more detail on filtering.
    /// </summary>
    public class PathFilter : IScanFilter
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of <see cref="PathFilter"></see>.
        /// </summary>
        /// <param name="filter">The <see cref="NameFilter">filter</see> expression to apply.</param>
        public PathFilter(string filter)
        {
            nameFilter_ = new NameFilter(filter);
        }

        #endregion Constructors

        #region IScanFilter Members

        /// <summary>
        /// Test a name to see if it matches the filter.
        /// </summary>
        /// <param name="name">The name to test.</param>
        /// <returns>True if the name matches, false otherwise.</returns>
        /// <remarks><see cref="Path.GetFullPath(string)"/> is used to get the full path before matching.</remarks>
        public virtual bool IsMatch(string name)
        {
            bool result = false;

            if (name != null)
            {
                string cooked = name.Length > 0 ? Path.GetFullPath(name) : "";
                result = nameFilter_.IsMatch(cooked);
            }
            return result;
        }

        private readonly

        #endregion IScanFilter Members

        #region Instance Fields

        NameFilter nameFilter_;

        #endregion Instance Fields
    }

    /// <summary>
    /// ExtendedPathFilter filters based on name, file size, and the last write time of the file.
    /// </summary>
    /// <remarks>Provides an example of how to customise filtering.</remarks>
    public class ExtendedPathFilter : PathFilter
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of ExtendedPathFilter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="minSize">The minimum file size to include.</param>
        /// <param name="maxSize">The maximum file size to include.</param>
        public ExtendedPathFilter(string filter,
            long minSize, long maxSize)
            : base(filter)
        {
            MinSize = minSize;
            MaxSize = maxSize;
        }

        /// <summary>
        /// Initialise a new instance of ExtendedPathFilter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="minDate">The minimum <see cref="DateTime"/> to include.</param>
        /// <param name="maxDate">The maximum <see cref="DateTime"/> to include.</param>
        public ExtendedPathFilter(string filter,
            DateTime minDate, DateTime maxDate)
            : base(filter)
        {
            MinDate = minDate;
            MaxDate = maxDate;
        }

        /// <summary>
        /// Initialise a new instance of ExtendedPathFilter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="minSize">The minimum file size to include.</param>
        /// <param name="maxSize">The maximum file size to include.</param>
        /// <param name="minDate">The minimum <see cref="DateTime"/> to include.</param>
        /// <param name="maxDate">The maximum <see cref="DateTime"/> to include.</param>
        public ExtendedPathFilter(string filter,
            long minSize, long maxSize,
            DateTime minDate, DateTime maxDate)
            : base(filter)
        {
            MinSize = minSize;
            MaxSize = maxSize;
            MinDate = minDate;
            MaxDate = maxDate;
        }

        #endregion Constructors

        #region IScanFilter Members

        /// <summary>
        /// Test a filename to see if it matches the filter.
        /// </summary>
        /// <param name="name">The filename to test.</param>
        /// <returns>True if the filter matches, false otherwise.</returns>
        /// <exception cref="FileNotFoundException">The <see paramref="fileName"/> doesnt exist</exception>
        public override bool IsMatch(string name)
        {
            bool result = base.IsMatch(name);

            if (result)
            {
                FileInfo fileInfo = new(name);
                result =
                    MinSize <= fileInfo.Length &&
                    MaxSize >= fileInfo.Length &&
                    MinDate <= fileInfo.LastWriteTime &&
                    MaxDate >= fileInfo.LastWriteTime
                    ;
            }
            return result;
        }

        #endregion IScanFilter Members

        #region Properties

        /// <summary>
        /// Get/set the minimum size/length for a file that will match this filter.
        /// </summary>
        /// <remarks>The default value is zero.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">value is less than zero; greater than <see cref="MaxSize"/></exception>
        public long MinSize
        {
            get => minSize_;
            set
            {
                if (value < 0 || maxSize_ < value)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                minSize_ = value;
            }
        }

        /// <summary>
        /// Get/set the maximum size/length for a file that will match this filter.
        /// </summary>
        /// <remarks>The default value is <see cref="long.MaxValue"/></remarks>
        /// <exception cref="ArgumentOutOfRangeException">value is less than zero or less than <see cref="MinSize"/></exception>
        public long MaxSize
        {
            get => maxSize_;
            set
            {
                if (value < 0 || minSize_ > value)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                maxSize_ = value;
            }
        }

        /// <summary>
        /// Get/set the minimum <see cref="DateTime"/> value that will match for this filter.
        /// </summary>
        /// <remarks>Files with a LastWrite time less than this value are excluded by the filter.</remarks>
        public DateTime MinDate
        {
            get => minDate_;

            set
            {
                if (value > maxDate_)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Exceeds MaxDate");
                }

                minDate_ = value;
            }
        }

        /// <summary>
        /// Get/set the maximum <see cref="DateTime"/> value that will match for this filter.
        /// </summary>
        /// <remarks>Files with a LastWrite time greater than this value are excluded by the filter.</remarks>
        public DateTime MaxDate
        {
            get => maxDate_;

            set
            {
                if (minDate_ > value)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Exceeds MinDate");
                }

                maxDate_ = value;
            }
        }

        #endregion Properties

        #region Instance Fields

        private long minSize_;
        private long maxSize_ = long.MaxValue;
        private DateTime minDate_ = DateTime.MinValue;
        private DateTime maxDate_ = DateTime.MaxValue;

        #endregion Instance Fields
    }

    /// <summary>
    /// NameAndSizeFilter filters based on name and file size.
    /// </summary>
    /// <remarks>A sample showing how filters might be extended.</remarks>
    [Obsolete("Use ExtendedPathFilter instead")]
    public class NameAndSizeFilter : PathFilter
    {
        /// <summary>
        /// Initialise a new instance of NameAndSizeFilter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="minSize">The minimum file size to include.</param>
        /// <param name="maxSize">The maximum file size to include.</param>
        public NameAndSizeFilter(string filter, long minSize, long maxSize)
            : base(filter)
        {
            MinSize = minSize;
            MaxSize = maxSize;
        }

        /// <summary>
        /// Test a filename to see if it matches the filter.
        /// </summary>
        /// <param name="name">The filename to test.</param>
        /// <returns>True if the filter matches, false otherwise.</returns>
        public override bool IsMatch(string name)
        {
            bool result = base.IsMatch(name);

            if (result)
            {
                FileInfo fileInfo = new(name);
                long length = fileInfo.Length;
                result =
                    MinSize <= length &&
                    MaxSize >= length;
            }
            return result;
        }

        /// <summary>
        /// Get/set the minimum size for a file that will match this filter.
        /// </summary>
        public long MinSize
        {
            get => minSize_;
            set
            {
                if (value < 0 || maxSize_ < value)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                minSize_ = value;
            }
        }

        /// <summary>
        /// Get/set the maximum size for a file that will match this filter.
        /// </summary>
        public long MaxSize
        {
            get => maxSize_;
            set
            {
                if (value < 0 || minSize_ > value)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                maxSize_ = value;
            }
        }

        #region Instance Fields

        private long minSize_;
        private long maxSize_ = long.MaxValue;

        #endregion Instance Fields
    }
    /// <summary>
    /// NameFilter is a string matching class which allows for both positive and negative
    /// matching.
    /// A filter is a sequence of independant <see cref="Regex">regular expressions</see> separated by semi-colons ';'.
    /// To include a semi-colon it may be quoted as in \;. Each expression can be prefixed by a plus '+' sign or
    /// a minus '-' sign to denote the expression is intended to include or exclude names.
    /// If neither a plus or minus sign is found include is the default.
    /// A given name is tested for inclusion before checking exclusions.  Only names matching an include spec
    /// and not matching an exclude spec are deemed to match the filter.
    /// An empty filter matches any name.
    /// </summary>
    /// <example>The following expression includes all name ending in '.dat' with the exception of 'dummy.dat'
    /// "+\.dat$;-^dummy\.dat$"
    /// </example>
    public class NameFilter : IScanFilter
    {
        #region Constructors

        /// <summary>
        /// Construct an instance based on the filter expression passed
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        public NameFilter(string filter)
        {
            filter_ = filter;
            inclusions_ = new List<Regex>();
            exclusions_ = new List<Regex>();
            Compile();
        }

        #endregion Constructors

        /// <summary>
        /// Test a string to see if it is a valid regular expression.
        /// </summary>
        /// <param name="expression">The expression to test.</param>
        /// <returns>True if expression is a valid <see cref="Regex"/> false otherwise.</returns>
        public static bool IsValidExpression(string expression)
        {
            bool result = true;
            try
            {
                Regex exp = new(expression, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            catch (ArgumentException)
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Test an expression to see if it is valid as a filter.
        /// </summary>
        /// <param name="toTest">The filter expression to test.</param>
        /// <returns>True if the expression is valid, false otherwise.</returns>
        public static bool IsValidFilterExpression(string toTest)
        {
            bool result = true;

            try
            {
                if (toTest != null)
                {
                    string[] items = SplitQuoted(toTest);
                    for (int i = 0; i < items.Length; ++i)
                    {
                        if (items[i] != null && items[i].Length > 0)
                        {
                            string toCompile = items[i][0] == '+' ? items[i][1..] : items[i][0] == '-' ? items[i][1..] : items[i];
                            Regex testRegex = new(toCompile, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Split a string into its component pieces
        /// </summary>
        /// <param name="original">The original string</param>
        /// <returns>Returns an array of <see cref="string"/> values containing the individual filter elements.</returns>
        public static string[] SplitQuoted(string original)
        {
            char escape = '\\';
            char[] separators = { ';' };

            List<string> result = new();

            if (!string.IsNullOrEmpty(original))
            {
                int endIndex = -1;
                StringBuilder b = new();

                while (endIndex < original.Length)
                {
                    endIndex += 1;
                    if (endIndex >= original.Length)
                    {
                        result.Add(b.ToString());
                    }
                    else if (original[endIndex] == escape)
                    {
                        endIndex += 1;
                        if (endIndex >= original.Length)
                        {
                            throw new ArgumentException("Missing terminating escape character", nameof(original));
                        }
                        // include escape if this is not an escaped separator
                        if (Array.IndexOf(separators, original[endIndex]) < 0)
                        {
                            _ = b.Append(escape);
                        }

                        _ = b.Append(original[endIndex]);
                    }
                    else
                    {
                        if (Array.IndexOf(separators, original[endIndex]) >= 0)
                        {
                            result.Add(b.ToString());
                            b.Length = 0;
                        }
                        else
                        {
                            _ = b.Append(original[endIndex]);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Convert this filter to its string equivalent.
        /// </summary>
        /// <returns>The string equivalent for this filter.</returns>
        public override string ToString()
        {
            return filter_;
        }

        /// <summary>
        /// Test a value to see if it is included by the filter.
        /// </summary>
        /// <param name="name">The value to test.</param>
        /// <returns>True if the value is included, false otherwise.</returns>
        public bool IsIncluded(string name)
        {
            bool result = false;
            if (inclusions_.Count == 0)
            {
                result = true;
            }
            else
            {
                foreach (Regex r in inclusions_)
                {
                    if (r.IsMatch(name))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Test a value to see if it is excluded by the filter.
        /// </summary>
        /// <param name="name">The value to test.</param>
        /// <returns>True if the value is excluded, false otherwise.</returns>
        public bool IsExcluded(string name)
        {
            bool result = false;
            foreach (Regex r in exclusions_)
            {
                if (r.IsMatch(name))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        #region IScanFilter Members

        /// <summary>
        /// Test a value to see if it matches the filter.
        /// </summary>
        /// <param name="name">The value to test.</param>
        /// <returns>True if the value matches, false otherwise.</returns>
        public bool IsMatch(string name)
        {
            return IsIncluded(name) && !IsExcluded(name);
        }

        #endregion IScanFilter Members

        /// <summary>
        /// Compile this filter.
        /// </summary>
        private void Compile()
        {
            // TODO: Check to see if combining RE's makes it faster/smaller.
            // simple scheme would be to have one RE for inclusion and one for exclusion.
            if (filter_ == null)
            {
                return;
            }

            string[] items = SplitQuoted(filter_);
            for (int i = 0; i < items.Length; ++i)
            {
                if (items[i] != null && items[i].Length > 0)
                {
                    bool include = items[i][0] != '-';
                    string toCompile = items[i][0] == '+' ? items[i][1..] : items[i][0] == '-' ? items[i][1..] : items[i];

                    // NOTE: Regular expressions can fail to compile here for a number of reasons that cause an exception
                    // these are left unhandled here as the caller is responsible for ensuring all is valid.
                    // several functions IsValidFilterExpression and IsValidExpression are provided for such checking
                    if (include)
                    {
                        inclusions_.Add(new Regex(toCompile, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline));
                    }
                    else
                    {
                        exclusions_.Add(new Regex(toCompile, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline));
                    }
                }
            }
        }

        #region Instance Fields

        private readonly string filter_;
        private readonly List<Regex> inclusions_;
        private readonly List<Regex> exclusions_;

        #endregion Instance Fields
    }
    /// <summary>
    /// InvalidNameException is thrown for invalid names such as directory traversal paths and names with invalid characters
    /// </summary>
    [Serializable]
    public class InvalidNameException : SharpZipBaseException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidNameException class with a default error message.
        /// </summary>
        public InvalidNameException() : base("An invalid name was specified")
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidNameException class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        public InvalidNameException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidNameException class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A message describing the exception.</param>
        /// <param name="innerException">The inner exception</param>
        public InvalidNameException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidNameException class with serialized data.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual information
        /// about the source or destination.
        /// </param>
        protected InvalidNameException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
    /// <summary>
    /// Scanning filters support filtering of names.
    /// </summary>
    public interface IScanFilter
    {
        /// <summary>
        /// Test a name to see if it 'matches' the filter.
        /// </summary>
        /// <param name="name">The name to test.</param>
        /// <returns>Returns true if the name matches the filter, false if it does not match.</returns>
        bool IsMatch(string name);
    }
    /// <summary>
    /// INameTransform defines how file system names are transformed for use with archives, or vice versa.
    /// </summary>
    public interface INameTransform
    {
        /// <summary>
        /// Given a file name determine the transformed value.
        /// </summary>
        /// <param name="name">The name to transform.</param>
        /// <returns>The transformed file name.</returns>
        string TransformFile(string name);

        /// <summary>
        /// Given a directory name determine the transformed value.
        /// </summary>
        /// <param name="name">The name to transform.</param>
        /// <returns>The transformed directory name</returns>
        string TransformDirectory(string name);
    }
    #region EventArgs

    /// <summary>
    /// Event arguments for scanning.
    /// </summary>
    public class ScanEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of <see cref="ScanEventArgs"/>
        /// </summary>
        /// <param name="name">The file or directory name.</param>
        public ScanEventArgs(string name)
        {
            Name = name;
        }

        #endregion Constructors

        /// <summary>
        /// The file or directory name for this event.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get set a value indicating if scanning should continue or not.
        /// </summary>
        public bool ContinueRunning { get; set; } = true;

        #region Instance Fields


        #endregion Instance Fields
    }

    /// <summary>
    /// Event arguments during processing of a single file or directory.
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of <see cref="ScanEventArgs"/>
        /// </summary>
        /// <param name="name">The file or directory name if known.</param>
        /// <param name="processed">The number of bytes processed so far</param>
        /// <param name="target">The total number of bytes to process, 0 if not known</param>
        public ProgressEventArgs(string name, long processed, long target)
        {
            Name = name;
            Processed = processed;
            Target = target;
        }

        #endregion Constructors

        /// <summary>
        /// The name for this event if known.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get set a value indicating whether scanning should continue or not.
        /// </summary>
        public bool ContinueRunning { get; set; } = true;

        /// <summary>
        /// Get a percentage representing how much of the <see cref="Target"></see> has been processed
        /// </summary>
        /// <value>0.0 to 100.0 percent; 0 if target is not known.</value>
        public float PercentComplete
        {
            get
            {
                float result = Target <= 0 ? 0 : Processed / (float)Target * 100.0f;
                return result;
            }
        }

        /// <summary>
        /// The number of bytes processed so far
        /// </summary>
        public long Processed { get; }

        /// <summary>
        /// The number of bytes to process.
        /// </summary>
        /// <remarks>Target may be 0 or negative if the value isnt known.</remarks>
        public long Target { get; }

        #region Instance Fields


        #endregion Instance Fields
    }

    /// <summary>
    /// Event arguments for directories.
    /// </summary>
    public class DirectoryEventArgs : ScanEventArgs
    {
        #region Constructors

        /// <summary>
        /// Initialize an instance of <see cref="DirectoryEventArgs"></see>.
        /// </summary>
        /// <param name="name">The name for this directory.</param>
        /// <param name="hasMatchingFiles">Flag value indicating if any matching files are contained in this directory.</param>
        public DirectoryEventArgs(string name, bool hasMatchingFiles)
            : base(name)
        {
            HasMatchingFiles = hasMatchingFiles;
        }

        #endregion Constructors

        /// <summary>
        /// Get a value indicating if the directory contains any matching files or not.
        /// </summary>
        public bool HasMatchingFiles { get; }

        #region Instance Fields

        #endregion Instance Fields
    }

    /// <summary>
    /// Arguments passed when scan failures are detected.
    /// </summary>
    public class ScanFailureEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of <see cref="ScanFailureEventArgs"></see>
        /// </summary>
        /// <param name="name">The name to apply.</param>
        /// <param name="e">The exception to use.</param>
        public ScanFailureEventArgs(string name, Exception e)
        {
            Name = name;
            Exception = e;
            ContinueRunning = true;
        }

        #endregion Constructors

        /// <summary>
        /// The applicable name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The applicable exception.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Get / set a value indicating whether scanning should continue.
        /// </summary>
        public bool ContinueRunning { get; set; }

        #region Instance Fields


        #endregion Instance Fields
    }

    #endregion EventArgs

    #region Delegates

    /// <summary>
    /// Delegate invoked before starting to process a file.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments.</param>
    public delegate void ProcessFileHandler(object sender, ScanEventArgs e);

    /// <summary>
    /// Delegate invoked during processing of a file or directory
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments.</param>
    public delegate void ProgressHandler(object sender, ProgressEventArgs e);

    /// <summary>
    /// Delegate invoked when a file has been completely processed.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments.</param>
    public delegate void CompletedFileHandler(object sender, ScanEventArgs e);

    /// <summary>
    /// Delegate invoked when a directory failure is detected.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments.</param>
    public delegate void DirectoryFailureHandler(object sender, ScanFailureEventArgs e);

    /// <summary>
    /// Delegate invoked when a file failure is detected.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">The event arguments.</param>
    public delegate void FileFailureHandler(object sender, ScanFailureEventArgs e);

    #endregion Delegates

    /// <summary>
    /// FileSystemScanner provides facilities scanning of files and directories.
    /// </summary>
    public class FileSystemScanner
    {
        #region Constructors

        /// <summary>
        /// Initialise a new instance of <see cref="FileSystemScanner"></see>
        /// </summary>
        /// <param name="filter">The <see cref="PathFilter">file filter</see> to apply when scanning.</param>
        public FileSystemScanner(string filter)
        {
            fileFilter_ = new PathFilter(filter);
        }

        /// <summary>
        /// Initialise a new instance of <see cref="FileSystemScanner"></see>
        /// </summary>
        /// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
        /// <param name="directoryFilter">The <see cref="PathFilter"> directory filter</see> to apply.</param>
        public FileSystemScanner(string fileFilter, string directoryFilter)
        {
            fileFilter_ = new PathFilter(fileFilter);
            directoryFilter_ = new PathFilter(directoryFilter);
        }

        /// <summary>
        /// Initialise a new instance of <see cref="FileSystemScanner"></see>
        /// </summary>
        /// <param name="fileFilter">The file <see cref="IScanFilter">filter</see> to apply.</param>
        public FileSystemScanner(IScanFilter fileFilter)
        {
            fileFilter_ = fileFilter;
        }

        /// <summary>
        /// Initialise a new instance of <see cref="FileSystemScanner"></see>
        /// </summary>
        /// <param name="fileFilter">The file <see cref="IScanFilter">filter</see>  to apply.</param>
        /// <param name="directoryFilter">The directory <see cref="IScanFilter">filter</see>  to apply.</param>
        public FileSystemScanner(IScanFilter fileFilter, IScanFilter directoryFilter)
        {
            fileFilter_ = fileFilter;
            directoryFilter_ = directoryFilter;
        }

        #endregion Constructors

        #region Delegates

        /// <summary>
        /// Delegate to invoke when a directory is processed.
        /// </summary>
        public event EventHandler<DirectoryEventArgs> ProcessDirectory;

        /// <summary>
        /// Delegate to invoke when a file is processed.
        /// </summary>
        public ProcessFileHandler ProcessFile;

        /// <summary>
        /// Delegate to invoke when processing for a file has finished.
        /// </summary>
        public CompletedFileHandler CompletedFile;

        /// <summary>
        /// Delegate to invoke when a directory failure is detected.
        /// </summary>
        public DirectoryFailureHandler DirectoryFailure;

        /// <summary>
        /// Delegate to invoke when a file failure is detected.
        /// </summary>
        public FileFailureHandler FileFailure;

        #endregion Delegates

        /// <summary>
        /// Raise the DirectoryFailure event.
        /// </summary>
        /// <param name="directory">The directory name.</param>
        /// <param name="e">The exception detected.</param>
        private bool OnDirectoryFailure(string directory, Exception e)
        {
            DirectoryFailureHandler handler = DirectoryFailure;
            bool result = handler != null;
            if (result)
            {
                ScanFailureEventArgs args = new(directory, e);
                handler(this, args);
                alive_ = args.ContinueRunning;
            }
            return result;
        }

        /// <summary>
        /// Raise the FileFailure event.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <param name="e">The exception detected.</param>
        private bool OnFileFailure(string file, Exception e)
        {
            FileFailureHandler handler = FileFailure;

            bool result = handler != null;

            if (result)
            {
                ScanFailureEventArgs args = new(file, e);
                FileFailure(this, args);
                alive_ = args.ContinueRunning;
            }
            return result;
        }

        /// <summary>
        /// Raise the ProcessFile event.
        /// </summary>
        /// <param name="file">The file name.</param>
        private void OnProcessFile(string file)
        {
            ProcessFileHandler handler = ProcessFile;

            if (handler != null)
            {
                ScanEventArgs args = new(file);
                handler(this, args);
                alive_ = args.ContinueRunning;
            }
        }

        /// <summary>
        /// Raise the complete file event
        /// </summary>
        /// <param name="file">The file name</param>
        private void OnCompleteFile(string file)
        {
            CompletedFileHandler handler = CompletedFile;

            if (handler != null)
            {
                ScanEventArgs args = new(file);
                handler(this, args);
                alive_ = args.ContinueRunning;
            }
        }

        /// <summary>
        /// Raise the ProcessDirectory event.
        /// </summary>
        /// <param name="directory">The directory name.</param>
        /// <param name="hasMatchingFiles">Flag indicating if the directory has matching files.</param>
        private void OnProcessDirectory(string directory, bool hasMatchingFiles)
        {
            EventHandler<DirectoryEventArgs> handler = ProcessDirectory;

            if (handler != null)
            {
                DirectoryEventArgs args = new(directory, hasMatchingFiles);
                handler(this, args);
                alive_ = args.ContinueRunning;
            }
        }

        /// <summary>
        /// Scan a directory.
        /// </summary>
        /// <param name="directory">The base directory to scan.</param>
        /// <param name="recurse">True to recurse subdirectories, false to scan a single directory.</param>
        public void Scan(string directory, bool recurse)
        {
            alive_ = true;
            ScanDir(directory, recurse);
        }

        private void ScanDir(string directory, bool recurse)
        {
            try
            {
                string[] names = Directory.GetFiles(directory);
                bool hasMatch = false;
                for (int fileIndex = 0; fileIndex < names.Length; ++fileIndex)
                {
                    if (!fileFilter_.IsMatch(names[fileIndex]))
                    {
                        names[fileIndex] = null;
                    }
                    else
                    {
                        hasMatch = true;
                    }
                }

                OnProcessDirectory(directory, hasMatch);

                if (alive_ && hasMatch)
                {
                    foreach (string fileName in names)
                    {
                        try
                        {
                            if (fileName != null)
                            {
                                OnProcessFile(fileName);
                                if (!alive_)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (!OnFileFailure(fileName, e))
                            {
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!OnDirectoryFailure(directory, e))
                {
                    throw;
                }
            }

            if (alive_ && recurse)
            {
                try
                {
                    string[] names = Directory.GetDirectories(directory);
                    foreach (string fulldir in names)
                    {
                        if (directoryFilter_ == null || directoryFilter_.IsMatch(fulldir))
                        {
                            ScanDir(fulldir, true);
                            if (!alive_)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!OnDirectoryFailure(directory, e))
                    {
                        throw;
                    }
                }
            }
        }

        #region Instance Fields

        /// <summary>
        /// The file filter currently in use.
        /// </summary>
        private readonly IScanFilter fileFilter_;

        /// <summary>
        /// The directory filter currently in use.
        /// </summary>
        private readonly IScanFilter directoryFilter_;

        /// <summary>
        /// Flag indicating if scanning should continue running.
        /// </summary>
        private bool alive_;

        #endregion Instance Fields
    }
    /// <summary>
    /// A MemoryPool that will return a Memory which is exactly the length asked for using the bufferSize parameter.
    /// This is in contrast to the default ArrayMemoryPool which will return a Memory of equal size to the underlying
    /// array which at least as long as the minBufferSize parameter.
    /// Note: The underlying array may be larger than the slice of Memory
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class ExactMemoryPool<T> : MemoryPool<T>
    {
        public static new readonly MemoryPool<T> Shared = new ExactMemoryPool<T>();

        public override IMemoryOwner<T> Rent(int bufferSize = -1)
        {
            return (uint)bufferSize > int.MaxValue || bufferSize < 0
                ? throw new ArgumentOutOfRangeException(nameof(bufferSize))
                : (IMemoryOwner<T>)new ExactMemoryPoolBuffer(bufferSize);
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override int MaxBufferSize => int.MaxValue;

        private sealed class ExactMemoryPoolBuffer : IMemoryOwner<T>, IDisposable
        {
            private T[] array;
            private readonly int size;

            public ExactMemoryPoolBuffer(int size)
            {
                this.size = size;
                array = ArrayPool<T>.Shared.Rent(size);
            }

            public Memory<T> Memory
            {
                get
                {
                    T[] array = this.array;
                    return array == null ? throw new ObjectDisposedException(nameof(ExactMemoryPoolBuffer)) : new Memory<T>(array)[..size];
                }
            }

            public void Dispose()
            {
                T[] array = this.array;
                if (array == null)
                {
                    return;
                }

                this.array = null;
                ArrayPool<T>.Shared.Return(array);
            }
        }
    }
    internal static class Empty
    {
#if NET45
		internal static class EmptyArray<T>
		{
			public static readonly T[] Value = new T[0];
		}
		public static T[] Array<T>() => EmptyArray<T>.Value;
#else
        public static T[] Array<T>()
        {
            return System.Array.Empty<T>();
        }
#endif
    }
    internal static class ByteOrderStreamExtensions
    {
        internal static byte[] SwappedBytes(ushort value)
        {
            return new[] { (byte)value, (byte)(value >> 8) };
        }

        internal static byte[] SwappedBytes(short value)
        {
            return new[] { (byte)value, (byte)(value >> 8) };
        }

        internal static byte[] SwappedBytes(uint value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        internal static byte[] SwappedBytes(int value)
        {
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
        }

        internal static byte[] SwappedBytes(long value)
        {
            return new[] {
            (byte)value,         (byte)(value >>  8), (byte)(value >> 16), (byte)(value >> 24),
            (byte)(value >> 32), (byte)(value >> 40), (byte)(value >> 48), (byte)(value >> 56)
        };
        }

        internal static byte[] SwappedBytes(ulong value)
        {
            return new[] {
            (byte)value,         (byte)(value >>  8), (byte)(value >> 16), (byte)(value >> 24),
            (byte)(value >> 32), (byte)(value >> 40), (byte)(value >> 48), (byte)(value >> 56)
        };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long SwappedS64(byte[] bytes)
        {
            return ((long)bytes[0] << 0) | ((long)bytes[1] << 8) | ((long)bytes[2] << 16) | ((long)bytes[3] << 24) |
            ((long)bytes[4] << 32) | ((long)bytes[5] << 40) | ((long)bytes[6] << 48) | ((long)bytes[7] << 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong SwappedU64(byte[] bytes)
        {
            return ((ulong)bytes[0] << 0) | ((ulong)bytes[1] << 8) | ((ulong)bytes[2] << 16) | ((ulong)bytes[3] << 24) |
            ((ulong)bytes[4] << 32) | ((ulong)bytes[5] << 40) | ((ulong)bytes[6] << 48) | ((ulong)bytes[7] << 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SwappedS32(byte[] bytes)
        {
            return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint SwappedU32(byte[] bytes)
        {
            return (uint)SwappedS32(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short SwappedS16(byte[] bytes)
        {
            return (short)(bytes[0] | (bytes[1] << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort SwappedU16(byte[] bytes)
        {
            return (ushort)SwappedS16(bytes);
        }

        internal static byte[] ReadBytes(this Stream stream, int count)
        {
            byte[] bytes = new byte[count];
            int remaining = count;
            while (remaining > 0)
            {
                int bytesRead = stream.Read(bytes, count - remaining, remaining);
                if (bytesRead < 1)
                {
                    throw new EndOfStreamException();
                }

                remaining -= bytesRead;
            }

            return bytes;
        }

        /// <summary> Read an unsigned short in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadLEShort(this Stream stream)
        {
            return SwappedS16(stream.ReadBytes(2));
        }

        /// <summary> Read an int in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadLEInt(this Stream stream)
        {
            return SwappedS32(stream.ReadBytes(4));
        }

        /// <summary> Read a long in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLELong(this Stream stream)
        {
            return SwappedS64(stream.ReadBytes(8));
        }

        /// <summary> Write an unsigned short in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLEShort(this Stream stream, int value)
        {
            stream.Write(SwappedBytes(value), 0, 2);
        }

        /// <inheritdoc cref="WriteLEShort"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteLEShortAsync(this Stream stream, int value, CT ct)
        {
            await stream.WriteAsync(SwappedBytes(value), 0, 2, ct);
        }

        /// <summary> Write a ushort in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLEUshort(this Stream stream, ushort value)
        {
            stream.Write(SwappedBytes(value), 0, 2);
        }

        /// <inheritdoc cref="WriteLEUshort"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteLEUshortAsync(this Stream stream, ushort value, CT ct)
        {
            await stream.WriteAsync(SwappedBytes(value), 0, 2, ct);
        }

        /// <summary> Write an int in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLEInt(this Stream stream, int value)
        {
            stream.Write(SwappedBytes(value), 0, 4);
        }

        /// <inheritdoc cref="WriteLEInt"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteLEIntAsync(this Stream stream, int value, CT ct)
        {
            await stream.WriteAsync(SwappedBytes(value), 0, 4, ct);
        }

        /// <summary> Write a uint in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLEUint(this Stream stream, uint value)
        {
            stream.Write(SwappedBytes(value), 0, 4);
        }

        /// <inheritdoc cref="WriteLEUint"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteLEUintAsync(this Stream stream, uint value, CT ct)
        {
            await stream.WriteAsync(SwappedBytes(value), 0, 4, ct);
        }

        /// <summary> Write a long in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLELong(this Stream stream, long value)
        {
            stream.Write(SwappedBytes(value), 0, 8);
        }

        /// <inheritdoc cref="WriteLELong"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteLELongAsync(this Stream stream, long value, CT ct)
        {
            await stream.WriteAsync(SwappedBytes(value), 0, 8, ct);
        }

        /// <summary> Write a ulong in little endian byte order. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLEUlong(this Stream stream, ulong value)
        {
            stream.Write(SwappedBytes(value), 0, 8);
        }

        /// <inheritdoc cref="WriteLEUlong"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task WriteLEUlongAsync(this Stream stream, ulong value, CT ct)
        {
            await stream.WriteAsync(SwappedBytes(value), 0, 8, ct);
        }
    }
}
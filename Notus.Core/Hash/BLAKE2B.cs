using System;
using System.IO;
using System.Text;

namespace Notus.HashLib
{
    public sealed class BLAKE2B
    {
        private const int BLOCK_SIZE = 128;
        private const int DIGEST_SIZE = 64;
        private const int CHAIN_SIZE = 8;
        private const int COUNTER_SIZE = 2;
        private const int DEF_LEAFSIZE = 1024 * 1000 * 10;
        private const int FLAG_SIZE = 2;
        private const int MAX_PRLBLOCK = 1024 * 1000 * 400;
        private const int PARALLEL_DEG = 4;
        private const int MIN_PRLBLOCK = PARALLEL_DEG * BLOCK_SIZE;
        private const int ROUND_COUNT = 12;
        private const ulong ULL_MAX = 18446744073709551615;
        private static readonly ulong[] m_cIV =
        {
            0x6A09E667F3BCC908UL, 0xBB67AE8584CAA73BUL, 0x3C6EF372FE94F82BUL, 0xA54FF53A5F1D36F1UL,
            0x510E527FADE682D1UL, 0x9B05688C2B3E6C1FUL, 0x1F83D9ABFB41BD6BUL, 0x5BE0CD19137E2179UL

        };

        private bool m_isDisposed = false;
        private bool m_isParallel = false;
        private int m_leafSize = DEF_LEAFSIZE;
        private byte[] m_msgBuffer = new byte[BLOCK_SIZE];
        private int m_msgLength = 0;
        private int m_parallelBlockSize = PARALLEL_DEG * DEF_LEAFSIZE;
        private Blake2bState[] m_State;
        private ulong[] m_treeConfig = new ulong[CHAIN_SIZE];
        private bool m_treeDestroy;
        private Blake2Params m_treeParams;
        private int m_minParallel = PARALLEL_DEG * BLOCK_SIZE;

        private struct Blake2bState
        {
            internal ulong[] H;
            internal ulong[] T;
            internal ulong[] F;

            internal void Init()
            {
                H = new ulong[CHAIN_SIZE];
                T = new ulong[COUNTER_SIZE];
                F = new ulong[FLAG_SIZE];
            }

            internal void Reset()
            {
                if (H != null)
                {
                    Array.Clear(H, 0, H.Length);
                    H = null;
                }
                if (T != null)
                {
                    Array.Clear(T, 0, T.Length);
                    T = null;
                }
                if (F != null)
                {
                    Array.Clear(F, 0, F.Length);
                    F = null;
                }
            }
        };
        public int BlockSize
        {
            get { return BLOCK_SIZE; }
        }
        public int DigestSize
        {
            get { return DIGEST_SIZE; }
        }
        public string Name
        {
            get
            {
                if (m_isParallel)
                    return "BlakeBP512";
                else
                    return "Blake2Bp512";
            }
        }

        public BLAKE2B(bool Parallel = false)
        {
            m_isParallel = Parallel;
            m_treeDestroy = true;

            if (m_isParallel)
            {
                m_msgBuffer = new byte[2 * PARALLEL_DEG * BLOCK_SIZE];
                m_State = new Blake2bState[PARALLEL_DEG];
                for (int i = 0; i < PARALLEL_DEG; ++i)
                    m_State[i].Init();
                m_treeParams = new Blake2Params(64, 0, 4, 2, 0, 0, 0, 64, 4);
                m_minParallel = PARALLEL_DEG * BLOCK_SIZE;
                m_parallelBlockSize = m_leafSize * PARALLEL_DEG;
                Reset();
            }
            else
            {
                m_State = new Blake2bState[1];
                m_State[0].Init();
                m_treeParams = new Blake2Params(64, 0, 1, 1, 0, 0, 0, 0, 0);
                Initialize(m_treeParams, m_State[0]);
            }
        }
        public BLAKE2B(Blake2Params Params)
        {
            m_isParallel = m_treeParams.ThreadDepth > 1;
            m_treeParams = Params;

            if (m_isParallel)
            {
                if (Params.LeafLength != 0 && (Params.LeafLength < BLOCK_SIZE || Params.LeafLength % BLOCK_SIZE != 0))
                    throw new CryptoHashException("BlakeBP512:Ctor", "The LeafLength parameter is invalid! Must be evenly divisible by digest block size.");
                if (Params.ThreadDepth < 2 || Params.ThreadDepth % 2 != 0)
                    throw new CryptoHashException("BlakeBP512:Ctor", "The ThreadDepth parameter is invalid! Must be an even number greater than 1.");

                m_msgBuffer = new byte[2 * Params.ThreadDepth * BLOCK_SIZE];
                m_State = new Blake2bState[Params.ThreadDepth];
                m_minParallel = m_treeParams.ThreadDepth * BLOCK_SIZE;
                m_leafSize = Params.LeafLength == 0 ? DEF_LEAFSIZE : Params.LeafLength;
                m_parallelBlockSize = Params.ThreadDepth * m_leafSize;
                Reset();
            }
            else
            {
                m_treeParams = new Blake2Params(64, 0, 1, 1, 0, 0, 0, 0, 0);
                Initialize(m_treeParams, m_State[0]);
            }
        }

        ~BLAKE2B()
        {
            Dispose(false);
        }
        public void BlockUpdate(byte[] Input, int InOffset, int Length)
        {
            if (Length == 0)
                return;

            if (m_isParallel)
            {
                int ttlLen = Length + m_msgLength;
                int minPrl = m_msgBuffer.Length + (m_minParallel - BLOCK_SIZE);
                if (ttlLen > minPrl)
                {
                    int rmd = m_msgBuffer.Length - m_msgLength;
                    if (rmd != 0)
                        Buffer.BlockCopy(Input, InOffset, m_msgBuffer, m_msgLength, rmd);

                    m_msgLength = 0;
                    Length -= rmd;
                    InOffset += rmd;
                    ttlLen -= m_msgBuffer.Length;
                    System.Threading.Tasks.Parallel.For(0, m_treeParams.ThreadDepth, i =>
                    {
                        ProcessBlock(m_msgBuffer, i * BLOCK_SIZE, m_State[i], BLOCK_SIZE);
                        ProcessBlock(m_msgBuffer, (i * BLOCK_SIZE) + (m_treeParams.ThreadDepth * BLOCK_SIZE), m_State[i], BLOCK_SIZE);
                    });
                    if (Length > minPrl)
                    {
                        int prcLen = Length - m_minParallel;
                        if (prcLen % m_minParallel != 0)
                            prcLen -= (prcLen % m_minParallel);
                        System.Threading.Tasks.Parallel.For(0, m_treeParams.ThreadDepth, i =>
                        {
                            ProcessLeaf(Input, InOffset + (i * BLOCK_SIZE), m_State[i], (ulong)prcLen);
                        });

                        Length -= prcLen;
                        InOffset += prcLen;
                        ttlLen -= prcLen;
                    }
                }

                if (ttlLen > m_msgBuffer.Length)
                {
                    int rmd = m_msgBuffer.Length - m_msgLength;
                    if (rmd != 0)
                        Buffer.BlockCopy(Input, InOffset, m_msgBuffer, m_msgLength, rmd);

                    Length -= rmd;
                    InOffset += rmd;
                    m_msgLength = m_msgBuffer.Length;
                    System.Threading.Tasks.Parallel.For(0, m_treeParams.ThreadDepth, i =>
                    {
                        ProcessBlock(m_msgBuffer, i * BLOCK_SIZE, m_State[i], BLOCK_SIZE);
                    });
                    m_msgLength -= m_minParallel;
                    rmd = m_msgBuffer.Length / 2;
                    Buffer.BlockCopy(m_msgBuffer, rmd, m_msgBuffer, 0, rmd);
                }
            }
            else
            {
                if (m_msgLength + Length > BLOCK_SIZE)
                {
                    int rmd = BLOCK_SIZE - m_msgLength;
                    if (rmd != 0)
                        Buffer.BlockCopy(Input, InOffset, m_msgBuffer, m_msgLength, rmd);

                    ProcessBlock(m_msgBuffer, 0, m_State[0], BLOCK_SIZE);
                    m_msgLength = 0;
                    InOffset += rmd;
                    Length -= rmd;
                }
                while (Length > BLOCK_SIZE)
                {
                    ProcessBlock(Input, InOffset, m_State[0], BLOCK_SIZE);
                    InOffset += BLOCK_SIZE;
                    Length -= BLOCK_SIZE;
                }
            }
            if (Length != 0)
            {
                Buffer.BlockCopy(Input, InOffset, m_msgBuffer, m_msgLength, Length);
                m_msgLength += Length;
            }
        }
        public string Sign(string input)
        {
            return SignWithHashMethod("", input);
        }
        public string Sign(byte[] inputArr)
        {
            return SignWithHashMethod("", Encoding.UTF8.GetString(inputArr));
        }
        public string SignWithHashMethod(string keyText, string input)
        {
            int keySize = 256;
            int b = keySize;
            if (keyText.Length > b)
            {
                keyText = Notus.Convert.Byte2Hex(ComputeHash(Encoding.UTF8.GetBytes(keyText))).ToLower();
            }

            byte[] iPadDizi = Encoding.UTF8.GetBytes(Notus.Toolbox.Text.AddRightPad("", b, "6"));
            byte[] oPadDizi = Encoding.UTF8.GetBytes(Notus.Toolbox.Text.AddRightPad("", b, System.Convert.ToChar(92).ToString()));
            byte[] keyDizi = Encoding.UTF8.GetBytes(Notus.Toolbox.Text.AddRightPad(keyText, b, System.Convert.ToChar(0).ToString()));

            string k_ipad = "";
            string k_opad = "";
            for (int a = 0; a < keySize; a++)
            {
                k_ipad = k_ipad + ((char)(keyDizi[a] ^ iPadDizi[a])).ToString();
                k_opad = k_opad + ((char)(keyDizi[a] ^ oPadDizi[a])).ToString();
            }

            return
                Notus.Convert.Byte2Hex(
                    ComputeHash(
                        Encoding.UTF8.GetBytes(
                            k_opad +
                            Notus.Convert.Byte2Hex(
                                ComputeHash(Encoding.UTF8.GetBytes(k_ipad + input))
                            ).ToLower()
                        )
                    )
                ).ToLower();
        }
        public byte[] ComputeHash(byte[] Input)
        {
            byte[] hash = new byte[DigestSize];

            BlockUpdate(Input, 0, Input.Length);
            DoFinal(hash, 0);

            return hash;
        }

        public int DoFinal(byte[] Output, int OutOffset)
        {
            if (m_isParallel)
            {
                byte[] hashCodes = new byte[m_treeParams.ThreadDepth * DIGEST_SIZE];

                if (m_msgLength < m_msgBuffer.Length)
                    Array.Clear(m_msgBuffer, m_msgLength, m_msgBuffer.Length - m_msgLength);

                ulong prtBlk = ULL_MAX;

                if (m_msgLength > m_minParallel)
                {
                    int blkCount = (m_msgLength - m_minParallel) / BLOCK_SIZE;
                    if (m_msgLength % BLOCK_SIZE != 0)
                        ++blkCount;

                    for (int i = 0; i < blkCount; ++i)
                    {
                        ProcessBlock(m_msgBuffer, (i * BLOCK_SIZE), m_State[i], BLOCK_SIZE);
                        Buffer.BlockCopy(m_msgBuffer, m_minParallel + (i * BLOCK_SIZE), m_msgBuffer, i * BLOCK_SIZE, BLOCK_SIZE);
                        m_msgLength -= BLOCK_SIZE;
                    }

                    if (m_msgLength % BLOCK_SIZE != 0)
                        prtBlk = (ulong)blkCount - 1;
                }

                for (int i = 0; i < m_treeParams.ThreadDepth; ++i)
                {
                    m_State[i].F[0] = ULL_MAX;
                    int blkSze = BLOCK_SIZE;

                    if (i == m_treeParams.ThreadDepth - 1)
                        m_State[i].F[1] = ULL_MAX;

                    if (i == (int)prtBlk)
                    {
                        blkSze = m_msgLength % BLOCK_SIZE;
                        m_msgLength += BLOCK_SIZE - blkSze;
                        Array.Clear(m_msgBuffer, (i * BLOCK_SIZE) + blkSze, BLOCK_SIZE - blkSze);
                    }
                    else if (m_msgLength < 1)
                    {
                        blkSze = 0;
                        Array.Clear(m_msgBuffer, i * BLOCK_SIZE, BLOCK_SIZE);
                    }
                    else if (m_msgLength < BLOCK_SIZE)
                    {
                        blkSze = m_msgLength;
                        Array.Clear(m_msgBuffer, (i * BLOCK_SIZE) + blkSze, BLOCK_SIZE - blkSze);
                    }

                    ProcessBlock(m_msgBuffer, i * BLOCK_SIZE, m_State[i], (ulong)blkSze);
                    m_msgLength -= BLOCK_SIZE;

                    Le512ToBlock(m_State[i].H, hashCodes, i * DIGEST_SIZE);
                }

                m_msgLength = 0;
                m_treeParams.NodeDepth = 1;
                m_treeParams.NodeOffset = 0;
                m_treeParams.MaxDepth = 2;
                Initialize(m_treeParams, m_State[0]);

                for (int i = 0; i < m_treeParams.ThreadDepth; ++i)
                    BlockUpdate(hashCodes, i * DIGEST_SIZE, DIGEST_SIZE);

                for (int i = 0; i < hashCodes.Length - BLOCK_SIZE; i += BLOCK_SIZE)
                    ProcessBlock(m_msgBuffer, i, m_State[0], BLOCK_SIZE);

                m_State[0].F[0] = ULL_MAX;
                m_State[0].F[1] = ULL_MAX;
                ProcessBlock(m_msgBuffer, m_msgLength - BLOCK_SIZE, m_State[0], BLOCK_SIZE);
                Le512ToBlock(m_State[0].H, Output, OutOffset);
            }
            else
            {
                int padLen = m_msgBuffer.Length - m_msgLength;
                if (padLen > 0)
                    Array.Clear(m_msgBuffer, m_msgLength, padLen);

                m_State[0].F[0] = ULL_MAX;
                ProcessBlock(m_msgBuffer, 0, m_State[0], (ulong)m_msgLength);
                Le512ToBlock(m_State[0].H, Output, OutOffset);
            }

            Reset();

            return DIGEST_SIZE;
        }

        public int Generate(MacParams MacKey, byte[] Output)
        {
            if (Output.Length == 0)
                throw new CryptoHashException("Blake2Bp512:Generate", "Buffer size must be at least 1 byte!");
            if (MacKey.Key.Length < DIGEST_SIZE)
                throw new CryptoHashException("Blake2Bp512:Generate", "The key must be at least 64 bytes long!");

            int bufSize = DIGEST_SIZE;
            byte[] inpCtr = new byte[BLOCK_SIZE];

            LoadMacKey(MacKey);
            ProcessBlock(m_msgBuffer, 0, m_State[0], BLOCK_SIZE);
            Buffer.BlockCopy(m_State[0].H, 0, inpCtr, DIGEST_SIZE, DIGEST_SIZE);
            Array.Clear(inpCtr, 8, DIGEST_SIZE - 8);
            Increment(inpCtr);
            ProcessBlock(inpCtr, 0, m_State[0], BLOCK_SIZE);

            if (bufSize < Output.Length)
            {
                Buffer.BlockCopy(m_State[0].H, 0, Output, 0, bufSize);
                int rmd = Output.Length - bufSize;

                while (rmd > 0)
                {
                    Buffer.BlockCopy(m_State[0].H, 0, inpCtr, DIGEST_SIZE, DIGEST_SIZE);
                    Increment(inpCtr);
                    ProcessBlock(inpCtr, 0, m_State[0], BLOCK_SIZE);

                    if (rmd > DIGEST_SIZE)
                    {
                        Buffer.BlockCopy(m_State[0].H, 0, Output, bufSize, DIGEST_SIZE);
                        bufSize += DIGEST_SIZE;
                        rmd -= DIGEST_SIZE;
                    }
                    else
                    {
                        rmd = Output.Length - bufSize;
                        Buffer.BlockCopy(m_State[0].H, 0, Output, bufSize, rmd);
                        rmd = 0;
                    }
                }
            }
            else
            {
                Buffer.BlockCopy(m_State[0].H, 0, Output, 0, Output.Length);
            }

            return Output.Length;
        }

        public void LoadMacKey(MacParams MacKey)
        {
            if (MacKey.Key.Length < 32 || MacKey.Key.Length > 64)
                throw new CryptoHashException("Blake2Bp512", "Mac Key has invalid length!");

            if (MacKey.Salt != null)
            {
                if (MacKey.Salt.Length != 16)
                    throw new CryptoHashException("Blake2Bp512", "Salt has invalid length!");

                m_treeConfig[4] = BytesToLe64(MacKey.Salt, 0);
                m_treeConfig[5] = BytesToLe64(MacKey.Salt, 8);
            }

            if (MacKey.Info != null)
            {
                if (MacKey.Info.Length != 16)
                    throw new CryptoHashException("Blake2Bp512", "Info has invalid length!");

                m_treeConfig[6] = BytesToLe64(MacKey.Info, 0);
                m_treeConfig[7] = BytesToLe64(MacKey.Info, 8);
            }

            byte[] mkey = new byte[BLOCK_SIZE];
            Buffer.BlockCopy(MacKey.Key, 0, mkey, 0, MacKey.Key.Length);
            m_treeParams.KeyLength = (byte)MacKey.Key.Length;

            if (m_isParallel)
            {
                for (int i = 0; i < m_treeParams.ThreadDepth; ++i)
                {
                    Buffer.BlockCopy(mkey, 0, m_msgBuffer, i * BLOCK_SIZE, mkey.Length);
                    m_treeParams.NodeOffset = i;
                    Initialize(m_treeParams, m_State[i]);
                }
                m_msgLength = m_minParallel;
                m_treeParams.NodeOffset = 0;
            }
            else
            {
                Buffer.BlockCopy(mkey, 0, m_msgBuffer, 0, mkey.Length);
                m_msgLength = BLOCK_SIZE;
                Initialize(m_treeParams, m_State[0]);
            }
        }

        public void Reset()
        {
            m_msgLength = 0;
            Array.Clear(m_msgBuffer, 0, m_msgBuffer.Length);

            if (m_isParallel)
            {
                for (int i = 0; i < m_treeParams.ThreadDepth; ++i)
                {
                    m_treeParams.NodeOffset = i;
                    Initialize(m_treeParams, m_State[i]);
                }
                m_treeParams.NodeOffset = 0;
            }
            else
            {
                Initialize(m_treeParams, m_State[0]);
            }
        }

        public void Update(byte Input)
        {
            BlockUpdate(new byte[] { Input }, 0, 1);
        }

        static ulong BytesToLe64(byte[] Block, int InOffset)
        {
            return
                ((ulong)Block[InOffset] |
                ((ulong)Block[InOffset + 1] << 8) |
                ((ulong)Block[InOffset + 2] << 16) |
                ((ulong)Block[InOffset + 3] << 24) |
                ((ulong)Block[InOffset + 4] << 32) |
                ((ulong)Block[InOffset + 5] << 40) |
                ((ulong)Block[InOffset + 6] << 48) |
                ((ulong)Block[InOffset + 7] << 56));
        }

        void Compress(byte[] Input, int InOffset, Blake2bState State)
        {
            ulong[] msg = new ulong[16];

            msg[0] = BytesToLe64(Input, InOffset);
            msg[1] = BytesToLe64(Input, InOffset + 8);
            msg[2] = BytesToLe64(Input, InOffset + 16);
            msg[3] = BytesToLe64(Input, InOffset + 24);
            msg[4] = BytesToLe64(Input, InOffset + 32);
            msg[5] = BytesToLe64(Input, InOffset + 40);
            msg[6] = BytesToLe64(Input, InOffset + 48);
            msg[7] = BytesToLe64(Input, InOffset + 56);
            msg[8] = BytesToLe64(Input, InOffset + 64);
            msg[9] = BytesToLe64(Input, InOffset + 72);
            msg[10] = BytesToLe64(Input, InOffset + 80);
            msg[11] = BytesToLe64(Input, InOffset + 88);
            msg[12] = BytesToLe64(Input, InOffset + 96);
            msg[13] = BytesToLe64(Input, InOffset + 104);
            msg[14] = BytesToLe64(Input, InOffset + 112);
            msg[15] = BytesToLe64(Input, InOffset + 120);

            ulong v0 = State.H[0];
            ulong v1 = State.H[1];
            ulong v2 = State.H[2];
            ulong v3 = State.H[3];
            ulong v4 = State.H[4];
            ulong v5 = State.H[5];
            ulong v6 = State.H[6];
            ulong v7 = State.H[7];
            ulong v8 = m_cIV[0];
            ulong v9 = m_cIV[1];
            ulong v10 = m_cIV[2];
            ulong v11 = m_cIV[3];
            ulong v12 = m_cIV[4] ^ State.T[0];
            ulong v13 = m_cIV[5] ^ State.T[1];
            ulong v14 = m_cIV[6] ^ State.F[0];
            ulong v15 = m_cIV[7] ^ State.F[1];

            v0 += v4 + msg[0];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[1];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[2];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[3];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[4];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[5];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[6];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[7];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[8];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[9];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[10];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[11];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[12];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[13];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[14];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[15];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[14];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[10];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[4];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[8];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[9];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[15];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[13];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[6];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[1];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[12];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[0];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[2];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[11];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[7];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[5];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[3];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[11];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[8];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[12];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[0];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[5];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[2];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[15];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[13];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[10];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[14];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[3];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[6];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[7];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[1];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[9];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[4];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[7];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[9];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[3];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[1];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[13];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[12];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[11];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[14];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[2];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[6];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[5];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[10];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[4];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[0];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[15];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[8];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[9];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[0];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[5];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[7];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[2];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[4];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[10];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[15];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[14];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[1];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[11];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[12];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[6];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[8];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[3];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[13];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[2];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[12];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[6];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[10];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[0];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[11];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[8];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[3];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[4];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[13];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[7];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[5];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[15];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[14];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[1];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[9];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[12];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[5];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[1];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[15];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[14];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[13];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[4];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[10];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[0];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[7];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[6];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[3];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[9];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[2];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[8];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[11];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[13];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[11];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[7];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[14];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[12];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[1];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[3];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[9];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[5];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[0];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[15];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[4];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[8];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[6];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[2];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[10];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[6];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[15];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[14];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[9];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[11];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[3];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[0];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[8];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[12];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[2];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[13];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[7];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[1];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[4];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[10];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[5];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[10];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[2];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[8];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[4];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[7];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[6];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[1];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[5];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[15];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[11];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[9];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[14];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[3];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[12];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[13];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[0];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[0];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[1];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[2];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[3];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[4];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[5];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[6];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[7];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[8];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[9];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[10];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[11];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[12];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[13];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[14];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[15];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v0 += v4 + msg[14];
            v12 ^= v0;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v0 += v4 + msg[10];
            v12 ^= v0;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v8 += v12;
            v4 ^= v8;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            v1 += v5 + msg[4];
            v13 ^= v1;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v1 += v5 + msg[8];
            v13 ^= v1;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v9 += v13;
            v5 ^= v9;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v2 += v6 + msg[9];
            v14 ^= v2;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v2 += v6 + msg[15];
            v14 ^= v2;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v10 += v14;
            v6 ^= v10;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v3 += v7 + msg[13];
            v15 ^= v3;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v3 += v7 + msg[6];
            v15 ^= v3;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v11 += v15;
            v7 ^= v11;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v0 += v5 + msg[1];
            v15 ^= v0;
            v15 = ((v15 >> 32) | (v15 << (64 - 32)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 24) | (v5 << (64 - 24)));
            v0 += v5 + msg[12];
            v15 ^= v0;
            v15 = ((v15 >> 16) | (v15 << (64 - 16)));
            v10 += v15;
            v5 ^= v10;
            v5 = ((v5 >> 63) | (v5 << (64 - 63)));

            v1 += v6 + msg[0];
            v12 ^= v1;
            v12 = ((v12 >> 32) | (v12 << (64 - 32)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 24) | (v6 << (64 - 24)));
            v1 += v6 + msg[2];
            v12 ^= v1;
            v12 = ((v12 >> 16) | (v12 << (64 - 16)));
            v11 += v12;
            v6 ^= v11;
            v6 = ((v6 >> 63) | (v6 << (64 - 63)));

            v2 += v7 + msg[11];
            v13 ^= v2;
            v13 = ((v13 >> 32) | (v13 << (64 - 32)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 24) | (v7 << (64 - 24)));
            v2 += v7 + msg[7];
            v13 ^= v2;
            v13 = ((v13 >> 16) | (v13 << (64 - 16)));
            v8 += v13;
            v7 ^= v8;
            v7 = ((v7 >> 63) | (v7 << (64 - 63)));

            v3 += v4 + msg[5];
            v14 ^= v3;
            v14 = ((v14 >> 32) | (v14 << (64 - 32)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 24) | (v4 << (64 - 24)));
            v3 += v4 + msg[3];
            v14 ^= v3;
            v14 = ((v14 >> 16) | (v14 << (64 - 16)));
            v9 += v14;
            v4 ^= v9;
            v4 = ((v4 >> 63) | (v4 << (64 - 63)));

            State.H[0] ^= v0 ^ v8;
            State.H[1] ^= v1 ^ v9;
            State.H[2] ^= v2 ^ v10;
            State.H[3] ^= v3 ^ v11;
            State.H[4] ^= v4 ^ v12;
            State.H[5] ^= v5 ^ v13;
            State.H[6] ^= v6 ^ v14;
            State.H[7] ^= v7 ^ v15;
        }

        void Increase(Blake2bState State, ulong Length)
        {
            State.T[0] += Length;
            if (State.T[0] < Length)
                ++State.T[1];
        }

        void Increment(byte[] Counter)
        {
            Le64ToBytes(BytesToLe64(Counter, 0) + 1, Counter, 0);
        }

        void Initialize(Blake2Params Params, Blake2bState State)
        {
            Array.Clear(State.T, 0, COUNTER_SIZE);
            Array.Clear(State.F, 0, FLAG_SIZE);
            Array.Copy(m_cIV, 0, State.H, 0, CHAIN_SIZE);

            m_treeConfig[0] = Params.DigestLength;
            m_treeConfig[0] |= (ulong)Params.KeyLength << 8;
            m_treeConfig[0] |= (ulong)Params.FanOut << 16;
            m_treeConfig[0] |= (ulong)Params.MaxDepth << 24;
            m_treeConfig[0] |= (ulong)Params.LeafLength << 32;
            m_treeConfig[1] = (ulong)Params.NodeOffset;
            m_treeConfig[2] = Params.NodeDepth;
            m_treeConfig[2] |= (ulong)Params.InnerLength << 8;

            State.H[0] ^= m_treeConfig[0];
            State.H[1] ^= m_treeConfig[1];
            State.H[2] ^= m_treeConfig[2];
            State.H[3] ^= m_treeConfig[3];
            State.H[4] ^= m_treeConfig[4];
            State.H[5] ^= m_treeConfig[5];
            State.H[6] ^= m_treeConfig[6];
            State.H[7] ^= m_treeConfig[7];
        }

        static void Le64ToBytes(ulong DWord, byte[] Block, int Offset)
        {
            Block[Offset] = (byte)DWord;
            Block[Offset + 1] = (byte)(DWord >> 8);
            Block[Offset + 2] = (byte)(DWord >> 16);
            Block[Offset + 3] = (byte)(DWord >> 24);
            Block[Offset + 4] = (byte)(DWord >> 32);
            Block[Offset + 5] = (byte)(DWord >> 40);
            Block[Offset + 6] = (byte)(DWord >> 48);
            Block[Offset + 7] = (byte)(DWord >> 56);
        }

        static void Le512ToBlock(ulong[] Input, byte[] Output, int OutOffset)
        {
            Le64ToBytes(Input[0], Output, OutOffset);
            Le64ToBytes(Input[1], Output, OutOffset + 8);
            Le64ToBytes(Input[2], Output, OutOffset + 16);
            Le64ToBytes(Input[3], Output, OutOffset + 24);
            Le64ToBytes(Input[4], Output, OutOffset + 32);
            Le64ToBytes(Input[5], Output, OutOffset + 40);
            Le64ToBytes(Input[6], Output, OutOffset + 48);
            Le64ToBytes(Input[7], Output, OutOffset + 56);
        }

        void ProcessBlock(byte[] Input, int InOffset, Blake2bState State, ulong Length)
        {
            Increase(State, Length);
            Compress(Input, InOffset, State);
        }

        void ProcessLeaf(byte[] Input, int InOffset, Blake2bState State, ulong Length)
        {
            do
            {
                ProcessBlock(Input, InOffset, State, BLOCK_SIZE);
                InOffset += m_minParallel;
                Length -= (ulong)m_minParallel;
            }
            while (Length > 0);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!m_isDisposed && Disposing)
            {
                try
                {
                    m_isDisposed = true;
                    m_isParallel = false;
                    m_leafSize = 0;
                    m_msgLength = 0;
                    m_parallelBlockSize = 0;
                    m_minParallel = 0;
                    if (m_treeDestroy)
                        m_treeParams.Reset();
                    m_treeDestroy = false;

                    if (m_State != null)
                    {
                        for (int i = 0; i < m_State.Length; ++i)
                            m_State[i].Reset();
                    }
                    if (m_msgBuffer != null)
                    {
                        Array.Clear(m_msgBuffer, 0, m_msgBuffer.Length);
                        m_msgBuffer = null;
                    }
                    if (m_treeConfig != null)
                    {
                        Array.Clear(m_treeConfig, 0, m_treeConfig.Length);
                        m_treeConfig = null;
                    }
                }
                finally
                {
                    m_isDisposed = true;
                }
            }
        }
    }

    public struct Blake2Params
    {
        private const int HDR_SIZE = 36;
        public byte DigestLength;
        public byte KeyLength;
        public byte FanOut;
        public byte MaxDepth;
        public int LeafLength;
        public long NodeOffset;
        public byte NodeDepth;
        public byte InnerLength;
        public byte ThreadDepth;
        public byte Reserved2;
        public long Reserved3;
        public long Reserved4;
        public Blake2Params(byte DigestLength, byte KeyLength, byte FanOut, byte MaxDepth, int LeafLength, long NodeOffset, byte NodeDepth, byte InnerLength, byte ThreadDepth)
        {
            this.DigestLength = DigestLength;
            this.KeyLength = KeyLength;
            this.FanOut = FanOut;
            this.MaxDepth = MaxDepth;
            this.LeafLength = LeafLength;
            this.NodeOffset = NodeOffset;
            this.NodeDepth = NodeDepth;
            this.InnerLength = InnerLength;
            this.ThreadDepth = ThreadDepth;
            Reserved2 = 0;
            Reserved3 = 0;
            Reserved4 = 0;
        }
        public Blake2Params(Stream DescriptionStream)
        {
            BinaryReader reader = new BinaryReader(DescriptionStream);
            DigestLength = reader.ReadByte();
            KeyLength = reader.ReadByte();
            FanOut = reader.ReadByte();
            MaxDepth = reader.ReadByte();
            LeafLength = reader.ReadInt16();
            NodeOffset = reader.ReadInt64();
            NodeDepth = reader.ReadByte();
            InnerLength = reader.ReadByte();
            ThreadDepth = reader.ReadByte();
            Reserved2 = reader.ReadByte();
            Reserved3 = reader.ReadByte();
            Reserved4 = reader.ReadByte();
        }
        public Blake2Params(byte[] DescriptionArray) :
            this(new MemoryStream(DescriptionArray))
        {
        }

        public Blake2Params Clone()
        {
            return new Blake2Params(DigestLength, KeyLength, FanOut, MaxDepth, LeafLength, NodeOffset, NodeDepth, InnerLength, ThreadDepth);
        }
        public bool Equals(Blake2Params Obj)
        {
            if (this.GetHashCode() != Obj.GetHashCode())
                return false;

            return true;
        }
        public override int GetHashCode()
        {
            int result = 31 * DigestLength;

            result += 31 * KeyLength;
            result += 31 * FanOut;
            result += 31 * MaxDepth;
            result += 31 * LeafLength;
            result += 31 * (int)NodeOffset;
            result += 31 * NodeDepth;
            result += 31 * InnerLength;
            result += 31 * ThreadDepth;
            result += 31 * Reserved2;
            result += 31 * (int)Reserved3;
            result += 31 * (int)Reserved4;

            return result;
        }
        public static int GetHeaderSize()
        {
            return HDR_SIZE;
        }
        public void Reset()
        {
            DigestLength = 0;
            KeyLength = 0;
            FanOut = 0;
            MaxDepth = 0;
            LeafLength = 0;
            NodeOffset = 0;
            NodeDepth = 0;
            InnerLength = 0;
            ThreadDepth = 0;
            Reserved2 = 0;
            Reserved3 = 0;
            Reserved4 = 0;
        }
        public byte[] ToBytes()
        {
            return ToStream().ToArray();
        }
        public MemoryStream ToStream()
        {
            MemoryStream stream = new MemoryStream(GetHeaderSize());
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(DigestLength);
            writer.Write(KeyLength);
            writer.Write(FanOut);
            writer.Write(MaxDepth);
            writer.Write(LeafLength);
            writer.Write(NodeOffset);
            writer.Write(InnerLength);
            writer.Write(ThreadDepth);
            writer.Write(Reserved2);
            writer.Write(Reserved3);
            writer.Write(Reserved4);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    };

    public sealed class MacParams
    {
        bool _isDisposed;
        byte[] _Key;
        byte[] _Info;
        byte[] _Salt;
        public byte[] Key
        {
            get { return _Key == null ? null : (byte[])_Key.Clone(); }
            set { _Key = value; }
        }
        public byte[] Info
        {
            get { return _Info == null ? null : (byte[])_Info.Clone(); }
            set { _Info = value; }
        }
        public byte[] Salt
        {
            get { return _Salt == null ? null : (byte[])_Salt.Clone(); }
            set { _Salt = value; }
        }
        public MacParams()
        {
            _isDisposed = false;
        }
        public MacParams(byte[] Key)
        {
            if (Key != null)
            {
                _Key = new byte[Key.Length];
                Buffer.BlockCopy(Key, 0, _Key, 0, _Key.Length);
            }
        }
        public MacParams(byte[] Key, byte[] Salt)
        {
            if (Key != null)
            {
                _Key = new byte[Key.Length];
                Buffer.BlockCopy(Key, 0, _Key, 0, _Key.Length);
            }
            if (Salt != null)
            {
                _Salt = new byte[Salt.Length];
                Buffer.BlockCopy(Salt, 0, _Salt, 0, Salt.Length);
            }
        }
        public MacParams(byte[] Key, byte[] Salt, byte[] Info)
        {
            if (Key != null)
            {
                _Key = new byte[Key.Length];
                Buffer.BlockCopy(Key, 0, _Key, 0, _Key.Length);
            }
            if (Salt != null)
            {
                _Salt = new byte[Salt.Length];
                Buffer.BlockCopy(Salt, 0, _Salt, 0, Salt.Length);
            }
            if (Info != null)
            {
                _Info = new byte[Info.Length];
                Buffer.BlockCopy(Info, 0, _Info, 0, Info.Length);
            }
        }
        ~MacParams()
        {
            Dispose();
        }
        public static MacParams DeSerialize(Stream KeyStream)
        {
            BinaryReader reader = new BinaryReader(KeyStream);
            short keyLen = reader.ReadInt16();
            short saltLen = reader.ReadInt16();
            short infoLen = reader.ReadInt16();

            byte[] key = null;
            byte[] salt = null;
            byte[] info = null;

            if (keyLen > 0)
                key = reader.ReadBytes(keyLen);
            if (saltLen > 0)
                salt = reader.ReadBytes(saltLen);
            if (infoLen > 0)
                info = reader.ReadBytes(infoLen);

            return new MacParams(key, salt, info);
        }
        public static Stream Serialize(MacParams MacObj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(MacObj.Key != null ? (short)MacObj.Key.Length : (short)0);
            writer.Write(MacObj.Salt != null ? (short)MacObj.Salt.Length : (short)0);
            writer.Write(MacObj.Info != null ? (short)MacObj.Info.Length : (short)0);

            if (MacObj.Key != null)
                writer.Write(MacObj.Key);
            if (MacObj.Salt != null)
                writer.Write(MacObj.Salt);
            if (MacObj.Info != null)
                writer.Write(MacObj.Info);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public MemoryStream ToStream()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(_Key != null ? (short)_Key.Length : (short)0);
            writer.Write(_Salt != null ? (short)_Salt.Length : (short)0);
            writer.Write(_Info != null ? (short)_Info.Length : (short)0);

            if (_Key != null)
                writer.Write(_Key);
            if (_Salt != null)
                writer.Write(_Salt);
            if (_Info != null)
                writer.Write(_Info);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public object Clone()
        {
            return new MacParams(_Key, _Salt, _Info);
        }

        public object DeepCopy()
        {
            return DeSerialize(Serialize(this));
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!_isDisposed && Disposing)
            {
                try
                {
                    if (_Key != null)
                    {
                        Array.Clear(_Key, 0, _Key.Length);
                        _Key = null;
                    }

                    if (_Salt != null)
                    {
                        Array.Clear(_Salt, 0, _Salt.Length);
                        _Salt = null;
                    }
                    if (_Info != null)
                    {
                        Array.Clear(_Info, 0, _Info.Length);
                        _Info = null;
                    }
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
    };

    public sealed class CryptoHashException : Exception
    {
        public string Origin { get; set; }
        public CryptoHashException(String Message) :
            base(Message)
        {
        }

        public CryptoHashException(String Message, Exception InnerException) :
            base(Message, InnerException)
        {
        }
        public CryptoHashException(String Origin, String Message) :
            base(Message)
        {
            this.Origin = Origin;
        }

        public CryptoHashException(String Origin, String Message, Exception InnerException) :
            base(Message, InnerException)
        {
            this.Origin = Origin;
        }
    }

}
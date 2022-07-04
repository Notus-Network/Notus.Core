using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Notus.Encryption
{
	/// <summary>
	/// Helper methods for ChaCha20 Encryption.
	/// </summary>
	public sealed class ChaCha20 : IDisposable
	{
		public const int allowedKeyLength = 32;
		public const int allowedNonceLength = 12;
		public const int processBytesAtTime = 64;
		private const int stateLength = 16;
		private uint[] state;
		private bool isDisposed;
		public ChaCha20(byte[] key, byte[] nonce, uint counter)
		{
			this.state = new uint[stateLength];
			this.isDisposed = false;

			this.KeySetup(key);
			this.IvSetup(nonce, counter);
		}
		public uint[] State
		{
			get
			{
				return this.state;
			}
		}
		private static readonly byte[] sigma = Encoding.ASCII.GetBytes("expand 32-byte k");
		private static readonly byte[] tau = Encoding.ASCII.GetBytes("expand 16-byte k");
		private void KeySetup(byte[] key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("Key is null");
			}

			if (key.Length != allowedKeyLength)
			{
				throw new ArgumentException($"Key length must be {allowedKeyLength}. Actual: {key.Length}");
			}

			state[4] = Util.U8To32Little(key, 0);
			state[5] = Util.U8To32Little(key, 4);
			state[6] = Util.U8To32Little(key, 8);
			state[7] = Util.U8To32Little(key, 12);

			byte[] constants = (key.Length == allowedKeyLength) ? sigma : tau;
			int keyIndex = key.Length - 16;

			state[8] = Util.U8To32Little(key, keyIndex + 0);
			state[9] = Util.U8To32Little(key, keyIndex + 4);
			state[10] = Util.U8To32Little(key, keyIndex + 8);
			state[11] = Util.U8To32Little(key, keyIndex + 12);

			state[0] = Util.U8To32Little(constants, 0);
			state[1] = Util.U8To32Little(constants, 4);
			state[2] = Util.U8To32Little(constants, 8);
			state[3] = Util.U8To32Little(constants, 12);
		}
		private void IvSetup(byte[] nonce, uint counter)
		{
			if (nonce == null)
			{
				Dispose();
				throw new ArgumentNullException("Nonce is null");
			}

			if (nonce.Length != allowedNonceLength)
			{
				Dispose();
				throw new ArgumentException($"Nonce length must be {allowedNonceLength}. Actual: {nonce.Length}");
			}

			state[12] = counter;
			state[13] = Util.U8To32Little(nonce, 0);
			state[14] = Util.U8To32Little(nonce, 4);
			state[15] = Util.U8To32Little(nonce, 8);
		}

		public void EncryptBytes(byte[] output, byte[] input, int numBytes)
		{
			this.WorkBytes(output, input, numBytes);
		}
		public void EncryptStream(Stream output, Stream input, int howManyBytesToProcessAtTime = 1024)
		{
			this.WorkStreams(output, input, howManyBytesToProcessAtTime);
		}
		public async Task EncryptStreamAsync(Stream output, Stream input, int howManyBytesToProcessAtTime = 1024)
		{
			await this.WorkStreamsAsync(output, input, howManyBytesToProcessAtTime);
		}
		public void EncryptBytes(byte[] output, byte[] input)
		{
			this.WorkBytes(output, input, input.Length);
		}
		public byte[] EncryptBytes(byte[] input, int numBytes)
		{
			byte[] returnArray = new byte[numBytes];
			this.WorkBytes(returnArray, input, numBytes);
			return returnArray;
		}
		public byte[] EncryptBytes(byte[] input)
		{
			byte[] returnArray = new byte[input.Length];
			this.WorkBytes(returnArray, input, input.Length);
			return returnArray;
		}
		public byte[] EncryptString(string input)
		{
			byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(input);
			byte[] returnArray = new byte[utf8Bytes.Length];

			this.WorkBytes(returnArray, utf8Bytes, utf8Bytes.Length);
			return returnArray;
		}
		public void DecryptBytes(byte[] output, byte[] input, int numBytes)
		{
			this.WorkBytes(output, input, numBytes);
		}
		public void DecryptStream(Stream output, Stream input, int howManyBytesToProcessAtTime = 1024)
		{
			this.WorkStreams(output, input, howManyBytesToProcessAtTime);
		}
		public async Task DecryptStreamAsync(Stream output, Stream input, int howManyBytesToProcessAtTime = 1024)
		{
			await this.WorkStreamsAsync(output, input, howManyBytesToProcessAtTime);
		}
		public void DecryptBytes(byte[] output, byte[] input)
		{
			WorkBytes(output, input, input.Length);
		}
		public byte[] DecryptBytes(byte[] input, int numBytes)
		{
			byte[] returnArray = new byte[numBytes];
			WorkBytes(returnArray, input, numBytes);
			return returnArray;
		}
		public byte[] DecryptBytes(byte[] input)
		{
			byte[] returnArray = new byte[input.Length];
			WorkBytes(returnArray, input, input.Length);
			return returnArray;
		}
		public string DecryptUTF8ByteArray(byte[] input)
		{
			byte[] tempArray = new byte[input.Length];

			WorkBytes(tempArray, input, input.Length);
			return System.Text.Encoding.UTF8.GetString(tempArray);
		}
		private void WorkStreams(Stream output, Stream input, int howManyBytesToProcessAtTime = 1024)
		{
			int readBytes;

			byte[] inputBuffer = new byte[howManyBytesToProcessAtTime];
			byte[] outputBuffer = new byte[howManyBytesToProcessAtTime];

			while ((readBytes = input.Read(inputBuffer, 0, howManyBytesToProcessAtTime)) > 0)
			{
				WorkBytes(output: outputBuffer, input: inputBuffer, numBytes: readBytes);
				output.Write(outputBuffer, 0, readBytes);
			}
		}
		private async Task WorkStreamsAsync(Stream output, Stream input, int howManyBytesToProcessAtTime = 1024)
		{
			byte[] readBytesBuffer = new byte[howManyBytesToProcessAtTime];
			byte[] writeBytesBuffer = new byte[howManyBytesToProcessAtTime];
			int howManyBytesWereRead = await input.ReadAsync(readBytesBuffer, 0, howManyBytesToProcessAtTime);

			while (howManyBytesWereRead > 0)
			{
				WorkBytes(output: writeBytesBuffer, input: readBytesBuffer, numBytes: howManyBytesWereRead);
				await output.WriteAsync(writeBytesBuffer, 0, howManyBytesWereRead);
				howManyBytesWereRead = await input.ReadAsync(readBytesBuffer, 0, howManyBytesToProcessAtTime);
			}
		}
		private void WorkBytes(byte[] output, byte[] input, int numBytes)
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException("state", "The ChaCha state has been disposed");
			}

			if (input == null)
			{
				throw new ArgumentNullException("input", "Input cannot be null");
			}

			if (output == null)
			{
				throw new ArgumentNullException("output", "Output cannot be null");
			}

			if (numBytes < 0 || numBytes > input.Length)
			{
				throw new ArgumentOutOfRangeException("numBytes", "The number of bytes to read must be between [0..input.Length]");
			}

			if (output.Length < numBytes)
			{
				throw new ArgumentOutOfRangeException("output", $"Output byte array should be able to take at least {numBytes}");
			}

			uint[] x = new uint[stateLength];
			byte[] tmp = new byte[processBytesAtTime];
			int offset = 0;

			while (numBytes > 0)
			{
				Buffer.BlockCopy(this.state, 0, x, 0, stateLength * sizeof(uint));

				for (int i = 0; i < 10; i++)
				{
					QuarterRound(x, 0, 4, 8, 12);
					QuarterRound(x, 1, 5, 9, 13);
					QuarterRound(x, 2, 6, 10, 14);
					QuarterRound(x, 3, 7, 11, 15);

					QuarterRound(x, 0, 5, 10, 15);
					QuarterRound(x, 1, 6, 11, 12);
					QuarterRound(x, 2, 7, 8, 13);
					QuarterRound(x, 3, 4, 9, 14);
				}

				for (int i = 0; i < stateLength; i++)
				{
					Util.ToBytes(tmp, Util.Add(x[i], this.state[i]), 4 * i);
				}

				this.state[12] = Util.AddOne(state[12]);
				if (this.state[12] <= 0)
				{
					this.state[13] = Util.AddOne(state[13]);
				}

				if (numBytes <= processBytesAtTime)
				{
					for (int i = 0; i < numBytes; i++)
					{
						output[i + offset] = (byte)(input[i + offset] ^ tmp[i]);
					}

					return;
				}

				for (int i = 0; i < processBytesAtTime; i++)
				{
					output[i + offset] = (byte)(input[i + offset] ^ tmp[i]);
				}

				numBytes -= processBytesAtTime;
				offset += processBytesAtTime;
			}
		}
		private static void QuarterRound(uint[] x, uint a, uint b, uint c, uint d)
		{
			x[a] = Util.Add(x[a], x[b]);
			x[d] = Util.Rotate(Util.XOr(x[d], x[a]), 16);

			x[c] = Util.Add(x[c], x[d]);
			x[b] = Util.Rotate(Util.XOr(x[b], x[c]), 12);

			x[a] = Util.Add(x[a], x[b]);
			x[d] = Util.Rotate(Util.XOr(x[d], x[a]), 8);

			x[c] = Util.Add(x[c], x[d]);
			x[b] = Util.Rotate(Util.XOr(x[b], x[c]), 7);
		}
		~ChaCha20()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
				}
				if (state != null)
				{
					Array.Clear(state, 0, state.Length);
				}

				state = null;
			}

			isDisposed = true;
		}
	}

	public static class Util
	{
		public static uint Rotate(uint v, int c)
		{
			unchecked
			{
				return (v << c) | (v >> (32 - c));
			}
		}
		public static uint XOr(uint v, uint w)
		{
			return unchecked(v ^ w);
		}
		public static uint Add(uint v, uint w)
		{
			return unchecked(v + w);
		}
		public static uint AddOne(uint v)
		{
			return unchecked(v + 1);
		}
		public static uint U8To32Little(byte[] p, int inputOffset)
		{
			unchecked
			{
				return ((uint)p[inputOffset]
					| ((uint)p[inputOffset + 1] << 8)
					| ((uint)p[inputOffset + 2] << 16)
					| ((uint)p[inputOffset + 3] << 24));
			}
		}
		public static void ToBytes(byte[] output, uint input, int outputOffset)
		{
			unchecked
			{
				output[outputOffset] = (byte)input;
				output[outputOffset + 1] = (byte)(input >> 8);
				output[outputOffset + 2] = (byte)(input >> 16);
				output[outputOffset + 3] = (byte)(input >> 24);
			}
		}
	}
}

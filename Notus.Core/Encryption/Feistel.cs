using System;

// https://github.com/allantargino/Feistel-Cipher/blob/master/FeistelCipher/Program.cs

namespace Notus.Encryption
{
    public class Feistel
    {
        private static byte Encrypt(byte msg, Func<byte, byte, byte> FunctionF, byte[] keys)
        {
            byte step = msg;
            for (int i = 0; i < keys.Length; i++)
            {
                step = FeistelStep(step, keys[i], FunctionF);
            }

            return step;
        }
        private static byte Decrypt(byte msg, Func<byte, byte, byte> FunctionF, byte[] keys)
        {
            byte step = msg;
            step = InversionLR(step);
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                step = FeistelStep(step, keys[i], FunctionF);
            }
            step = InversionLR(step);

            return step;
        }
        private static byte FunctionF(byte x, byte key)
        {
            return Xor(x, key);
        }
        private static byte FeistelStep(byte msg, byte key, Func<byte, byte, byte> FunctionF)
        {
            var R = GetR(msg);
            var L = GetL(msg);

            var funcResult = OperateR(R, key, FunctionF);

            var xorResult = OperateL(L, funcResult, Xor);

            var finalResult = InversionLR(xorResult, R);

            return finalResult;
        }
        private static byte Xor(byte x, byte y)
        {
            return (byte)((int)x ^ (int)y);
        }
        private static byte GetR(byte x)
        {
            var temp = (byte)(((int)x) << 4);
            return (byte)(((int)temp) >> 4);
        }
        private static byte GetL(byte x)
        {
            var temp = (byte)(((int)x) >> 4);
            return (byte)(((int)temp) << 4);
        }
        private static byte InversionLR(byte l, byte r)
        {
            l = (byte)(((int)l) >> 4);
            r = (byte)(((int)r) << 4);
            return Xor(r, l);
        }
        private static byte InversionLR(byte msg)
        {
            var R = GetR(msg);
            var L = GetL(msg);
            return InversionLR(L, R);
        }
        private static byte OperateL(byte l, byte key, Func<byte, byte, byte> function)
        {
            key = (byte)(((int)key) << 4);
            return function(l, key);
        }
        private static byte OperateR(byte r, byte key, Func<byte, byte, byte> function)
        {
            return function(r, key);
        }
        private static void PrintOutByte(string initialText, byte x)
        {
            var temp = (int)x;
            int bits = 0;
            int factor = 1;
            for (int i = 7; i >= 0; i--)
            {
                bits += (temp % 2) * factor;
                factor *= 10;
                temp /= 2;
            }
            Console.WriteLine(initialText + "\t" + bits.ToString("00000000") + "\n");
        }
        private static byte EnsureKeyHas4Bits(byte key)
        {
            return (byte)((int)key % 16);
        }
    }
}

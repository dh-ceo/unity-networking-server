using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO;

namespace DevelopersHub.Unity.Networking
{
    public struct Carrier : IDisposable
    {

        #region Decimal
        public double GetDouble()
        {
            if (space + 8 > values.Length)
            {
                return 0.0;
            }
            double num = BitConverter.ToDouble(values, space);
            space += 8;
            return num;
        }

        public void SetDouble(double value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }
        #endregion

        #region Int
        public short GetInt16()
        {
            if (space + 2 > values.Length)
            {
                return 0;
            }
            int int16 = (int)BitConverter.ToInt16(values, space);
            space += 2;
            return (short)int16;
        }

        public ushort GetUInt16()
        {
            if (space + 2 > values.Length)
            {
                return 0;
            }
            int uint16 = (int)BitConverter.ToUInt16(values, space);
            space += 2;
            return (ushort)uint16;
        }

        public int GetInt32()
        {
            if (space + 4 > values.Length)
            {
                return 0;
            }
            int int32 = BitConverter.ToInt32(values, space);
            space += 4;
            return int32;
        }

        public uint GetUInt32()
        {
            if (space + 4 > values.Length)
            {
                return 0;
            }
            int uint32 = (int)BitConverter.ToUInt32(values, space);
            space += 4;
            return (uint)uint32;
        }

        public long GetInt64()
        {
            if (space + 8 > values.Length)
            {
                return 0;
            }
            long int64 = BitConverter.ToInt64(values, space);
            space += 8;
            return int64;
        }

        public ulong GetUInt64()
        {
            if (space + 8 > values.Length)
            {
                return 0;
            }
            long uint64 = (long)BitConverter.ToUInt64(values, space);
            space += 8;
            return (ulong)uint64;
        }

        public void SetInt16(short value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        public void SetUInt16(ushort value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        public void SetInt32(int value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        public void SetUInt32(uint value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        public void SetInt64(long value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        public void SetUInt64(ulong value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }
        #endregion

        #region String
        public string GetString()
        {
            if (space + 4 > values.Length)
            {
                return "";
            }
            int int32 = BitConverter.ToInt32(values, space);
            space += 4;
            if (int32 <= 0 || space + int32 > values.Length)
            {
                return "";
            }
            string str = Encoding.UTF8.GetString(values, space, int32);
            space += int32;
            return str;
        }

        public char GetChar()
        {
            if (space + 2 > values.Length)
            {
                return char.MinValue;
            }
            int num = (int)BitConverter.ToChar(values, space);
            space += 2;
            return (char)num;
        }

        public void SetString(string value)
        {
            if (value == null)
            {
                SetInt32(0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                SetInt32(bytes.Length);
                SetBlock(bytes);
            }
        }

        public void SetChar(char value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }
        #endregion

        #region Other
        public byte[] GetBlock(int size)
        {
            if (size <= 0 || space + size > values.Length)
            {
                return new byte[0];
            }
            byte[] numArray = new byte[size];
            Buffer.BlockCopy((Array)values, space, (Array)numArray, 0, size);
            space += size;
            return numArray;
        }

        public object GetObject()
        {
            if (space + 4 > values.Length)
            {
                return (object)null;
            }
            int int32 = BitConverter.ToInt32(values, space);
            space += 4;
            if (int32 <= 0 || space + int32 > values.Length)
            {
                return (object)null;
            }
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.SetLength((long)int32);
            memoryStream.Read(values, space, int32);
            space += int32;
            object obj = new BinaryFormatter().Deserialize((Stream)memoryStream);
            memoryStream.Dispose();
            return obj;
        }

        public byte[] GetBytes()
        {
            if (space + 4 > values.Length)
            {
                return new byte[0];
            }
            int int32 = BitConverter.ToInt32(values, space);
            space += 4;
            if (int32 <= 0 || space + int32 > values.Length)
            {
                return new byte[0];
            }
            byte[] numArray = new byte[int32];
            Buffer.BlockCopy((Array)values, space, (Array)numArray, 0, int32);
            space += int32;
            return numArray;
        }

        public byte GetByte()
        {
            if (space + 1 > values.Length)
            {
                return 0;
            }
            int num = (int)values[space];
            ++space;
            return (byte)num;
        }

        public bool GetBoolean()
        {
            if (space + 1 > values.Length)
            {
                return false;
            }
            int num = BitConverter.ToBoolean(values, space) ? 1 : 0;
            ++space;
            return (uint)num > 0U;
        }

        public float GetSingle()
        {
            if (space + 4 > values.Length)
            {
                return 0.0f;
            }
            double single = (double)BitConverter.ToSingle(values, space);
            space += 4;
            return (float)single;
        }

        public void SetBlock(byte[] bytes)
        {
            CheckSize(bytes.Length);
            Buffer.BlockCopy((Array)bytes, 0, (Array)values, space, bytes.Length);
            space += bytes.Length;
        }

        public void SetBlock(byte[] bytes, int offset, int size)
        {
            CheckSize(size);
            Buffer.BlockCopy((Array)bytes, offset, (Array)values, space, size);
            space += size;
        }

        public void SetObject(object value)
        {
            MemoryStream memoryStream = new MemoryStream();
            new BinaryFormatter().Serialize((Stream)memoryStream, value);
            byte[] array = memoryStream.ToArray();
            int length = array.Length;
            memoryStream.Dispose();
            SetBlock(BitConverter.GetBytes(length));
            SetBlock(array);
        }

        public void SetBytes(byte[] value, int offset, int size)
        {
            SetBlock(BitConverter.GetBytes(size));
            SetBlock(value, offset, size);
        }

        public void SetBytes(byte[] value)
        {
            SetBlock(BitConverter.GetBytes(value.Length));
            SetBlock(value);
        }

        public void SetByte(byte value)
        {
            CheckSize(1);
            values[space] = value;
            ++space;
        }

        public void SetBoolean(bool value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        public void SetSingle(float value)
        {
            SetBlock(BitConverter.GetBytes(value));
        }

        #endregion

        #region Control
        public int space;
        public byte[] values;

        public Carrier(int initialSize = 4)
        {
            values = new byte[initialSize];
            space = 0;
        }

        public Carrier(byte[] bytes)
        {
            values = bytes;
            space = 0;
        }

        public void Dispose()
        {
            values = (byte[])null;
            space = 0;
        }

        public byte[] ToArray()
        {
            byte[] numArray = new byte[space];
            Buffer.BlockCopy((Array)values, 0, (Array)numArray, 0, space);
            return numArray;
        }

        public byte[] ToPacket()
        {
            byte[] numArray = new byte[4 + space];
            Buffer.BlockCopy((Array)BitConverter.GetBytes(space), 0, (Array)numArray, 0, 4);
            Buffer.BlockCopy((Array)values, 0, (Array)numArray, 4, space);
            return numArray;
        }

        private void CheckSize(int length)
        {
            int num = values.Length;
            if (length + space < num)
            {
                return;
            }
            if (num < 4)
            {
                num = 4;
            }
            int length1 = num * 2;
            while (length + space >= length1)
            {
                length1 *= 2;
            }
            byte[] numArray = new byte[length1];
            Buffer.BlockCopy((Array)values, 0, (Array)numArray, 0, space);
            values = numArray;
        }
        #endregion

    }
}
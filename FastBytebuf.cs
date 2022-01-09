using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Bytebuf
{
    public class FastBytebuf : IEnumerable<byte>, IDisposable
    {
        private int _hash = RandomNumberGenerator.Create().GetHashCode();
        private static int _chunkSize = 1024;
        private int _capacity;
        private byte[] _buf;

        private int _refCount = 0;

        private int _writerIndex = 0;
        private int _readerIndex = 0;

        private int _readerMarker = 0;
        private int _writerMarker = 0;


        public int GetWriterIndex()
        {
            return _writerIndex;
        }


        public static FastBytebuf NewBytebufWithDefault()
        {
            var buffer = new byte[_chunkSize];
            return new FastBytebuf(buffer);
        }

        public static FastBytebuf NewByteBufWithCapacity(int cap)
        {
            if (cap > 0)
            {
                _chunkSize = cap;
            }

            var buffer = new byte[_chunkSize];
            return new FastBytebuf(buffer);
        }

        public static FastBytebuf NewFromReader(BinaryReader reader)
        {
            var byteBuf = NewBytebufWithDefault();
            byteBuf.ReadFromIO(reader);
            return byteBuf;
        }

        public FastBytebuf(byte[] srcbuf)
        {
            // 初始化各需要初始化的成员
            _capacity = _chunkSize;
            _buf = new byte[_capacity];
            _readerMarker = -1;
            _writerMarker = -1;
            Write(srcbuf);
            // 初始化完成后，应将索引归零
            _writerIndex = 0;
            _readerIndex = 0;
        }


        /**
         * 扩容2倍，相当于将capacity的值放大
         */
        private void EnsureCapacity(int size)
        {
            if (size < _capacity)
            {
                return;
            }

            var tempCapacity = ((size - 1) / _chunkSize + 1) * _chunkSize;
            var newBuf = new byte[tempCapacity];
            _buf.CopyTo(newBuf, 0);
            _buf = null;
            _buf = newBuf;
            _capacity = tempCapacity;
        }

        /**
         * 返回ByteBuf 尺寸
         */
        public int Size()
        {
            return _writerIndex;
        }

        /**
         * 返回ByteBuf 容量
         */
        public int Capacity()
        {
            return _capacity;
        }


        public byte[] AvailableBytes()
        {
            var p = new byte[_writerIndex];
            _buf.Take(_writerIndex).ToArray().CopyTo(p, 0);
            return p;
        }


        /**
         * Flush all bytes and reset index
         */
        public void Flush()
        {
            _buf = null;
            _buf = new byte[_chunkSize];
            _readerIndex = 0;
            _writerIndex = 0;
            _readerMarker = -1;
            _writerMarker = -1;
            _capacity = _chunkSize;
        }


        public bool Readable()
        {
            return _writerIndex > _readerIndex;
        }

        public int ReadableBytes()
        {
            return _writerIndex - _readerIndex;
        }

        public bool Accessible()
        {
            return _refCount > 0;
        }

        public void Retain()
        {
            _refCount++;
        }


        public int RefCnt()
        {
            return _refCount;
        }

        public void Release()
        {
            _refCount--;
            if (_refCount <= 0) ForceRelease();
        }


        public void ReleaseSafe()
        {
            if (_buf == null)
            {
                return;
            }

            Release();
        }

        public void ForceRelease()
        {
            _refCount = 0;
            _buf = null;
        }

        public int Write(byte[] buffer)
        {
            int n = buffer.Length;
            EnsureCapacity(_writerIndex + n);
            buffer.CopyTo(_buf, _writerIndex);
            _writerIndex += n;
            return n;
        }

        public void PutBoolean(int wid, bool val)
        {
            if (1 + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            if (val)
            {
                _buf[wid] = 0x01;
            }
            else
            {
                _buf[wid] = 0x01;
            }
        }


        public void PutByte(int wid, byte val)
        {
            if (1 + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            _buf[wid] = val;
        }


        public void PutBytes(int wid, byte[] val)
        {
            if (val.Length + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            val.CopyTo(_buf, wid);
        }


        public void PutShort(int wid, short val)
        {
            if (sizeof(short) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutUShort(int wid, ushort val)
        {
            if (sizeof(ushort) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutShortLE(int wid, short val)
        {
            if (sizeof(short) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }

        public void PutUShortLE(int wid, ushort val)
        {
            if (sizeof(ushort) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }


        public void PutInt(int wid, int val)
        {
            if (sizeof(int) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutUInt(int wid, uint val)
        {
            if (sizeof(uint) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutIntLE(int wid, int val)
        {
            if (sizeof(int) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }


        public void PutUIntLE(int wid, uint val)
        {
            if (sizeof(uint) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }


        public void PutLong(int wid, long val)
        {
            if (sizeof(long) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutULong(int wid, ulong val)
        {
            if (sizeof(ulong) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutLongLE(int wid, long val)
        {
            if (sizeof(long) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }


        public void PutULongLE(int wid, ulong val)
        {
            if (sizeof(ulong) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }


        public void PutFloat(int wid, float val)
        {
            if (sizeof(float) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutFloatLE(int wid, float val)
        {
            if (sizeof(float) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }

        public void PutDouble(int wid, double val)
        {
            if (sizeof(double) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            // reverse to BigEndpoint
            Array.Reverse(bytes);
            bytes.CopyTo(_buf, wid);
        }

        public void PutDoubleLE(int wid, double val)
        {
            if (sizeof(double) + wid > _capacity)
            {
                throw new ByteBufException.ExceedCapacityException("writerIndex is out of bound");
            }

            var bytes = BitConverter.GetBytes(val);
            bytes.CopyTo(_buf, wid);
        }


        public void WriteBoolean(bool val)
        {
            EnsureCapacity(_writerIndex + 1);
            PutBoolean(_writerIndex, val);
            _writerIndex++;
        }

        public void WriteByte(byte val)
        {
            EnsureCapacity(_writerIndex + 1);
            PutByte(_writerIndex, val);
            _writerIndex++;
        }

        public void WriteBytes(byte[] val)
        {
            EnsureCapacity(_writerIndex + val.Length);
            PutBytes(_writerIndex, val);
            _writerIndex += val.Length;
        }


        public static FastBytebuf operator +(FastBytebuf lhs, FastBytebuf rhs)
        {
            var newbuf = NewBytebufWithDefault();
            newbuf.WriteByteBuf(lhs);
            newbuf.WriteByteBuf(rhs);
            return newbuf;
        }

        public void WriteByteBuf(FastBytebuf val)
        {
            var newBytes = new byte[val._writerIndex];
            val.ReadBytes(ref newBytes);
            WriteBytes(newBytes);
        }

        public void WriteShort(short val)
        {
            EnsureCapacity(_writerIndex + sizeof(short));
            PutShort(_writerIndex, val);
            _writerIndex += sizeof(short);
        }

        public void WriteUShort(ushort val)
        {
            EnsureCapacity(_writerIndex + sizeof(ushort));
            PutUShort(_writerIndex, val);
            _writerIndex += sizeof(ushort);
        }

        public void WriteShortLE(short val)
        {
            EnsureCapacity(_writerIndex + sizeof(short));
            PutShortLE(_writerIndex, val);
            _writerIndex += sizeof(short);
        }

        public void WriteUShortLE(ushort val)
        {
            EnsureCapacity(_writerIndex + sizeof(ushort));
            PutUShortLE(_writerIndex, val);
            _writerIndex += sizeof(ushort);
        }


        public void WriteInt(int val)
        {
            EnsureCapacity(_writerIndex + sizeof(int));
            PutInt(_writerIndex, val);
            _writerIndex += sizeof(int);
        }

        public void WriteUInt(uint val)
        {
            EnsureCapacity(_writerIndex + sizeof(uint));
            PutUInt(_writerIndex, val);
            _writerIndex += sizeof(uint);
        }

        public void WriteIntLE(int val)
        {
            EnsureCapacity(_writerIndex + sizeof(int));
            PutIntLE(_writerIndex, val);
            _writerIndex += sizeof(int);
        }


        public void WriteUIntLE(uint val)
        {
            EnsureCapacity(_writerIndex + sizeof(uint));
            PutUIntLE(_writerIndex, val);
            _writerIndex += sizeof(uint);
        }


        public void WriteLong(long val)
        {
            EnsureCapacity(_writerIndex + sizeof(long));
            PutLong(_writerIndex, val);
            _writerIndex += sizeof(long);
        }

        public void WriteULong(ulong val)
        {
            EnsureCapacity(_writerIndex + sizeof(ulong));
            PutULong(_writerIndex, val);
            _writerIndex += sizeof(ulong);
        }

        public void WriteLongLE(long val)
        {
            EnsureCapacity(_writerIndex + sizeof(long));
            PutLongLE(_writerIndex, val);
            _writerIndex += sizeof(long);
        }


        public void WriteULongLE(ulong val)
        {
            EnsureCapacity(_writerIndex + sizeof(ulong));
            PutULongLE(_writerIndex, val);
            _writerIndex += sizeof(ulong);
        }

        public void WriteFlaot(float val)
        {
            EnsureCapacity(_writerIndex + sizeof(float));
            PutFloat(_writerIndex, val);
            _writerIndex += sizeof(float);
        }

        public void WriteFlaotLE(float val)
        {
            EnsureCapacity(_writerIndex + sizeof(float));
            PutFloatLE(_writerIndex, val);
            _writerIndex += sizeof(float);
        }

        public void WriteDouble(double val)
        {
            EnsureCapacity(_writerIndex + sizeof(double));
            PutDouble(_writerIndex, val);
            _writerIndex += sizeof(double);
        }

        public void WriteDoubleLE(double val)
        {
            EnsureCapacity(_writerIndex + sizeof(double));
            PutDoubleLE(_writerIndex, val);
            _writerIndex += sizeof(double);
        }

        private void WriteString(byte[] s)
        {
            if (s.Length > 0)
            {
                WriteBytes(s);
            }
        }

        public void WriteStringWithByteLength(string str)
        {
            var bs = Encoding.UTF8.GetBytes(str);
            if (bs.Length > byte.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of byte maxvalue");
            }

            WriteByte((byte) bs.Length);
            WriteString(bs);
        }


        public void WriteStringWithUShortLength(string str)
        {
            var bs = Encoding.UTF8.GetBytes(str);
            if (bs.Length > ushort.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of ushort maxvalue");
            }

            WriteUShort((ushort) bs.Length);
            WriteString(bs);
        }


        public void WriteStringWithUShortLELength(string str)
        {
            var bs = Encoding.UTF8.GetBytes(str);
            if (bs.Length > ushort.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of ushort maxvalue");
            }

            WriteULongLE((ushort) bs.Length);
            WriteString(bs);
        }

        public void WriteStringWithUIntLength(string str)
        {
            var bs = Encoding.UTF8.GetBytes(str);
            if (bs.Length > uint.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of uint maxvalue");
            }

            WriteUInt((uint) bs.Length);
            WriteString(bs);
        }

        public void WriteStringWithUIntLELength(string str)
        {
            var bs = Encoding.UTF8.GetBytes(str);
            if (bs.Length > uint.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of uint maxvalue");
            }

            WriteUIntLE((uint) bs.Length);
            WriteString(bs);
        }


        public void WriteStringWithByteLength(byte[] bs)
        {
            if (bs.Length > byte.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of byte maxvalue");
            }

            WriteByte((byte) bs.Length);
            WriteBytes(bs);
        }


        public void WriteStringWithUShortLength(byte[] bs)
        {
            if (bs.Length > ushort.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of ushort maxvalue");
            }

            WriteUShort((ushort) bs.Length);
            WriteBytes(bs);
        }

        public void WriteStringWithUShortLELength(byte[] bs)
        {
            if (bs.Length > ushort.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of ushort maxvalue");
            }

            WriteUShortLE((ushort) bs.Length);
            WriteBytes(bs);
        }


        public void WriteStringWithUIntLength(byte[] bs)
        {
            if (bs.Length > uint.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of uint maxvalue");
            }

            WriteUInt((uint) bs.Length);
            WriteBytes(bs);
        }

        public void WriteStringWithUIntLELength(byte[] bs)
        {
            if (bs.Length > uint.MaxValue)
            {
                throw new ByteBufException.OutOfMaxValueException("out of uint maxvalue");
            }

            WriteUIntLE((uint) bs.Length);
            WriteBytes(bs);
        }


        /**
         * 写入二进制流
         */
        public void WriteToIO(BinaryWriter writer)
        {
            lock (writer)
            {
                while (true)
                {
                    byte[] chunk = new byte[_chunkSize];
                    int n = Read(ref chunk);
                    if (n == -1) break;
                    writer.Write(chunk.Take(n).ToArray());
                }
            }
        }


        public bool GetBool(int rid)
        {
            if (rid < 0 || rid > _capacity - 1)
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            return _buf[rid] != 0;
        }


        public byte GetByte(int rid)
        {
            if (rid < 0 || rid > _capacity - 1)
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            return _buf[rid];
        }

        public int GetBytes(ref byte[] p, int start, params int[] indexes)
        {
            var end = _capacity;
            if (indexes != null && indexes.Length >= 1 && indexes[0] < end)
            {
                end = indexes[0];
            }

            if (start > end)
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            _buf.Skip(start).Take(end - start).ToArray().CopyTo(p, 0);
            return end - start;
        }


        public ushort GetUShort(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(ushort))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(ushort)).ToArray();
            Array.Reverse(b);
            return BitConverter.ToUInt16(b, 0);
        }


        public short GetShort(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(short))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(short)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToInt16(b, 0);
        }


        public ushort GetUShortLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(ushort))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(ushort)).ToArray();
            ;
            return BitConverter.ToUInt16(b, 0);
        }


        public short GetShortLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(short))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(short)).ToArray();
            ;
            return BitConverter.ToInt16(b, 0);
        }


        public uint GetUInt(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(uint))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(uint)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToUInt32(b, 0);
        }

        public int GetInt(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(int))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(int)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToInt32(b, 0);
        }


        public uint GetUIntLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(uint))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(uint)).ToArray();
            ;
            return BitConverter.ToUInt32(b, 0);
        }

        public int GetIntLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(int))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(int)).ToArray();
            ;
            return BitConverter.ToInt32(b, 0);
        }


        public ulong GetULong(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(ulong))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(ulong)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToUInt64(b, 0);
        }

        public long GetLong(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(long))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(long)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToInt64(b, 0);
        }


        public ulong GetULongLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(ulong))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(ulong)).ToArray();
            ;
            return BitConverter.ToUInt64(b, rid);
        }

        public long GetLongLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(long))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(long)).ToArray();
            ;
            return BitConverter.ToInt64(b, 0);
        }


        public float GetFloat(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(float))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(float)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToSingle(b, 0);
        }

        public float GetFloatLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(float))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(float)).ToArray();
            ;
            return BitConverter.ToSingle(b, 0);
        }


        public double GetDouble(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(double))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(double)).ToArray();
            ;
            Array.Reverse(b);
            return BitConverter.ToDouble(b, 0);
        }

        public double GetDoubleLE(int rid)
        {
            if (rid < 0 || rid > _capacity - sizeof(double))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var b = AvailableBytes().Skip(rid).Take(sizeof(double)).ToArray();
            ;
            return BitConverter.ToDouble(b, 0);
        }


        public int Read(ref byte[] b)
        {
            if (_readerIndex >= _writerIndex)
            {
                return -1;
            }

            var n = b.Length;
            if (b.Length + _readerIndex > _writerIndex)
            {
                n = _writerIndex - _readerIndex;
            }

            _buf.Skip(_readerIndex).Take(n).ToArray().CopyTo(b, 0);
            _readerIndex += n;
            return n;
        }


        public bool ReadBoolean()
        {
            if (_readerIndex > _writerIndex - 1)
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetBool(_readerIndex);
            _readerIndex++;
            return val;
        }

        public byte ReadByte()
        {
            if (_readerIndex > _writerIndex - 1)
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetByte(_readerIndex);
            _readerIndex++;
            return val;
        }


        public void ReadBytes(ref byte[] p)
        {
            var n = GetBytes(ref p, _readerIndex, _readerIndex + p.Length);
            _readerIndex += n;
        }


        public ushort ReadUShort()
        {
            if (_readerIndex > _writerIndex - sizeof(ushort))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetUShort(_readerIndex);
            _readerIndex += sizeof(ushort);
            return val;
        }

        public short ReadShort()
        {
            if (_readerIndex > _writerIndex - sizeof(short))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetShort(_readerIndex);
            _readerIndex += sizeof(short);
            return val;
        }

        public ushort ReadUShortLE()
        {
            if (_readerIndex > _writerIndex - sizeof(ushort))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetUShortLE(_readerIndex);
            _readerIndex += sizeof(ushort);
            return val;
        }

        public short ReadShortLE()
        {
            if (_readerIndex > _writerIndex - sizeof(short))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetShortLE(_readerIndex);
            _readerIndex += sizeof(ushort);
            return val;
        }


        public uint ReadUInt()
        {
            if (_readerIndex > _writerIndex - sizeof(uint))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetUInt(_readerIndex);
            _readerIndex += sizeof(uint);
            return val;
        }

        public int ReadInt()
        {
            if (_readerIndex > _writerIndex - sizeof(int))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetInt(_readerIndex);
            _readerIndex += sizeof(int);
            return val;
        }

        public uint ReadUIntLE()
        {
            if (_readerIndex > _writerIndex - sizeof(uint))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetUIntLE(_readerIndex);
            _readerIndex += sizeof(uint);
            return val;
        }

        public int ReadIntLE()
        {
            if (_readerIndex > _writerIndex - sizeof(int))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetIntLE(_readerIndex);
            _readerIndex += sizeof(int);
            return val;
        }


        public ulong ReadULong()
        {
            if (_readerIndex > _writerIndex - sizeof(ulong))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetULong(_readerIndex);
            _readerIndex += sizeof(ulong);
            return val;
        }

        public long ReadLong()
        {
            if (_readerIndex > _writerIndex - sizeof(long))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetLong(_readerIndex);
            _readerIndex += sizeof(long);
            return val;
        }


        public ulong ReadULongLE()
        {
            if (_readerIndex > _writerIndex - sizeof(ulong))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetULongLE(_readerIndex);
            _readerIndex += sizeof(ulong);
            return val;
        }

        public long ReadLongLE()
        {
            if (_readerIndex > _writerIndex - sizeof(long))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetLongLE(_readerIndex);
            _readerIndex += sizeof(long);
            return val;
        }


        public float ReadFloat()
        {
            if (_readerIndex > _writerIndex - sizeof(float))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetFloat(_readerIndex);
            _readerIndex += sizeof(float);
            return val;
        }

        public float ReadFloatLE()
        {
            if (_readerIndex > _writerIndex - sizeof(float))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetFloatLE(_readerIndex);
            _readerIndex += sizeof(float);
            return val;
        }


        public double ReadDouble()
        {
            if (_readerIndex > _writerIndex - sizeof(double))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetDouble(_readerIndex);
            _readerIndex += sizeof(double);
            return val;
        }

        public double ReadDoubleLE()
        {
            if (_readerIndex > _writerIndex - sizeof(double))
            {
                throw new ByteBufException.ExceedCapacityException("rid is out of bound");
            }

            var val = GetDoubleLE(_readerIndex);
            _readerIndex += sizeof(double);
            return val;
        }


        public string ReadString(uint strLen)
        {
            if (strLen < 1)
            {
                return "";
            }

            byte[] bytes = new byte[strLen];
            ReadBytes(ref bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadeStringWithByteLength()
        {
            // 读长度
            var strLen = ReadByte();
            return ReadString(strLen);
        }

        public string ReadeStringWithUShortLength()
        {
            // 读长度
            var strLen = ReadUShort();
            return ReadString(strLen);
        }

        public string ReadeStringWithUShortLELength()
        {
            // 读长度
            var strLen = ReadUShortLE();
            return ReadString(strLen);
        }

        public string ReadeStringWithUIntLength()
        {
            // 读长度
            var strLen = ReadUInt();
            return ReadString(strLen);
        }

        public string ReadeStringWithUIntLELength()
        {
            // 读长度
            var strLen = ReadUIntLE();
            return ReadString(strLen);
        }


        public byte[] ReadeBytesWithByteLength()
        {
            // 读长度
            var strLen = ReadByte();
            byte[] bytes = new byte[strLen];
            ReadBytes(ref bytes);
            return bytes;
        }

        public byte[] ReadeBytesWithUShortLength()
        {
            // 读长度
            var strLen = ReadUShort();
            byte[] bytes = new byte[strLen];
            ReadBytes(ref bytes);
            return bytes;
        }

        public byte[] ReadeBytesWithUShortLELength()
        {
            // 读长度
            var strLen = ReadUShortLE();
            byte[] bytes = new byte[strLen];
            ReadBytes(ref bytes);
            return bytes;
        }

        public byte[] ReadeBytesWithUIntLength()
        {
            // 读长度
            var strLen = ReadUInt();
            byte[] bytes = new byte[strLen];
            ReadBytes(ref bytes);
            return bytes;
        }

        public byte[] ReadeBytesWithUIntLELength()
        {
            // 读长度
            var strLen = ReadUIntLE();
            byte[] bytes = new byte[strLen];
            ReadBytes(ref bytes);
            return bytes;
        }


        /**
         * 从IO中读
         */
        public void ReadFromIO(BinaryReader reader)
        {
            while (true)
            {
                try
                {
                    var chunk = reader.ReadBytes(_chunkSize);
                    WriteBytes(chunk);
                }
                catch (EndOfStreamException)
                {
                    return;
                }
            }
        }


        public void MarkReaderIndex()
        {
            _readerMarker = _readerIndex;
        }

        public void MarkWriterIndex()
        {
            _writerMarker = _writerIndex;
        }

        public void ResetReaderIndex()
        {
            if (_readerMarker == -1) return;
            _readerIndex = _readerMarker;
            _readerMarker = -1;
        }

        public void ResetWriterIndex()
        {
            if (_writerMarker == -1) return;
            _writerIndex = _writerMarker;
            _writerMarker = -1;
        }

        public void SetReaderIndex(int rid)
        {
            _readerIndex = rid;
        }

        public void SetWriterIndex(int wid)
        {
            _writerIndex = wid;
        }

        public int GetReaderIndex()
        {
            return _readerIndex;
        }


        public void Skip(int val)
        {
            _readerIndex = _readerIndex + val;
        }

        public void DiscardReadBytes()
        {
            _buf = _buf.Skip(_readerIndex).Take(_writerIndex - _readerIndex).ToArray();
            _writerIndex -= _readerIndex;
            _readerIndex = 0;
        }

        // Copy deep copy to create an new ByteBuf
        public FastBytebuf DeepCopy()
        {
            var p = new byte[_buf.Length];
            _buf.CopyTo(p, 0);
            return new FastBytebuf(p);
        }


        public FastBytebuf ShallowCopy()
        {
            var b = NewByteBufWithCapacity(_capacity);
            b._hash = _hash;
            b._buf = _buf;
            b._readerIndex = _readerIndex;
            b._refCount = _refCount;
            b._writerIndex = _writerIndex;
            return b;
        }


        public void PrettyPrint()
        {
            FastByteBufUtil.PrettyPrint(this);
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>) AvailableBytes()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var v in AvailableBytes())
            {
                sb.Append(v);
            }

            return sb.ToString();
        }


        public override bool Equals(Object obj)
        {
            return _buf.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public void Dispose()
        {
            ForceRelease();
        }
    }
}
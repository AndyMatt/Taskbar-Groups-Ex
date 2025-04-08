using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarGroupsEx.Handlers
{    internal class ByteReader : IDisposable
    {
        byte[] bytes;
        uint _iterator;

        const int GUID_Size = 16;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public ByteReader(byte[] bytes)
        {
            this.bytes = bytes;
            _iterator = 0;
        }

        public ByteReader(ByteReader byteReader)
        {
            this.bytes = byteReader.bytes;
            this._iterator = byteReader._iterator;
        }

        public bool End()
        {
            return !(_iterator < bytes.Length);
        }

        public uint read_uint(bool advance = true)
        {
            uint result = (uint)bytes[_iterator + 3] << 24 | (uint)bytes[_iterator + 2] << 16 | (uint)bytes[_iterator + 1] << 8 | bytes[_iterator];
            _iterator += advance ? (uint)sizeof(uint) : 0;
            return result;
        }

        public uint scan_uint(uint offset = 0)
        {
            uint _of = offset + _iterator;
            uint result = (uint)bytes[_of + 3] << 24 | (uint)bytes[_of + 2] << 16 | (uint)bytes[_of + 1] << 8 | bytes[_of];
            return result;
        }

        public ushort read_ushort(bool advance = true)
        {
            ushort result = (ushort)(bytes[_iterator + 1] << 8 | bytes[_iterator]);
            _iterator += advance ? (uint)sizeof(ushort) : 0;
            return result;
        }

        public byte read_byte(bool advance = true)
        {
            return advance ? bytes[_iterator++] : bytes[_iterator];
        }

        public byte[] read_bytes(uint size, bool advance = true)
        {
            byte[] result = new byte[size];
            Array.Copy(bytes, _iterator, result, 0, size);
            _iterator += advance ? size : 0;
            return result;
        }

        public string read_AsciiString(bool advance = true)
        {
            uint strLength = 0;

            while (bytes[_iterator + strLength] != 0)
            {
                strLength++;
            }

            string result = Encoding.ASCII.GetString(bytes, (int)_iterator, (int)strLength);
            _iterator += advance ? strLength + 1 : 0;
            return result.Trim('\0');
        }

        public string read_UnicodeString(int strLength, bool advance = true)
        {
            string result = Encoding.Unicode.GetString(bytes, (int)_iterator, (int)strLength * 2);
            _iterator += advance ? (uint)strLength * 2 : 0;
            return result.Trim('\0');
        }

        public string read_LPWSTR(bool advance = true)
        {
            uint strLength = read_uint() * 2;
            string result = Encoding.Unicode.GetString(bytes, (int)_iterator, (int)strLength);
            _iterator += advance ? strLength : 0;
            return result.Trim('\0');
        }

        public string read_UnicodeString(bool advance = true)
        {
            uint pos = _iterator;

            do
            {
                pos += 2;
            }
            while (bytes[pos] != 0 << 8 | bytes[pos + 1] != 0);

            uint length = pos - _iterator;

            string result = Encoding.Unicode.GetString(bytes, (int)_iterator, (int)length);
            _iterator += advance ? length + 2 : 0;
            return result.Trim('\0');
        }

        public uint findGuid(Guid guid)
        {
            return findPosition(guid.ToByteArray());
        }

        public void jump2guid(Guid guid)
        {
            _iterator = findGuid(guid);
            read_guid();
        }

        public uint findPosition(byte[] byteSequence)
        {
            if (byteSequence.Length > bytes.Length - _iterator)
                return uint.MaxValue;

            uint _itrTrmp = _iterator;

            while (_itrTrmp < bytes.Length)
            {
                uint byteSeqItr = 0;
                while (bytes[_itrTrmp + byteSeqItr] == byteSequence[byteSeqItr])
                {
                    byteSeqItr++;
                    if (byteSeqItr == byteSequence.Length - 1)
                    {
                        return _itrTrmp;
                    }
                }

                _itrTrmp++;
            }
            return uint.MaxValue;
        }

        public Guid read_guid()
        {
            ReadOnlySpan<byte> guidBytes = new ReadOnlySpan<byte>(bytes, (int)_iterator, GUID_Size);
            _iterator += GUID_Size;
            return new Guid(guidBytes);
        }
    }
}

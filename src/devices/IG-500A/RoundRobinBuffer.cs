using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable 1591
namespace Iot.Device.Imu
{
    public class RoundRobinBuffer
    {
        private readonly int _bufferSizeInBytes;
        private readonly byte[] _array;
        private readonly object _lock;
        private int _sizeUsed;
        private int _nextWriteByte;
        private int _nextReadByte;

        public RoundRobinBuffer(int bufferSizeInBytes)
        {
            _lock = new object();
            _bufferSizeInBytes = bufferSizeInBytes;
            _array = new byte[bufferSizeInBytes];
            _nextWriteByte = 0;
            _nextReadByte = 0;
            _sizeUsed = 0;
        }

        public int BufferSize
        {
            get
            {
                return _bufferSizeInBytes;
            }
        }

        public int BytesInBuffer
        {
            get
            {
                return _sizeUsed;
            }
        }

        public float PercentageUsed
        {
            get
            {
                return (float)_sizeUsed / _bufferSizeInBytes * 100;
            }
        }

        public void InsertBytes(byte[] dataToInsert, int count)
        {
            int dataLen = count;
            lock (_lock)
            {
                if (_sizeUsed + dataLen >= _bufferSizeInBytes)
                {
                    throw new InvalidOperationException("Buffer overflow");
                }

                if (_nextWriteByte + dataLen <= _bufferSizeInBytes)
                {
                    Array.ConstrainedCopy(dataToInsert, 0, _array, _nextWriteByte, dataLen);
                    _nextWriteByte = (_nextWriteByte + dataLen) % _bufferSizeInBytes;
                    _sizeUsed += dataLen;
                }
                else
                {
                    Array.ConstrainedCopy(dataToInsert, 0, _array, _nextWriteByte, _bufferSizeInBytes - _nextWriteByte);
                    int remaining = dataLen - (_bufferSizeInBytes - _nextWriteByte);
                    Array.ConstrainedCopy(dataToInsert, dataLen - remaining, _array, 0, remaining);
                    _nextWriteByte = remaining;
                    _sizeUsed += dataLen;
                }
            }
        }

        public int GetBuffer(byte[] buffer, int count)
        {
            lock (_lock)
            {
                int toCopy = 0;
                if (_sizeUsed == 0)
                {
                    return 0;
                }
                else if (count <= _sizeUsed)
                {
                    toCopy = count;
                }
                else
                {
                    toCopy = _sizeUsed;
                }

                if (_nextReadByte + toCopy <= _bufferSizeInBytes)
                {
                    Array.ConstrainedCopy(_array, _nextReadByte, buffer, 0, toCopy);
                }
                else
                {
                    int firstPart = _bufferSizeInBytes - _nextReadByte;
                    Array.ConstrainedCopy(_array,  _nextReadByte, buffer, 0, firstPart);
                    Array.ConstrainedCopy(_array, 0, buffer, firstPart, toCopy - firstPart);
                }

                return toCopy;
            }
        }

        public void ConsumeBytes(int count)
        {
            lock (_lock)
            {
                _nextReadByte = (_nextReadByte + count) % _bufferSizeInBytes;
                _sizeUsed -= count;
            }
        }
    }
}

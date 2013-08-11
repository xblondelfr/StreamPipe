using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public class StreamPipe : Stream
    {
        public static readonly int DEFAULT_BUFFER_SIZE = 8192;

        byte[] _internalBuffer;
        int _readPosition;
        int _writePosition;
        object _lockObject;
        bool _isWriteFinished;
        int _availableBytes;

        public int BufferSize { get; private set; }

        public StreamPipe(int bufferSize)
            : base()
        {
            _lockObject = new object();
            _internalBuffer = new byte[bufferSize];
            BufferSize = bufferSize;
            _readPosition = 0;
            _writePosition = 0;
            _isWriteFinished = false;
        }

        public StreamPipe()
            : this(DEFAULT_BUFFER_SIZE)
        {
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
            get 
            {
                lock(_lockObject)
                    return !_isWriteFinished; 
            }
        }

        public override void Flush()
        {
            // Nothing to do on the underlying device
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lockObject)
            {
                int total = 0;
                while (_availableBytes == 0 && !_isWriteFinished)
                    Monitor.Wait(_lockObject);
                int actualCount = GetMaxNextReadCount(Math.Min(_availableBytes, count));
                _internalBuffer.Skip(_readPosition).Take(actualCount).ToArray().CopyTo(buffer, offset);
                _readPosition = (_readPosition + actualCount) % BufferSize;
                _availableBytes -= actualCount;
                count -= actualCount;
                offset += actualCount;
                total += actualCount;
                Monitor.Pulse(_lockObject);
                return total;
            }
        }

        private int GetMaxNextReadCount(int count)
        {
            if (_availableBytes == 0)
                return 0;
            int available;
            if (_writePosition > _readPosition)
                available = _writePosition - _readPosition;
            else
                available = BufferSize - _readPosition;
            return Math.Min(available, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lockObject)
            {
                while (count > 0)
                {
                    while (_availableBytes == BufferSize)
                        Monitor.Wait(_lockObject);
                    int actualCount = GetMaxNextWriteCount(count);
                    buffer.Skip(offset).Take(actualCount).ToArray().CopyTo(_internalBuffer, _writePosition);
                    _writePosition = (_writePosition + actualCount) % BufferSize;
                    _availableBytes += actualCount;
                    count -= actualCount;
                    offset += actualCount;
                    Monitor.Pulse(_lockObject);
                }
            }
        }

        private int GetMaxNextWriteCount(int count)
        {
            if (_availableBytes == BufferSize)
                return 0;
            int available;
            if (_writePosition >= _readPosition)
                available = BufferSize - _writePosition;
            else
                available = _readPosition - _writePosition;
            return Math.Min(available, count);
        }

        public void WriteIsFinished()
        {
            lock (_lockObject)
            {
                _isWriteFinished = true;
                Monitor.Pulse(_lockObject);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HenkesUtils {
    public class ChainStream : Stream {

        private List<StreamInfo> baseStreams;
        private long currentPosition;

        public ChainStream(List<Stream> streams) {
            if(streams == null) throw new ArgumentNullException(nameof(streams));

            if(streams.Count() == 0) throw new ArgumentException();

            baseStreams = new List<StreamInfo>(streams.Count);

            long offset = 0;

            foreach(Stream stream in streams) {
                if(!stream.CanRead) throw new ArgumentException();
                if(!stream.CanSeek) throw new ArgumentException();

                baseStreams.Add(new StreamInfo(stream, offset));

                offset += stream.Length;
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length {
            get {
                var lastInfo = baseStreams[baseStreams.Count - 1];
                return lastInfo.length + lastInfo.offset;
            }
        }

        public override long Position { set => currentPosition = value; get => currentPosition; }

        public override void Flush() {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int bufferWriteOffset, int count) {
            if(buffer == null) throw new ArgumentNullException(nameof(buffer));
            if(bufferWriteOffset < 0) throw new ArgumentException();
            if(count < 0) throw new ArgumentException();
            if(bufferWriteOffset + count > buffer.Length) throw new ArgumentException();

            int bytesRead = 0;
            foreach(StreamInfo streamInfo in baseStreams) {
                long localStart = currentPosition - streamInfo.offset;
                if(localStart < 0) continue;
                if(localStart > streamInfo.length) continue;

                long localEnd = localStart + count;
                if(localEnd > streamInfo.length) {
                    localEnd = streamInfo.length;
                }
                int readCount = (int)(localEnd - localStart);

                streamInfo.stream.Seek(localStart, SeekOrigin.Begin);

                int localBufferWriteOffset = bufferWriteOffset + bytesRead;

                bytesRead += streamInfo.stream.Read(buffer, localBufferWriteOffset, readCount);
            }

            currentPosition += bytesRead;

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch(origin) {
                case SeekOrigin.Begin:
                    return currentPosition = offset;
                case SeekOrigin.Current:
                    return currentPosition += offset;
                case SeekOrigin.End:
                    return currentPosition = Length + offset;
                default:
                    throw new ArgumentException();
            }
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }

        private struct StreamInfo {
            public long offset;
            public long length;
            public Stream stream;

            public StreamInfo(Stream stream, long offset) : this() {
                this.stream = stream;
                this.offset = offset;
                this.length = stream.Length;
            }
        }
    }
}

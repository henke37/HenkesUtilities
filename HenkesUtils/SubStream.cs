using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HenkesUtils {
    public class SubStream : Stream {
        private Stream stream;

        public long offset;
        public long limit;

        private long currentPosition;

        public SubStream(SubStream baseSub, long offset, long limit = long.MaxValue) {
            if(offset < 0) throw new ArgumentOutOfRangeException("The offset can't be negative!", nameof(offset));
            this.offset = offset;
            if(limit < 0) throw new ArgumentOutOfRangeException("The limit can't be negative!", nameof(limit));
            this.limit = limit;

            FromBaseSubStream(baseSub, offset, limit);
        }

        private void FromBaseSubStream(SubStream baseSub, long offset, long limit) {
            this.stream = baseSub.stream;
            this.offset = baseSub.offset + offset;
            this.limit = (limit < baseSub.limit ? limit : baseSub.limit);
        }

        public SubStream(Stream stream, long offset, long limit = long.MaxValue) {
            if(offset < 0) throw new ArgumentOutOfRangeException("The offset can't be negative!", nameof(offset));
            this.offset = offset;
            if(limit < 0) throw new ArgumentOutOfRangeException("The limit can't be negative!", nameof(limit));
            this.limit = limit;

            {
                var baseSub = stream as SubStream;
                if(baseSub != null) {
                    FromBaseSubStream(baseSub, offset, limit);
                    return;
                }
            }
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if(!stream.CanSeek) throw new ArgumentException("The stream must be seekable!", nameof(stream));


        }

		public SubStream(BinaryReader r, long limit=long.MaxValue) : this(r.BaseStream, r.BaseStream.Position, limit) {
		}

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => true;
        public override bool CanWrite => stream.CanWrite;
        public override bool CanTimeout => stream.CanTimeout;

        public override long Length {
            get {
                var uncappedLen = stream.Length - offset;
                if(uncappedLen > limit) return limit;
                return uncappedLen;
            }
        }

        public override long Position { set => currentPosition = value; get => currentPosition; }

        public override void Flush() {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int bufferWriteOffset, int count) {
            if(buffer == null) throw new ArgumentNullException(nameof(buffer));
            if(bufferWriteOffset < 0) throw new ArgumentOutOfRangeException(nameof(bufferWriteOffset));
            if(count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if(bufferWriteOffset + count > buffer.Length) throw new ArgumentException();

            stream.Seek(currentPosition + offset, SeekOrigin.Begin);
            int bytesToRead = count;
            if(bytesToRead > Length) bytesToRead = (int)Length;
            int bytesRead = stream.Read(buffer, bufferWriteOffset, bytesToRead);
            currentPosition += bytesRead;

            return bytesRead;
        }

		public override void Write(byte[] buffer, int bufferReadOffset, int count) {
			if(buffer == null) throw new ArgumentNullException(nameof(buffer));
			if(count < 0) throw new ArgumentOutOfRangeException(nameof(count));
			if(bufferReadOffset < 0) throw new ArgumentOutOfRangeException(nameof(bufferReadOffset));

			if(count + currentPosition > Length) throw new ArgumentOutOfRangeException();

			stream.Seek(currentPosition + offset, SeekOrigin.Begin);
			int bytesToWrite = count;

			stream.Write(buffer, bufferReadOffset, bytesToWrite);
			currentPosition += bytesToWrite;
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

        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }
    }
}
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test_StreamPipe
{
    [TestClass]
    public class UnitTests
    {
        /// <summary>
        /// Simple read/write
        /// </summary>
        [TestMethod]
        public void BasicWriteReadTest1()
        {
            StreamPipe target = new StreamPipe();

            byte[] testData = new byte[] { 0xED, 0x4B, 0x0C, 0x40 };
            target.Write(testData, 0, testData.Length);
            
            byte[] buffer = new byte[testData.Length];
            int length = target.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(testData.Length, length);
            CollectionAssert.AreEqual(testData, buffer);
        }

        /// <summary>
        /// Read data in two phases
        /// </summary>
        [TestMethod]
        public void BasicWriteReadTest2()
        {
            StreamPipe target = new StreamPipe();

            byte[] testData = new byte[] { 0xED, 0x4B, 0x0C, 0x40 };
            target.Write(testData, 0, testData.Length);

            byte[] buffer = new byte[testData.Length];
            int length;
            length = target.Read(buffer, 0, 2);
            Assert.AreEqual(2, length);
            length = target.Read(buffer, 2, 2);
            Assert.AreEqual(2, length);

            CollectionAssert.AreEqual(testData, buffer);
        }

        /// <summary>
        /// Read more data than what has been written
        /// </summary>
        [TestMethod]
        public void BasicWriteReadTest3()
        {
            StreamPipe target = new StreamPipe();

            byte[] testData = new byte[] { 0xED, 0x4B, 0x0C, 0x40 };
            target.Write(testData, 0, testData.Length);

            byte[] buffer = new byte[testData.Length * 2];
            int length = target.Read(buffer, 0, buffer.Length);

            Assert.AreEqual(testData.Length, length);
            CollectionAssert.AreEqual(testData, buffer.Take(length).ToArray());
        }

        /// <summary>
        /// Wrap the buffer around
        /// </summary>
        [TestMethod]
        public void BasicWriteReadTest5()
        {
            StreamPipe target = new StreamPipe(4);

            byte[] testData = new byte[] { 0, 1, 2, 3, 4, 5, 6 };
            byte[] buffer = new byte[testData.Length];

            int pos = 0;
            int left = testData.Length;
            int length = 0;
            while (left > 0)
            {
                int size = Math.Min(left, target.BufferSize);
                target.Write(testData, pos, size);
                length += target.Read(buffer, pos, size);
                pos += size;
                left -= size;
            }

            Assert.AreEqual(testData.Length, length);
            CollectionAssert.AreEqual(testData, buffer.Take(length).ToArray());
        }

        /// <summary>
        /// Wrap the buffer around while writing
        /// </summary>
        [TestMethod]
        public void BasicWriteReadTest6()
        {
            StreamPipe target = new StreamPipe(4);

            byte[] testData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] buffer = new byte[testData.Length];

            // with a size of 3, writes and reads will wrap around the buffer
            // during the operation
            // just in case someone modified testData, we perform a sanity check on size
            int size = 3;
            Assert.AreEqual(0, testData.Length % size);

            int pos = 0;
            int left = testData.Length;
            int length = 0;
            while (left > 0)
            {
                target.Write(testData, pos, size);
                // while reading, a wrap around may return us less data
                // than what is stored in the buffer
                int sz = 0;
                while (sz < size)
                    sz += target.Read(buffer, pos + sz, size - sz);
                length += size;
                pos += size;
                left -= size;
            }

            Assert.AreEqual(testData.Length, length);
            CollectionAssert.AreEqual(testData, buffer);
        }

        /// <summary>
        /// One thread writes while the other thread reads
        /// </summary>
        [TestMethod]
        public void ConcurrentReadWriteTest1()
        {
            StreamPipe target = new StreamPipe(4);

            byte[] testData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] buffer = new byte[testData.Length];

            Task w = new Task(() =>
            {
                // we write the whole buffer in one shot
                target.Write(testData, 0, testData.Length);
                target.WriteIsFinished();
            }
            );
            w.Start();

            int length = 0;
            int size = 0;
            int pos = 0;
            bool finished = false;
            // we read the whole buffer until it's over
            while (!finished)
            {
                size = target.Read(buffer, pos, buffer.Length - length);
                length += size;
                pos += size;
                // we have finished reading when we do not read anything
                finished = size == 0;
            }
            Assert.AreEqual(testData.Length, length);
            CollectionAssert.AreEqual(testData, buffer);
        }

        /// <summary>
        /// One thread writes while the other thread reads
        /// Use a text writer to write a whole text
        /// </summary>
        [TestMethod]
        public void ConcurrentReadWriteTest2()
        {
            StreamPipe target = new StreamPipe(4);

            string testData =
                "Lorem ipsum dolor sit amet, " + Environment.NewLine +
                "consectetuer adipiscing elit, " + Environment.NewLine +
                "sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat." + Environment.NewLine;

            Task w = new Task(() =>
            {
                using (TextWriter writer = new StreamWriter(target))
                    writer.Write(testData);
                target.WriteIsFinished();
            }
            );
            w.Start();

            string actual;
            using (TextReader reader = new StreamReader(target))
                actual = reader.ReadToEnd();

            Assert.AreEqual(testData, actual);
        }


    }
}

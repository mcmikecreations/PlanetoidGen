using PlanetoidGen.BusinessLogic.Helpers;
using Xunit;

namespace PlanetoidGen.BusinessLogic.Tests.Helpers
{
    public class GrowingRingBufferTests
    {
        [Fact]
        public void BufferConstructs()
        {
            GrowingRingBuffer<int> buffer1 = new();
            GrowingRingBuffer<int> buffer2 = new(0);
            GrowingRingBuffer<int> buffer3 = new(1);
            GrowingRingBuffer<int> buffer4 = new(2);

            Assert.Equal(1, buffer1.Capacity);
            Assert.Equal(1, buffer2.Capacity);
            Assert.Equal(1, buffer3.Capacity);
            Assert.Equal(2, buffer4.Capacity);
        }

        [Fact]
        public void BufferAdd()
        {
            GrowingRingBuffer<int> buffer = new();
            for (var i = 0; i < 5; ++i)
            {
                buffer.Add(i);
            }

            Assert.Equal(6, buffer.Capacity);
            Assert.Equal(5, buffer.Count);
            Assert.Equal(5, buffer.Head);
            Assert.Equal(0, buffer.Tail);
        }

        [Fact]
        public void BufferRemove()
        {
            GrowingRingBuffer<int> buffer = new() { 1 };
            var value = buffer.Remove();
            Assert.Equal(2, buffer.Capacity);
            Assert.Equal(1, value);
            Assert.Equal(1, buffer.Head);
            Assert.Equal(1, buffer.Tail);
            Assert.Equal(0, buffer.Count);
        }

        [Fact]
        public void BufferEnumerator()
        {
            GrowingRingBuffer<int> buffer = new(5);

            foreach (var _ in buffer)
            {
                Assert.True(false);
            }

            buffer.Add(1);
            buffer.Add(1);
            buffer.Add(1);

            foreach (var i in buffer)
            {
                Assert.Equal(1, i);
            }
        }
    }
}

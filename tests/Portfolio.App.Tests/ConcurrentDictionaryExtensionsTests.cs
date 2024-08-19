using System.Collections.Concurrent;
using FluentAssertions;
using Moq;

namespace Portfolio.App.Tests
{
    public class ConcurrentDictionaryExtensionsTests
    {
        [Test]
        public async Task GetOrAddAsync_ShouldReturnSameInstance_WhenCalledConcurrently()
        {
            // Arrange
            var dictionary = new ConcurrentDictionary<int, Lazy<Task<string>>>();
            var factoryMock = new Mock<Func<int, Task<string>>>();

            factoryMock.Setup(f => f(It.IsAny<int>())).ReturnsAsync((int key) => $"Value {key}");

            // Act
            var task1 = Task.Run(() => dictionary.GetOrAddAsync(1, factoryMock.Object));
            var task2 = Task.Run(() => dictionary.GetOrAddAsync(1, factoryMock.Object));

            await Task.WhenAll(task1, task2);

            // Assert
            task1.Result.Should().Be(task2.Result);
            factoryMock.Verify(f => f(It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task GetOrAddAsync_ShouldInvokeFactoryOnce_WhenCalledConcurrently()
        {
            // Arrange
            var dictionary = new ConcurrentDictionary<int, Lazy<Task<string>>>();
            var factoryMock = new Mock<Func<int, Task<string>>>();
            var factoryCallCount = 0;

            factoryMock.Setup(f => f(It.IsAny<int>()))
                .ReturnsAsync((int key) =>
                {
                    Interlocked.Increment(ref factoryCallCount);
                    return $"Value {key}";
                });

            // Act
            var task1 = Task.Run(() => dictionary.GetOrAddAsync(1, factoryMock.Object));
            var task2 = Task.Run(() => dictionary.GetOrAddAsync(1, factoryMock.Object));
            var task3 = Task.Run(() => dictionary.GetOrAddAsync(1, factoryMock.Object));

            await Task.WhenAll(task1, task2, task3);

            // Assert
            task1.Result.Should().Be(task2.Result);
            task2.Result.Should().Be(task3.Result);
            factoryCallCount.Should().Be(1);  // Ensure the factory was called exactly once
        }

        [Test]
        public async Task GetOrAddAsync_ShouldNotAddValue_WhenFactoryThrowsException()
        {
            // Arrange
            var dictionary = new ConcurrentDictionary<int, Lazy<Task<string>>>();

            Func<int, Task<string>> valueFactory = async key =>
            {
                await Task.Delay(10); // Simulate async operation
                throw new InvalidOperationException("Factory error");
            };

            // Act
            Exception caughtException = null;
            try
            {
                await dictionary.GetOrAddAsync(1, valueFactory);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            caughtException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Be("Factory error");

            dictionary.Should().BeEmpty(); // The dictionary should not contain the key since the factory failed
        }

        [Test]
        public async Task TryAddAsync_ShouldReturnTrue_WhenCalledForFirstTime()
        {
            // Arrange
            var dictionary = new ConcurrentDictionary<int, Lazy<Task<string>>>();
            var factoryMock = new Mock<Func<int, Task<string>>>();

            factoryMock.Setup(f => f(It.IsAny<int>())).ReturnsAsync((int key) => $"Value {key}");

            // Act
            var result = await dictionary.TryAddAsync(1, factoryMock.Object);

            // Assert
            result.Should().BeTrue();
            factoryMock.Verify(f => f(It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task TryAddAsync_ShouldReturnFalse_WhenKeyAlreadyExists()
        {
            // Arrange
            var dictionary = new ConcurrentDictionary<int, Lazy<Task<string>>>();
            var factoryMock = new Mock<Func<int, Task<string>>>();

            factoryMock.Setup(f => f(It.IsAny<int>())).ReturnsAsync((int key) => $"Value {key}");

            await dictionary.TryAddAsync(1, factoryMock.Object);  // First add

            // Act
            var result = await dictionary.TryAddAsync(1, factoryMock.Object);  // Second attempt to add

            // Assert
            result.Should().BeFalse();
            factoryMock.Verify(f => f(It.IsAny<int>()), Times.Once);  // Factory should only be called once
        }

        [Test]
        public async Task AddOrUpdateAsync_ShouldAddOrUpdateValueCorrectly()
        {
            // Arrange
            var dictionary = new ConcurrentDictionary<int, Lazy<Task<string>>>();

            Func<int, Task<string>> addValueFactory = async key =>
            {
                await Task.Delay(10); // Simulate async operation
                return $"Added Value {key}";
            };

            Func<int, string, Task<string>> updateValueFactory = async (key, oldValue) =>
            {
                await Task.Delay(10); // Simulate async operation
                return $"Updated Value {key}";
            };

            // Act - First time, it should add the value
            var addedValue = await dictionary.AddOrUpdateAsync(1, addValueFactory, updateValueFactory);

            // Act - Second time, it should update the value
            var updatedValue = await dictionary.AddOrUpdateAsync(1, addValueFactory, updateValueFactory);

            // Assert
            addedValue.Should().Be("Added Value 1");
            updatedValue.Should().Be("Updated Value 1");
            dictionary[1].Value.Result.Should().Be("Updated Value 1");
        }

    }
}

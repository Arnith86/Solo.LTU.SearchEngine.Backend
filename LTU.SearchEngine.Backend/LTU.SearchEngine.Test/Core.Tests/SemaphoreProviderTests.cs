using LTU.SearchEngine.Backend.Core;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Collections.Concurrent;
using System.Runtime.ConstrainedExecution;

namespace LTU.SearchEngine.Test.Core.Tests;


public class SemaphoreProviderTests
{
	[Fact]
	public void GetOrAddSemaphore_SameKey_ReturnsSameInstance()
	{
		// Arrange
		var sut = new SemaphoreProvider();

		// Act
		var s1 = sut.GetOrAddSemaphore("example.com", 2);
		var s2 = sut.GetOrAddSemaphore("example.com", 2);

		// Assert
		Assert.Same(s1, s2); // same reference
	}

	[Fact]
	public void GetOrAddSemaphore_DifferentKeys_ReturnDifferentInstances()
	{
		// Arrange
		var provider = new SemaphoreProvider();

		// Act
		var s1 = provider.GetOrAddSemaphore("a.com", 2);
		var s2 = provider.GetOrAddSemaphore("b.com", 2);

		// Assert
		Assert.NotSame(s1, s2);
	}

	[Fact]
	public void GetOrAddSemaphore_UsesMaxConcurrencyOnFirstCreation()
	{
		// Arrange
		var provider = new SemaphoreProvider();

		// Act
		var semaphore = provider.GetOrAddSemaphore("example.com", 3);
		var semaphore2 = provider.GetOrAddSemaphore("example.com", 6);

		// Assert
		// SemaphoreSlim.CurrentCount == initial count
		Assert.Equal(3, semaphore2.CurrentCount);
	}

	[Fact]
	public async Task GetOrAddSemaphore_IsThreadSafe()
	{
		// Arrange
		var provider = new SemaphoreProvider();
		var results = new ConcurrentBag<SemaphoreSlim>();

		// Act - Create and runs parallel execution
		var tasks = Enumerable.Range(0, 50).Select(_ =>
			Task.Run(() =>
			{
				var s = provider.GetOrAddSemaphore("concurrent.com", 2);
				results.Add(s);
			})
		);

		await Task.WhenAll(tasks);

		// Assert
		// Even if multiple threads call GetOrAddSemaphore(...) at the same time,
		// the provider must create only one SemaphoreSlim instance per key.
		// For this tests, every single semaphore reference returned must be the same object in memory
		var first = results.First();
		Assert.All(results, s => Assert.Same(first, s));
	}

	[Fact]
	public async Task Semaphore_EnforcesConcurrencyLimit()
	{
		// Arrange
		var provider = new SemaphoreProvider();
		var semaphore = provider.GetOrAddSemaphore("limit.com", 2);

		int active = 0;
		int maxObserved = 0;

		// Only 2 tasks should be allowed inside the critical section at the same time.
		async Task Work()
		{
			// Ask if there are free semaphores available, if not wait until one is released.
			await semaphore.WaitAsync();
			try
			{
				/*	
				 *	Interlocked provides atomic operations.
				 *	That means:
				 * 	The operation happens as one indivisible CPU instruction. No other thread 
				 *	can interrupt it. No race condition is possible 
				*/
				var current = Interlocked.Increment(ref active);
				maxObserved = Math.Max(maxObserved, current);

				await Task.Delay(50); // simulate work
			}
			finally
			{
				Interlocked.Decrement(ref active);
				semaphore.Release();
			}
		}

		// Act
		var tasks = Enumerable.Range(0, 10).Select(_ => Work());
		await Task.WhenAll(tasks);

		// Assert
		Assert.Equal(2, maxObserved); // never exceeded maxConcurrency
	}
}

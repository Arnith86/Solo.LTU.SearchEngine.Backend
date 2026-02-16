using System.Collections.Concurrent;

namespace LTU.SearchEngine.Backend.Core;

/// <summary>
/// Provides domain-scoped <see cref="SemaphoreSlim"/> instances for controlling <br />
/// concurrent access to shared resources.
/// </summary>
/// <remarks>
/// <para>
/// This provider is responsible for creating and managing semaphores keyed by a string identifier <br />
/// (e.g. domain name, host, or resource group).
/// </para>
/// <para>
/// It enables fine-grained concurrency control such as limiting the number of concurrent crawl jobs <br />
/// per domain while still allowing high overall system parallelism.
/// </para>
/// <para>Semaphores are created lazily and stored in a thread-safe dictionary, ensuring that:</para>
/// <list type="bullet">
/// <item><description>Only one semaphore exists per key</description></item>
/// <item><description>Semaphore creation is thread-safe</description></item>
/// <item><description>Concurrency limits are enforced consistently across the system</description></item>
/// </list>
/// <para>
/// This class is typically registered as a singleton in the dependency injection container <br />
/// to provide global concurrency coordination.
/// </para>
/// </remarks>
public class SemaphoreProvider
{
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

	/// <summary>
	/// Gets an existing semaphore for the specified key, or creates a new one if it does not exist.
	/// </summary>
	/// <param name="key">
	/// A unique identifier for the concurrency scope (e.g. domain name or resource identifier).
	/// </param>
	/// <param name="maxConcurrency">
	/// The maximum number of concurrent operations allowed for this key. <br />
	/// This value is only used when creating a new semaphore.
	/// </param>
	/// <returns>A <see cref="SemaphoreSlim"/> instance associated with the specified key.</returns>
	/// <remarks>
	/// <para>
	/// If a semaphore already exists for the given key, the existing instance is returned <br />
	/// and the <paramref name="maxConcurrency"/> value is ignored.
	/// </para>
	/// <para>This method is thread-safe and may be called concurrently from multiple threads.</para>
	/// </remarks>
	public SemaphoreSlim GetOrAddSemaphore(string key, int maxConcurrency) => 
		_semaphores.GetOrAdd(key, _ => new SemaphoreSlim(maxConcurrency));
}

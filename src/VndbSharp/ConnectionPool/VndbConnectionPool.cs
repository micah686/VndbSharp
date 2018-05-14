using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

#if UserAuth
using System.Security;
#endif

namespace VndbSharp.ConnectionPool
{
	internal class VndbConnectionPool : IDisposable
	{
		private AsyncCollection<VndbConnection> _connectionPool;
		private Boolean _initialized = false;

		/// <summary>
		///	Exposed singleton of the <see cref="VndbConnectionPool"/> class.
		/// </summary>
		public static VndbConnectionPool Instance { get; }

		static VndbConnectionPool()
		{
			Instance = new VndbConnectionPool();
		}

		public Task<VndbConnection> GetConnectionAsync(CancellationToken ct = default(CancellationToken))
		{
			this.ThrowIfUninitialized();
			return this._connectionPool.TakeAsync(ct);
		}

		public void ReturnConnection(VndbConnection connection)
		{
			this._connectionPool.Add(connection);
		}

		// TODO: Find a better way to do this, maybe force a DiC Container?
		public void Initialize(Boolean useTls)
		{
			if (this._initialized)
				return;

			this._connectionPool = new AsyncCollection<VndbConnection>(new ConcurrentStack<VndbConnection>());
			for (var i = 0; i < 5; i++)
				this._connectionPool.Add(new VndbConnection(useTls));
			this._connectionPool.CompleteAdding();

			this._initialized = true;
		}

#if UserAuth
		// TODO: Find a better way to do this, maybe force a DiC Container?
		public void Initialize(String username, SecurePassword password)
		{
			if (this._initialized)
				return;

			this._connectionPool = new AsyncCollection<VndbConnection>(new ConcurrentStack<VndbConnection>());
			for (var i = 0; i < 5; i++)
				this._connectionPool.Add(new VndbConnection(username, password));
			this._connectionPool.CompleteAdding();

			this._initialized = true;
		}
#endif

		private void ThrowIfUninitialized()
		{
			if (!this._initialized)
				throw new InvalidOperationException($"{nameof(VndbConnectionPool)} has not been initialized.");
		}

		/// <summary>
		///		<para>Disposes <see cref="VndbConnectionPool"/> by disposing all of the internal <see cref="VndbConnection"/></para>
		/// </summary>
		public void Dispose()
		{
			foreach (var connection in this._connectionPool.GetConsumingEnumerable())
			{
				connection.Dispose();
			}
		}
	}
}

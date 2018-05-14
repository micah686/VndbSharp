using System;
using System.Threading.Tasks;
using OneOf;
using VndbSharp.ConnectionPool;
using VndbSharp.Interfaces;
using VndbSharp.Models.Common;

namespace VndbSharp
{
	public partial class Vndb
	{
		public async Task<OneOf<Boolean, IVndbError>> SetVoteListAsync(UInt32 id, Byte? vote)
			=> await this.SetInternalAsync(Constants.SetVotelistCommand, id, vote.HasValue ? new { vote } : null)
				.ConfigureAwait(false);

		public async Task<OneOf<Boolean, IVndbError>> SetVisualNovelListAsync(UInt32 id, Status? status)
			=> await this.SetInternalAsync(Constants.SetVisualNovelListCommand, id, status.HasValue ? new { status } : null)
				.ConfigureAwait(false);

		public async Task<OneOf<Boolean, IVndbError>> SetVisualNovelListAsync(UInt32 id, String notes)
			=> await this.SetInternalAsync(Constants.SetVisualNovelListCommand, id, new { notes }, true)
				.ConfigureAwait(false);

		public async Task<OneOf<Boolean, IVndbError>> SetVisualNovelListAsync(UInt32 id, Status? status, String notes)
			=> await this.SetInternalAsync(Constants.SetVisualNovelListCommand, id, status.HasValue ? new { status, notes } : null, true)
				.ConfigureAwait(false);

		public async Task<OneOf<Boolean, IVndbError>> SetWishlistAsync(UInt32 id, Priority? priority)
			=> await this.SetInternalAsync(Constants.SetWishlistCommand, id, priority.HasValue ? new { priority } : null)
				.ConfigureAwait(false);

		protected async Task<OneOf<Boolean, IVndbError>> SetInternalAsync<T>(String method, UInt32 id, T data,
			Boolean includeNull = false)
		{
			var connectionPool = VndbConnectionPool.Instance;
			connectionPool.Initialize(this._useTls);
			var connection = await connectionPool.GetConnectionAsync().ConfigureAwait(false);
			return await connection.SendSetRequestAsync(method, id, data, includeNull).ConfigureAwait(false);
		}
	}
}

using System;
using System.Threading.Tasks;
using VndbSharp.ConnectionPool;
using VndbSharp.Extensions;
using VndbSharp.Interfaces;
using VndbSharp.Models;
using VndbSharp.Models.Character;
using VndbSharp.Models.Errors;
using VndbSharp.Models.Producer;
using VndbSharp.Models.Release;
using VndbSharp.Models.Staff;
using VndbSharp.Models.User;
using VndbSharp.Models.VisualNovel;
using OneOf;

namespace VndbSharp
{
	public partial class Vndb
	{
		public async Task<OneOf<DatabaseStats, IVndbError>> GetDatabaseStatsAsync()
		{
			var connectionPool = VndbConnectionPool.Instance;
			connectionPool.Initialize(this._useTls);
			var connection = await connectionPool.GetConnectionAsync().ConfigureAwait(false);

			return await connection.SendGetRequestAsync<DatabaseStats, Object>(Constants.DbStatsCommand, null, false)
				.ConfigureAwait(false);
		}

		public async Task<OneOf<VndbResponse<VisualNovel>, IVndbError>> GetVisualNovelAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<VisualNovel>>(Constants.GetVisualNovelCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<Release>, IVndbError>> GetReleaseAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<Release>>(Constants.GetReleaseCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<Producer>, IVndbError>> GetProducerAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<Producer>>(Constants.GetProducerCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<Character>, IVndbError>> GetCharacterAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<Character>>(Constants.GetCharacterCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<Staff>, IVndbError>> GetStaffAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<Staff>>(Constants.GetStaffCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<User>, IVndbError>> GetUserAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<User>>(Constants.GetUserCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<VoteList>, IVndbError>> GetVoteListAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<VoteList>>(Constants.GetVotelistCommand, filters, flags, options)
				.ConfigureAwait(false);

		public async Task<OneOf<VndbResponse<VisualNovelList>, IVndbError>> GetVisualNovelListAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<VisualNovelList>>(Constants.GetVisualNovelListCommand, filters, flags, options)
				.ConfigureAwait(false);


		public async Task<OneOf<VndbResponse<Wishlist>, IVndbError>> GetWishlistAsync(IFilter filters, VndbFlags flags = VndbFlags.Basic,
			IRequestOptions options = null)
			=> await this.GetInternalAsync<VndbResponse<Wishlist>>(Constants.GetWishlistCommand, filters, flags, options)
				.ConfigureAwait(false);

		protected async Task<OneOf<T, IVndbError>> GetInternalAsync<T>(String method, IFilter filter, VndbFlags flags,
			IRequestOptions options = null)
			where T : class
		{
			// Need a way to communicate to the end user that these null values are not from the API?
			if (this.CheckFlags && !VndbUtils.ValidateFlagsByMethod(method, flags, out var invalidFlags))
			{
				this._invalidFlags?.Invoke(method, flags, invalidFlags);
				return new LibraryError("CheckFlags is enabled and VndbSharp detected invalid flags");
			}

			if (!filter.IsFilterValid())
				return new LibraryError("A filter was not considered valid.");

			var command = $"{method} {flags.AsString(method)} ({filter})";

			var connectionPool = VndbConnectionPool.Instance;
			connectionPool.Initialize(this._useTls);
			var connection = await connectionPool.GetConnectionAsync().ConfigureAwait(false);
			return await connection.SendGetRequestAsync<T, IRequestOptions>(command, options, false)
				.ConfigureAwait(false);
		}
	}
}

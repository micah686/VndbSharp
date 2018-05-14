using System;
#if UserAuth
using System.Security;
#endif
using System.Threading.Tasks;
using Newtonsoft.Json;
using VndbSharp.ConnectionPool;
using VndbSharp.Models;

namespace VndbSharp
{
	/// <summary>
	///		The main class to issue get and set commands to the Vndb API with
	/// </summary>
	public partial class Vndb
	{
		/// <summary>
		///		Creates a new instance of the Vndb class, to issue commands to the API
		/// </summary>
		/// <param name="useTls">Should the connection to the API be secure</param>
		public Vndb(Boolean useTls = false)
		{
			this.UseTls = useTls;
		}

#if UserAuth
		[Obsolete("SecureString is not secure on non-Windows OSes when using .Net Core, or at all in Mono.\n" +
				  "By Removing this attribute, you acknowledge the risks and will not make PRs or Issues " +
				  "regarding this unless the situation in .Net Core / Mono changes.", true)]
		public Vndb(String username, SecureString password)
		{
#warning SecureString is not secure on non-Windows OSes when using .Net Core, or at all in Mono. By removing the ObsoleteAttribute on this constructor, and/or this warning, you acknowledge the risks and will not make PRs or Issues regarding this unless the situation in .Net Core / Mono changes.
			// To read more above the above messages, check out https://github.com/Nikey646/VndbSharp/wiki/Mono-and-.Net-Core#securestring--username--password-logins
			// If that link is down, do some research on SecureString implementations in .Net Core, to see if they encrypt the data in memory on Unix.
			this.UseTls = true;
			this.Username = username;
			this.Password = password;
			this.Password.MakeReadOnly();
		}
#endif

		/// <summary>
		///		Issues the provided command to the Vndb API
		/// </summary>
		/// <param name="command">The command you want to issue</param>
		/// <returns>The raw result of the command unparsed, or the String representation of the exception that occured</returns>
		public async Task<String> DoRawAsync(String command)
		{
			try
			{
				VndbConnectionPool.Instance.Initialize(true);
				var connection = await VndbConnectionPool.Instance.GetConnectionAsync().ConfigureAwait(false);
				var isLoggedIn = await connection.LoginAsync().ConfigureAwait(false);
				if (isLoggedIn.IsT1)
					return JsonConvert.SerializeObject(isLoggedIn.AsT1);

				await connection.SendDataAsync(connection.FormatRequest(command)).ConfigureAwait(false);
				return await connection.ReceiveDataAsync().ConfigureAwait(false);
			}
			catch (Exception crap)
			{
				return crap.ToString();
			}
		}
		#region .  Fluent Client Settings  .

		/// <summary>
		///		A helper method to set the Client Name and Client Version sent to the Vndb Api
		/// </summary>
		/// <param name="clientName">The name of your client</param>
		/// <param name="clientVersion">The version of your client</param>
		/// <returns>The <see cref="Vndb"/> instance</returns>
		public Vndb WithClientDetails(String clientName, Version clientVersion)
			=> this.WithClientDetails(clientName, clientVersion.ToString());

		/// <summary>
		///		A helper method to set the Client Name and Client Version sent to the Vndb Api
		/// </summary>
		/// <param name="clientName">The name of your client</param>
		/// <param name="clientVersion">The version of your client. Valid values: a-z 0-9 _ . / -</param>
		/// <returns>The <see cref="Vndb"/> instance</returns>
		/// <exception cref="ArgumentOutOfRangeException">When <paramref name="clientVersion"/> is not a valid <see cref="Version"/></exception>
		public Vndb WithClientDetails(String clientName, String clientVersion)
		{
			VndbUtils.ClientName = clientName;
			VndbUtils.ClientVersion = clientVersion;
			return this;
		}

		/// <summary>
		///		Sets whether <see cref="VndbFlags"/> should be checked before being sent
		/// </summary>
		/// <param name="checkFlags">Should <see cref="VndbFlags"/> be checked before being sent</param>
		/// <returns>The <see cref="Vndb"/> instance</returns>
		public Vndb WithFlagsCheck(Boolean checkFlags)
			=> this.WithFlagsCheck(checkFlags, null);

		/// <summary>
		///		Sets whether <see cref="VndbFlags"/> should be checked before being sent, and provides a callback to retrieve the invalid flags
		/// </summary>
		/// <param name="checkFlags">Should <see cref="VndbFlags"/> be checked before being sent</param>
		/// <param name="invalidCallback">A callback with which the Method, Provided Flags, and Invalid Flags will be passed to when the Flags are Invalid</param>
		/// <returns>The <see cref="Vndb"/> instance</returns>
		public Vndb WithFlagsCheck(Boolean checkFlags, Action<String, VndbFlags, VndbFlags> invalidCallback)
		{
			this.CheckFlags = checkFlags;
			this._invalidFlags = invalidCallback;
			return this;
		}

		[Obsolete("Values are unused")]
		public Vndb WithBufferSize(Int32 both)
			=> this.WithBufferSize(both, both);

		[Obsolete("Values are unused")]
		public Vndb WithBufferSize(Int32 receive, Int32 send)
		{
//			this.ReceiveBufferSize = receive;
//			this.SendBufferSize = send;
			return this;
		}

		#endregion

		#region .  Public Properties  .

		/// <summary>
		///		Should the Connection to the Vndb API be done over a secure stream
		/// </summary>
		public Boolean UseTls
		{
			get => this._useTls;
			set
			{
				this._useTls = value;
				VndbConnectionPool.Instance.Dispose(); // Will renew the connections
			}
		}

		/// <summary>
		///		Sets whether <see cref="VndbFlags"/> should be checked before being sent
		/// </summary>
		public Boolean CheckFlags { get; set; } = true;

		#endregion

		#region .  Backing Fields  .

		private Boolean _useTls = false;

		private Action<String, VndbFlags, VndbFlags> _invalidFlags;

		#endregion
	}
}

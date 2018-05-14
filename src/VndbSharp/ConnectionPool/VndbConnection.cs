using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
#if UserAuth
using System.Security;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VndbSharp.Interfaces;
using OneOf;
using VndbSharp.Extensions;
using VndbSharp.Models;
using VndbSharp.Models.Errors;

namespace VndbSharp.ConnectionPool
{
	internal sealed class VndbConnection : IDisposable
	{
		private Boolean _loggedIn;
		private Stream _stream;
		private TcpClient _client;

#if UserAuth
		private String _username;
		private SecureString _password;
#endif

		private readonly Boolean _useTls;
		private Int32 _sendBufferSize = 4096; // Make optional?
		private Int32 _receiveBufferSize = 4096; // Make optional?

#if UserAuth
		/// <summary>
		///		Indicates if a User is Logged in or not
		/// </summary>
		public Boolean IsUserAuthenticated => this._password != null && this._loggedIn && this._stream != null;
#endif

		public VndbConnection(Boolean useTls)
		{
			this._useTls = useTls;
		}

#if UserAuth
		[Obsolete("SecureString is not secure on non-Windows OSes when using .Net Core, or at all in Mono.\n" +
				  "By Removing this attribute, you acknowledge the risks and will not make PRs or Issues " +
				  "regarding this unless the situation in .Net Core / Mono changes.", true)]
		public VndbConnection(String username, SecureString password)
		{
#warning SecureString is not secure on non-Windows OSes when using .Net Core, or at all in Mono. By removing the ObsoleteAttribute on this constructor, and/or this warning, you acknowledge the risks and will not make PRs or Issues regarding this unless the situation in .Net Core / Mono changes.
			this._useTls = true;
			this._username = username;
			this._password = password;
			this._password.MakeReadOnly();
		}
#endif

		public async Task<OneOf<TResult, IVndbError>> SendGetRequestAsync<TResult, TData>(String prefix, TData data = null, Boolean includeNull = true)
			where TResult : class
			where TData : class
		{
			try
			{
				var isLoggedIn = await this.LoginAsync().ConfigureAwait(false);
				if (isLoggedIn.IsT1)
					return OneOf<TResult, IVndbError>.FromT1(isLoggedIn.AsT1);

				var command = this.FormatCommand(prefix, data, includeNull);
				var requestData = this.FormatRequest(command);

				Debug.WriteLine($"Get Request | {command}");
				await this.SendDataAsync(requestData).ConfigureAwait(false);
				var response = await this.ReceiveDataAsync().ConfigureAwait(false);
				Debug.WriteLine($"Get Response | {response}");

				var results = response.ToVndbResults();
				if (results.Length == 2 &&
					(results[0] == Constants.Results || results[0] == Constants.DbStats))
					return results[1].FromJson<TResult>();

				if (results.Length != 2 || results[0] != Constants.Error)
					throw new UnexpectedResponseException(this.GetString(requestData), response);

				return OneOf<TResult, IVndbError>.FromT1(this.ParseError(results[1]));
			}
			finally
			{
				VndbConnectionPool.Instance.ReturnConnection(this);
			}
		}

		public async Task<OneOf<Boolean, IVndbError>> SendSetRequestAsync<T>(String method, UInt32 id, T data,
			Boolean includeNull = false)
		{
			try
			{
				var isLoggedIn = await this.LoginAsync().ConfigureAwait(false);
				if (isLoggedIn.IsT1)
					return OneOf<Boolean, IVndbError>.FromT1(isLoggedIn.AsT1);

				var command = this.FormatCommand($"{method} {id}", data, includeNull);
				var requestData = this.FormatRequest(command);

				Debug.WriteLine($"Set Request | {command}");
				await this.SendDataAsync(requestData).ConfigureAwait(false);
				var response = await this.ReceiveDataAsync().ConfigureAwait(false);
				Debug.WriteLine($"Set Response | {response}");

				var results = response.ToVndbResults();
				if (results.Length == 2 &&
				    (results[0] == Constants.Ok))
					return true;

				if (results.Length != 2 || results[0] != Constants.Error)
					throw new UnexpectedResponseException(this.GetString(requestData), response);

				return OneOf<Boolean, IVndbError>.FromT1(this.ParseError(results[1]));
			}
			finally
			{
				VndbConnectionPool.Instance.ReturnConnection(this);
			}
		}

		internal async Task<OneOf<Boolean, IVndbError>> LoginAsync()
		{
			if (this._client?.Connected == true && this._loggedIn)
				return true;

			this.InitializeClient();

			await this._client.ConnectAsync(Constants.ApiDomain, this._useTls ? Constants.ApiPortTls : Constants.ApiPort)
				.ConfigureAwait(false);

			if (this._useTls)
			{
				var stream = new SslStream(this._client.GetStream());
				await stream.AuthenticateAsClientAsync(Constants.ApiDomain).ConfigureAwait(false);
				this._stream = stream;
			}
			else
			{
				this._stream = this._client.GetStream();
			}

#if UserAuth
			var login = new Login(VndbUtils.ClientName, VndbUtils.ClientVersion, this._username, this._password);
#else
			var login = new Login(VndbUtils.ClientName, VndbUtils.ClientVersion);
#endif

			var command = this.FormatCommand(Constants.LoginCommand, login, false);
			var requestData = this.FormatRequest(command);

			await this.SendDataAsync(requestData).ConfigureAwait(false);
			var response = await this.ReceiveDataAsync().ConfigureAwait(false);

			if (response == Constants.Ok)
			{
				this._loggedIn = true;
				return true;
			}

			if (response.IsEmpty())
				throw new UnexpectedResponseException("login", response);

			var results = response.ToVndbResults();
			if (results.Length != 2 || results[0] != Constants.Error)
				throw new UnexpectedResponseException("login", response);

			return OneOf<Boolean, IVndbError>.FromT1(this.ParseError(results[1]));
		}

		internal async Task SendDataAsync(Byte[] data)
			=> await this._stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);

		internal async Task<String> ReceiveDataAsync()
		{
			var ms = new MemoryStream();
			var buffer = new Byte[this._receiveBufferSize];

			Int32 bytesRead;
			while ((bytesRead = await this._stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
			{
				await ms.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
				if (buffer[--bytesRead] == Constants.EotChar)
					break;
			}

			using (ms)
				return this.GetString(ms.ToArray()).TrimEnd(Constants.EotChar);
		}

		internal Byte[] FormatRequest(String data)
			=> this.GetBytes($"{data}{Constants.EotChar}");

		private void InitializeClient()
		{
			this.Dispose(true);

			this._client = new TcpClient
			{
				SendBufferSize = this._sendBufferSize,
				ReceiveBufferSize = this._receiveBufferSize,
			};

			this._loggedIn = false;
		}

		private String FormatCommand<T>(String prefix, T data, Boolean includeNull)
		{
			if (data == null && !includeNull)
				return prefix;

			var json = JsonConvert.SerializeObject(data,
				new JsonSerializerSettings
				{
					ContractResolver = VndbContractResolver.Instance,
					NullValueHandling = includeNull
						? NullValueHandling.Include
						: NullValueHandling.Ignore,
				});

			return json == "null" ? prefix : $"{prefix} {json}";
		}

		private Byte[] GetBytes(String data)
			=> Encoding.UTF8.GetBytes(data);

		private String GetString(Byte[] data)
			=> Encoding.UTF8.GetString(data);

		private IVndbError ParseError(String json)
		{
			Debug.WriteLine(json);

			var response = JObject.Parse(json);
			if (!response.TryGetValue("id", StringComparison.OrdinalIgnoreCase, out var typeToken))
				throw new UnexpectedResponseException("error parser", json);

			switch (typeToken.Value<String>())
			{
				case "parse":
					return Error.Build<ParseError>(response);
				case "missing":
					return Error.Build<MissingError>(response);
				case "badarg":
					return Error.Build<BadArgumentError>(response);
				case "needlogin":
					return Error.Build<LoginRequiredError>(response);
				case "throttled":
					return Error.Build<ThrottledError>(response);
				case "auth":
					return Error.Build<BadAuthenticationError>(response);
				case "loggedin":
					return Error.Build<LoggedInError>(response);
				case "gettype":
					return Error.Build<GetTypeError>(response);
				case "getinfo":
					return Error.Build<GetInfoError>(response);
				case "filter":
					return Error.Build<InvalidFilterError>(response);
				case "settype":
					return Error.Build<SetTypeError>(response);
				default:
					return null;
			}
		}

		#region .  IDisposable  .

		/// <summary>
		///		Disposes of the current instance
		/// </summary>
		public void Dispose()
			=> ((IDisposable)this).Dispose();

		/// <summary>
		///		Disposes of the current instance
		/// </summary>
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///		Disposes of all IDisposable properties
		/// </summary>
		private void Dispose(Boolean disposing)
		{
			if (!disposing)
				return;

#if netstandard1_3
			this._client?.Dispose();
#endif
			this._client = null;

			this._stream?.Dispose();
			this._stream = null;

#if UserAuth
			this.Password?.Dispose();
#endif
		}

		#endregion
	}
}
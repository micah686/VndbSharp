using System;
#if UserAuth
using System.Security;
#endif
using Newtonsoft.Json;

namespace VndbSharp.Models
{
	internal class Login
	{
		public Login(String clientName, String clientVersion)
		{
			this.ClientName = clientName;
			this.ClientVersion = clientVersion;
		}

#if UserAuth
		public Login(String clientName, String clientVersion, String username, SecureString password)
			: this()
		{
			this.Username = username;
			this.Password = password;
		}

		[JsonProperty("password")]
		public SecureString Password { get; set; }

		[JsonProperty("username")]
		public String Username { get; set; }
#endif

		[JsonProperty("protocol")]
		public UInt32 ProtocolVersion = 1;

		[JsonProperty("client")]
		public String ClientName;

		[JsonProperty("clientver")]
		public String ClientVersion;
	}
}

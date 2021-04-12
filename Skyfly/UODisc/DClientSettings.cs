using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc
{
	/// <summary>
	/// Settings for <see cref="DClient"/>
	/// </summary>
	public struct DClientSettings
	{
		/// <summary>
		/// Discord Token
		/// </summary>
		public string Token { get; set; }
		/// <summary>
		/// Discord Guild Id
		/// </summary>
		public ulong GuildId { get; set; }
		/// <summary>
		/// Discord Command Prefix
		/// </summary>
		public char CommandPrefix { get; set; }

		/// <summary>
		/// Settings for <see cref="DClient"/>
		/// </summary>
		/// <param name="token">Discord Token</param>
		/// <param name="guildId">Discord Guild Id</param>
		/// <param name="commandPrefix">Discord Command Prefix</param>
		public DClientSettings(string token, ulong guildId, char commandPrefix = '!') : this()
		{
			Token = token;
			GuildId = guildId;
			CommandPrefix = commandPrefix;
		}
	}
}

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
		/// Discord Command Channel Id
		/// </summary>
		public ulong CommandChannelId { get; set; }

		/// <summary>
		/// Discord Command Prefix
		/// </summary>
		public char CommandPrefix { get; set; }

		/// <summary>
		/// Discord Log Channel Id
		/// </summary>
		public ulong LogChannelId { get; set; }

		/// <summary>
		/// Forces the client to use a specific socket
		/// <para>0 - Auto-Detect</para>
		/// <para>1 - .Net</para>
		/// <para>2 - Mono</para>
		/// </summary>
		public int ForceSocket { get; set; }

		/// <summary>
		/// Settings for <see cref="DClient"/>
		/// </summary>
		/// <param name="token">Discord Token</param>
		/// <param name="guildId">Discord Guild Id</param>
		/// <param name="commandPrefix">Discord Command Prefix</param>
		/// <param name="forceSocket">
		/// Forces the client to use a specific socket
		/// <para>0 - Auto-Detect</para>
		/// <para>1 - .Net</para>
		/// <para>2 - Mono</para></param>
		public DClientSettings(string token, ulong guildId, ulong commandChannelId, ulong logChannelId, char commandPrefix = '!', int forceSocket = 0) : this()
		{
			Token = token;
			GuildId = guildId;
			CommandChannelId = commandChannelId;
			CommandPrefix = commandPrefix;
			LogChannelId = logChannelId;
			ForceSocket = forceSocket;
		}
	}
}

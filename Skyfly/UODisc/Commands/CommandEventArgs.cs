using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc.Commands
{
	public sealed class CommandEventArgs
	{
		/// <summary>
	 /// Null if private message
	 /// </summary>
		public DiscordGuild Guild { get; set; }

		public DiscordChannel Channel { get; set; }

		public DiscordUser User { get; set; }

		/// <summary>
		/// Null if private message
		/// </summary>
		public DiscordMember Member { get; set; }

		public DiscordMessage Message { get; set; }

		public AccessLevel AccessLevel { get; set; }

		/// <summary>
		/// Empty list if no parameters (never null)
		/// </summary>
		public List<string> Parameters { get; }

		/// <summary>
		/// <see cref="string.Empty"/> if no parameters
		/// </summary>
		public string ParameterString { get; set; }

		public CommandEventArgs(DiscordGuild guild, DiscordChannel channel, DiscordUser user,
							    DiscordMember member, DiscordMessage message,
							    AccessLevel accessLevel, List<string> parameters, string parameterString)
		{
			Guild = guild;
			Channel = channel;
			User = user;
			Member = member;
			Message = message;
			AccessLevel = accessLevel;
			Parameters = parameters;
			ParameterString = parameterString;
		}
	}
}

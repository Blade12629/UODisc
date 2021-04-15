using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Server.Custom.Skyfly.UODisc.Embeds;
using Server.Network;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class OnlinePlayersCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "OnlinePlayers";

		public AccessLevel AccessLevel => AccessLevel.Counselor;

		public CommandType CommandType => CommandType.None;

		public string Description => "Displays a list of players currently online";

		public string Usage => "{prefix}OnlinePlayers [page, default: 0]";

		public int MinParameters => 0;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			int page = 1;
			if (args.Parameters.Count > 0 && int.TryParse(args.Parameters[0], out int p))
				page = Math.Min(1, page);

			EmbedPageBuilder epb = new EmbedPageBuilder(2);
			NetState[] states = NetState.Instances.ToArray();

			if (states.Length == 0)
			{
				args.Channel.SendMessageAsync("No players currently online");
				return;
			}

			epb.AddColumn("Player");
			epb.AddColumn("Connected Since");

			for (int i = 0; i < states.Length; i++)
			{
				if (states[i].IsDisposing || !states[i].Running || states[i].Mobile == null ||
					states[i].Mobile.AccessLevel > args.AccessLevel)
					continue;

				epb.Add("Player", states[i].Mobile.Name ?? "Unkown");
				epb.Add("Connected Since", states[i].ConnectedFor.ToString());
			}

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
			{
				Title = "Online Players",
				Timestamp = DateTime.UtcNow
			};

			DiscordEmbed embed = epb.BuildPage(builder, page);
			args.Channel.SendMessageAsync(embed: embed);
		}
	}
}

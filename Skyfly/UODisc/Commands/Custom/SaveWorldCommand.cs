using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class SaveWorldCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "SaveWorld";

		public AccessLevel AccessLevel => AccessLevel.GameMaster;

		public CommandType CommandType => CommandType.None;

		public string Description => "Saves the world";

		public string Usage => "{prefix}SaveWorld";

		public int MinParameters => 0;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			DClient.DiscordLog($"{args.User.Username} started a world save", LogLevel.Info);

			args.Channel.SendEmbedMessage("Saving the world...").ConfigureAwait(false).GetAwaiter().GetResult();
			World.Save();
			args.Channel.SendEmbedMessage("World was saved...").ConfigureAwait(false);
		}
	}
}

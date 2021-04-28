using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Network;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class BanPlayerCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "BanPlayer";

		public AccessLevel AccessLevel => AccessLevel.GameMaster;

		public CommandType CommandType => CommandType.None;

		public string Description => "Bans a specific player";

		public string Usage => "{prefix}BanPlayer <player> [reason]";

		public int MinParameters => 1;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			NetState[] states = NetState.Instances.ToArray();

			for (int i = 0; i < states.Length; i++)
			{
				if (states[i].IsDisposing || !states[i].Running || states[i].Mobile == null ||
					!states[i].Mobile.Name.Equals(args.Parameters[0], StringComparison.CurrentCultureIgnoreCase))
					continue;

				string reason = args.Parameters.Count > 1 ? args.Parameters[1] : "unspecified";

				states[i].Mobile.SendMessage($"You have been banned for: {reason}");
				states[i].Mobile.Account.Banned = true;
				states[i].Dispose();

				DClient.DiscordLog($"{args.User.Username} banned player {args.Parameters[0]} for reason: {reason}", LogLevel.Info);
				args.Channel.SendEmbedMessage($"Banned player {args.Parameters[0]} for reason: {reason}").ConfigureAwait(false);
				return;
			}

			args.Channel.SendEmbedMessage($"Player {args.Parameters[0]} not found").ConfigureAwait(false);
		}
	}
}

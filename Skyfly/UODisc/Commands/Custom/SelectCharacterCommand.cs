using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class SelectCharacterCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "SelectCharacter";

		public AccessLevel AccessLevel => AccessLevel.Player;

		public CommandType CommandType => CommandType.None;

		public string Description => "Selects as which character you want to be shown";

		public string Usage => "{prefix}SelectCharacter <player>";

		public int MinParameters => 1;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			DiscordUserLink dul = DClient.UserManager[args.User.Id];

			if (dul == null || dul.Accounts == null || dul.Accounts.Length == 0)
			{
				args.Channel.SendEmbedMessage("You need to link your account first").ConfigureAwait(false);
				return;
			}

			for (int i = 0; i < dul.Accounts.Length; i++)
			{
				var acc = dul.Accounts[i];

				for (int x = 0; x < acc.Length; x++)
				{
					Mobile m = acc[x];

					if (m.Name.Equals(args.Parameters[0]))
					{
						dul.SelectedCharacter = m;
						DClient.UserManager.AddOrUpdate(dul);
						args.Channel.SendEmbedMessage("Selected your character").ConfigureAwait(false);
						return;
					}
				}
			}

			args.Channel.SendEmbedMessage("Character not found!").ConfigureAwait(false);
		}
	}
}

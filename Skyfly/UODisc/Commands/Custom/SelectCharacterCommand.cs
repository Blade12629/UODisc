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
				args.Channel.SendMessageAsync("You need to link your account first");
				return;
			}

			for (int i = 0; i < World.Mobiles.Count; i++)
			{
				Mobile m = World.Mobiles.ElementAt(i).Value;

				if (!m.Player || !m.Name.Equals(args.Parameters[0]))
					continue;

				bool endAndContinue = false;
				for (int x = 0; x < dul.Accounts.Length; x++)
				{
					if (m.Account.Username.Equals(dul.Accounts[x].Username))
					{
						endAndContinue = true;
						break;
					}
				}

				if (endAndContinue)
					continue;

				dul.SelectedCharacter = m;
				DClient.UserManager.AddOrUpdate(dul);
				args.Channel.SendMessageAsync("Selected your character");
				return;
			}

			args.Channel.SendMessageAsync("Character not found!");
		}
	}
}

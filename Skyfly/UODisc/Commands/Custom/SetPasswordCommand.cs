using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class SetPasswordCommand : ICommand
	{
		public bool IsDisabled { get; set; } = true;

		public string Command => "SetPassword";

		public AccessLevel AccessLevel => AccessLevel.Player;

		public CommandType CommandType => CommandType.Private;

		public string Description => "Sets your password";

		public string Usage => "{prefix}SetPassword <password>\n{prefix}SetPassword <account> <password>";

		public int MinParameters => 1;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			const int PASSWORD_MIN_LENGTH = 7;

			DiscordUserLink dul = DClient.UserManager[args.User.Id];

			if (dul == null || dul.Accounts == null || dul.Accounts.Length == 0)
			{
				args.Channel.SendEmbedMessage("No account found (Did you link your account?)").ConfigureAwait(false);
				return;
			}

			if (args.Parameters.Count >= 2)
			{
				if (args.Parameters[1].Length < PASSWORD_MIN_LENGTH)
				{
					args.Channel.SendEmbedMessage($"Your password needs to be atleast {PASSWORD_MIN_LENGTH} characters long").ConfigureAwait(false);
					return;
				}

				for (int i = 0; i < dul.Accounts.Length; i++)
				{
					if (dul.Accounts[i].Username.Equals(args.Parameters[0], StringComparison.CurrentCultureIgnoreCase))
					{
						dul.Accounts[i].SetPassword(args.Parameters[1]);
						args.Channel.SendEmbedMessage("Successfully changed your password").ConfigureAwait(false);
						return;
					}
				}

				args.Channel.SendEmbedMessage($"Account {args.Parameters[0]} not found").ConfigureAwait(false);
			}
			else
			{
				if (args.Parameters[0].Length < PASSWORD_MIN_LENGTH)
				{
					args.Channel.SendEmbedMessage($"Your password needs to be atleast {PASSWORD_MIN_LENGTH} characters long").ConfigureAwait(false);
					return;
				}

				if (dul.Accounts.Length > 1)
				{
					HelpCommand.ShowHelp(args.Channel, this, "Multiple accounts found, please specify one");
					return;
				}

				dul.Accounts[0].SetPassword(args.Parameters[0]);
				args.Channel.SendEmbedMessage("Successfully changed your password").ConfigureAwait(false);
			}
		}
	}
}

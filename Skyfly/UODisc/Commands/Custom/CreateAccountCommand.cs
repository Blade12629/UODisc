using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Accounting;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class CreateAccountCommand : ICommand
	{
		public bool IsDisabled { get; set; } = true;

		public string Command => "CreateAccount";

		public AccessLevel AccessLevel => AccessLevel.Player;

		public CommandType CommandType => CommandType.Private;

		public string Description => "Creates a new account";

		public string Usage => "{prefix}CreateAccount <username> <password>";

		public int MinParameters => 2;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			DiscordUserLink dul = DClient.UserManager[args.User.Id];

			if (dul != null && dul.Accounts != null && dul.Accounts.Length >= DClient.UserManager.AccountsPerIp)
			{
				args.Channel.SendEmbedMessage("You can't create more accounts").ConfigureAwait(false);
				return;
			}
			else if (dul == null)
				dul = new DiscordUserLink(args.User.Id);

			Account acc = new Account(args.Parameters[0], args.Parameters[1]);
			dul.AddAccount(acc);
			DClient.UserManager.AddOrUpdate(dul);

			args.Channel.SendEmbedMessage($"Created account:\n```\n{args.Parameters[0]}\n{args.Parameters[1]}\n```").ConfigureAwait(false);
			DClient.DiscordLog($"New account created: {args.Parameters[0]}", LogLevel.Info);
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Commands;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class LinkAccountCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "LinkAccount";

		public AccessLevel AccessLevel => AccessLevel.Player;

		public CommandType CommandType => CommandType.None;

		public string Description => "Links the current discord account with your ingame account";

		public string Usage => "{prefix}LinkAccount";

		public int MinParameters => 0;

		readonly char[] _possibleCodeChars;

		//Verification codes are cleared everytime the server restarts
		//this is on purpose so we don't store codes for too long
		readonly ConcurrentDictionary<string, ulong> _verificationCodes;
		readonly ConcurrentDictionary<ulong, string> _verificationUsers;

		public LinkAccountCommand()
		{
			_possibleCodeChars = new char[]
			{
				'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
				'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E',
				'f', 'F', 'g', 'G', 'h', 'H', 'i', 'I', 'j', 'J',
				'k', 'K', 'l', 'L', 'm', 'M', 'n', 'N', 'o', 'O',
				'p', 'P', 'q', 'Q', 'r', 'R', 's', 'S', 't', 'T',
				'u', 'U', 'v', 'V', 'w', 'W', 'x', 'X', 'y', 'Y',
			};

			_verificationCodes = new ConcurrentDictionary<string, ulong>();
			_verificationUsers = new ConcurrentDictionary<ulong, string>();

			CommandSystem.Register("linkaccount", AccessLevel.Player, new CommandEventHandler(LinkAccountIngame));
		}

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			if (DClient.UserManager[args.User.Id] != null)
			{
				args.Channel.SendEmbedMessage("You already linked your account!").ConfigureAwait(false);
				return;
			}

			var dmChannel = DClient.GetDmChanelAsync(args.User.Id).ConfigureAwait(false).GetAwaiter().GetResult();

			if (_verificationUsers.TryGetValue(args.User.Id, out string verCode))
			{
				if (dmChannel == null)
				{
					args.Channel.SendEmbedMessage("Verification code already created").ConfigureAwait(false);
					return;
				}

				dmChannel.SendEmbedMessage($"Verification code already created: {verCode}").ConfigureAwait(false);
				return;
			}

			if (dmChannel == null)
			{
				args.Channel.SendEmbedMessage("Unable to open direct message channel").ConfigureAwait(false);
				return;
			}

			verCode = GenerateCode(Utility.Random(6, 4));
			_verificationCodes.TryAdd(verCode, args.User.Id);
			_verificationUsers.TryAdd(args.User.Id, verCode);

			dmChannel.SendEmbedMessage($"Verification code created, login onto any of your characters (in UO) and type the following (Code is Case-Sensitive): [linkaccount {verCode}").ConfigureAwait(false);

			if (args.Guild != null)
				args.Message.DeleteAsync().ConfigureAwait(false);
		}

		void LinkAccountIngame(Server.Commands.CommandEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(e.ArgString) || !_verificationCodes.TryGetValue(e.ArgString, out ulong userId))
			{
				e.Mobile.SendMessage($"Verification code {e.ArgString} not found!");
				return;
			}

			Accounting.Account acc = e.Mobile.Account as Accounting.Account;
			DiscordUserLink dul = DClient.UserManager[acc];

			if (dul != null && dul.DiscordUserId == 0)
			{
				dul.DiscordUserId = userId;
			}
			else if (dul == null)
			{
				dul = new DiscordUserLink(acc, userId);
			}

			DClient.UserManager.AddOrUpdate(dul);
			e.Mobile.SendMessage("You successfully linked your account to user discord account");
		}

		string GenerateCode(int length)
		{
			StringBuilder sb = new StringBuilder(length);
			string code;

			do
			{
				for (int i = 0; i < length; i++)
					sb.Append(_possibleCodeChars[Utility.Random(0, _possibleCodeChars.Length - 1)]);

				code = sb.ToString();
			}
			while (_verificationCodes.ContainsKey(code));
			
			return sb.ToString();
		}
	}
}

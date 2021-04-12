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
		ConcurrentDictionary<string, ulong> _verificationCodes;
		ConcurrentDictionary<ulong, string> _verificationUsers;

		readonly string _saveFile = System.IO.Path.Combine(DClient.SaveFolder, "Ver.bin");

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

			CommandSystem.Register("linkaccount", AccessLevel.Player, new CommandEventHandler(LinkAccountIngame));
		}

		public void Invoke(DClient client, CommandHandler handler, CommandEventArgs args)
		{
			if (DClient.Instance.UserManager[args.Member.Id] != null)
			{
				args.Channel.SendMessageAsync("You already linked your account!");
				return;
			}

			var dmChannel = client.GetDmChanelAsync(args.User.Id).ConfigureAwait(false).GetAwaiter().GetResult();

			if (_verificationUsers.TryGetValue(args.User.Id, out string verCode))
			{
				if (dmChannel == null)
				{
					args.Channel.SendMessageAsync("Verification code already created");
					return;
				}

				dmChannel.SendMessageAsync($"Verification code already created: {verCode}");
				return;
			}

			if (dmChannel == null)
			{
				args.Channel.SendMessageAsync("Unable to open direct message channel");
				return;
			}

			verCode = GenerateCode(Utility.Random(10));
			_verificationCodes.TryAdd(verCode, args.User.Id);
			_verificationUsers.TryAdd(args.User.Id, verCode);

			dmChannel.SendMessageAsync($"Verification code created, login onto any of your characters (in UO) and type the following (Code is Case-Sensitive): [linkaccount {verCode}");
		}

		void LinkAccountIngame(Server.Commands.CommandEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(e.ArgString) || !_verificationCodes.TryGetValue(e.ArgString, out ulong userId))
			{
				e.Mobile.SendMessage($"Verification code {e.ArgString} not found!");
				return;
			}

			Accounting.Account acc = e.Mobile.Account as Accounting.Account;
			DiscordUserLink dul = DClient.Instance.UserManager[acc];

			if (dul != null && dul.DiscordUserId == 0)
			{
				dul.DiscordUserId = userId;
			}
			else if (dul == null)
			{
				dul = new DiscordUserLink(acc, userId);
			}

			DClient.Instance.UserManager.AddOrUpdate(dul);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc.Commands.Custom
{
	[Command]
	public class WorldMessageCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "WorldMessage";

		public AccessLevel AccessLevel => AccessLevel.GameMaster;

		public CommandType CommandType => CommandType.None;

		public string Description => "Sends a world message";

		public string Usage => "{prefix}WorldMessage <hue> <hideUsername> <world message>";

		public int MinParameters => 3;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			if (!int.TryParse(args.Parameters[0], out int hue))
			{
				HelpCommand.ShowHelp(args.Channel, this, "Unable to parse hue");
				return;
			}
			if (!bool.TryParse(args.Parameters[1], out bool hideUsername))
			{
				HelpCommand.ShowHelp(args.Channel, this, "Unable to parse hideUsername (true/false)");
				return;
			}

			string msg = args.ParameterString.Remove(0, args.Parameters[0].Length + args.Parameters[1].Length + 2);

			if (!hideUsername)
				msg = $"{args.User.Username}: " + msg;

			World.Broadcast(hue, false, msg);
			args.Channel.SendMessageAsync("Sent message");
		}
	}
}

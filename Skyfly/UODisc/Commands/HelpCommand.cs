using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Server.Custom.Skyfly.UODisc.Embeds;

namespace Server.Custom.Skyfly.UODisc.Commands
{
	[Command]
	public class HelpCommand : ICommand
	{
		public bool IsDisabled { get; set; }

		public string Command => "help";

		public AccessLevel AccessLevel => AccessLevel.Player;
		public CommandType CommandType => CommandType.None;

		public string Description => "Displays a command list or infos about a specific command";

		public string Usage => "{prefix}help [page]\n" +
							   "{prefix}help <command>";

		public int MinParameters => 0;
		public bool AllowOverwritingAccessLevel => false;

		public void Invoke(CommandHandler handler, CommandEventArgs args)
		{
			char prefix = DClient.Settings.CommandPrefix;

			int page = 1;
			if (args.Parameters.Count > 0)
			{
				if (char.IsDigit(args.Parameters[0][0]))
				{
					if (int.TryParse(args.Parameters[0], out int page_))
					{
						args.Parameters.RemoveAt(0);
						page = page_;
					}
				}

				if (args.Parameters.Count > 0 && ShowHelp(handler, args, prefix))
					return;
			}

			ListCommands(handler, args, prefix, page);
		}

		private void ListCommands(CommandHandler handler, CommandEventArgs args, char prefix, int page = 1)
		{
			List<(ICommand, AccessLevel)> commands = handler.Commands.Values.Select(s => (s, s.AccessLevel)).ToList();

			for (int i = 0; i < commands.Count; i++)
			{
				AccessLevel newAccess = commands[i].Item2;

				if (newAccess > args.AccessLevel)
				{
					commands.RemoveAt(i);
					i--;
					continue;
				}

				commands[i] = (commands[i].Item1, newAccess);
			}

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder
			{
				Title = "Command List",
				Description = "Prefix: " + prefix,
				Timestamp = DateTime.UtcNow
			};

			EmbedPageBuilder epb = new EmbedPageBuilder(3);
			epb.AddColumn("Command");
			epb.AddColumn("Access");
			epb.AddColumn("Description");

			for (int i = 0; i < commands.Count; i++)
			{
				epb.Add("Command", commands[i].Item1.Command);
				epb.Add("Access", commands[i].Item2.ToString());
				epb.Add("Description", commands[i].Item1.Description);
			}

			DiscordEmbed embed = epb.BuildPage(builder, page);

			args.Channel.SendMessageAsync(embed: embed).Wait();
		}


		private static bool ShowHelp(CommandHandler handler, CommandEventArgs args, char prefix, string notice = null)
		{
			string command = args.Parameters[0].Trim('!');

			if (handler.Commands.TryGetValue(command, out ICommand cmd))
			{
				ShowHelp(args.Channel, cmd, prefix, notice);
				return true;
			}

			return false;
		}

		public static void ShowHelp(DiscordChannel channel, ICommand command, string notice = null)
		{
			ShowHelp(channel, command, DClient.Settings.CommandPrefix, notice);
		}

		public static void ShowHelp(DiscordChannel channel, ICommand command, char prefix, string notice = null)
		{
			DiscordEmbedBuilder builder = new DiscordEmbedBuilder
			{
				Title = $"Command Info: {command.Command}",
				Timestamp = DateTime.UtcNow,
				Footer = new DiscordEmbedBuilder.EmbedFooter
				{
					Text = "< > = required\n" +
							"[ ] = optional\n" +
							"/ = choose between\n" +
							"Text with spaces needs to be inside: \"\""
				}
			};

			if (!string.IsNullOrEmpty(notice))
			{
				if (notice.Length > 1000)
					notice = notice.Substring(0, 1000);

				builder = builder.AddField($"**Notice**", notice);
			}

			builder = builder.AddField("Access Level", command.AccessLevel.ToString())
							 .AddField("Description", command.Description)
							 .AddField("Usage", command.Usage.Replace("{prefix}", prefix.ToString()))
							 .AddField("Type", command.CommandType.ToString())
							 .AddField("Is Disabled", command.IsDisabled ? "True" : "False");


			channel.SendMessageAsync(embed: builder.Build()).Wait();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace Server.Custom.Skyfly.UODisc.Commands
{
	public sealed class CommandHandler
	{
		public char CommandPrefix { get; set; }
		public bool Debug { get; set; }
		public IReadOnlyDictionary<string, ICommand> Commands => _commands;

		Dictionary<string, ICommand> _commands;

		public CommandHandler(char commandPrefix)
		{
#if DEBUG
			Debug = true;
#endif

			if (!Debug && Core.Debug)
				Debug = true;

			_commands = new Dictionary<string, ICommand>();
			CommandPrefix = commandPrefix;
		}

		public ICommand this[string cmd]
		{
			get
			{
				_commands.TryGetValue(cmd.ToLower(), out ICommand icmd);

				return icmd;
			}
		}

		public void InitCommands()
		{
			if (_commands.Count > 0)
				_commands.Clear();

			Assembly ass = typeof(CommandHandler).Assembly;
			Type[] types = ass.GetTypes();

			for (int i = 0; i < types.Length; i++)
			{
				if (types[i].GetCustomAttribute<CommandAttribute>() == null)
					continue;

				ICommand cmd;
				try
				{
					cmd = Activator.CreateInstance(types[i]) as ICommand;
				}
				catch (Exception ex)
				{
					DClient.Error("Command Initialization", ex.ToString());
					continue;
				}

				if (cmd == null)
					continue;

				RegisterCommand(cmd);
			}

			DClient.WriteLine($"Registered a total of {_commands.Count} commands");
		}

		public void Invoke(MessageCreateEventArgs e)
		{
			try
			{
				if (e == null ||
					!e.Message.Content[0].Equals(CommandPrefix))
					return;

				List<string> parameters = Split(e.Message.Content);

				if (parameters == null)
					parameters = new List<string>();

				string command;
				if (parameters.Count == 0)
					command = e.Message.Content;
				else
					command = parameters[0];

				command = command.TrimStart(CommandPrefix);

				AccessLevel access = DClient.UserManager.GetAccessLevel(e.Author.Id);

				//Return when command channel is set, we are outside of it and have accesslevel vip or lower
				//Return when we cannot find the command itself
				//Return when the command is disabled
				if ((e.Guild != null && access <= AccessLevel.VIP && DClient.Settings.CommandChannelId != 0 && DClient.Settings.CommandChannelId != e.Channel.Id) ||
					!_commands.TryGetValue(command.ToLower(), out ICommand cmd))
					return;
				else if (cmd.IsDisabled)
				{
					e.Channel.SendMessageAsync("Command is currently disabled");
					return;
				}

				switch (cmd.CommandType)
				{
					default:
					case CommandType.None:
						break;

					case CommandType.Private:
						if (e.Guild != null)
						{
							e.Channel.SendMessageAsync("You can only use this command in a private chat!");
							return;
						}
						break;

					case CommandType.Public:
						if (e.Guild == null)
						{
							e.Channel.SendMessageAsync("You can only use this command in a server chat!");
							return;
						}
						break;
				}

				if (access < cmd.AccessLevel)
				{
					e.Channel.SendMessageAsync("You do not have enough permissions to use this command");
					return;
				}

				if (parameters.Count > 0)
					parameters.RemoveAt(0);

				if (cmd.MinParameters > 0 && parameters.Count < cmd.MinParameters)
				{
					e.Channel.SendMessageAsync("Not enough parameters");
					return;
				}

				string afterCmd = e.Message.Content;

				if (afterCmd.Length > cmd.Command.Length + 1)
					afterCmd = afterCmd.Remove(0, cmd.Command.Length + 2);
				else
					afterCmd = string.Empty;

				DiscordMember member = null;
				if (e.Guild != null)
				{
					try
					{
						member = e.Guild.GetMemberAsync(e.Author.Id).ConfigureAwait(false).GetAwaiter().GetResult();
					}
					catch (AggregateException ex)
					{
						if (!ex.InnerExceptions.Any(t => t is NotFoundException))
							throw;
					}
				}

				CommandEventArgs arg = new CommandEventArgs(e.Guild, e.Channel, e.Author, member,
															e.Message, access, parameters, afterCmd);

				ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
				{
					try
					{
						cmd.Invoke(this, arg);
					}
					catch (Exception ex)
					{
						if (ex is UnauthorizedException)
						{
							e.Channel.SendMessageAsync("Internal Error: Unauthorized (This error can happen when the bot tries to send a message to someone via DM but the user has disabled DMs from non friends)");
							return;
						}

						if (Debug)
							e.Channel.SendMessageAsync(GetDebugExceptionMessage(ex));
						else
							e.Channel.SendMessageAsync($"Something went wrong executing this command");
					}
				}));

			}
			catch (Exception ex)
			{
				DClient.Error(ex);
			}

			string GetDebugExceptionMessage(Exception ex)
			{
				return $"Something went wrong executing this command (L: {GetLineNumber(ex)} At: {(ex.TargetSite.DeclaringType?.FullName ?? "unkown")}.{ex.TargetSite.Name}: {ex.Message})";
			}
		}

		List<string> Split(string content)
		{
			List<string> result = new List<string>();

			if (string.IsNullOrWhiteSpace(content))
				return result;

			StringBuilder sb = new StringBuilder(content);

			//remove command prefix
			if (sb.Length > 0)
				sb.Remove(0, 1);

			//Todo make converting parameters to their respective type easier
			//Todo create chat listener to allow synchronizing game and discord chat
			StringBuilder rb = new StringBuilder(content.Length);

			bool isString = false;
			bool lastWasEscapeChar = false;
			while (sb.Length > 0)
			{
				char c = sb[0];
				sb.Remove(0, 1);

				switch (c)
				{
					case '"':
						if (lastWasEscapeChar)
						{
							lastWasEscapeChar = false;
							break;
						}

						if (!isString)
						{
							isString = true;
							continue;
						}
						else
						{
							isString = false;
							result.Add(rb.ToString());
							rb.Clear();
							continue;
						}

					case ' ':
						lastWasEscapeChar = false;
						result.Add(rb.ToString());
						rb.Clear();
						continue;

					case '/':
						lastWasEscapeChar = true;
						break;
				}

				rb.Append(c);
			}

			if (rb.Length > 0)
				result.Add(rb.ToString());

			return result;
		}

		void RegisterCommand(ICommand cmd)
		{
			string cmdName = cmd.Command.ToLower();

			if (_commands.ContainsKey(cmdName))
			{
				DClient.WriteLine($"Discord: Command {cmdName} already exists, skipping...");
				return;
			}

			for (int i = 0; i < cmdName.Length; i++)
			{
				if (cmdName[i] == ' ')
				{
					DClient.WriteLine($"Discord: Command {cmdName} contains illegal character \" \", skipping...");
					return;
				}
				else if (!char.IsLetterOrDigit(cmdName[i]))
				{
					DClient.WriteLine($"Discord: Command {cmdName} contains illegal character \"{cmdName[i]}\", skipping...");
					return;
				}
			}

			_commands.Add(cmdName, cmd);
			DClient.WriteLine($"Discord: Registered command {cmdName}");
		}

		static int GetLineNumber(Exception ex)
		{
			const string lineSearch = ":line ";

			if (ex == null || string.IsNullOrEmpty(ex.StackTrace))
				return -1;

			int indexStart = ex.StackTrace.IndexOf(lineSearch, StringComparison.CurrentCultureIgnoreCase);

			if (indexStart == -1)
				return -1;

			string stack = ex.StackTrace.Remove(0, indexStart + lineSearch.Length);

			int indexEnd = stack.IndexOf("\r\n", StringComparison.CurrentCultureIgnoreCase);

			if (indexEnd > 0)
				stack = stack.Substring(0, indexEnd);

			stack = stack.Trim(' ');

			if (int.TryParse(stack, out int lineNumber))
				return lineNumber;

			return -1;
		}
	}
}

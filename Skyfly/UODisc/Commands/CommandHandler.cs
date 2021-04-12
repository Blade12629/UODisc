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
				catch (Exception)
				{
					continue;
				}

				if (cmd == null)
					continue;

				RegisterCommand(cmd);
			}
		}

		public void Invoke(MessageCreateEventArgs e)
		{
			try
			{
				if (e == null ||
					!e.Message.Content[0].Equals(CommandPrefix))
					return;

				List<string> parameters = e.Message.Content.Split(' ').Skip(0).ToList();

				if (parameters == null)
					parameters = new List<string>();

				string command;
				if (parameters.Count == 0)
					command = e.Message.Content;
				else
					command = parameters[0];

				command = command.TrimStart(CommandPrefix);

				AccessLevel access = DClient.Instance.UserManager.GetAccessLevel(e.Author.Id);

				if (!_commands.TryGetValue(command.ToLower(), out ICommand cmd))
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
						cmd.Invoke(DClient.Instance, this, arg);
					}
#pragma warning disable CA1031 // Do not catch general exception types
					catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
					{
						if (ex is UnauthorizedException)
						{
							e.Channel.SendMessageAsync("Internal Error: Unauthorized");
							return;
						}

						if (Debug)
							e.Channel.SendMessageAsync(GetDebugExceptionMessage(ex));
						else
							e.Channel.SendMessageAsync($"Something went wrong executing this command");
					}
				}));

			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine(ex);
				Utility.PopColor();
			}

			string GetDebugExceptionMessage(Exception ex)
			{
				return $"Something went wrong executing this command (L: {GetLineNumber(ex)} At: {ex.TargetSite.DeclaringType}.{ex.TargetSite.Name}: {ex.Message})";
			}
		}

		void RegisterCommand(ICommand cmd)
		{
			string cmdName = cmd.Command.ToLower();

			if (_commands.ContainsKey(cmdName))
				return;

			_commands.Add(cmdName, cmd);

			Utility.PushColor(ConsoleColor.Cyan);
			Console.WriteLine("Registered command " + cmdName);
			Utility.PopColor();
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

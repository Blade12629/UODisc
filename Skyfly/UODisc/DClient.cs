using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Server.Custom.Skyfly.UODisc.Commands;
using Server.Custom.Skyfly.UODisc.Embeds;

namespace Server.Custom.Skyfly.UODisc
{
	public static class DClient
	{
		public static event Action<MessageCreateEventArgs> OnMessageRecieved;
		public static event Action<MessageReactionAddEventArgs> OnMessageReactionAdded;
		public static event Action<MessageReactionRemoveEventArgs> OnMessageReactionRemoved;
		public static event Action<MessageReactionsClearEventArgs> OnMessageReactionCleared;

		public static string SaveFolder => System.IO.Path.Combine(Environment.CurrentDirectory, "Saves", "Discord");
		public static char InvisibleChar => '‎';
		public static string InvisibleCharStr => "‎";

		public static DClientSettings Settings { get; private set; }
		public static bool IsReady { get; private set; }
		public static DiscordUserManager UserManager => _userMgr;
		public static CommandHandler CommandHandler => _cmdHandler;
		public static bool IsDisabled { get; private set; }

		static readonly string _saveFile = System.IO.Path.Combine(SaveFolder, "Client.bin");

		static DiscordClient _dclient;
		static DiscordUserManager _userMgr;
		static CommandHandler _cmdHandler;
		static ILogger _discordLogger;

		public static void Initialize()
		{
			var entry = Config.Find("Discord.Token");

			if (entry == null)
			{
				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine("Discord: Config not found, autogenerating...");
				Utility.PopColor();

				Config.Set("Discord.Token", "Your Discord Bot Token");
				Config.Set("Discord.GuildId", 0ul);
				Config.Set("Discord.CommandPrefix", "!");
				Config.Set("Discord.CommandChannelId", 0ul);
				Config.Set("Discord.ForceSocket", 0);

				Config.Save();

				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine("Discord: Config generated, disabling discord for this run...");
				Utility.PopColor();

				IsDisabled = true;
				return;
			}

			string token = Config.Get("Discord.Token", string.Empty);

			if (string.IsNullOrEmpty(token) || token.Equals("Your Discord Bot Token"))
			{
				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine("Discord: Invalid token, disabling discord...");
				Utility.PopColor();

				IsDisabled = true;
				return;
			}

			ulong guildId = Config.Get("Discord.GuildId", 0ul);

			if (guildId == 0)
			{
				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine("Discord: Invalid guild id, disabling discord...");
				Utility.PopColor();

				IsDisabled = true;
				return;
			}

			char cmdPrefix = Config.Get("Discord.CommandPrefix", "!")[0];

			if (cmdPrefix == char.MinValue || cmdPrefix == char.MaxValue || char.IsLetterOrDigit(cmdPrefix))
			{
				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine("Discord: Invalid command prefix, disabling discord...");
				Utility.PopColor();

				IsDisabled = true;
				return;
			}

			ulong commandChannelId = Config.Get("Discord.CommandChannelId", 0ul);

			if (commandChannelId == 0)
			{
				Utility.PushColor(ConsoleColor.Red);
				Console.WriteLine("Discord: Invalid command channel id, disabling discord...");
				Utility.PopColor();

				IsDisabled = true;
				return;
			}

			ulong logChannelId = Config.Get("Discord.LogChannelId", 0ul);

			if (logChannelId == 0)
			{
				Utility.PushColor(ConsoleColor.Yellow);
				Console.WriteLine("Discord: Warning no log channel id found, no logging will happen");
				Utility.PopColor();
			}

			int forceSocket = Config.Get("Discord.ForceSocket", 0);

			Settings = new DClientSettings(token, guildId, commandChannelId, logChannelId, cmdPrefix, forceSocket);
			_userMgr = new DiscordUserManager();
			_cmdHandler = new CommandHandler(cmdPrefix);

			Start();

			Server.Commands.CommandSystem.Register("discordlink", AccessLevel.GameMaster, new Server.Commands.CommandEventHandler(e =>
			{
				if (!ulong.TryParse(e.ArgString, out ulong id))
				{
					e.Mobile.SendMessage("Unable to parse id");
					return;
				}

				Accounting.Account acc = e.Mobile.Account as Accounting.Account;
				DiscordUserLink dul = UserManager[acc];

				if (dul == null)
					dul = new DiscordUserLink(acc, id);
				else
					dul.DiscordUserId = id;

				UserManager.AddOrUpdate(dul);
			}));

			//I don't know any other way to load this after the accounts have been loaded
			//so we will load this after the world has been loaded
			Load();

			_dclient.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public static void DiscordLog(string message, LogLevel level = LogLevel.Trace, bool usePrefix = true)
		{
			_discordLogger?.Log(message, level, usePrefix);
		}

		public static async Task<DiscordChannel> GetChannelAsync(ulong id)
		{
			return await RunSafeAsync(async () => await _dclient.GetChannelAsync(id).ConfigureAwait(false)).ConfigureAwait(false);
		}

		public static async Task<DiscordGuild> GetGuildAsync()
		{
			return await RunSafeAsync(async () => await _dclient.GetGuildAsync(Settings.GuildId).ConfigureAwait(false)).ConfigureAwait(false);
		}

		public static async Task<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong id)
		{
			return await RunSafeAsync(async () => await channel.GetMessageAsync(id).ConfigureAwait(false)).ConfigureAwait(false);
		}

		public static async Task<DiscordMessage> GetMessageAsync(ulong channelId, ulong msgId)
		{
			DiscordChannel channel = await GetChannelAsync(channelId).ConfigureAwait(false);

			if (channel == null)
				return null;

			return await GetMessageAsync(channel, msgId).ConfigureAwait(false);
		}

		public static async Task<bool> SendMessageAsync(ulong channelId, string message = null, DiscordEmbed embed = null)
		{
			DiscordChannel channel = await GetChannelAsync(channelId).ConfigureAwait(false);

			if (channel == null)
				return false;

			await channel.SendMessageAsync(content: message, embed: embed).ConfigureAwait(false);
			return true;
		}

		public static async Task<bool> SendEmbedMessageAsync(ulong channelId, string message, string title = null)
		{
			DiscordChannel channel = await GetChannelAsync(channelId).ConfigureAwait(false);

			if (channel == null)
				return false;

			return await SendEmbedMessageAsync(channel, message, title).ConfigureAwait(false);
		}

		public static async Task<bool> SendEmbedMessageAsync(DiscordChannel channel, string message, string title = null)
		{
			if (string.IsNullOrEmpty(title))
				title = InvisibleCharStr;

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder
			{
				Title = title,
				Description = message,
				Timestamp = DateTime.UtcNow
			};

			await channel.SendMessageAsync(embed: builder.Build()).ConfigureAwait(false);
			return true;
		}

		public static async Task<bool> ModifyMessageAsync(ulong channelId, ulong msgId, string message = null, DiscordEmbed embed = null)
		{
			DiscordMessage msg = await GetMessageAsync(channelId, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.ModifyAsync(message ?? new Optional<string>(), embed ?? new Optional<DiscordEmbed>()).ConfigureAwait(false);

			return true;
		}

		public static async Task<bool> ModifyMessageAsync(DiscordChannel channel, ulong msgId, string message = null, DiscordEmbed embed = null)
		{
			DiscordMessage msg = await GetMessageAsync(channel, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.ModifyAsync(message ?? new Optional<string>(), embed ?? new Optional<DiscordEmbed>()).ConfigureAwait(false);

			return true;
		}

		public static async Task<bool> DeleteMessageAsync(ulong channelId, ulong msgId, string reason = null)
		{
			DiscordMessage msg = await GetMessageAsync(channelId, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.DeleteAsync(reason).ConfigureAwait(false);
			return true;
		}

		public static async Task<bool> DeleteMessageAsync(DiscordChannel channel, ulong msgId, string reason = null)
		{
			DiscordMessage msg = await GetMessageAsync(channel, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.DeleteAsync(reason).ConfigureAwait(false);
			return true;
		}

		public static async Task<DiscordUser> GetUserAsync(ulong userId)
		{
			return await RunSafeAsync(async () => await _dclient.GetUserAsync(userId).ConfigureAwait(false)).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets a guild member
		/// </summary>
		/// <param name="userId">User needs to be in the same guild as <see cref="DClientSettings.GuildId"/></param>
		public static async Task<DiscordMember> GetMemberAsync(ulong userId)
		{
			DiscordGuild guild = await GetGuildAsync().ConfigureAwait(false);

			return await RunSafeAsync(async () => await guild.GetMemberAsync(userId).ConfigureAwait(false)).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a direct message channel between the bot and the user
		/// </summary>
		/// <param name="userId">User needs to be in the same guild as <see cref="DClientSettings.GuildId"/></param>
		public static async Task<DiscordDmChannel> GetDmChanelAsync(ulong userId)
		{
			DiscordMember member = await GetMemberAsync(userId).ConfigureAwait(false);

			if (member == null)
				return null;

			return await member.CreateDmChannelAsync().ConfigureAwait(false);
		}

		static async Task<T> RunSafeAsync<T>(Func<Task<T>> f) where T : class
		{
			try
			{
				return await f().ConfigureAwait(false);
			}
			catch (NotFoundException)
			{
				return null;
			}
			catch (UnauthorizedException)
			{
				return null;
			}
		}

		public static void Error(Exception ex)
		{
			WriteLine($"Error: {ex}", ConsoleColor.Red);
		}

		public static void Error(string name, string error = null, string info = null)
		{
			WriteLine($"Error: {name}{(string.IsNullOrEmpty(error) ? "" : $": {error}")}{(string.IsNullOrEmpty(info) ? "" : $" ({info})")}", ConsoleColor.Red);
		}

		public static void WriteLine(object msg, ConsoleColor color = ConsoleColor.Cyan)
		{
			Utility.PushColor(color);
			Console.WriteLine($"Discord: {msg}");
			Utility.PopColor();
		}

		static void Start()
		{
			if (IsReady)
			{
				Error("Client", "Client already started");
				return;
			}

			WriteLine("Loading client");

			if (string.IsNullOrEmpty(Settings.Token))
			{
				Error("Settings", "Token cannot be null or empty");
				return;
			}
			else if (Settings.GuildId <= 0)
			{
				Error("Settings", "GuildId cannot be <= 0");
				return;
			}

			switch (Settings.CommandPrefix)
			{
				default:
					break;

				case char.MinValue:
				case char.MaxValue:
				case ' ':
					Error("Settings", "Invalid command prefix");
					return;
			}

			if (char.IsLetterOrDigit(Settings.CommandPrefix))
			{
				Error("Settings", "Command prefix cannot be a digit or letter");
				return;
			}

			_dclient = new DiscordClient(new DiscordConfiguration
			{
				TokenType = TokenType.Bot,
				Token = Settings.Token
			});

			switch(Settings.ForceSocket)
			{
				default:
				case 0:
					if (Type.GetType("Mono.Runtime") != null)
						goto case 2;
					else
						goto case 1;

				case 1:
					WriteLine("Using .net websocket client");
					_dclient.SetWebSocketClient<DSharpPlus.Net.WebSocket.WebSocket4NetClient>();
					break;

				case 2:
					WriteLine("Using mono websocket client");
					_dclient.SetWebSocketClient<DSharpPlus.Net.WebSocket.WebSocketSharpClient>();
					break;
			}

			SubscribeEvents();

			if (Settings.LogChannelId > 0)
			{
				_discordLogger = LoggerFactory.GetLogger(Settings.LogChannelId);
				DiscordLog("Discord client loaded");
			}

			WriteLine("Client loaded");
		}

		static void Load()
		{
			_cmdHandler.InitCommands();

			if (!System.IO.File.Exists(_saveFile))
				return;

			Persistence.Deserialize(_saveFile, r =>
			{
				int ver = r.ReadInt();

				_userMgr.Deserialize(r);
			});
		}

		static void Save()
		{
			Persistence.Serialize(_saveFile, w =>
			{
				w.Write(0);

				_userMgr.Serialize(w);
			});
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		static void SubscribeEvents()
		{
			EventSink.WorldSave += _ => Save();

			_dclient.SocketErrored += async e => await SocketError(e).ConfigureAwait(false);
			_dclient.ClientErrored += async e => await ClientError(e).ConfigureAwait(false);
			_dclient.Ready += async e => await ClientReady(e).ConfigureAwait(false);
			_dclient.MessageCreated += async e => await ClientMessage(e).ConfigureAwait(false);
			_dclient.MessageReactionAdded += async e => await ReactionAdded(e).ConfigureAwait(false);
			_dclient.MessageReactionRemoved += async e => await ReactionRemoved(e).ConfigureAwait(false);
			_dclient.MessageReactionsCleared += async e => await ReactionsCleared(e).ConfigureAwait(false);
		}

		static async Task ClientMessage(MessageCreateEventArgs e)
		{
			if (e.Author.Id == _dclient.CurrentUser.Id ||
				string.IsNullOrEmpty(e.Message.Content))
				return;

			_cmdHandler.Invoke(e);
			OnMessageRecieved?.Invoke(e);
		}

		static async Task ClientReady(ReadyEventArgs e)
		{
			IsReady = true;
			WriteLine("Client is ready");
		}

		static async Task SocketError(SocketErrorEventArgs e)
		{
			IsReady = false;
			Error(e.Exception);
		}

		static async Task ClientError(ClientErrorEventArgs e)
		{
			IsReady = false;
			Error(e.Exception);
		}

		static async Task ReactionAdded(MessageReactionAddEventArgs e)
		{
			OnMessageReactionAdded?.Invoke(e);
		}

		static async Task ReactionRemoved(MessageReactionRemoveEventArgs e)
		{
			OnMessageReactionRemoved?.Invoke(e);
		}

		static async Task ReactionsCleared(MessageReactionsClearEventArgs e)
		{
			OnMessageReactionCleared?.Invoke(e);
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}

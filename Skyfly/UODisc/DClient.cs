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

namespace Server.Custom.Skyfly.UODisc
{
	public sealed class DClient
	{
		public static DClient Instance { get; private set; }
		public static string SaveFolder => System.IO.Path.Combine(Environment.CurrentDirectory, "Saves", "Discord");

		public DClientSettings Settings { get; private set; }
		public bool IsReady { get; private set; }
		public DiscordUserManager UserManager => _userMgr;
		public CommandHandler CommandHandler => _cmdHandler;

		static bool _doesClientExist;
		static readonly string _saveFile = System.IO.Path.Combine(SaveFolder, "Client.bin");

		DiscordClient _dclient;
		DiscordUserManager _userMgr;
		CommandHandler _cmdHandler;

		DClient(DClientSettings settings)
		{
			if (_doesClientExist)
				throw new NotImplementedException("Multiple instances of DClient is currently not supported");

			_doesClientExist = true;
			Settings = settings;
			_userMgr = new DiscordUserManager();
			_cmdHandler = new CommandHandler(settings.CommandPrefix);
		}

		public static void Configure()
		{
			Instance = new DClient(new DClientSettings("ODMwOTQ5MDI0MDMzOTMxMzI0.YHOHlQ.C0K2cFmhbkmWNxsINdIbpVgBbXE", 763438725345968138));
			Instance.Start();

			Server.Commands.CommandSystem.Register("discordlink", AccessLevel.GameMaster, new Server.Commands.CommandEventHandler(e =>
			{
				if (!ulong.TryParse(e.ArgString, out ulong id))
				{
					e.Mobile.SendMessage("Unable to parse id");
					return;
				}

				Accounting.Account acc = e.Mobile.Account as Accounting.Account;
				DiscordUserLink dul = Instance.UserManager[acc];

				if (dul == null)
					dul = new DiscordUserLink(acc, id);
				else
					dul.DiscordUserId = id;

				Instance.UserManager.AddOrUpdate(dul);
			}));
		}

		public static void Initialize()
		{
			//I don't know any other way to load this after the accounts have been loaded
			//so we will load this after the world has been loaded
			Instance.Load();

			Instance._dclient.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public async Task<DiscordChannel> GetChannelAsync(ulong id)
		{
			return await RunSafeAsync(async () => await _dclient.GetChannelAsync(id).ConfigureAwait(false)).ConfigureAwait(false);
		}

		public async Task<DiscordGuild> GetGuildAsync()
		{
			return await RunSafeAsync(async () => await _dclient.GetGuildAsync(Settings.GuildId).ConfigureAwait(false)).ConfigureAwait(false);
		}

		public async Task<DiscordMessage> GetMessageAsync(DiscordChannel channel, ulong id)
		{
			return await RunSafeAsync(async () => await channel.GetMessageAsync(id).ConfigureAwait(false)).ConfigureAwait(false);
		}

		public async Task<DiscordMessage> GetMessageAsync(ulong channelId, ulong msgId)
		{
			DiscordChannel channel = await GetChannelAsync(channelId).ConfigureAwait(false);

			if (channel == null)
				return null;

			return await GetMessageAsync(channel, msgId).ConfigureAwait(false);
		}

		public async Task<bool> SendMessageAsync(ulong channelId, string message = null, DiscordEmbed embed = null)
		{
			DiscordChannel channel = await GetChannelAsync(channelId).ConfigureAwait(false);

			if (channel == null)
				return false;

			await channel.SendMessageAsync(content: message, embed: embed).ConfigureAwait(false);
			return true;
		}

		public async Task<bool> ModifyMessageAsync(ulong channelId, ulong msgId, string message = null, DiscordEmbed embed = null)
		{
			DiscordMessage msg = await GetMessageAsync(channelId, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.ModifyAsync(message == null ? default : message, embed == null ? default : embed).ConfigureAwait(false);

			return true;
		}

		public async Task<bool> ModifyMessageAsync(DiscordChannel channel, ulong msgId, string message = null, DiscordEmbed embed = null)
		{
			DiscordMessage msg = await GetMessageAsync(channel, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.ModifyAsync(message == null ? default : message, embed == null ? default : embed).ConfigureAwait(false);

			return true;
		}

		public async Task<bool> DeleteMessageAsync(ulong channelId, ulong msgId, string reason = null)
		{
			DiscordMessage msg = await GetMessageAsync(channelId, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.DeleteAsync(reason).ConfigureAwait(false);
			return true;
		}

		public async Task<bool> DeleteMessageAsync(DiscordChannel channel, ulong msgId, string reason = null)
		{
			DiscordMessage msg = await GetMessageAsync(channel, msgId).ConfigureAwait(false);

			if (msg == null)
				return false;

			await msg.DeleteAsync(reason).ConfigureAwait(false);
			return true;
		}

		public async Task<DiscordUser> GetUserAsync(ulong userId)
		{
			return await RunSafeAsync(async () => await _dclient.GetUserAsync(userId).ConfigureAwait(false)).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets a guild member
		/// </summary>
		/// <param name="userId">User needs to be in the same guild as <see cref="DClientSettings.GuildId"/></param>
		public async Task<DiscordMember> GetMemberAsync(ulong userId)
		{
			DiscordGuild guild = await GetGuildAsync().ConfigureAwait(false);

			return await RunSafeAsync(async () => await guild.GetMemberAsync(userId).ConfigureAwait(false)).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a direct message channel between the bot and the user
		/// </summary>
		/// <param name="userId">User needs to be in the same guild as <see cref="DClientSettings.GuildId"/></param>
		public async Task<DiscordDmChannel> GetDmChanelAsync(ulong userId)
		{
			DiscordMember member = await GetMemberAsync(userId).ConfigureAwait(false);

			if (member == null)
				return null;

			return await member.CreateDmChannelAsync().ConfigureAwait(false);
		}

		async Task<T> RunSafeAsync<T>(Func<Task<T>> f) where T : class
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

		void Start()
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

			_dclient = new DiscordClient(new DiscordConfiguration()
			{
				TokenType = TokenType.Bot,
				Token = Settings.Token
			});

			if (Type.GetType("Mono.Runtime") != null)
			{
				WriteLine("Using mono websocket client");
				_dclient.SetWebSocketClient<DSharpPlus.Net.WebSocket.WebSocketSharpClient>();
			}
			else
			{
				WriteLine("Using .net websocket client");
				_dclient.SetWebSocketClient<DSharpPlus.Net.WebSocket.WebSocket4NetClient>();
			}

			SubscribeEvents();
			WriteLine("Client loaded");
		}

		void Load()
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

		void Save()
		{
			Persistence.Serialize(_saveFile, w =>
			{
				w.Write(0);

				_userMgr.Serialize(w);
			});
		}

		void Error(Exception ex)
		{
			WriteLine($"Error: {ex}", ConsoleColor.Red);
		}

		void Error(string name, string error = null, string info = null)
		{
			WriteLine($"Error: {name}{(string.IsNullOrEmpty(error) ? "" : $": {error}")}{(string.IsNullOrEmpty(info) ? "" : $" ({info})")}", ConsoleColor.Red);
		}

		void WriteLine(object msg, ConsoleColor color = ConsoleColor.Cyan)
		{
			Utility.PushColor(color);
			Console.WriteLine($"Discord: {msg}");
			Utility.PopColor();
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		void SubscribeEvents()
		{
			EventSink.WorldSave += _ => Save();

			_dclient.SocketErrored += async e => await SocketError(e).ConfigureAwait(false);
			_dclient.ClientErrored += async e => await ClientError(e).ConfigureAwait(false);
			_dclient.Ready += async e => await ClientReady(e).ConfigureAwait(false);
			_dclient.MessageCreated += async e => await ClientMessage(e).ConfigureAwait(false);
		}

		async Task ClientMessage(MessageCreateEventArgs e)
		{
			if (e.Author.Id == _dclient.CurrentUser.Id ||
				string.IsNullOrEmpty(e.Message.Content))
				return;

			//Disallow private chat and any other channels except #bot-test
			if (e.Channel.Guild != null && e.Channel.Id != 830949825942913067)
				return;
			else if (e.Channel.Guild == null)
				return;

			_cmdHandler.Invoke(e);
		}

		async Task ClientReady(ReadyEventArgs e)
		{
			IsReady = true;
			WriteLine("Client is ready");
		}

		async Task SocketError(SocketErrorEventArgs e)
		{
			IsReady = false;
			Error(e.Exception);
		}

		async Task ClientError(ClientErrorEventArgs e)
		{
			IsReady = false;
			Error(e.Exception);
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}

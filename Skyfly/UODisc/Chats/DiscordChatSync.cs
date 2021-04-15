using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc.Chats
{
	/// <summary>
	/// One-way sync for a discord chat
	/// </summary>
	public class DiscordChatSync : BaseChatSync
	{
		public ulong DiscordChannelId { get; private set; }
		public DiscordChannel DiscordChannel { get; private set; }

		/// <summary>
		/// One-way sync for a discord chat
		/// </summary>
		/// <param name="discordChannelId">Discord channel id</param>
		/// <param name="syncDelay">Delay between synchronization tries
		/// <para><see cref="TimeSpan.Zero"/> uses the default delay of 100 ms</para>
		/// <para>Minimum: 100 ms</para></param>
		public DiscordChatSync(ulong discordChannelId, TimeSpan syncDelay) : base(syncDelay.TotalMilliseconds < 100 ? TimeSpan.FromMilliseconds(100) : syncDelay)
		{
			DiscordChannelId = discordChannelId;
		}

		public DiscordChatSync(ulong discordChannelId) : this(discordChannelId, TimeSpan.Zero)
		{

		}

		public override void Start()
		{
			if (IsRunning)
				return;

			DiscordChannel = DClient.GetChannelAsync(DiscordChannelId).ConfigureAwait(false).GetAwaiter().GetResult();

			if (DiscordChannel == null)
				return;

			base.Start();
		}

		protected override IEnumerable<string> GatherMessages()
		{
			StringBuilder sb = new StringBuilder();

			while(_syncQueue.TryDequeue(out string s))
			{
				sb.AppendLine(s);

				if (sb.Length >= 2000)
				{
					yield return sb.ToString();
					sb.Clear();
				}
			}

			if (sb.Length > 0)
				yield return sb.ToString();

			yield break;
		}

		protected override void SendMessage(string msg)
		{
			DiscordChannel.SendMessageAsync(msg);
		}
	}
}

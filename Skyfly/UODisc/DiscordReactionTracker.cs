using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Server.Custom.Skyfly.UODisc
{
	/// <summary>
	/// Tracks reactions in every/a specific channel and for every/a specific message
	/// </summary>
	public class DiscordReactionTracker
	{
		public event Action<MessageReactionAddEventArgs> OnReactionAdded;
		public event Action<MessageReactionRemoveEventArgs> OnReactionRemoved;
		public event Action<MessageReactionsClearEventArgs> OnReactionsCleared;

		/// <summary>
		/// Channel to track, 0 for any channel
		/// </summary>
		public ulong DiscordChannelId { get; set; }
		/// <summary>
		/// Message to track, 0 for any message
		/// </summary>
		public ulong DiscordMessageId { get; set; }

		public bool IsStarted { get; private set; }

		/// <summary>
		/// Tracks reactions in every/a specific channel and for every/a specific message
		/// </summary>
		/// <param name="discordChannelId">0 for any channel</param>
		/// <param name="discordMessageId">0 for any message</param>
		public DiscordReactionTracker(ulong discordChannelId, ulong discordMessageId, bool startTracking = false)
		{
			DiscordChannelId = discordChannelId;
			DiscordMessageId = discordMessageId;

			if (startTracking)
				Start();
		}

		/// <summary>
		/// Starts tracking reactions
		/// </summary>
		public virtual void Start()
		{
			if (IsStarted)
				return;

			IsStarted = true;

			DClient.OnMessageReactionAdded += NewReaction;
			DClient.OnMessageReactionRemoved += ReactionRemoved;
			DClient.OnMessageReactionCleared += ReactionsCleared;
		}

		/// <summary>
		/// Stops tracking reactions
		/// </summary>
		public virtual void Stop()
		{
			if (!IsStarted)
				return;

			IsStarted = false;

			DClient.OnMessageReactionAdded -= NewReaction;
			DClient.OnMessageReactionRemoved -= ReactionRemoved;
			DClient.OnMessageReactionCleared -= ReactionsCleared;
		}

		protected virtual void NewReaction(MessageReactionAddEventArgs e)
		{
			if (!CheckRequirements(e.Channel, e.Message))
				return;

			OnReactionAdded?.Invoke(e);
		}

		protected virtual void ReactionRemoved(MessageReactionRemoveEventArgs e)
		{
			if (!CheckRequirements(e.Channel, e.Message))
				return;

			OnReactionRemoved?.Invoke(e);
		}

		protected virtual void ReactionsCleared(MessageReactionsClearEventArgs e)
		{
			if (!CheckRequirements(e.Channel, e.Message))
				return;

			OnReactionsCleared?.Invoke(e);
		}

		/// <summary>
		/// Check if we should care about any changed reaction state
		/// </summary>
		protected virtual bool CheckRequirements(DiscordChannel channel, DiscordMessage message)
		{
			if (channel == null || message == null)
				return false;
			else if (DiscordChannelId == 0)
				return true;
			else if (DiscordMessageId != 0 && message.Id != DiscordMessageId)
				return false;

			return DiscordChannelId == channel.Id &&
				  (DiscordMessageId == 0 || (DiscordMessageId != 0 && DiscordMessageId == message.Id));
		}
	}
}

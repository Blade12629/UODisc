using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc
{
	public static class Extensions
	{
		public static async Task<bool> SendEmbedMessage(this DiscordChannel channel, string message, string title = null)
		{
			return await DClient.SendEmbedMessageAsync(channel, message, title).ConfigureAwait(false);
		}
	}
}

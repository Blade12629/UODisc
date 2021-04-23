using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Server.Custom.Skyfly.UODisc.Chats.Custom.KnivesChat;

namespace Server.Custom.Skyfly.UODisc.Chats
{
	public static class ChatSyncs
	{
		public static List<BaseChatSync> GetDefaultChatSyncs()
		{
			return new List<BaseChatSync>()
			{
				//new GlobalDiscordChatSync(0),
			};
		}
	}
}

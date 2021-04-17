//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Server.Custom.Skyfly.UODisc.Chats.Custom.KnivesChat
//{
//	public class GlobalDiscordChatSync : DiscordChatSync
//	{
//		public static string ChannelName => "Public";

//		public GlobalDiscordChatSync(ulong discordChannelId) : base(discordChannelId)
//		{

//		}

//		public override void Start()
//		{
//			if (IsRunning)
//				return;

//			Knives.Chat3.Events.Chat += OnMessageRecieved;

//			base.Start();
//		}

//		public override void Stop(bool waitForTask = false)
//		{
//			if (!IsRunning)
//				return;

//			Knives.Chat3.Events.Chat -= OnMessageRecieved;

//			base.Stop(waitForTask);
//		}

//		void OnMessageRecieved(Knives.Chat3.ChatEventArgs e)
//		{
//			if (!e.Channel.Name.Equals(ChannelName, StringComparison.CurrentCultureIgnoreCase) ||
//				(e.Mobile.Title != null && e.Mobile.Title.Equals(GameChatSync.FakeMobileTitle)))
//				return;

//			ReceiveMessage($"{e.Mobile.Name}: {e.Speech}");
//		}
//	}
//}

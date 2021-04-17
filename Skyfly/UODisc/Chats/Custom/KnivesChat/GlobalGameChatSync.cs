//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Server.Custom.Skyfly.UODisc.Chats.Custom.KnivesChat
//{
//	public class GlobalGameChatSync : GameChatSync
//	{
//		public ulong DiscordChannelId { get; private set; }

//		public GlobalGameChatSync(ulong discordChannelId) : base("Public")
//		{
//			DiscordChannelId = discordChannelId;
//		}

//		public override void Start()
//		{
//			base.Start();

//			if (Channel != null)
//				DClient.OnMessageRecieved += OnMessageReceived;
//		}

//		void OnMessageReceived(DSharpPlus.EventArgs.MessageCreateEventArgs e)
//		{
//			if (e.Channel.Id != DiscordChannelId || e.Message.Content[0] == DClient.Settings.CommandPrefix)
//				return;

//			DiscordUserLink dul = DClient.UserManager[e.Author.Id];

//			if (dul == null || dul.SelectedCharacter == null)
//				return;

//			ReceiveMessage($"{dul.SelectedCharacter.Name}: {e.Message.Content}");
//		}

//		public override void Stop(bool waitForTask = false)
//		{
//			if (!IsRunning)
//				return;

//			if (Channel != null)
//				DClient.OnMessageRecieved -= OnMessageReceived;

//			base.Stop(waitForTask);
//		}
//	}
//}

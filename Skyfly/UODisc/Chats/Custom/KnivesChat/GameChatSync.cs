using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Knives.Chat3;

namespace Server.Custom.Skyfly.UODisc.Chats.Custom.KnivesChat
{
	public class GameChatSync : BaseChatSync
	{

		public Channel Channel { get; private set; }
		public string ChannelName { get; private set; }
		public Mobile FakeMobile { get; private set; }

		public static readonly string FakeMobileTitle = "GameChatSyncFakeMobile";

		public GameChatSync(string channel)
		{
			ChannelName = channel;
		}

		public override void Start()
		{
			if (IsRunning)
				return;

			Channel = Channel.GetByName(ChannelName);

			if (Channel == null)
				return;

			FakeMobile = World.Mobiles.Values.FirstOrDefault(m => m.Map == Map.Internal && m.Title != null && m.Title.Equals(FakeMobileTitle));

			if (FakeMobile == null)
				FakeMobile = GetFakeMobile();

			base.Start();
		}

		Mobile GetFakeMobile()
		{
			if (FakeMobile != null && !FakeMobile.Deleted)
				return FakeMobile;

			Mobile m = new Mobile();
			m.Title = FakeMobileTitle;
			m.MoveToWorld(Point3D.Zero, Map.Internal);

			return m;
		}

		protected virtual void SendMessage(Mobile from, string msg)
		{
			Channel.OnChat(from, msg, false);
		}

		protected override void SendMessage(string msg)
		{
			SendMessage(FakeMobile, msg);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Accounting;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc
{
	public sealed class DiscordUserLink : IEquatable<DiscordUserLink>
	{
		public Account Account { get; set; }
		public ulong DiscordUserId { get; set; }

		public DiscordUserLink()
		{

		}

		public DiscordUserLink(Account acc)
		{
			Account = acc;
		}

		public DiscordUserLink(ulong discordUserId)
		{
			DiscordUserId = discordUserId;
		}

		public DiscordUserLink(Account account, ulong discordUserId) : this(account)
		{
			DiscordUserId = discordUserId;
		}

		public void Serialize(GenericWriter w)
		{
			w.Write(0);

			if (Account == null)
				w.Write((byte)0);
			else
			{
				w.Write((byte)1);
				w.Write(Account.Username);
			}

			w.Write(DiscordUserId);
		}

		public void Deserialize(GenericReader r)
		{
			int ver = r.ReadInt();

			if (r.ReadByte() != 0)
			{
				IAccount acc = Accounts.GetAccount(r.ReadString());

				if (acc != null)
					Account = (Account)acc;
			}

			DiscordUserId = r.ReadULong();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as DiscordUserLink);
		}

		public bool Equals(DiscordUserLink other)
		{
			return other != null &&
				   EqualityComparer<Account>.Default.Equals(Account, other.Account) &&
				   DiscordUserId == other.DiscordUserId;
		}

		public bool Equals(DiscordUser user)
		{
			return user != null && user.Id == DiscordUserId;
		}

		public bool Equals(DiscordMember member)
		{
			return member != null && member.Id == DiscordUserId;
		}

		public bool Equals(Mobile m)
		{
			if (m == null)
				return false;

			return Equals(m as Mobiles.PlayerMobile);
		}

		public bool Equals(Mobiles.PlayerMobile pm)
		{
			return pm != null && pm.Account != null && Account != null && 
				   Equals(pm.Account);
		}

		public bool Equals(IAccount acc)
		{
			return acc != null && Account != null && acc.Username.Equals(Account.Username, StringComparison.CurrentCulture);
		}

		public override int GetHashCode()
		{
			var hashCode = -584914611;
			hashCode = hashCode * -1521134295 + EqualityComparer<Account>.Default.GetHashCode(Account);
			hashCode = hashCode * -1521134295 + DiscordUserId.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(DiscordUserLink left, DiscordUserLink right)
		{
			return EqualityComparer<DiscordUserLink>.Default.Equals(left, right);
		}

		public static bool operator !=(DiscordUserLink left, DiscordUserLink right)
		{
			return !(left == right);
		}
	}
}

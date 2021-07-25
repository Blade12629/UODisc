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
		public Account[] Accounts { get; set; }
		public ulong DiscordUserId { get; set; }
		public Mobile SelectedCharacter { get; set; }

		public DiscordUserLink()
		{

		}

		public DiscordUserLink(ulong discordUserId)
		{
			DiscordUserId = discordUserId;
		}

		public DiscordUserLink(params Account[] accs)
		{
			Accounts = accs;
		}

		public DiscordUserLink(Account acc) : this(new Account[] { acc })
		{
		}

		public DiscordUserLink(Account account, ulong discordUserId) : this(account)
		{
			DiscordUserId = discordUserId;
		}

		public void AddAccount(Account acc)
		{
			if (acc == null)
				return;

			Account[] accs;

			if (Accounts != null)
			{
				accs = new Account[(Accounts?.Length ?? 0) + 1];

				for (int i = 0; i < Accounts.Length; i++)
				{
					if (Accounts[i].Username.Equals(acc.Username))
						return;

					accs[i] = Accounts[i];
				}

				accs[Accounts.Length] = acc;
			}
			else
			{
				accs = new Account[]
				{
					acc
				};
			}

			Accounts = accs;
		}

		public void RemoveAccount(Account acc)
		{
			if (acc == null || Accounts == null || Accounts.Length == 0)
				return;

			List<Account> accs = new List<Account>(Accounts);

			for (int i = 0; i < accs.Count; i++)
			{
				if (accs[i].Username.Equals(acc.Username))
				{
					accs.RemoveAt(i);
					Accounts = accs.ToArray();
					return;
				}
			}
		}

		public void Serialize(GenericWriter w)
		{
			w.Write(2);

			//2
			if (Accounts == null || Accounts.Length == 0)
				w.Write(0);
			else
			{
				w.Write(Accounts.Length);

				for (int i = 0; i < Accounts.Length; i++)
					w.Write(Accounts[i].Username);
			}

			//1
			if (SelectedCharacter == null)
				w.Write((byte)0);
			else
			{
				w.Write((byte)1);
				w.Write(SelectedCharacter);
			}

			//0
			//if (Account == null)
			//	w.Write((byte)0);
			//else
			//{
			//	w.Write((byte)1);
			//	w.Write(Account.Username);
			//}

			w.Write(DiscordUserId);
		}

		public void Deserialize(GenericReader r)
		{
			int ver = r.ReadInt();

			switch (ver)
			{
				case 2:
					{
						int length = r.ReadInt();

						if (length > 0)
						{
							List<Account> accs = new List<Account>(length);

							for (int i = 0; i < length; i++)
							{
								string accName = r.ReadString();
								Account acc = Accounting.Accounts.GetAccount(accName) as Account;

								if (acc != null)
									accs.Add(acc);
							}

							if (accs.Count > 0)
								Accounts = accs.ToArray();
						}
					}
					goto case 1;

				case 1:
					if (r.ReadByte() != 0)
					{
						SelectedCharacter = r.ReadMobile();
					}
					goto case 0;

				case 0:
					if (ver >= 2)
					{
						DiscordUserId = r.ReadULong();
						break;
					}

					if (r.ReadByte() != 0)
					{
						IAccount acc = Accounting.Accounts.GetAccount(r.ReadString());

						if (acc != null)
							AddAccount((Account)acc);
					}

					DiscordUserId = r.ReadULong();
					break;
			}
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as DiscordUserLink);
		}

		public bool Equals(DiscordUserLink other)
		{
			return other != null &&
				   EqualityComparer<Account[]>.Default.Equals(Accounts, other.Accounts) &&
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
			return pm != null && pm.Account != null && Accounts != null && 
				   Equals(pm.Account);
		}

		public bool Equals(IAccount acc)
		{
			if (Accounts == null)
				return false;

			for (int i = 0; i < Accounts.Length; i++)
				if (Accounts[i].Username.Equals(acc.Username, StringComparison.CurrentCultureIgnoreCase))
					return true;

			return false;
		}

		public override int GetHashCode()
		{
			var hashCode = -584914611;
			hashCode = hashCode * -1521134295 + EqualityComparer<Account[]>.Default.GetHashCode(Accounts);
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Accounting;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc
{
	public sealed class DiscordUserManager
	{
		Dictionary<Account, DiscordUserLink> _accLinks;
		Dictionary<ulong, DiscordUserLink> _discUserLinks;

		readonly object _syncRoot;

		public DiscordUserManager()
		{
			_syncRoot = new object();

			_accLinks = new Dictionary<Account, DiscordUserLink>();
			_discUserLinks = new Dictionary<ulong, DiscordUserLink>();
		}

		public DiscordUserLink this[Account acc]
		{
			get
			{
				lock(_syncRoot)
				{
					_accLinks.TryGetValue(acc, out DiscordUserLink dul);
					return dul;
				}
			}
			set
			{
				AddOrUpdate(value);
			}
		}

		public DiscordUserLink this[ulong discordUserId]
		{
			get
			{
				lock (_syncRoot)
				{
					_discUserLinks.TryGetValue(discordUserId, out DiscordUserLink dul);
					return dul;
				}
			}
			set
			{
				AddOrUpdate(value);
			}
		}

		public DiscordUserLink this[DiscordUser user]
		{
			get => this[user.Id];
			set => this[user.Id] = value;
		}

		public DiscordUserLink this[DiscordMember member]
		{
			get => this[member.Id];
			set => this[member.Id] = value;
		}

		public AccessLevel GetAccessLevel(ulong discordUserId)
		{
			DiscordUserLink link = this[discordUserId];

			if (link == null || link.Account == null)
				return AccessLevel.Player;

			return GetAccessLevel(link.Account);
		}

		public AccessLevel GetAccessLevel(Account acc)
		{
			return acc.AccessLevel;
		}

		public void AddOrUpdate(DiscordUserLink dul)
		{
			lock(_syncRoot)
			{
				if (dul.Account != null)
				{
					if (!_accLinks.ContainsKey(dul.Account))
						_accLinks.Add(dul.Account, dul);
					else
						_accLinks[dul.Account] = dul;
				}

				if (dul.DiscordUserId > 0)
				{
					if (!_discUserLinks.ContainsKey(dul.DiscordUserId))
						_discUserLinks.Add(dul.DiscordUserId, dul);
					else
						_discUserLinks[dul.DiscordUserId] = dul;
				}
			}
		}

		public void Remove(DiscordUserLink dul)
		{
			lock(_syncRoot)
			{
				if (dul.Account != null)
					_accLinks.Remove(dul.Account);

				if (dul.DiscordUserId > 0)
					_discUserLinks.Remove(dul.DiscordUserId);
			}
		}

		public void Serialize(GenericWriter w)
		{
			w.Write(0);

			DiscordUserLink[] links;

			//lock this just incase to be safe since this could be modified from discord while we are saving it
			lock (_syncRoot)
				links = _accLinks.Values.ToArray();

			w.Write(links.Length);
			for (int i = 0; i < links.Length; i++)
				links[i].Serialize(w);
		}

		public void Deserialize(GenericReader r)
		{
			int ver = r.ReadInt();

			int total = r.ReadInt();
			for (int i = 0; i < total; i++)
			{
				DiscordUserLink dul = new DiscordUserLink();
				dul.Deserialize(r);

				if (dul.Account == null && dul.DiscordUserId == 0)
					continue;

				AddOrUpdate(dul);
			}
		}
	}
}

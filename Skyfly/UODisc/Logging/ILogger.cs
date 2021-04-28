using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc
{
	public interface ILogger
	{
		ulong DiscordChannelId { get; }

		void Disable();
		void Log(string msg, LogLevel level = LogLevel.Trace, bool usePrefix = true);
	}
}

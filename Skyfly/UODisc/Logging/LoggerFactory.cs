using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc
{
	public static class LoggerFactory
	{
		//200 is the minimum the logger will accept, anything below it will be rejected and 200 ms will be chosen instead
		static int _loggerDelayMS => 200;
		static ConcurrentDictionary<ulong, ILogger> _loggers;

		static LoggerFactory()
		{
			_loggers = new ConcurrentDictionary<ulong, ILogger>();
		}

		public static ILogger GetLogger(ulong discordChannelId)
		{
			ILogger logger;
			//Our logger might not exist, create him in that case
			if (!_loggers.TryGetValue(discordChannelId, out logger))
			{
				logger = new DiscLogger(discordChannelId, TimeSpan.FromMilliseconds(_loggerDelayMS));

				//Add a new logger if possible
				if (_loggers.TryAdd(discordChannelId, logger))
					return logger;

				//A logger was added before we could add it, disable the newer logger and take the older logger
				logger.Disable();

				_loggers.TryGetValue(discordChannelId, out logger);
				return logger;
			}

			return logger;
		}
	}
}

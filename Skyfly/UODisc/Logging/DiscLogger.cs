using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc
{
	public class DiscLogger : ILogger
	{
		public ulong DiscordChannelId { get; }
		public TimeSpan CacheSendDelay { get; }
		public DiscordChannel Channel { get; }

		readonly Task _task;
		readonly ConcurrentQueue<string> _queue;
		readonly CancellationTokenSource _token;


		static readonly int _maxMessageLength = 2400;

		public DiscLogger(ulong discordChannelId, TimeSpan cacheSendDelay)
		{
			DiscordChannelId = discordChannelId;
			Channel = DClient.GetChannelAsync(discordChannelId).ConfigureAwait(false).GetAwaiter().GetResult();

			TimeSpan defaultDelay = TimeSpan.FromMilliseconds(200);
			if (cacheSendDelay < defaultDelay)
				CacheSendDelay = defaultDelay;
			else
				CacheSendDelay = cacheSendDelay;

			_token = new CancellationTokenSource();
			_task = new Task(LogTick, _token.Token, TaskCreationOptions.LongRunning);
			_queue = new ConcurrentQueue<string>();

			_task.Start();
		}

		public void Disable()
		{
			_token?.Cancel();
		}

		public void Log(string msg, LogLevel level = LogLevel.Trace, bool usePrefix = true)
		{
			switch(level)
			{
				default:
					break;

				case LogLevel.Debug:
#if RELEASE
					return;
#endif
					break;
			}

			if (usePrefix)
				msg = $"[{level}] {msg}";

			_queue.Enqueue(msg);
		}

		void LogTick()
		{
			double timeout = 0;
			while(!DClient.IsReady)
			{
				Task.Delay(CacheSendDelay).ConfigureAwait(false).GetAwaiter().GetResult();
				timeout += CacheSendDelay.TotalSeconds;

				//we time out, discord client probably failed horribly at starting
				if (timeout > TimeSpan.FromMinutes(5).TotalSeconds || _token.IsCancellationRequested)
					return;
			}

			bool skipped;
			while (!_token.IsCancellationRequested)
			{
				skipped = true;

				foreach (string v in GatherMessages())
				{
					skipped = false;
					SendMessage(v);
					Task.Delay(CacheSendDelay).ConfigureAwait(false).GetAwaiter().GetResult();

					if (_token.IsCancellationRequested)
						break;
				}

				if (skipped)
					Task.Delay(CacheSendDelay).ConfigureAwait(false).GetAwaiter().GetResult();
			}
		}

		IEnumerable<string> GatherMessages()
		{
			StringBuilder sb = new StringBuilder();

			while (_queue.TryDequeue(out string s))
			{
				while(s.Length > _maxMessageLength)
				{
					yield return s.Substring(0, _maxMessageLength);
					s = s.Remove(0, _maxMessageLength);
				}

				if (string.IsNullOrEmpty(s))
					continue;

				if (sb.Length + s.Length >= _maxMessageLength)
				{
					yield return sb.ToString();
					sb.Clear();
				}

				sb.AppendLine(s);
			}

			if (sb.Length > 0)
				yield return sb.ToString();
		}

		void SendMessage(string msg)
		{
			Channel.SendMessageAsync(msg).ConfigureAwait(false);
		}
	}
}

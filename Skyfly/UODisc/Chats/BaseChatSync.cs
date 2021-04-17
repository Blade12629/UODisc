using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc.Chats
{
	/// <summary>
	/// One-way chat synchronization
	/// </summary>
	public abstract class BaseChatSync
	{
		public bool IsRunning { get; private set; }
		public TimeSpan SyncDelay
		{
			get => _syncDelay;
			set
			{
				if (value <= TimeSpan.Zero)
					_syncDelay = TimeSpan.Zero;
				else
					_syncDelay = value;
			}
		}

		static TimeSpan _defaultDelay => TimeSpan.FromMilliseconds(100);

		TimeSpan _syncDelay;
		Task _syncTask;

		protected CancellationTokenSource _syncToken;
		protected ConcurrentQueue<string> _syncQueue;

		/// <summary>
		/// One-way chat synchronization
		/// </summary>
		/// <param name="syncDelay">Delay between synchronization tries
		/// <para><see cref="TimeSpan.Zero"/> uses the default delay of 100 ms</para></param>
		public BaseChatSync(TimeSpan syncDelay)
		{
			_syncDelay = syncDelay;
			_syncToken = new CancellationTokenSource();
			_syncTask = new Task(SyncTick, _syncToken.Token, TaskCreationOptions.LongRunning);
			_syncQueue = new ConcurrentQueue<string>();
		}

		public BaseChatSync() : this(TimeSpan.Zero)
		{

		}
	
		public virtual void Start()
		{
			if (IsRunning)
				return;

			IsRunning = true;
			_syncTask.Start();
		}

		public virtual void Stop(bool waitForTask = false)
		{
			if (!IsRunning)
				return;

			IsRunning = false;
			_syncToken.Cancel();

			if (waitForTask)
				_syncTask.Wait();
		}

		public virtual void Clear()
		{
			while (_syncQueue.TryDequeue(out string _)) ;
		}

		public virtual void ReceiveMessage(string msg)
		{
			if (!IsRunning)
				return;

			_syncQueue.Enqueue(msg);
		}

		void SyncTick()
		{
			while(!_syncToken.IsCancellationRequested)
			{
				foreach(string v in GatherMessages())
				{
					SendMessage(v);

					if (_syncToken.IsCancellationRequested)
						break;
				}

				if (_syncToken.IsCancellationRequested)
					break;

				Task.Delay(_syncDelay == TimeSpan.Zero ? _defaultDelay : _syncDelay).ConfigureAwait(false).GetAwaiter().GetResult();
			}
		}

		protected virtual IEnumerable<string> GatherMessages()
		{
			while (_syncQueue.TryDequeue(out string s))
				yield return s;

			yield break;
		}

		protected abstract void SendMessage(string msg);
	}
}

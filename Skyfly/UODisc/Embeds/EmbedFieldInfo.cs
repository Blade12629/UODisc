using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Custom.Skyfly.UODisc.Embeds
{
	/// <summary>
	/// Threadsafe EmbedFieldInfo
	/// </summary>
	public class EmbedFieldInfo
	{
		public object SyncRoot { get; }

		public int Count => _lines.Count;

		private List<string> _lines { get; }

		/// <summary>
		/// Threadsafe EmbedFieldInfo
		/// </summary>
		public EmbedFieldInfo() : this(Array.Empty<string>())
		{
		}

		/// <summary>
		/// Threadsafe EmbedFieldInfo
		/// </summary>
		/// <param name="fields">use <see cref="Array.Empty{T}"/> or with predefined values</param>
		/// <exception cref="ArgumentNullException"></exception>
		public EmbedFieldInfo(params string[] fields)
		{
			SyncRoot = new object();

			lock (SyncRoot)
			{
				if (fields == null)
					throw new ArgumentNullException(nameof(fields));

				_lines = new List<string>(fields);
			}
		}

		/// <summary>
		/// Gets or sets a specific line
		/// </summary>
		/// <exception cref="IndexOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public string this[int index]
		{
			get
			{
				lock (SyncRoot)
				{
					if (index >= _lines.Count)
						return null;

					return _lines[index];
				}
			}
			set
			{
				lock (SyncRoot)
				{
					if (index < 0)
						throw new IndexOutOfRangeException("Index cannot be less than 0");
					else if (index >= _lines.Count)
						throw new IndexOutOfRangeException($"{nameof(index)} Index cannot be higher or equal to the current count");
					else if (string.IsNullOrEmpty(value))
						throw new ArgumentNullException(nameof(value));

					_lines[index] = value;
				}
			}
		}

		/// <summary>
		/// Adds a new line
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public void Add(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			lock (SyncRoot)
			{
				_lines.Add(value);
			}
		}

		/// <summary>
		/// Gets <paramref name="count"/> lines starting at <paramref name="index"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public List<string> GetRange(int start, int count)
		{
			lock (SyncRoot)
			{
				if (start < 0)
					throw new ArgumentOutOfRangeException(nameof(start));
				else if (count <= 0)
					throw new ArgumentOutOfRangeException(nameof(count));

				if (start >= _lines.Count)
				{
					start = _lines.Count - 1;
					count = 1;
				}
				else if (start + count >= _lines.Count)
					count = _lines.Count - start;

				return _lines.GetRange(start, count);
			}
		}

		/// <summary>
		/// Converts <paramref name="count"/> lines starting at <paramref name="index"/> to a single string
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public string ToString(int start, int count)
		{
			List<string> lines = GetRange(start, count);

			if (lines.Count == 0)
				return string.Empty;

			StringBuilder sbuilder = new StringBuilder();

			for (int i = 0; i < lines.Count; i++)
				sbuilder.AppendLine(lines[i]);

			return sbuilder.ToString();
		}

		/// <summary>
		/// Removes <paramref name="count"/> lines starting at <paramref name="index"/>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void RemoveAt(int index, int count = 1)
		{
			lock (SyncRoot)
			{
				if (index < 0 || index >= _lines.Count)
					throw new ArgumentOutOfRangeException(nameof(index));
				else if (count == 0)
					throw new ArgumentOutOfRangeException(nameof(count));

				if (index + count >= _lines.Count)
					count = _lines.Count - index;

				for (int i = 0; i < count; i++)
					_lines.RemoveAt(index);
			}
		}
	}
}

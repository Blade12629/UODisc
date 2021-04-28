using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Server.Custom.Skyfly.UODisc.Embeds
{
	/// <summary>
	/// Threadsafe Embed Page Builder
	/// </summary>
	public class EmbedPageBuilder
	{
		public const int MAX_ROWS = 3;

		public object SyncRoot { get; }

		readonly Dictionary<string, EmbedFieldInfo> _fields;
		readonly int _maxRows;

		/// <summary>
		/// Threadsafe Embed Page Builder
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public EmbedPageBuilder(int maxRows)
		{
			SyncRoot = new object();

			lock (SyncRoot)
			{
				if (maxRows > MAX_ROWS)
					throw new ArgumentOutOfRangeException(nameof(maxRows), $"You can only have a max of {MAX_ROWS} rows!");
				else if (maxRows <= 0)
					throw new ArgumentOutOfRangeException(nameof(maxRows));

				_fields = new Dictionary<string, EmbedFieldInfo>();
				_maxRows = maxRows;
			}
		}

		/// <summary>
		/// Adds a column
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void AddColumn(string title, params string[] values)
		{
			if (string.IsNullOrEmpty(title))
				throw new ArgumentNullException(nameof(title));

			lock (SyncRoot)
			{
				if (_fields.ContainsKey(title))
					return;
				else if (_fields.Count == _maxRows)
					throw new InvalidOperationException("Cannot add any additional row");

				_fields.Add(title, values == null ? new EmbedFieldInfo() : new EmbedFieldInfo(values));
			}
		}

		/// <summary>
		/// Adds the <paramref name="value"/> to the column with the <paramref name="title"/>
		/// </summary>
		/// <param name="title"></param>
		/// <param name="value"></param>
		public void Add(string title, string value)
		{
			if (string.IsNullOrEmpty(title))
				throw new ArgumentNullException(nameof(title));
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			value = value.TrimEnd(' ').TrimStart(' ');
			title = title.TrimEnd(' ').TrimStart(' ');

			if (value.Length > 53)
			{
				for (int i = 53; i > 1; i--)
				{
					if (value[i] != ' ')
						continue;

					string firstPart = value.Substring(0, i);
					string secondPart = value.Remove(0, i);

					Add(title, firstPart);

					foreach (var field in _fields.Where(f => !f.Key.Equals(title, StringComparison.CurrentCulture)))
						_fields[field.Key].Add("‎");

					Add(title, secondPart);
					return;
				}
			}

			lock (SyncRoot)
			{
				_fields[title].Add(value);
			}
		}

		/// <summary>
		/// Gets a specific column
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public EmbedFieldInfo GetColumn(string title)
		{
			if (string.IsNullOrEmpty(title))
				throw new ArgumentNullException(nameof(title));

			lock (SyncRoot)
			{
				if (!_fields.ContainsKey(title))
					return null;

				return _fields[title];
			}
		}

		/// <summary>
		/// Gets a column for a page
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public DiscordEmbedField GetColumnForPage(string title, int page)
		{
			if (page <= 0)
				throw new ArgumentOutOfRangeException(nameof(page));
			else if (string.IsNullOrEmpty(title))
				throw new ArgumentNullException(nameof(title));

			lock (SyncRoot)
			{
				if (!_fields.ContainsKey(title))
					return null;

				EmbedFieldInfo efi = _fields[title];
				string column = efi.ToString((page - 1) * 10, 10);


				Type et = typeof(DiscordEmbedField);
				ConstructorInfo[] constructors = et.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
				ConstructorInfo constructor = constructors[0];

				DiscordEmbedField def = (DiscordEmbedField)constructor.Invoke(null);
				def.Name = title;
				def.Value = column;
				def.Inline = true;

				return def;
			}
		}

		/// <summary>
		/// Gets the amount of pages in total
		/// </summary>
		public int GetMaxPages()
		{
			lock (SyncRoot)
			{
				EmbedFieldInfo efi = _fields.Values.ElementAt(0);

				double pagesD = efi.Count / 10.0;
				int pages = (int)pagesD;

				if (pages < pagesD)
					pages++;

				return pages;
			}
		}

		/// <summary>
		/// Builds a specific page
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public DiscordEmbed BuildPage(DiscordEmbedBuilder builder, int page)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));
			else if (page <= 0)
				throw new ArgumentOutOfRangeException(nameof(page));

			string pageStr = $"Page {page}/{GetMaxPages()}";

			lock (SyncRoot)
			{
				foreach (var field in _fields)
					builder.AddField(field.Key, field.Value.ToString((page - 1) * 10, 10), true);

				if (builder.Footer == null)
				{
					builder.Footer = new DiscordEmbedBuilder.EmbedFooter
					{
						Text = ""
					};
				}

				builder.Footer.Text += builder.Footer.Text.Length > 0 ? " " + pageStr : pageStr;

				return builder.Build();
			}
		}

		/// <summary>
		/// Builds a specific page
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public DiscordEmbed BuildPage(int page)
		{
			return BuildPage(new DiscordEmbedBuilder(), page);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Server.Custom.Skyfly.UODisc.Commands
{
	public interface ICommand
	{
		bool IsDisabled { get; set; }
		/// <summary>
		/// Command Name
		/// </summary>
		string Command { get; }
		/// <summary>
		/// The default access level
		/// </summary>
		AccessLevel AccessLevel { get; }
		/// <summary>
		/// Public, private chat or both
		/// </summary>
		CommandType CommandType { get; }
		string Description { get; }
		/// <summary>
		/// How to use the command
		/// </summary>
		string Usage { get; }
		/// <summary>
		/// Amount of parameters atleast required
		/// </summary>
		int MinParameters { get; }

		void Invoke(CommandHandler handler, CommandEventArgs args);
	}
}

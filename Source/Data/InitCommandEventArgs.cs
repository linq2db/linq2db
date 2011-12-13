using System;
using System.Data;

namespace LinqToDB.Data
{
	public delegate void InitCommandEventHandler(object sender, InitCommandEventArgs ea);

	public class InitCommandEventArgs : EventArgs
	{
		private readonly IDbCommand _command;
		public           IDbCommand  Command
		{
			get { return _command; }
		}

		public InitCommandEventArgs(IDbCommand command)
		{
			_command = command;
		}
	}
}

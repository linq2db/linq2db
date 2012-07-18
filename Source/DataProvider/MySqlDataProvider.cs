// MySql Connector/Net
// http://dev.mysql.com/downloads/connector/net/
//
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using MySql.Data.MySqlClient;

namespace LinqToDB.DataProvider
{
	using Common;
	using SqlProvider;

	public class MySqlDataProvider :  DataProviderBaseOld
	{
		#region Static configuration

		public static char ParameterSymbol
		{
			get { return MySqlSqlProvider.ParameterSymbol;  }
			set { MySqlSqlProvider.ParameterSymbol = value; }
		}

		public static bool TryConvertParameterSymbol
		{
			get { return MySqlSqlProvider.TryConvertParameterSymbol;  }
			set { MySqlSqlProvider.TryConvertParameterSymbol = value; }
		}

		public  static string  CommandParameterPrefix
		{
			get { return MySqlSqlProvider.CommandParameterPrefix;  }
			set { MySqlSqlProvider.CommandParameterPrefix = value; }
		}

		public  static string  SprocParameterPrefix
		{
			get { return MySqlSqlProvider.SprocParameterPrefix;  }
			set { MySqlSqlProvider.SprocParameterPrefix = value; }
		}

		public  static List<char>  ConvertParameterSymbols
		{
			get { return MySqlSqlProvider.ConvertParameterSymbols;  }
			set { MySqlSqlProvider.ConvertParameterSymbols = value; }
		}

		public static void ConfigureOldStyle()
		{
			ParameterSymbol           = '?';
			ConvertParameterSymbols   = new List<char>(new[] { '@' });
			TryConvertParameterSymbol = true;
		}

		public static void ConfigureNewStyle()
		{
			ParameterSymbol           = '@';
			ConvertParameterSymbols   = null;
			TryConvertParameterSymbol = false;
		}

		static MySqlDataProvider()
		{
			ConfigureOldStyle();
		}
		
		#endregion

		public override IDbConnection CreateConnectionObject()
		{
			return new MySqlConnection();
		}

		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new MySqlDataAdapter();
		}

		private void ConvertParameterNames(IDbCommand command)
		{
			foreach (IDataParameter p in command.Parameters)
			{
				if (p.ParameterName[0] != ParameterSymbol)
					p.ParameterName =
						Convert(
							Convert(p.ParameterName, ConvertType.SprocParameterToName),
							command.CommandType == CommandType.StoredProcedure ? ConvertType.NameToSprocParameter : ConvertType.NameToCommandParameter).ToString();
			}
		}

		public override bool DeriveParameters(IDbCommand command)
		{
			if (command is MySqlCommand)
			{
				MySqlCommandBuilder.DeriveParameters((MySqlCommand)command);

				if (TryConvertParameterSymbol && ConvertParameterSymbols.Count > 0)
					ConvertParameterNames(command);

				return true;
			}

			return false;
		}

		public override IDbDataParameter GetParameter(
			IDbCommand command,
			NameOrIndexParameter nameOrIndex)
		{
			if (nameOrIndex.ByName)
			{
				// if we have a stored procedure, then maybe command paramaters were formatted
				// (SprocParameterPrefix added). In this case we need to format given parameter name first
				// and only then try to take parameter by formatted parameter name
				string parameterName = command.CommandType == CommandType.StoredProcedure
					? Convert(nameOrIndex.Name, ConvertType.NameToSprocParameter).ToString()
					: nameOrIndex.Name;

				return (IDbDataParameter)(command.Parameters[parameterName]);
			}
			return (IDbDataParameter)(command.Parameters[nameOrIndex.Index]);
		}

		public override object Convert(object value, ConvertType convertType)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is MySqlException)
						return ((MySqlException)value).Number;
					break;

				case ConvertType.ExceptionToErrorMessage:
					if (value is MySqlException)
						return ((MySqlException)value).Message;
					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override Type ConnectionType
		{
			get { return typeof(MySqlConnection); }
		}

		public override string Name
		{
			get { return LinqToDB.ProviderName.MySql; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MySqlSqlProvider();
		}

		public override void Configure(System.Collections.Specialized.NameValueCollection attributes)
		{
			string paremeterPrefix = attributes["ParameterPrefix"];
			if (paremeterPrefix != null)
				CommandParameterPrefix = SprocParameterPrefix = paremeterPrefix;

			paremeterPrefix = attributes["CommandParameterPrefix"];
			if (paremeterPrefix != null)
				CommandParameterPrefix = paremeterPrefix;

			paremeterPrefix = attributes["SprocParameterPrefix"];
			if (paremeterPrefix != null)
				SprocParameterPrefix = paremeterPrefix;

			string configName = attributes["ParameterSymbolConfig"];
			if (configName != null)
			{
				switch (configName)
				{
					case "OldStyle":
						ConfigureOldStyle();
						break;
					case "NewStyle":
						ConfigureNewStyle();
						break;
				}
			}

			string parameterSymbol = attributes["ParameterSymbol"];
			if (parameterSymbol != null && parameterSymbol.Length == 1)
				ParameterSymbol = parameterSymbol[0];

			string convertParameterSymbols = attributes["ConvertParameterSymbols"];
			if (convertParameterSymbols != null)
				ConvertParameterSymbols = new List<char>(convertParameterSymbols.ToCharArray());

			string tryConvertParameterSymbol = attributes["TryConvertParameterSymbol"];
			if (tryConvertParameterSymbol != null)
				TryConvertParameterSymbol = LinqToDB.Common.ConvertOld.ToBoolean(tryConvertParameterSymbol);

			base.Configure(attributes);
		}
	}
}

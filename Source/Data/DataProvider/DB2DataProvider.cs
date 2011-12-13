using System;
using System.Data;
using System.Data.Common;

using IBM.Data.DB2;

namespace LinqToDB.Data.DataProvider
{
	using Sql.SqlProvider;

	class DB2DataProvider :  DataProviderBase
	{
		public override IDbConnection CreateConnectionObject () { return new DB2Connection (); }
		public override DbDataAdapter CreateDataAdapterObject() { return new DB2DataAdapter(); }
		public override ISqlProvider  CreateSqlProvider      () { return new DB2SqlProvider(); }

		public override Type   ConnectionType { get { return typeof(DB2Connection);         } }
		public override string Name           { get { return DataProvider.ProviderName.DB2; } }
		public override string EndOfSql       { get { return ";"; } }

		public override bool DeriveParameters(IDbCommand command)
		{
			if (command is DB2Command)
			{
				DB2CommandBuilder.DeriveParameters((DB2Command)command);
				return true;
			}

			return false;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is DB2Exception)
					{
						var ex = (DB2Exception)value;

						foreach (DB2Error error in ex.Errors)
							return error.RowNumber;

						return 0;
					}

					break;
				 
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);

			if (commandParameters != null) foreach (var p in commandParameters)
			{
				if (p.Value is bool)
					p.Value = (bool)p.Value ? 1 : 0;
				else if (p.Value is Guid)
				{
					p.Value  = ((Guid)p.Value).ToByteArray();
					p.DbType = DbType.Binary;
					p.Size   = 16;
				}
			}
		}
	}
}

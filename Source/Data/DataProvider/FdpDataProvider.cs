/***

 * FdpDataProvider
needed FirebirdClient http://sourceforge.net/project/showfiles.php?group_id=9028&package_id=62107
tested with FirebirdClient 2.1.0 Beta 3

Known troubles:
1) Some tests fails due to Fb SQL-syntax specific
2) ResultSet mapping doesn't work - not supported by client
3) UnitTests.CS.DataAccess.OutRefTest tests: Test2 && TestNullable2 doesnt work:
	parameters directions should be provided correctly to functions run, that's why
	output parameterd would be mapped to Entity e, so asserts should be same a in Test1.

"Features"
1) Type conversation due to http://www.firebirdsql.org/manual/migration-mssql-data-types.html
	BUT! for Binary types BLOB is used! not CHAR!
2) InOut parameters faking: InOut parameters are not suppotred by Fb, but they could be
	emulated: each InOut parameter should be defined in RETURNS() section, and allso has a mirror 
	in parameter section with name [prefix][inOutParameterName], see OutRefTest SP. Faking settings:
	FdpDataProvider.InOutInputParameterPrefix = "in_";
	FdpDataProvider.IsInOutParameterEmulation = true;
3) Returned values faking. Each parameter with "magic name" woul be treated as ReturnValue.
	see Scalar_ReturnParameter SP. Faking settings:
	FdpDataProvider.ReturnParameterName = "RETURN_VALUE";
	FdpDataProvider.IsReturnValueEmulation = true;

 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Linq;

using LinqToDB.Data.Sql.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

using FirebirdSql.Data.FirebirdClient;

namespace LinqToDB.Data.DataProvider
{
	public class FdpDataProvider : DataProviderBase
	{
		#region InOut & ReturnValue emulation
		public static string InOutInputParameterPrefix = "in_";
		public static string ReturnParameterName = "RETURN_VALUE";

		public static bool IsReturnValueEmulation = true;
		public static bool IsInOutParameterEmulation = true;

		public static bool QuoteIdentifiers
		{
			get { return FirebirdSqlProvider.QuoteIdentifiers; }
			set { FirebirdSqlProvider.QuoteIdentifiers = value; }
		}
		#endregion

		#region Overloads
		public override Type ConnectionType
		{
			get { return typeof (FbConnection); }
		}

		public override string Name
		{
			get { return DataProvider.ProviderName.Firebird; }
		}

		public override int MaxBatchSize
		{
			get { return 0; }
		}

		public override IDbConnection CreateConnectionObject()
		{
			return new FbConnection();
		}

		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new FbDataAdapter();
		}

		public override bool DeriveParameters(IDbCommand command)
		{
			if (command is FbCommand)
			{
				FbCommandBuilder.DeriveParameters((FbCommand) command);

				if (IsReturnValueEmulation)
					foreach (IDbDataParameter par in command.Parameters)
						if (IsReturnValue(par))
							par.Direction = ParameterDirection.ReturnValue;

				return true;
			}

			return false;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is FbException)
					{
						var ex = (FbException) value;
						if (ex.Errors.Count > 0)
							return ex.Errors[0].Number;
					}

					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new FirebirdSqlProvider();
		}

		public override bool IsValueParameter(IDbDataParameter parameter)
		{
			return parameter.Direction != ParameterDirection.ReturnValue
			       && parameter.Direction != ParameterDirection.Output;
		}

		private string GetInputParameterName(string ioParameterName)
		{
			return (string) Convert(
				InOutInputParameterPrefix + (string) Convert(ioParameterName, ConvertType.SprocParameterToName),
				ConvertType.NameToSprocParameter);
		}

		private static IDbDataParameter GetParameter(string parameterName, IEnumerable<IDbDataParameter> commandParameters)
		{
			return commandParameters.FirstOrDefault(par => string.Compare(parameterName, par.ParameterName, true) == 0);
		}

		private bool IsReturnValue(IDbDataParameter parameter)
		{
			if (string.Compare(parameter.ParameterName,
			                   (string) Convert(ReturnParameterName, ConvertType.NameToSprocParameter), true) == 0
				)
				return true;

			return false;
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText,
		                                    ref IDbDataParameter[] commandParameters)
		{
			if (commandParameters != null)
				foreach (var par in commandParameters)
				{
					if (par.Value is bool)
					{
						var value = (bool) par.Value ? "1" : "0";

						par.DbType = DbType.AnsiString;
						par.Value = value;
						par.Size = value.Length;
					}
					else if (par.Value is Guid)
					{
						var value = par.Value.ToString();

						par.DbType = DbType.AnsiStringFixedLength;
						par.Value = value;
						par.Size = value.Length;
					}

					#region "smart" input-output parameter detection
					if (commandType == CommandType.StoredProcedure && IsInOutParameterEmulation)
					{
						var iParameterName = GetInputParameterName(par.ParameterName);
						var fakeIOParameter = GetParameter(iParameterName, commandParameters);

						if (fakeIOParameter != null)
						{
							fakeIOParameter.Value = par.Value;

							// direction should be output, or parameter mistmath for procedure exception
							// would be thrown
							par.Direction = ParameterDirection.Output;

							// direction should be Input
							fakeIOParameter.Direction = ParameterDirection.Input;
						}
					}
					#endregion
				}

			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);
		}

		public override bool InitParameter(IDbDataParameter parameter)
		{
			if (parameter.Value is bool)
			{
				var value = (bool) parameter.Value ? "1" : "0";

				parameter.DbType = DbType.AnsiString;
				parameter.Value = value;
				parameter.Size = value.Length;
			}
			else if (parameter.Value is Guid)
			{
				var value = parameter.Value.ToString();

				parameter.DbType = DbType.AnsiStringFixedLength;
				parameter.Value = value;
				parameter.Size = value.Length;
			}

			return base.InitParameter(parameter);
		}

		public override void Configure(NameValueCollection attributes)
		{
			var inOutInputParameterPrefix = attributes["InOutInputParameterPrefix"];
			if (inOutInputParameterPrefix != null)
				InOutInputParameterPrefix = inOutInputParameterPrefix;

			var returnParameterName = attributes["ReturnParameterName"];
			if (returnParameterName != null)
				ReturnParameterName = returnParameterName;

			var isReturnValueEmulation = attributes["IsReturnValueEmulation"];
			if (isReturnValueEmulation != null)
				IsReturnValueEmulation = Common.Convert.ToBoolean(isReturnValueEmulation);

			var isInOutParameterEmulation = attributes["IsInOutParameterEmulation"];
			if (isInOutParameterEmulation != null)
				IsInOutParameterEmulation = Common.Convert.ToBoolean(isInOutParameterEmulation);

			var quoteIdentifiers = attributes["QuoteIdentifiers"];
			if (quoteIdentifiers != null)
				QuoteIdentifiers = Common.Convert.ToBoolean(quoteIdentifiers);

			base.Configure(attributes);
		}
		#endregion

		#region FbDataReaderEx
		public override IDataReader GetDataReader(MappingSchema schema, IDataReader dataReader)
		{
			return
				dataReader is FbDataReader
					? new FbDataReaderEx((FbDataReader) dataReader)
					: base.GetDataReader(schema, dataReader);
		}

		private class FbDataReaderEx : DataReaderBase<FbDataReader>, IDataReader
		{
			public FbDataReaderEx(FbDataReader rd) : base(rd)
			{
			}

			#region IDataReader Members
			public new object GetValue(int i)
			{
				var value = DataReader.GetValue(i);

				if (value is DateTime)
				{
					var dt = (DateTime) value;

					if (dt.Year == 1970 && dt.Month == 1 && dt.Day == 1)
						return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
				}

				return value;
			}

			public new DateTime GetDateTime(int i)
			{
				var dt = DataReader.GetDateTime(i);

				if (dt.Year == 1970 && dt.Month == 1 && dt.Day == 1)
					return new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);

				return dt;
			}
			#endregion
		}
		#endregion
	}
}
using System;
using System.Data;
using System.Data.Common;
using System.Data.OracleClient;
using System.Globalization;

namespace BLToolkit.Data.DataProvider
{
	using Sql.SqlProvider;

#if FW4
	[Obsolete("OracleDataProvider has been deprecated. http://go.microsoft.com/fwlink/?LinkID=144260")]
#pragma warning disable 0618
#endif
	/// <summary>
	/// Implements access to the Data Provider for Oracle.
	/// </summary>
	/// <remarks>
	/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
	/// </remarks>
	/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
	public sealed class OracleDataProvider : DataProviderBase
	{
		private string _parameterPrefix = "P";
		public  string  ParameterPrefix
		{
			get { return _parameterPrefix; }
			set
			{
				_parameterPrefix = string.IsNullOrEmpty(value)? null:
					value.ToUpper(CultureInfo.InvariantCulture);
			}
		}

		/// <summary>
		/// Creates the database connection object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>The database connection object.</returns>
		public override IDbConnection CreateConnectionObject()
		{
			return new OracleConnection();
		}

		/// <summary>
		/// Creates the data adapter object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>A data adapter object.</returns>
		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new OracleDataAdapter();
		}

		/// <summary>
		/// Populates the specified <see cref="IDbCommand"/> object's Parameters collection with 
		/// parameter information for the stored procedure specified in the <see cref="IDbCommand"/>.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <param name="command">The <see cref="IDbCommand"/> referencing the stored procedure for which the parameter information is to be derived. The derived parameters will be populated into the Parameters of this command.</param>
		public override bool DeriveParameters(IDbCommand command)
		{
			OracleCommandBuilder.DeriveParameters((OracleCommand)command);
			return true;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return ParameterPrefix == null? value: ParameterPrefix + value;

				case ConvertType.SprocParameterToName:
					var name = (string)value;

					if (name.Length > 0)
					{
						if (name[0] == ':')
							return name.Substring(1);

						if (ParameterPrefix != null &&
							name.ToUpper(CultureInfo.InvariantCulture).StartsWith(ParameterPrefix))
						{
							return name.Substring(ParameterPrefix.Length);
						}
					}

					break;

				case ConvertType.ExceptionToErrorNumber:
					if (value is OracleException)
						return ((OracleException)value).Code;
					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		/// <summary>
		/// Returns connection type.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <value>An instance of the <see cref="Type"/> class.</value>
		public override Type ConnectionType
		{
			get { return typeof(OracleConnection); }
		}

		public const string NameString = "Oracle";

		/// <summary>
		/// Returns the data provider name.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataProvider Method</seealso>
		/// <value>Data provider name.</value>
		public override string Name
		{
			get { return NameString; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new OracleSqlProvider();
		}
	}

#if FW4
#pragma warning restore 0618
#endif
}

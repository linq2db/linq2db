using System;
using System.Data;
using System.Data.OleDb;

namespace BLToolkit.Data.DataProvider
{
	// Data Provider for DataDirect Sybase ADO Provider 4.2
	//
	public sealed class SybaseAdoDataProvider : OleDbDataProvider
	{
		public override bool DeriveParameters(IDbCommand command)
		{
			OleDbCommandBuilder.DeriveParameters((OleDbCommand)command);
			return true;
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return "?";

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return value;
			}

			return base.Convert(value, convertType);
		}

		public override void AttachParameter(IDbCommand command, IDbDataParameter parameter)
		{
			if (parameter.Value is string && parameter.DbType == DbType.Guid)
				parameter.DbType = DbType.AnsiString;

			base.AttachParameter(command, parameter);
		}

		public override string Name
		{
			get { return "SybaseAdo"; }
		}
	}
}

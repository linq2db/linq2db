using System;
using System.Data;

namespace LinqToDB.DataProvider.Sybase
{
	internal class SybaseProviderMethods
	{
		public Action<IDbDataParameter> SetUInt16;
		public Action<IDbDataParameter> SetUInt32;
		public Action<IDbDataParameter> SetUInt64;
		public Action<IDbDataParameter> SetText;
		public Action<IDbDataParameter> SetNText;
		public Action<IDbDataParameter> SetBinary;
		public Action<IDbDataParameter> SetVarBinary;
		public Action<IDbDataParameter> SetImage;
		public Action<IDbDataParameter> SetMoney;
		public Action<IDbDataParameter> SetSmallMoney;
		public Action<IDbDataParameter> SetDate;
		public Action<IDbDataParameter> SetTime;
		public Action<IDbDataParameter> SetSmallDateTime;
		public Action<IDbDataParameter> SetTimestamp;
	}
}

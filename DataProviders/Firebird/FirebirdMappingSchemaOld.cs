using System;
using System.Data.SqlTypes;
using System.IO;

namespace LinqToDB.DataProvider
{
	using Mapping;

	class FirebirdMappingSchemaOld : MappingSchemaOld
	{
		public byte[] ConvertToByteArray(string value)
		{
			return System.Text.Encoding.UTF8.GetBytes(value);
		}

		public bool ConvertToBoolean(string value)
		{
			if (value.Length == 1)
				switch (value[0])
				{
					case '1': case 'T' :case 'Y' : case 't': case 'y': return true;
					case '0': case 'F' :case 'N' : case 'f': case 'n': return false;
				}

			return Common.ConvertTo<bool>.From(value);
		}

		public Stream ConvertToStream(string value)
		{
			return new System.IO.MemoryStream(ConvertToByteArray(value));
		}


#if !SILVERLIGHT

		public SqlBinary ConvertToSqlBinary(string value)
		{
			return Common.ConvertTo<SqlBinary>.From(ConvertToByteArray(value));
		}


		public SqlBytes ConvertToSqlBytes(string value)
		{
			return Common.ConvertTo<SqlBytes>.From(ConvertToByteArray(value));
		}

#endif
	}
}

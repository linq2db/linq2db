using System;

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

			return Common.ConvertOld.ToBoolean(value);
		}

		public System.IO.Stream ConvertToStream(string value)
		{
			return new System.IO.MemoryStream(ConvertToByteArray(value));
		}


#if !SILVERLIGHT

		public System.Data.SqlTypes.SqlBinary ConvertToSqlBinary(string value)
		{
			return Common.ConvertOld.ToSqlBinary(ConvertToByteArray(value));
		}


		public System.Data.SqlTypes.SqlBytes ConvertToSqlBytes(string value)
		{
			return Common.ConvertOld.ToSqlBytes(ConvertToByteArray(value));
		}

#endif
	}
}

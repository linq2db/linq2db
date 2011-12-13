using System;
using System.Linq;
using LinqToDB.Common;
using LinqToDB.Properties;

namespace LinqToDB.Mapping
{
	public class MapIndex
	{
		public MapIndex(string[] names)
		{
			if (null == names)
				throw new ArgumentNullException("names");

			if (names.Length == 0)
				throw new ArgumentException(Resources.MapIndex_EmptyNames, "names");

			Fields = NameOrIndexParameter.FromStringArray(names);
		}

		public MapIndex(int[] indices)
		{
			if (null == indices)
				throw new ArgumentNullException("indices");

			if (indices.Length == 0)
				throw new ArgumentException(Resources.MapIndex_EmptyIndices, "indices");

			Fields = NameOrIndexParameter.FromIndexArray(indices);
		}
		
		public MapIndex(params NameOrIndexParameter[] fields)
		{
			if (null == fields)
				throw new ArgumentNullException("fields");

			if (fields.Length == 0)
				throw new ArgumentException(Resources.MapIndex_EmptyFields, "fields");
			
			Fields = fields;
		}

		public NameOrIndexParameter[] Fields { get; private set; }

		private string _id;
		public  string  ID
		{
			get { return _id ?? (_id = string.Join(".", Fields.Select(_ => _.ToString()).ToArray())); }
		}

		[CLSCompliant(false)]
		public object GetValue(IMapDataSource source, object obj, int index)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var value = Fields[index].ByName?
				source.GetValue(obj, Fields[index].Name):
				source.GetValue(obj, Fields[index].Index);

			if (value == null)
			{
				var objectMapper = source as ObjectMapper;

				if (objectMapper != null)
				{
					var mm = Fields[index].ByName?
						objectMapper[Fields[index].Name]: objectMapper[Fields[index].Index];

					if (mm == null)
						throw new MappingException(string.Format(Resources.MapIndex_BadField,
							objectMapper.TypeAccessor.OriginalType.Name, Fields[index]));
				}
			}

			return value;
		}

		[CLSCompliant(false)]
		public object GetValueOrIndex(IMapDataSource source, object obj)
		{
			return Fields.Length == 1?
				GetValue(source, obj, 0):
				GetIndexValue(source, obj);
		}

		[CLSCompliant(false)]
		public CompoundValue GetIndexValue(IMapDataSource source, object obj)
		{
			var values = new object[Fields.Length];

			for (var i = 0; i < values.Length; i++)
				values[i] = GetValue(source, obj, i);

			return new CompoundValue(values);
		}
	}
}


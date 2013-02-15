namespace LinqToDB.Mapping
{
	public class MapValue
	{
		public MapValue(object origValue, params MapValueAttribute[] mapValues)
		{
			OrigValue = origValue;
			MapValues = mapValues;
		}

		public object              OrigValue { get; private set; }
		public MapValueAttribute[] MapValues { get; private set; }
	}
}

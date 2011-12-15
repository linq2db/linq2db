using System;

namespace LinqToDB
{
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property |
		AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple=true)]
	public sealed class MapFieldAttribute : Attribute
	{
		public MapFieldAttribute()
		{
		}

		public MapFieldAttribute(string mapName)
		{
			MapName = mapName;
		}

		public MapFieldAttribute(string mapName, string origName)
		{
			MapName  = mapName;
			OrigName = origName;
		}

		public string MapName                    { get; set; }
		public string OrigName                   { get; set; }
		public string Format                     { get; set; }
		public string Storage                    { get; set; }
		public bool   IsInheritanceDiscriminator { get; set; }
	}
}

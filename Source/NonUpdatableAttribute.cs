using System;

namespace LinqToDB
{
	[Serializable]
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property |
		AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true)]
	public class NonUpdatableAttribute : Attribute
	{
		public NonUpdatableAttribute()
			: this(true, true, false)
		{
		}

		public NonUpdatableAttribute(bool onInsert, bool onUpdate, bool isIdentity)
		{
			OnInsert   = onInsert;
			OnUpdate   = onUpdate;
			IsIdentity = isIdentity;
		}

		public bool   OnInsert   { get; set; }
		public bool   OnUpdate   { get; set; }
		public bool   IsIdentity { get; set; }
		public string FieldName  { get; set; }
	}
}

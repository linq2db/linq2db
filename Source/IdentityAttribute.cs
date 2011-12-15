using System;

namespace LinqToDB
{
	[Serializable]
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property |
		AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true)]
	public class IdentityAttribute : NonUpdatableAttribute
	{
		public IdentityAttribute() : base(true, true, true)
		{
		}
	}
}

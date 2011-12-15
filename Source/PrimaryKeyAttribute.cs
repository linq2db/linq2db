using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class PrimaryKeyAttribute : Attribute
	{
		public PrimaryKeyAttribute()
		{
			_order = -1;
		}

		public PrimaryKeyAttribute(int order)
		{
			_order = order;
		}

		private readonly int _order;
		public           int  Order
		{
			get { return _order; }
		}
	}
}

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Generator;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class SelectClause : BaseClause, IQuerySource
	{
		public Expression Selector { get; set; }

		public SelectClause([JetBrains.Annotations.NotNull] Type itemType, [JetBrains.Annotations.NotNull] string itemName, [JetBrains.Annotations.NotNull] Expression selector)
		{
			Selector = selector ?? throw new ArgumentNullException(nameof(selector));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public SelectClause([JetBrains.Annotations.NotNull] Expression selector) : this(selector.Type, "", selector)
		{
			
		}

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}

		public int QuerySourceId { get; }
		public Type ItemType { get; }
		public string ItemName { get; }

		private MemberInfo[] _members;

		public bool DoesContainMember(MemberInfo memberInfo, MappingSchema mappingSchema)
		{
			if (_members == null)
				_members = GeneratorHelper.GetMemberMapping(Selector, mappingSchema).Select(t => t.Item1).ToArray();

			return _members.Any(m => Equals(m, memberInfo));
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression ma)
		{
			throw new NotImplementedException();
		}
	}
}

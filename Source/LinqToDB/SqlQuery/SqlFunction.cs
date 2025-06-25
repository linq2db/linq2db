using System;
using System.Linq;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public class SqlFunction : SqlExpressionBase
	{
		public SqlFunction(DbDataType dbDataType, string name, params ISqlExpression[] parameters)
			: this(dbDataType, name, false, true, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(DbDataType dbDataType, string name, ParametersNullabilityType parametersNullability, params ISqlExpression[] parameters)
			: this(dbDataType, name, false, true, SqlQuery.Precedence.Primary, parametersNullability, null, parameters)
		{
		}

		public SqlFunction(DbDataType dbDataType, string name, bool isAggregate, bool isPure, params ISqlExpression[] parameters)
			: this(dbDataType, name, isAggregate, isPure, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(DbDataType dbDataType, string name, bool isAggregate, params ISqlExpression[] parameters)
			: this(dbDataType, name, isAggregate, true, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(DbDataType dbDataType, string name, bool isAggregate, int precedence, params ISqlExpression[] parameters)
			: this(dbDataType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(DbDataType dbDataType, string name, bool isAggregate, bool isPure, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull, params ISqlExpression[] parameters) 
		{
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var p in parameters)
				if (p == null) throw new ArgumentNullException(nameof(parameters));

			Type            = dbDataType;
			Name            = name;
			Precedence      = precedence;
			NullabilityType = nullabilityType;
			_canBeNull      = canBeNull;
			FunctionFlags = (isAggregate ? SqlFlags.IsAggregate : SqlFlags.None) |
			                (isPure ? SqlFlags.IsPure : SqlFlags.None);
			Parameters      = parameters;
		}

		public SqlFunction(Type systemType, string name, params ISqlExpression[] parameters)
			: this(systemType, name, false, true, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, ParametersNullabilityType nullabilityType, params ISqlExpression[] parameters)
			: this(systemType, name, false, true, SqlQuery.Precedence.Primary, nullabilityType, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, bool isPure, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, isPure, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, true, SqlQuery.Precedence.Primary, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, int precedence, params ISqlExpression[] parameters)
			: this(systemType, name, isAggregate, true, precedence, ParametersNullabilityType.IfAnyParameterNullable, null, parameters)
		{
		}

		public SqlFunction(Type systemType, string name, bool isAggregate, bool isPure, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull, params ISqlExpression[] parameters)
		: this(new DbDataType(systemType), name, isAggregate, isPure, precedence, nullabilityType, canBeNull, parameters)
		{
		}

		public          DbDataType                Type              { get; }
		public override Type                      SystemType        => Type.SystemType;
		public          string                    Name              { get; }
		public override int                       Precedence        { get; }
		public          SqlFlags                  FunctionFlags     { get; }
		public          bool                      IsAggregate       => (FunctionFlags & SqlFlags.IsAggregate) != 0;
		public          bool                      IsPure            => (FunctionFlags & SqlFlags.IsPure)      != 0;
		public          ISqlExpression[]          Parameters        { get; }
		public          bool?                     CanBeNullNullable => _canBeNull;
		public          ParametersNullabilityType NullabilityType   { get; }

		public bool DoNotOptimize { get; set; }

		public static SqlFunction CreateCount(Type type, ISqlTableSource table)
		{
			return new SqlFunction(type, "COUNT", true, true, SqlQuery.Precedence.Primary,
				ParametersNullabilityType.NotNullable, null, new SqlExpression("*", new SqlValue(table.SourceID)));
		}

		public SqlFunction WithName(string name)
		{
			if (name == Name)
				return this;
			return new SqlFunction(SystemType, name, IsAggregate, IsPure, Precedence, NullabilityType, _canBeNull, Parameters);
		}

		public SqlFunction WithParameters(ISqlExpression[] parameters)
		{
			return new SqlFunction(SystemType, Name, IsAggregate, IsPure, Precedence, NullabilityType, _canBeNull, parameters);
		}

		#region Overrides

		#endregion

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return QueryHelper.CalcCanBeNull(SystemType, _canBeNull, NullabilityType,
				Parameters.Select(p => p.CanBeNullable(nullability)));
		}

		bool?       _canBeNull;
		public  bool   CanBeNull
		{
			get => _canBeNull ?? NullabilityType != ParametersNullabilityType.NotNullable;
			set => _canBeNull = value;
		}

		#endregion

		#region Equals Members

		int? _hashCode;

		public override int GetHashCode()
		{
			// ReSharper disable NonReadonlyMemberInGetHashCode
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = SystemType.GetHashCode();

			hashCode = unchecked(hashCode + (hashCode * 397) ^ Name.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ CanBeNull.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ DoNotOptimize.GetHashCode());
			for (var i = 0; i < Parameters.Length; i++)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Parameters[i].GetHashCode());

			_hashCode = hashCode;
			return hashCode;
			// ReSharper restore NonReadonlyMemberInGetHashCode
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (!(other is SqlFunction func) || Name != func.Name || Parameters.Length != func.Parameters.Length || SystemType != func.SystemType)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(func.Parameters[i], comparer))
					return false;

			return comparer(this, func);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlFunction;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);

			writer
				.Append(Name)
				.Append('(');

			var indent = false;
			// Handling Exists
			if (Parameters.Length == 1 && Parameters[0] is SelectQuery)
			{
				writer.AppendLine();
				writer.Indent();
				indent = true;
			}

			for (var index = 0; index < Parameters.Length; index++)
			{
				var p = Parameters[index];
				p.ToString(writer);
				if (index < Parameters.Length - 1)
					writer.Append(", ");
			}

			if (indent)
			{
				writer.AppendLine();
				writer.UnIndent();
			}

			writer.Append(')');

			if (CanBeNullable(writer.Nullability))
				writer.Append("?");

			return writer;
		}

		#endregion

		public void Deconstruct(out Type systemType, out string name)
		{
			systemType = SystemType;
			name       = Name;
		}

		public void Deconstruct(out string name)
		{
			name = Name;
		}

		public void Deconstruct(out Type systemType, out string name, out ISqlExpression[] parameters)
		{
			systemType = SystemType;
			name       = Name;
			parameters = Parameters;
		}
	}
}

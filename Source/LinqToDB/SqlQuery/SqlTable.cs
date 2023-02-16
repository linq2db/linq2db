﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Common.Internal;
	using Data;
	using Mapping;
	using Remote;

	public class SqlTable : ISqlTableSource, IQueryExtendible
	{
		#region Init

		protected internal SqlTable(Type objectType, int? sourceId, SqlObjectName tableName)
		{
			SourceID   = sourceId ?? Interlocked.Increment(ref SelectQuery.SourceIDCounter);
			ObjectType = objectType;
			TableName  = tableName;
		}

		internal SqlTable(
			int                      id,
			string?                  expression,
			string                   alias,
			SqlObjectName            tableName,
			Type                     objectType,
			SequenceNameAttribute[]? sequenceAttributes,
			IEnumerable<SqlField>    fields,
			SqlTableType             sqlTableType,
			ISqlExpression[]?        tableArguments,
			TableOptions             tableOptions,
			string?                  tableID)
			: this(objectType, id, tableName)
		{
			Expression         = expression;
			Alias              = alias;
			SequenceAttributes = sequenceAttributes;
			ID                 = tableID;

			AddRange(fields);

			SqlTableType   = sqlTableType;
			TableArguments = tableArguments;
			TableOptions   = tableOptions;
		}

		#endregion

		#region Init from type

		public SqlTable(EntityDescriptor entityDescriptor, string? physicalName = null)
			: this(entityDescriptor.ObjectType, (int?)null, new(string.Empty))
		{
			TableName    = physicalName != null && entityDescriptor.Name.Name != physicalName ? entityDescriptor.Name with { Name = physicalName } : entityDescriptor.Name;
			TableOptions = entityDescriptor.TableOptions;

			foreach (var column in entityDescriptor.Columns)
			{
				var field = new SqlField(column);

				Add(field);

				if (field.Type.DataType == DataType.Undefined)
				{
					var dataType = entityDescriptor.MappingSchema.GetDataType(field.Type.SystemType);

					if (dataType.Type.DataType == DataType.Undefined)
					{
						dataType = entityDescriptor.MappingSchema.GetUnderlyingDataType(field.Type.SystemType, out var canBeNull);

						if (canBeNull)
							field.CanBeNull = true;
					}

					field.Type = field.Type.WithDataType(dataType.Type.DataType);

					// try to get type from converter
					if (field.Type.DataType == DataType.Undefined)
					{
						try
						{
							var converter = entityDescriptor.MappingSchema.GetConverter(
								field.Type,
								new DbDataType(typeof(DataParameter)), true);

							var parameter = converter?.ConvertValueToParameter?.Invoke(DefaultValue.GetValue(field.Type.SystemType, entityDescriptor.MappingSchema));
							if (parameter != null)
								field.Type = field.Type.WithDataType(parameter.DataType);
						}
						catch
						{
							// converter cannot handle default value?
						}
					}

					if (field.Type.Length    == null) field.Type = field.Type.WithLength   (dataType.Type.Length);
					if (field.Type.Precision == null) field.Type = field.Type.WithPrecision(dataType.Type.Precision);
					if (field.Type.Scale     == null) field.Type = field.Type.WithScale    (dataType.Type.Scale);
				}
			}

			var identityField = GetIdentityField();

			if (identityField != null)
			{
				var cd = entityDescriptor[identityField.Name]!;
				SequenceAttributes = cd.SequenceName == null ? null : new[] { cd.SequenceName };
			}
		}

		#endregion

		#region Init from Table

		public SqlTable(SqlTable table)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			SequenceAttributes = table.SequenceAttributes;

			foreach (var field in table.Fields)
				Add(new SqlField(field));

			SqlTableType       = table.SqlTableType;
			SqlQueryExtensions = table.SqlQueryExtensions;

			Expression         = table.Expression;
			TableArguments     = table.TableArguments;
		}

		public SqlTable(SqlTable table, IEnumerable<SqlField> fields, ISqlExpression[] tableArguments)
			: this(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			Expression         = table.Expression;
			SequenceAttributes = table.SequenceAttributes;
			TableOptions       = table.TableOptions;

			AddRange(fields);

			SqlTableType       = table.SqlTableType;
			TableArguments     = tableArguments;
			SqlQueryExtensions = table.SqlQueryExtensions;
		}

		#endregion

		#region Overrides

		public override string ToString()
		{
			using var sb = Pools.StringBuilder.Allocate();
			return ((IQueryElement)this).ToString(sb.Value, new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

		#endregion

		#region Public Members

		/// <summary>
		/// Search for table field by mapping class member name.
		/// </summary>
		/// <param name="memberName">Mapping class member name.</param>
		public SqlField? FindFieldByMemberName(string memberName)
		{
			_fieldsLookup.TryGetValue(memberName, out var field);
			return field;
		}

		public         string?           Alias          { get; set; }
		public virtual SqlObjectName     TableName      { get; set; }
		public         Type              ObjectType     { get; protected internal set; }
		public virtual SqlTableType      SqlTableType   { get; set; }
		public         TableOptions      TableOptions   { get; set; }
		public virtual string?           ID             { get; set; }

		/// <summary>
		/// Custom SQL expression format string (used together with <see cref="TableArguments"/>) to
		/// transform <see cref="SqlTable"/> to custom table expression.
		/// Arguments:
		/// <list type="bullet">
		/// <item>{0}: <see cref="TableName"/></item>
		/// <item>{1}: <see cref="Alias"/></item>
		/// <item>{2+}: arguments from <see cref="TableArguments"/> (with index adjusted by 2)</item>
		/// </list>
		/// </summary>
		public string?           Expression     { get; set; }
		public ISqlExpression[]? TableArguments { get; set; }

		internal string NameForLogging => Expression ?? TableName.Name;

		// list user to preserve order of fields in queries
		readonly List<SqlField>              _orderedFields = new();
		readonly Dictionary<string,SqlField> _fieldsLookup  = new();

		public           IReadOnlyList<SqlField>         Fields => _orderedFields;
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }

		// identity fields cached, as it is most used fields filter
		private readonly List<SqlField>                  _identityFields = new ();
		public IReadOnlyList<SqlField> IdentityFields => _identityFields;

		internal void ClearFields()
		{
			_fieldsLookup  .Clear();
			_orderedFields .Clear();
			_identityFields.Clear();
		}

		public SequenceNameAttribute[]? SequenceAttributes { get; protected internal set; }

		private SqlField? _all;
		public  SqlField   All => _all ??= SqlField.All(this);

		public SqlField? GetIdentityField()
		{
			foreach (var field in Fields)
				if (field.IsIdentity)
					return field;

			var keys = GetKeys(true);

			if (keys.Count == 1)
				return (SqlField)keys[0];

			return null;
		}

		public void Add(SqlField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

			if (field.Name == "*")
				_all = field;
			else
			{
				_fieldsLookup.Add(field.Name, field);
				_orderedFields.Add(field);

				if (field.IsIdentity)
					_identityFields.Add(field);
			}
		}

		public void AddRange(IEnumerable<SqlField> collection)
		{
			foreach (var item in collection)
				Add(item);
		}

		#endregion

		#region ISqlTableSource Members

		public int SourceID { get; }

		List<ISqlExpression>? _keyFields;

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
		{
			_keyFields ??=
			(
				from f in Fields
				where f.IsPrimaryKey
				orderby f.PrimaryKeyOrder
				select f as ISqlExpression
			)
			.ToList();

			if (_keyFields.Count == 0 && allIfEmpty)
				return Fields.Select(f => f as ISqlExpression).ToList();

			return _keyFields;
		}

		#endregion

		#region IQueryElement Members

		public virtual QueryElementType ElementType => QueryElementType.SqlTable;

		public virtual StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (TableName.Server   != null) sb.Append($"[{TableName.Server}].");
			if (TableName.Database != null) sb.Append($"[{TableName.Database}].");
			if (TableName.Schema   != null) sb.Append($"[{TableName.Schema}].");
			return sb.Append($"[{Expression ?? TableName.Name}({SourceID})]");
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull { get; set; } = true;

		int  ISqlExpression.Precedence => Precedence.Primary;
		Type ISqlExpression.SystemType => ObjectType;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return this == other;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public virtual ISqlExpression Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			if (TableArguments != null)
				for (var i = 0; i < TableArguments.Length; i++)
					TableArguments[i] = TableArguments[i].Walk(options, context, func)!;

			if (SqlQueryExtensions != null)
				foreach (var e in SqlQueryExtensions)
					e.Walk(options, context, func);

			return func(context, this);
		}

		#endregion

		#region System tables

		internal static SqlTable Inserted(EntityDescriptor entityDescriptor)
			=> new (entityDescriptor)
			{
				TableName    = new ("INSERTED"),
				SqlTableType = SqlTableType.SystemTable,
			};

		internal static SqlTable Deleted(EntityDescriptor entityDescriptor)
			=> new (entityDescriptor)
			{
				TableName    = new ("DELETED"),
				SqlTableType = SqlTableType.SystemTable,
			};

		#endregion

		internal static SqlTable Create<T>(IDataContext dataContext)
		{
			return new SqlTable(dataContext.MappingSchema.GetEntityDescriptor(typeof(T), dataContext.Options.ConnectionOptions.OnEntityDescriptorCreated));
		}

	}
}

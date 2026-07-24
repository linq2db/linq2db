using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using NHibernate;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.SqlTypes;
using NHibernate.Type;

// The char-separator string.Join overload is unavailable on netstandard2.0, so MA0089's suggested
// fix cannot be applied to the string.Join(",", ...) calls in this file.
#pragma warning disable MA0089

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// LINQ To DB metadata reader for an NHibernate <see cref="ISessionFactory"/>.
	/// </summary>
	internal sealed partial class NHMetadataReader : IMetadataReader
	{
		readonly ISessionFactory? _sessionFactory;
		private readonly ConcurrentDictionary<AbstractEntityPersister, PropertyMap> _propMapCache = new();

		public NHMetadataReader(
			ISessionFactory? sessionFactory)
		{
			_sessionFactory = sessionFactory;
		}

		sealed class PropInfo
		{
			public PropInfo(bool isPrimaryKey, int pkOrder, bool isIdentity, MemberInfo memberInfo, IType? propType, string[] columnNames, bool canBeNull)
			{
				IsPrimaryKey = isPrimaryKey;
				PkOrder      = pkOrder;
				IsIdentity   = isIdentity;
				MemberInfo   = memberInfo;
				PropType     = propType;
				ColumnNames  = columnNames;
				CanBeNull    = canBeNull;
			}

			public bool IsPrimaryKey     { get; }
			public int PkOrder           { get; }
			public bool IsIdentity       { get; }
			public MemberInfo MemberInfo { get; }
			public IType? PropType       { get; }
			public string[] ColumnNames  { get; }

			public bool CanBeNull        { get; }
			// public DataType DataType  { get; }

			public string MemberName => MemberInfo.Name;
		}

		sealed class PropertyMap
		{
			private Dictionary<string, PropInfo> _strict;
			private Dictionary<string, PropInfo> _noCase;
			private ILookup<string, PropInfo>    _stripped;

			public PropertyMap(AbstractEntityPersister metadata)
			{
				Metadata = metadata;
				var entityType = metadata.EntityMetamodel.EntityType.ReturnedClass;
				var plainProps = metadata.PropertyNames
					.Select((pn, idx) => new PropInfo(false, 0, false, entityType.GetProperty(pn)!, metadata.GetPropertyType(pn), metadata.GetSubclassPropertyColumnNames(pn), metadata.PropertyNullability[idx]));

				if (metadata.HasIdentifierProperty)
				{
					plainProps = plainProps.Concat(new[]
					{
						new PropInfo(true, 0, metadata.IsIdentifierAssignedByInsert, entityType.GetProperty(metadata.IdentifierPropertyName)!, metadata.IdentifierType, metadata.IdentifierColumnNames, false),
					});
				}
				else
				{
					var pk = new List<PropInfo>();
					var properties = entityType.GetProperties();
					for (int i = 0; i < metadata.KeyColumnNames.Length; i++)
					{
						var columnName = metadata.KeyColumnNames[i];
						var foundProp = properties.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.Ordinal))
							?? properties.FirstOrDefault(p => p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
							?? properties.FirstOrDefault(p => StripCharacters(p.Name).Equals(StripCharacters(columnName), StringComparison.OrdinalIgnoreCase));
						if (foundProp != null)
						{
							pk.Add(new PropInfo(true, i, false, foundProp, null, new[] { columnName }, false));
						}
					}

					plainProps = plainProps.Concat(pk);
				}

				Properties = plainProps.ToArray();

				var singleColumnName = Properties.Where(m => m.ColumnNames.Length == 1 && m.PropType?.IsAssociationType != true).ToList();

				_strict   = singleColumnName.ToDictionary(m => m.ColumnNames[0]);
				_noCase   = singleColumnName.ToDictionary(m => m.ColumnNames[0], StringComparer.InvariantCultureIgnoreCase);
				_stripped = singleColumnName.ToLookup(m => StripCharacters(m.ColumnNames[0]), StringComparer.InvariantCultureIgnoreCase);

			}

			public AbstractEntityPersister Metadata { get; }
			public PropInfo[] Properties            { get; }

			public PropInfo? FindPropByColumnName(string columnName)
			{
				PropInfo? info;
				if (_strict.TryGetValue(columnName, out info))
					return info;
				if (_noCase.TryGetValue(columnName, out info))
					return info;
				var stripped = StripCharacters(columnName);
				if (_stripped.Contains(stripped))
					return _stripped[stripped].First();

				return null;
			}

			public PropInfo? FindPropByMemberInfo(MemberInfo memberInfo)
			{
				return Properties.FirstOrDefault(p => p.MemberInfo == memberInfo);
			}
		}

		PropertyMap GetPropertyMap(AbstractEntityPersister metadata)
		{
			//return new PropertyMap(metadata);
			return _propMapCache.GetOrAdd(metadata, m => new PropertyMap(m));
		}

		bool GetPropertyMap(Type? type, out PropertyMap? propMap)
		{
			propMap = null;
			if (type == null)
				return false;
			if (_sessionFactory?.GetClassMetadata(type) is not AbstractEntityPersister metadata)
				return false;

			propMap = GetPropertyMap(metadata);
			return true;
		}

		public T[] GetAttributes<T>(Type type, bool inherit = true) where T : Attribute
		{
			if (_sessionFactory?.GetClassMetadata(type) is AbstractEntityPersister et)
			{
				if (typeof(T) == typeof(TableAttribute))
				{
					return new[] { (T)(Attribute)new TableAttribute(et.RootTableName) { /*Schema = et*/ } };
				}
			}

			return Array.Empty<T>();
		}

		static string StripCharacters(string name)
		{
			name = name.Replace("_", "", StringComparison.Ordinal);
			return name;
		}

		static PropInfo FindPropertyNameByColumnName(PropertyMap map, string columnName)
		{
			var info = map.FindPropByColumnName(columnName);
			if (info == null)
				throw new InvalidOperationException($"Could not find mapping for column '{columnName}'");
			return info;
		}

		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true) where T : Attribute
		{
			if (typeof(Expression).IsSameOrParentOf(type))
				return Array.Empty<T>();

			if (typeof(T) == typeof(ColumnAttribute))
			{
				if (GetPropertyMap(type, out var propMap))
				{
					var prop = propMap!.FindPropByMemberInfo(memberInfo);

					if (prop != null && prop.PropType?.IsAssociationType != true) // null PropType = composite-key member; still a real column
					{
						if (prop.ColumnNames.Length == 1)
						{
							SqlType? sqlType = null;
							if (prop.PropType is NullableType nullableType)
							{
								sqlType = nullableType.SqlType;
							}

							var column = new ColumnAttribute
							{
								Name = prop.ColumnNames[0],
								CanBeNull = prop.CanBeNull,
								DataType = sqlType != null ? DbTypeToDataType(sqlType.DbType) : DataType.Undefined,
								IsPrimaryKey = prop.IsPrimaryKey,
								PrimaryKeyOrder = prop.PkOrder,
								IsIdentity = prop.IsIdentity,
							};

							if (sqlType != null)
							{
								if (sqlType.Length > 0)
									column.Length = sqlType.Length;
								if (sqlType.Precision > 0)
									column.Precision = sqlType.Precision;
								if (sqlType.Scale > 0)
									column.Scale = sqlType.Scale;
							}

							return new T[] {(T) (Attribute) column};
						}
					}
				}
			}
			else if (typeof(T) == typeof(AssociationAttribute))
			{
				if (GetPropertyMap(type, out var thisEntityMap))
				{
					var prop = thisEntityMap!.FindPropByMemberInfo(memberInfo);
					if (prop != null)
					{
						if (prop.PropType?.IsAssociationType == true)
						{
							AssociationAttribute? association = null;
							if (prop.PropType.IsCollectionType)
							{
								var roleProp = prop.PropType.GetType().GetProperty("Role");
								if (roleProp != null)
								{
									if (roleProp.GetValue(prop.PropType) is string role)
									{
										var collectionMetadata = _sessionFactory!.GetCollectionMetadata(role);

										if (collectionMetadata is OneToManyPersister o2m && GetPropertyMap(o2m.ElementType.ReturnedClass, out var elementMap))
										{
											var thisKey = string.Join(",",
												thisEntityMap.Properties.Where(p => p.IsPrimaryKey).OrderBy(p => p.PkOrder).Select(p =>
													p.MemberName));
											var otherKey = string.Join(",",
												o2m.KeyColumnNames.Select(cn =>
													FindPropertyNameByColumnName(elementMap!, cn).MemberName));
											var canBeNull = true;
											association = new AssociationAttribute
											{
												ThisKey = thisKey,
												OtherKey = otherKey,
												CanBeNull = canBeNull,
											};
										}
										else if (collectionMetadata is AbstractCollectionPersister m2m && m2m.IsManyToMany)
										{
											association = BuildManyToManyAssociation(type, thisEntityMap!, m2m);
										}
									}
								}
							}
							else if (prop.PropType is ManyToOneType manyToOne && GetPropertyMap(manyToOne.ReturnedClass, out var otherPropMap))
							{
								if (manyToOne.IsReferenceToPrimaryKey)
								{
									var thisKey = string.Join(",", otherPropMap!.Properties.Where(p => p.IsPrimaryKey).Select(p => p.MemberName));
									var otherKey = thisKey;
									var canBeNull = true;
									association = new AssociationAttribute
									{
										ThisKey = thisKey,
										OtherKey = otherKey,
										CanBeNull = canBeNull,
									};
								}
							}

							if (association != null)
							{
								return new T[]
								{
									(T) (Attribute) association,
								};
							}

						}
					}
				}
			}

			return Array.Empty<T>();
		}

		// Builds an association for a many-to-many collection. NHibernate exposes the junction only by its
		// table name; linq2db needs a queryable entity for it, so we locate the entity mapped to that table
		// and synthesize a query expression that hops through it (this -> junction -> other).
		AssociationAttribute? BuildManyToManyAssociation(Type thisType, PropertyMap thisEntityMap, AbstractCollectionPersister m2m)
		{
			var otherType = m2m.ElementType.ReturnedClass;
			if (!GetPropertyMap(otherType, out var otherEntityMap))
				return null;

			var joinPersister = FindEntityPersisterByTable(m2m.TableName);
			if (joinPersister == null)
				return null;

			var joinType = joinPersister.EntityMetamodel.EntityType.ReturnedClass;
			if (!GetPropertyMap(joinType, out var joinEntityMap))
				return null;

			var queryExpression = BuildManyToManyQueryExpression(
				thisType, thisEntityMap, otherType, otherEntityMap!, joinType, joinEntityMap!, m2m);

			if (queryExpression == null)
				return null;

			return new AssociationAttribute
			{
				QueryExpression = queryExpression,
				CanBeNull       = true,
			};
		}

		// Finds the entity persister mapped to <paramref name="tableName"/> (the many-to-many junction table),
		// comparing on an unqualified, unquoted, case-insensitive table name.
		AbstractEntityPersister? FindEntityPersisterByTable(string tableName)
		{
			if (_sessionFactory == null)
				return null;

			var target = NormalizeTable(tableName);
			foreach (var meta in _sessionFactory.GetAllClassMetadata().Values)
			{
				if (meta is AbstractEntityPersister ep && string.Equals(NormalizeTable(ep.RootTableName), target, StringComparison.Ordinal))
					return ep;
			}

			return null;
		}

		static string NormalizeTable(string name)
		{
			var idx = name.LastIndexOf('.');
			if (idx >= 0)
				name = name.Substring(idx + 1);

			name = name
				.Replace("[",  "", StringComparison.Ordinal)
				.Replace("]",  "", StringComparison.Ordinal)
				.Replace("\"", "", StringComparison.Ordinal)
				.Replace("`",  "", StringComparison.Ordinal)
				.Replace("'",  "", StringComparison.Ordinal)
				.Trim();

			return name.ToUpperInvariant();
		}

		// (this, db) => db.GetTable&lt;junction&gt;().Where(j => j links this).SelectMany(j => db.GetTable&lt;other&gt;().Where(o => o linked by j))
		LambdaExpression? BuildManyToManyQueryExpression(
			Type thisType,  PropertyMap thisEntityMap,
			Type otherType, PropertyMap otherEntityMap,
			Type joinType,  PropertyMap joinEntityMap,
			AbstractCollectionPersister m2m)
		{
			var thisPk  = thisEntityMap .Properties.Where(p => p.IsPrimaryKey).OrderBy(p => p.PkOrder).ToList();
			var otherPk = otherEntityMap.Properties.Where(p => p.IsPrimaryKey).OrderBy(p => p.PkOrder).ToList();

			var keyCols     = m2m.KeyColumnNames;     // junction -> this entity
			var elementCols = m2m.ElementColumnNames; // junction -> other entity

			if (thisPk.Count == 0 || otherPk.Count == 0 || thisPk.Count != keyCols.Length || otherPk.Count != elementCols.Length)
				return null;

			var thisParam  = Expression.Parameter(thisType,             "t");
			var dcParam    = Expression.Parameter(typeof(IDataContext), "db");
			var joinParam  = Expression.Parameter(joinType,             "j");
			var otherParam = Expression.Parameter(otherType,            "o");

			var joinTable  = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(joinType),  dcParam);
			var otherTable = Expression.Call(Methods.LinqToDB.GetTable.MakeGenericMethod(otherType), dcParam);

			// j => junction row links this record: junction.<key column> == this.<primary key>
			Expression? joinPredicate = null;
			for (var i = 0; i < keyCols.Length; i++)
			{
				var joinMember = FindPropertyNameByColumnName(joinEntityMap, keyCols[i]).MemberInfo;
				var left       = Expression.MakeMemberAccess(joinParam, joinMember);
				var right      = Expression.MakeMemberAccess(thisParam, thisPk[i].MemberInfo);
				joinPredicate  = AndAlso(joinPredicate, EqualWithConvert(left, right));
			}

			var filteredJoin = Expression.Call(
				Methods.Queryable.Where.MakeGenericMethod(joinType),
				joinTable,
				Expression.Quote(Expression.Lambda(joinPredicate!, joinParam)));

			// o => target record linked by the junction row: other.<primary key> == junction.<element column>
			Expression? otherPredicate = null;
			for (var i = 0; i < elementCols.Length; i++)
			{
				var joinMember = FindPropertyNameByColumnName(joinEntityMap, elementCols[i]).MemberInfo;
				var left       = Expression.MakeMemberAccess(otherParam, otherPk[i].MemberInfo);
				var right      = Expression.MakeMemberAccess(joinParam, joinMember);
				otherPredicate = AndAlso(otherPredicate, EqualWithConvert(left, right));
			}

			var linkedOther = Expression.Call(
				Methods.Queryable.Where.MakeGenericMethod(otherType),
				otherTable,
				Expression.Quote(Expression.Lambda(otherPredicate!, otherParam)));

			var collectionSelectorType = typeof(Func<,>).MakeGenericType(joinType, typeof(IEnumerable<>).MakeGenericType(otherType));
			var collectionSelector     = Expression.Lambda(collectionSelectorType, linkedOther, joinParam);

			var selectMany = Expression.Call(
				Methods.Queryable.SelectManySimple.MakeGenericMethod(joinType, otherType),
				filteredJoin,
				Expression.Quote(collectionSelector));

			return Expression.Lambda(selectMany, thisParam, dcParam);
		}

		static Expression AndAlso(Expression? accumulated, Expression next)
		{
			return accumulated == null ? next : Expression.AndAlso(accumulated, next);
		}

		static Expression EqualWithConvert(Expression left, Expression right)
		{
			if (left.Type != right.Type)
				right = Expression.Convert(right, left.Type);

			return Expression.Equal(left, right);
		}

		static DataType DbTypeToDataType(DbType dbType)
		{
			DataType dataType = dbType switch
			{
				DbType.AnsiStringFixedLength => DataType.Char,
				DbType.AnsiString            => DataType.VarChar,
				DbType.StringFixedLength     => DataType.NChar,
				DbType.String                => DataType.NVarChar,
				DbType.Binary                => DataType.Blob,
				DbType.Boolean               => DataType.Boolean,
				DbType.SByte                 => DataType.SByte,
				DbType.Int16                 => DataType.Int16,
				DbType.Int32                 => DataType.Int32,
				DbType.Int64                 => DataType.Int64,
				DbType.Byte                  => DataType.Byte,
				DbType.UInt16                => DataType.UInt16,
				DbType.UInt32                => DataType.UInt32,
				DbType.UInt64                => DataType.UInt64,
				DbType.Single                => DataType.Single,
				DbType.Double                => DataType.Double,
				DbType.Decimal               => DataType.Decimal,
				DbType.Guid                  => DataType.Guid,
				DbType.Date                  => DataType.Date,
				DbType.Time                  => DataType.Time,
				DbType.DateTime              => DataType.DateTime,
				DbType.DateTime2             => DataType.DateTime2,
				DbType.DateTimeOffset        => DataType.DateTimeOffset,
				DbType.Object                => DataType.Variant,
				DbType.VarNumeric            => DataType.VarNumeric,
				_                            => DataType.NVarChar,
			};

			return dataType;
		}

		sealed class ValueConverter : IValueConverter
		{
			public ValueConverter(
				LambdaExpression convertToProviderExpression,
				LambdaExpression convertFromProviderExpression, bool handlesNulls)
			{
				FromProviderExpression = convertFromProviderExpression;
				ToProviderExpression   = convertToProviderExpression;
				HandlesNulls           = handlesNulls;
			}

			public bool             HandlesNulls           { get; }
			public LambdaExpression FromProviderExpression { get; }
			public LambdaExpression ToProviderExpression   { get; }

		}

		MappingAttribute[] IMetadataReader.GetAttributes(Type type)
		{
			var tableAttrs = GetAttributes<TableAttribute>(type);

			var queryFilter = BuildQueryFilterAttribute(type);
			if (queryFilter == null)
				return tableAttrs.Cast<MappingAttribute>().ToArray();

			var result = new MappingAttribute[tableAttrs.Length + 1];
			for (var i = 0; i < tableAttrs.Length; i++)
				result[i] = tableAttrs[i];
			result[tableAttrs.Length] = queryFilter;
			return result;
		}

		MappingAttribute[] IMetadataReader.GetAttributes(Type type, MemberInfo memberInfo)
		{
			var attrs = new List<MappingAttribute>();
			attrs.AddRange(GetAttributes<ColumnAttribute>(type, memberInfo));
			attrs.AddRange(GetAttributes<AssociationAttribute>(type, memberInfo));
			return attrs.ToArray();
		}

		string IMetadataReader.GetObjectID()
		{
			return $".{nameof(NHMetadataReader)}.{(_sessionFactory == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_sessionFactory)).ToString(CultureInfo.InvariantCulture)}.";
		}

		public MemberInfo[] GetDynamicColumns(Type type)
		{
			return Array.Empty<MemberInfo>();
		}
	}
}

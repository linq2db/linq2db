using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#if !EF6 && !EF31
using LinqToDB.Common.Internal;
#endif
using Microsoft.EntityFrameworkCore;
#if !EF31
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
#if EF31
using Microsoft.EntityFrameworkCore.Migrations;
#endif
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace LinqToDB.EntityFrameworkCore
{
	using Common;
	using Expressions;
	using Extensions;
	using Internal;
	using Mapping;
	using Metadata;
	using Reflection;
	using SqlQuery;

	using EfSqlExpression = SqlExpression;
	using EfExpressionPrinter = ExpressionPrinter;

	/// <summary>
	/// LINQ To DB metadata reader for EF.Core model.
	/// </summary>
	internal sealed class EFCoreMetadataReader : IMetadataReader
	{
#if EF6
		private const string PgBinaryExpressionName = "PostgresBinaryExpression";
#elif !EF31
		// renamed in 8.0.0
		private const string PgBinaryExpressionName = "PgBinaryExpression";
#endif

		private readonly string                                                       _objectId;
		private readonly IModel?                                                      _model;
		private readonly RelationalSqlTranslatingExpressionVisitorDependencies?       _dependencies;
		private readonly IRelationalTypeMappingSource?                                _mappingSource;
#if !EF31
		private readonly IRelationalAnnotationProvider?                               _annotationProvider;
#else
		private readonly IMigrationsAnnotationProvider?                               _annotationProvider;
#endif
		private readonly ConcurrentDictionary<MemberInfo, Sql.ExpressionAttribute?>   _calculatedExtensions = new();
#if !EF31
		private readonly IDiagnosticsLogger<DbLoggerCategory.Query>?                  _logger;
#endif
#if !EF6 && !EF31
		private readonly DatabaseDependencies?                                        _databaseDependencies;
#endif

		public EFCoreMetadataReader(IModel? model, IInfrastructure<IServiceProvider>? accessor)
		{
			_model    = model;

			if (accessor != null)
			{
				_dependencies         = accessor.GetService<RelationalSqlTranslatingExpressionVisitorDependencies>();
				_mappingSource        = accessor.GetService<IRelationalTypeMappingSource>();
#if EF31
				_annotationProvider = accessor.GetService<IMigrationsAnnotationProvider>();
#else
				_annotationProvider   = accessor.GetService<IRelationalAnnotationProvider>();
				_logger               = accessor.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();
#if !EF6
				_databaseDependencies = accessor.GetService<DatabaseDependencies>();
#endif
#endif
			}

#if EF31
			_objectId = FormattableString.Invariant($".{_model?.GetHashCode() ?? 0}.{_dependencies?.GetHashCode() ?? 0}.{_mappingSource?.GetHashCode() ?? 0}.{_annotationProvider?.GetHashCode() ?? 0}.");
#else
			_objectId = FormattableString.Invariant($".{_model?.GetHashCode() ?? 0}.{_dependencies?.GetHashCode() ?? 0}.{_mappingSource?.GetHashCode() ?? 0}.{_annotationProvider?.GetHashCode() ?? 0}.{_logger?.GetHashCode() ?? 0}.");
#endif
		}

		public MappingAttribute[] GetAttributes(Type type)
		{
			List<MappingAttribute>? result = null;

			var et = _model?.FindEntityType(type);
			if (et != null)
			{
				result = new();

				// TableAttribute
#if EF31
				result.Add(new TableAttribute(et.GetTableName()) { Schema = et.GetSchema() });
#else
				var storeObjectId = GetStoreObjectIdentifier(et);
				result.Add(new TableAttribute() { Schema = storeObjectId?.Schema, Name = storeObjectId?.Name });
#endif

				// QueryFilterAttribute
				var filter = et.GetQueryFilter();
				if (filter != null)
				{
					var dcParam     = Expression.Parameter(typeof(IDataContext), "dc");
					var contextProp = Expression.Property(Expression.Convert(dcParam, typeof(LinqToDBForEFToolsDataConnection)), "Context");
					var filterBody   = filter.Body.Transform(contextProp, static (contextProp, e) =>
					{
						if (typeof(DbContext).IsSameOrParentOf(e.Type))
						{
							Expression newExpr = contextProp;
							if (newExpr.Type != e.Type)
								newExpr = Expression.Convert(newExpr, e.Type);
							return newExpr;
						}

						return e;
					});

					var newFilter = Expression.Lambda(filterBody, filter.Parameters[0], dcParam);

					result.Add(new QueryFilterAttribute() { FilterLambda = newFilter });
				}

				// InheritanceMappingAttribute
				if (_model != null)
				{
					foreach (var e in _model.GetEntityTypes())
					{
						if (GetBaseTypeRecursive(e) == et && e.GetDiscriminatorValue() != null)
						{
							result.AddRange(GetMappingAttributesRecursive(e));
						}
					}
				}
			}
			else
			{
				// TableAttribute
				var tableAttribute = type.GetAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
				if (tableAttribute != null)
					(result = new()).Add(new TableAttribute(tableAttribute.Name) { Schema = tableAttribute.Schema });
			}

			return result == null ? [] : result.ToArray();
		}

		static IEntityType GetBaseTypeRecursive(IEntityType entityType)
		{
			if (entityType.BaseType == null)
				return entityType;
			return GetBaseTypeRecursive(entityType.BaseType);
		}
		
		static List<InheritanceMappingAttribute> GetMappingAttributesRecursive(IEntityType entityType)
		{
			var mappings = new List<InheritanceMappingAttribute>();
			return ProcessEntityType(entityType);

			List<InheritanceMappingAttribute> ProcessEntityType(IEntityType et)
			{
				if (!et.ClrType.IsAbstract)
				{
					mappings.Add(new()
					{
						Type = et.ClrType,
						Code = entityType.GetDiscriminatorValue()
					});
				}
				
				if (et.BaseType == null)
					return mappings;
				return ProcessEntityType(et.BaseType);
			}
		}

		static bool CompareProperty(MemberInfo? property, MemberInfo memberInfo)
		{
			if (property == memberInfo)
				return true;

			if (property == null)
				return false;
			
			if (memberInfo.DeclaringType?.IsAssignableFrom(property.DeclaringType) == true
			    && memberInfo.Name == property.Name 
			    && memberInfo.MemberType == property.MemberType 
			    && memberInfo.GetMemberType() == property.GetMemberType())
			{
				return true;
			}

			return false;
		}

		static bool CompareProperty(IProperty property, MemberInfo memberInfo)
		{
			return CompareProperty(property.GetIdentifyingMemberInfo(), memberInfo);
		}

		static DataType DbTypeToDataType(DbType dbType)
		{
			return dbType switch
			{
				DbType.AnsiString            => DataType.VarChar,
				DbType.AnsiStringFixedLength => DataType.VarChar,
				DbType.Binary                => DataType.Binary,
				DbType.Boolean               => DataType.Boolean,
				DbType.Byte                  => DataType.Byte,
				DbType.Currency              => DataType.Money,
				DbType.Date                  => DataType.Date,
				DbType.DateTime              => DataType.DateTime,
				DbType.DateTime2             => DataType.DateTime2,
				DbType.DateTimeOffset        => DataType.DateTimeOffset,
				DbType.Decimal               => DataType.Decimal,
				DbType.Double                => DataType.Double,
				DbType.Guid                  => DataType.Guid,
				DbType.Int16                 => DataType.Int16,
				DbType.Int32                 => DataType.Int32,
				DbType.Int64                 => DataType.Int64,
				DbType.Object                => DataType.Undefined,
				DbType.SByte                 => DataType.SByte,
				DbType.Single                => DataType.Single,
				DbType.String                => DataType.NVarChar,
				DbType.StringFixedLength     => DataType.NVarChar,
				DbType.Time                  => DataType.Time,
				DbType.UInt16                => DataType.UInt16,
				DbType.UInt32                => DataType.UInt32,
				DbType.UInt64                => DataType.UInt64,
				DbType.VarNumeric            => DataType.VarNumeric,
				DbType.Xml                   => DataType.Xml,
				_                            => DataType.Undefined
			};
		}
		
		public MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo)
		{
			if (typeof(Expression).IsSameOrParentOf(type))
				return [];

			List<MappingAttribute>? result = null;
			var hasColumn = false;

			var et = _model?.FindEntityType(type);
			if (et != null)
			{
				var props = et.GetProperties();
				var prop  = props.FirstOrDefault(p => CompareProperty(p, memberInfo));

				// ColumnAttribute
				if (prop != null)
				{
					hasColumn         = true;
#if EF31
					var discriminator = et.GetDiscriminatorProperty();
#else
					var discriminator = et.FindDiscriminatorProperty();
#endif

					var isPrimaryKey    = prop.IsPrimaryKey();
					var primaryKeyOrder = 0;
					if (isPrimaryKey)
					{
						var pk = prop.FindContainingPrimaryKey()!;
						var idx = 0;
						foreach (var p in pk.Properties)
						{
							if (CompareProperty(p, memberInfo))
							{
								primaryKeyOrder = idx;
								break;
							}

							idx++;
						}
					}

					var isIdentity  = false;
					var annotations = prop.GetAnnotations();
#if EF31
					if (_annotationProvider != null)
					{
						annotations = annotations.Concat(_annotationProvider.For(prop));
					}
#else
					var storeObjectId = GetStoreObjectIdentifier(et);

					if (_annotationProvider != null && storeObjectId != null)
					{
						if (prop.FindColumn(storeObjectId.Value) is IColumn column)
							annotations = annotations.Concat(_annotationProvider.For(column, false));
#if !EF6

						if (_annotationProvider.GetType().Name == "SqliteAnnotationProvider")
						{
							// copy-paste logic, not available anymore in v8
							// https://github.com/dotnet/efcore/blob/release/8.0/src/EFCore.Sqlite.Core/Metadata/Internal/SqliteAnnotationProvider.cs#L70-L75
							var primaryKey = prop.DeclaringType.ContainingEntityType.FindPrimaryKey();
							if (primaryKey is { Properties.Count: 1 }
								&& primaryKey.Properties[0] == prop
								&& prop.ValueGenerated == ValueGenerated.OnAdd
								&& prop.ClrType.IsInteger()
								&& prop.FindTypeMapping()?.Converter == null)
							{
								isIdentity = true;
							}
						}
#endif
					}
#endif

#if !EF6 && !EF31
					isIdentity = isIdentity || annotations
#else
					isIdentity = annotations
#endif
						.Any(static a =>
						{
							if (a.Name.EndsWith(":ValueGenerationStrategy"))
								return a.Value != null && string.Format(CultureInfo.InvariantCulture, "{0}", a.Value).Contains("Identity");

							if (a.Name.EndsWith(":Autoincrement"))
								return a.Value is bool b && b;

							// for postgres
							if (a.Name == "Relational:DefaultValueSql")
							{
								if (a.Value is string str)
								{
									return str.Contains("nextval", StringComparison.InvariantCultureIgnoreCase);
								}
							}

							return false;
						});

					var dataType = DataType.Undefined;

					if (prop.GetTypeMapping() is RelationalTypeMapping typeMapping)
					{
						if (typeMapping.DbType != null)
						{
							dataType = DbTypeToDataType(typeMapping.DbType.Value);
						}
						else
						{
							var ms = _model != null ? LinqToDBForEFTools.GetMappingSchema(_model, null, null) : MappingSchema.Default;
							dataType = ms.GetDataType(typeMapping.ClrType).Type.DataType;
						}
					}

					var behavior = prop.GetBeforeSaveBehavior();
					var skipOnInsert = prop.ValueGenerated.HasFlag(ValueGenerated.OnAdd);

					if (skipOnInsert)
					{
						skipOnInsert = isIdentity || behavior != PropertySaveBehavior.Save;
					}

					var skipOnUpdate = behavior != PropertySaveBehavior.Save ||
						prop.ValueGenerated.HasFlag(ValueGenerated.OnUpdate);

					var ca = new ColumnAttribute()
					{
#if EF31
						Name = prop.GetColumnName(),
#else
						Name = storeObjectId != null ? prop.GetColumnName(storeObjectId.Value) : null,
#endif
						CanBeNull = prop.IsNullable,
						DbType = prop.GetColumnType(),
						DataType = dataType,
						IsPrimaryKey = isPrimaryKey,
						PrimaryKeyOrder = primaryKeyOrder,
						IsIdentity = isIdentity,
						IsDiscriminator = discriminator == prop,
						SkipOnInsert = skipOnInsert,
						SkipOnUpdate = skipOnUpdate
					};

					var maxLen = prop.GetMaxLength();
					if (maxLen != null)
						ca.Length = maxLen.Value;

					(result ??= new()).Add(ca);

					// ValueConverterAttribute
					var converter = prop.GetValueConverter();
					if (converter != null)
					{
						var valueConverterAttribute = new ValueConverterAttribute()
						{
							ValueConverter = new ValueConverter(converter.ConvertToProviderExpression, converter.ConvertFromProviderExpression, false)
						};

						result.Add(valueConverterAttribute);
					}
				}

				// AssociationAttribute
				foreach (var navigation in et.GetNavigations())
				{
					if (CompareProperty(navigation.PropertyInfo, memberInfo))
					{
						var fk = navigation.ForeignKey;
#if !EF31
						if (!navigation.IsOnDependent)
#else
						if (!navigation.IsDependentToPrincipal())
#endif
						{
							var thisKey  = string.Join(",", fk.PrincipalKey.Properties.Select(static p => p.Name));
							var otherKey = string.Join(",", fk.Properties.Select(static p => p.Name));
							(result ??= new()).Add(new AssociationAttribute()
							{
								ThisKey   = thisKey,
								OtherKey  = otherKey,
#if !EF31
								CanBeNull = !fk.IsRequiredDependent,
#else
								// Could not track when EF decides to do INNER JOIN
								CanBeNull = true
#endif
							});
						}
						else
						{
							var thisKey  = string.Join(",", fk.Properties.Select(static p => p.Name));
							var otherKey = string.Join(",", fk.PrincipalKey.Properties.Select(static p => p.Name));
							(result ??= new()).Add(new AssociationAttribute()
							{
								ThisKey   = thisKey,
								OtherKey  = otherKey,
								CanBeNull = !fk.IsRequired
							});
						}
					}
				}
			}

			if (!hasColumn)
			{
				// ColumnAttribute
				var columnAttribute = memberInfo.GetAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();

				if (columnAttribute != null)
					(result ??= new()).Add(new ColumnAttribute()
					{
						Name = columnAttribute.Name,
						DbType = columnAttribute.TypeName,
					});
			}

			// Search for translator first
			// Sql.ExpressionAttribute
			if (_dependencies != null)
			{
				if (memberInfo.IsMethodEx())
				{
					var methodInfo = (MethodInfo) memberInfo;
					var func = GetDbFunctionFromMethodCall(type, methodInfo);
					if (func != null)
						(result ??= new()).Add(func);
				}
				else if (memberInfo.IsPropertyEx())
				{
					var propertyInfo = (PropertyInfo) memberInfo;
					var func = GetDbFunctionFromProperty(type, propertyInfo);
					if (func != null)
						(result ??= new()).Add(func);
				}
			}

			// Sql.FunctionAttribute
			if (memberInfo.IsMethodEx())
			{
				var method = (MethodInfo) memberInfo;

				var func = _model?.GetDbFunctions().FirstOrDefault(f => f.MethodInfo == method);
				if (func != null)
					(result ??= new()).Add(new Sql.FunctionAttribute()
					{
						Name           = func.Name,
						ServerSideOnly = true
					});

				var functionAttribute = memberInfo.GetAttribute<DbFunctionAttribute>();
				if (functionAttribute != null)
					(result ??= new()).Add(new Sql.FunctionAttribute()
					{
						Name           = functionAttribute.Name,
						ServerSideOnly = true,
					});
			}

			return result == null ? [] : result.ToArray();
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

		sealed class SqlTransparentExpression : EfSqlExpression
		{
			public ConstantExpression Expression { get; }

			public SqlTransparentExpression(ConstantExpression expression, RelationalTypeMapping? typeMapping) : base(expression.Type, typeMapping)
			{
				Expression = expression;
			}

#if !EF31
			protected override void Print(EfExpressionPrinter expressionPrinter)
#else
			public override void Print(EfExpressionPrinter expressionPrinter)
#endif
			{
#if !EF6 && !EF31
				expressionPrinter.PrintExpression(Expression);
#else
				expressionPrinter.Print(Expression);
#endif
			}

#if !EF31 && !EF6 && !EF8
			private static readonly ConstructorInfo _ctor = typeof(SqlTransparentExpression).GetConstructor([typeof(ExceptExpression), typeof(RelationalTypeMapping)])
				?? throw new InvalidOperationException();

			private static readonly MethodInfo _constantExpressionFactoryMethod = typeof(Expression).GetMethod(nameof(Constant), [typeof(object), typeof(Type)])
				?? throw new InvalidOperationException();


			public override Expression Quote()
			{
				// not tested
#pragma warning disable EF9100 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
				return New(
					_ctor,
					Call(_constantExpressionFactoryMethod, Constant(Expression.Value), Constant(Expression.Type)),
					RelationalExpressionQuotingUtilities.QuoteTypeMapping(TypeMapping));
#pragma warning restore EF9100 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
			}
#endif

			private bool Equals(SqlTransparentExpression other)
			{
				return ReferenceEquals(this, other);
			}

			public override bool Equals(object? obj)
			{
				if (obj is null) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((SqlTransparentExpression) obj);
			}

			public override int GetHashCode()
			{
				return RuntimeHelpers.GetHashCode(this);
			}
		}

#if !EF31
		private StoreObjectIdentifier? GetStoreObjectIdentifier(IEntityType entityType)
		{
			return entityType.GetTableName() switch
			{
				not null => StoreObjectIdentifier.Create(entityType, StoreObjectType.Table),
				null     => StoreObjectIdentifier.Create(entityType, StoreObjectType.View),
			};
		}
#endif

		private Sql.ExpressionAttribute? GetDbFunctionFromMethodCall(Type type, MethodInfo methodInfo)
		{
			if (_dependencies == null || _model == null)
				return null;

			methodInfo = (MethodInfo?) type.GetMemberEx(methodInfo) ?? methodInfo;

			var found = _calculatedExtensions.GetOrAdd(methodInfo, static (mi, ctx) =>
			{
				Sql.ExpressionAttribute? result = null;

				if (!ctx.methodInfo.IsGenericMethodDefinition && !mi.HasAttribute<Sql.ExpressionAttribute>())
				{
					var value = Expression.Constant(DefaultValue.GetValue(ctx.type), ctx.type);

					var objExpr = new SqlTransparentExpression(value, ctx.this_._mappingSource?.FindMapping(ctx.type));
					var parameterInfos = ctx.methodInfo.GetParameters();
					var parametersArray = new EfSqlExpression[parameterInfos.Length];
					for (var i = 0; i < parameterInfos.Length; i++)
					{
						var p = parameterInfos[i];

						parametersArray[i] = new SqlTransparentExpression(
							Expression.Constant(DefaultValue.GetValue(p.ParameterType), p.ParameterType),
							ctx.this_._mappingSource?.FindMapping(p.ParameterType));
					}

#if !EF6 && !EF31
					// https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801
					if (ctx.this_._dependencies!.MethodCallTranslatorProvider.GetType().Name == "MySqlMethodCallTranslatorProvider")
					{
						var contextProperty = ctx.this_._dependencies!.MethodCallTranslatorProvider.GetType().GetProperty("QueryCompilationContext")
						?? throw new InvalidOperationException("MySqlMethodCallTranslatorProvider.QueryCompilationContext property not found");

						if (contextProperty.GetValue(ctx.this_._dependencies!.MethodCallTranslatorProvider) == null)
						{
							contextProperty.SetValue(ctx.this_._dependencies!.MethodCallTranslatorProvider, ctx.this_._databaseDependencies!.QueryCompilationContextFactory.Create(false));
						}
					}
#endif

#if !EF31
					var newExpression = ctx.this_._dependencies!.MethodCallTranslatorProvider.Translate(ctx.this_._model!, objExpr, ctx.methodInfo, parametersArray, ctx.this_._logger!);
#else
					var newExpression = ctx.this_._dependencies!.MethodCallTranslatorProvider.Translate(ctx.this_._model!, objExpr, ctx.methodInfo, parametersArray);
#endif
					if (newExpression != null)
					{
						if (!ctx.methodInfo.IsStatic)
							parametersArray = new EfSqlExpression[] { objExpr }.Concat(parametersArray).ToArray();

						result = ConvertToExpressionAttribute(ctx.methodInfo, newExpression, parametersArray);
					}
				}

				return result;
			}, (methodInfo, type, this_: this));

			return found;
		}

		private Sql.ExpressionAttribute? GetDbFunctionFromProperty(Type type, PropertyInfo propInfo)
		{
			if (_dependencies == null || _model == null)
				return null;

			propInfo = (PropertyInfo)(type.GetMemberEx(propInfo) ?? propInfo);

			var found = _calculatedExtensions.GetOrAdd(propInfo, static (mi, ctx) =>
			{
				Sql.ExpressionAttribute? result = null;

				if ((ctx.propInfo.GetMethod?.IsStatic != true)
					&& !(mi is DynamicColumnInfo)
					&& !mi.HasAttribute<Sql.ExpressionAttribute>())
				{
					var objExpr = new SqlTransparentExpression(Expression.Constant(DefaultValue.GetValue(ctx.type), ctx.type), ctx.this_._mappingSource?.FindMapping(ctx.propInfo));

#if !EF31
					var newExpression = ctx.this_._dependencies!.MemberTranslatorProvider.Translate(objExpr, ctx.propInfo, ctx.propInfo.GetMemberType(), ctx.this_._logger!);
#else
					var newExpression = ctx.this_._dependencies!.MemberTranslatorProvider.Translate(objExpr, ctx.propInfo, ctx.propInfo.GetMemberType());
#endif
					if (newExpression != null && newExpression != objExpr)
					{
						var parametersArray = new Expression[] { objExpr };
						result = ConvertToExpressionAttribute(ctx.propInfo, newExpression, parametersArray);
					}
				}

				return result;
			}, (propInfo, type, this_: this));

			return found;
		}

		private static Sql.ExpressionAttribute ConvertToExpressionAttribute(MemberInfo memberInfo, Expression newExpression, Expression[] parameters)
		{
			string PrepareExpressionText(Expression? expr)
			{
				var idx = -1;

				for (var index = 0; index < parameters.Length; index++)
				{
					var param = parameters[index];
					var found = ReferenceEquals(expr, param);
					if (!found)
					{
						if (param is SqlTransparentExpression transparent)
						{
							if (transparent.Expression is ConstantExpression &&
							    expr is SqlConstantExpression)
							{
								//found = sqlConstantExpr.Value.Equals(constantExpr.Value);
								found = true;
							}
						}
					}

					if (found)
					{
						idx = index;
						break;
					}
				}

				if (idx >= 0)
					return FormattableString.Invariant($"{{{idx}}}");

				if (expr is SqlFragmentExpression fragment)
					return fragment.Sql;

				if (expr is SqlFunctionExpression sqlFunction)
				{
					var text = sqlFunction.Name;
					if (!string.IsNullOrEmpty(sqlFunction.Schema))
						text = sqlFunction.Schema + "." + sqlFunction.Name;

					if (!sqlFunction.IsNiladic)
					{
						text += "(";
						for (var i = 0; i < sqlFunction.Arguments.Count; i++)
						{
							var paramText = PrepareExpressionText(sqlFunction.Arguments[i]);
							if (i > 0)
								text += ", ";
							text += paramText;
						}

						text += ")";
					}

					return text;
				}

				// https://github.com/npgsql/efcore.pg/blob/main/src/EFCore.PG/Query/Expressions/Internal/PgBinaryExpression.cs
#if EF31
				if (newExpression.GetType().GetProperty("Left") != null &&
				    newExpression.GetType().GetProperty("Right") != null &&
				    newExpression.GetType().GetProperty("Operator") != null)
				{
					// Handling NpgSql's CustomBinaryExpression

					var left = newExpression.GetType().GetProperty("Left")?.GetValue(newExpression) as Expression;
					var right = newExpression.GetType().GetProperty("Right")?.GetValue(newExpression) as Expression;

					var operand = newExpression.GetType().GetProperty("Operator")?.GetValue(newExpression) as string;

					var text = $"{PrepareExpressionText(left)} {operand} {PrepareExpressionText(right)}";

					return text;
				}
#else
				if (newExpression.GetType().Name == PgBinaryExpressionName)
				{
					// Handling Npgsql PgBinaryExpression

					var left  = (Expression)newExpression.GetType().GetProperty("Left")!.GetValue(newExpression)!;
					var right = (Expression)newExpression.GetType().GetProperty("Right")!.GetValue(newExpression)!;

					var operand = string.Format(CultureInfo.InvariantCulture, "{0}", newExpression.GetType().GetProperty("OperatorType")!.GetValue(newExpression));

					var operandExpr = operand switch
					{
						"Contains"
							when left.Type.Name == "NpgsqlInetTypeMapping" ||
							     left.Type.Name == "NpgsqlCidrTypeMapping"
							=> ">>",
						"ContainedBy"
							when left.Type.Name == "NpgsqlInetTypeMapping" ||
							     left.Type.Name == "NpgsqlCidrTypeMapping"
							=> "<<",
						"Contains"                      => "@>",
						"ContainedBy"                   => "<@",
						"Overlaps"                      => "&&",
						"AtTimeZone"                    => "AT TIME ZONE",
						"NetworkContainedByOrEqual"     => "<<=",
						"NetworkContainsOrEqual"        => ">>=",
						"NetworkContainsOrContainedBy"  => "&&",
						"RangeIsStrictlyLeftOf"         => "<<",
						"RangeIsStrictlyRightOf"        => ">>",
						"RangeDoesNotExtendRightOf"     => "&<",
						"RangeDoesNotExtendLeftOf"      => "&>",
						"RangeIsAdjacentTo"             => "-|-",
						"RangeUnion"                    => "+",
						"RangeIntersect"                => "*",
						"RangeExcept"                   => "-",
						"TextSearchMatch"               => "@@",
						"TextSearchAnd"                 => "&&",
						"TextSearchOr"                  => "||",
						"JsonExists"                    => "?",
						"JsonExistsAny"                 => "?|",
						"JsonExistsAll"                 => "?&",
						_ => throw new InvalidOperationException($"Unknown {PgBinaryExpressionName}.OperatorType: '{operand}'")
					};

					switch (operand)
					{
						case "Contains":
							operandExpr = "@>"; break;
						case "ContainedBy":
							operandExpr = "<@"; break;
						case "Overlaps":
							operandExpr = "&&"; break;
					}

					var text = $"{PrepareExpressionText(left)} {operandExpr} {PrepareExpressionText(right)}";

					return text;
				}
#endif

				return "NULL";
			}

			var converted = UnwrapConverted(newExpression);
			var expressionText = PrepareExpressionText(converted);
			var result = new Sql.ExpressionAttribute(expressionText)
				{ ServerSideOnly = true, IsPredicate = memberInfo.GetMemberType() == typeof(bool) };

			if (converted is SqlFunctionExpression or SqlFragmentExpression)
				result.Precedence = Precedence.Primary;

			return result;
		}

		private static Expression UnwrapConverted(Expression expr)
		{
			if (expr is SqlFunctionExpression func)
			{
				if (string.Equals(func.Name, "COALESCE", StringComparison.InvariantCultureIgnoreCase) &&
				    func.Arguments?.Count == 2 && func.Arguments[1].NodeType == ExpressionType.Extension)
					return UnwrapConverted(func.Arguments[0]);
			}

			return expr;
		}

		public MemberInfo[] GetDynamicColumns(Type type)
		{
			return [];
		}

		string IMetadataReader.GetObjectID() => _objectId;
	}
}

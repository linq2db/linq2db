using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	using Expressions;
	using Linq;
	using Linq.Builder;
	using LinqToDB.Common;
	using Mapping;
	using Reflection;

	class RecordReaderBuilder
	{
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(IDataReader),  "rd");
		public        readonly ParameterExpression DataReaderLocal;

		public readonly List<ParameterExpression>  BlockVariables   = new ();
		public readonly List<Expression>           BlockExpressions = new ();

		public IDataContext           DataContext   { get; }
		public MappingSchema          MappingSchema { get; }
		public Type                   ObjectType    { get; }
		public Type                   OriginalType  { get; }
		public IDataReader            Reader        { get; }
		public Dictionary<string,int> ReaderIndexes { get; }

		int                  _varIndex;
		ParameterExpression? _variable;

		public RecordReaderBuilder(IDataContext dataContext, Type objectType, IDataReader reader, LambdaExpression? converterExpr)
		{
			DataContext   = dataContext;
			MappingSchema = dataContext.MappingSchema;
			OriginalType  = objectType;
			ObjectType    = objectType;
			Reader        = reader;
			ReaderIndexes = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, static i => i, MappingSchema.ColumnNameComparer);

			var typedDataReader = Expression.Convert(DataReaderParam, reader.GetType());
			DataReaderLocal     = BuildVariable(converterExpr?.GetBody(typedDataReader) ?? typedDataReader, "ldr");
		}

		Expression BuildReadExpression(bool buildBlock, Type objectType)
		{
			if (buildBlock && _variable != null)
				return _variable;

			var entityDescriptor = MappingSchema.GetEntityDescriptor(objectType);

			var recordType = RecordsHelper.GetRecordType(MappingSchema, objectType);
			var expr = recordType == RecordType.NotRecord
				? BuildDefaultConstructor(entityDescriptor, objectType)
				: BuildRecordConstructor (entityDescriptor, objectType, recordType);

			expr = ProcessExpression(expr);

			if (!buildBlock)
				return expr;

			return _variable = BuildVariable(expr);
		}

		private ParameterExpression BuildVariable(Expression expr, string? name = null)
		{
			if (name == null)
				name = expr.Type.Name + ++_varIndex;

			var variable = Expression.Variable(
				expr.Type,
				name.IndexOf('<') >= 0 ? null : name);

			BlockVariables.  Add(variable);
			BlockExpressions.Add(Expression.Assign(variable, expr));

			return variable;
		}

		int GetReaderIndex(string columnName)
		{
			if (!ReaderIndexes.TryGetValue(columnName, out var value))
				return -1;
			return value;
		}

		int GetReaderIndex(EntityDescriptor entityDescriptor, Type? objectType, string name)
		{
			if (!ReaderIndexes.TryGetValue(name, out var value))
			{
				foreach (var cd in entityDescriptor.Columns)
				{
					if ((objectType == null || cd.MemberAccessor.TypeAccessor.Type == objectType) && cd.MemberName == name)
					{
						return GetReaderIndex(cd.ColumnName);
					}
				}

				return -1;
			}

			return value;
		}

		IEnumerable<ReadColumnInfo> GetReadIndexes(EntityDescriptor entityDescriptor)
		{
			foreach (var c in entityDescriptor.Columns)
			{
				if (c.MemberAccessor.TypeAccessor.Type == entityDescriptor.ObjectType)
				{
					var index = GetReaderIndex(c.ColumnName);
					if (index >= 0)
					{
						yield return new ReadColumnInfo() { ReaderIndex = index, Column = c };
					}
				}
			}
		}

		Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, Type objectType)
		{
			var members = new List<(ColumnDescriptor column, ConvertFromDataReaderExpression expr)>();
			foreach (var info in GetReadIndexes(entityDescriptor))
			{
				if (info.Column.Storage != null ||
					  info.Column.MemberAccessor.MemberInfo is not PropertyInfo pi ||
					  pi.GetSetMethod(true) != null)
				{
					members.Add((
						info.Column,
						new ConvertFromDataReaderExpression(info.Column.StorageType, info.ReaderIndex, info.Column.ValueConverter, DataReaderLocal, DataContext)));
				}
			}

			var initExpr = Expression.MemberInit(
				Expression.New(objectType),
				members
					// IMPORTANT: refactoring this condition will affect hasComplex variable calculation below
					.Where (static m => !m.column.MemberAccessor.IsComplex)
					.Select(static m => (MemberBinding)Expression.Bind(m.column.StorageInfo, m.expr)));

			Expression expr = initExpr;

			// added from TableContext.BuildDefaultConstructor without LoadWith functionality
			var hasComplex = members.Count > initExpr.Bindings.Count;

			if (hasComplex)
			{
				var obj   = Expression.Variable(expr.Type);
				var exprs = new List<Expression> { Expression.Assign(obj, expr) };

				foreach (var m in members)
				{
					if (m.column.MemberAccessor.IsComplex)
					{
						exprs.Add(m.column.MemberAccessor.SetterExpression.GetBody(obj, m.expr));
					}
				}

				exprs.Add(obj);

				expr = Expression.Block(new[] { obj }, exprs);
			}

			return expr;
		}

		class ColumnInfo
		{
			public bool       IsComplex;
			public string     Name       = null!;
			public Expression Expression = null!;
		}

		IEnumerable<Expression?> GetExpressions(TypeAccessor typeAccessor, RecordType recordType, List<ColumnInfo> columns)
		{
			var members = typeAccessor.Members;
			if (recordType == RecordType.FSharp)
			{
				members = new List<MemberAccessor>();
				foreach (var member in typeAccessor.Members)
				{
					if (-1 != RecordsHelper.GetFSharpRecordMemberSequence(MappingSchema, typeAccessor.Type, member.MemberInfo))
						members.Add(member);
				}
			}

			foreach (var member in members)
			{
				ColumnInfo? column = null;
				foreach (var c in columns)
				{
					if (!c.IsComplex && c.Name == member.Name)
					{
						column = c;
						break;
					}
				}

				if (column != null)
				{
					yield return column.Expression;
				}
				else
				{
					// HERE was removed associations and LoadWith

					var name = member.Name + '.';
					var cols = new List<ColumnInfo>();
					foreach (var c in columns)
					{
						if (c.IsComplex && c.Name.StartsWith(name))
							cols.Add(c);
					}

					if (cols.Count == 0)
					{
						yield return null;
					}
					else
					{
						foreach (var col in cols)
						{
							col.Name = col.Name.Substring(name.Length);
							col.IsComplex = col.Name.Contains(".");
						}

						var typeAcc          = TypeAccessor.GetAccessor(member.Type);
						var memberRecordType = RecordsHelper.GetRecordType(MappingSchema, member.Type);

						var exprs = GetExpressions(typeAcc, memberRecordType, cols).ToList();

						if (memberRecordType != RecordType.NotRecord)
						{
							var ctor      = member.Type.GetConstructors().Single();
							var ctorParms = ctor.GetParameters();

							var parms = new List<Expression>();
							for (var i = 0; i < ctorParms.Length; i++)
							{
								var p = ctorParms[i];
								var e = (exprs.Count > i ? exprs[i] : null)
									?? Expression.Constant(p.DefaultValue ?? MappingSchema.GetDefaultValue(p.ParameterType), p.ParameterType);
								parms.Add(e);
							}

							yield return Expression.New(ctor, parms);
						}
						else
						{
							var bindings = new List<MemberBinding>();
							for (var i = 0; i < typeAcc.Members.Count && i < exprs.Count; i++)
							{
								if (exprs[i] != null)
								{
									bindings.Add(Expression.Bind(typeAcc.Members[i].MemberInfo, exprs[i]!));
								}
							}

							var expr = Expression.MemberInit(Expression.New(member.Type), bindings);

							yield return expr;
						}
					}
				}
			}
		}

		Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, Type objectType, RecordType recordType)
		{
			var ctor  = objectType.GetConstructors().Single();

			var columns = new List<ColumnInfo>();
			foreach (var info in GetReadIndexes(entityDescriptor))
			{
				columns.Add(new ColumnInfo
				{
					IsComplex  = info.Column.MemberAccessor.IsComplex,
					Name       = info.Column.MemberName,
					Expression = new ConvertFromDataReaderExpression(info.Column.MemberType, info.ReaderIndex, info.Column.ValueConverter, DataReaderLocal, DataContext)
				});
			}

			var exprs = GetExpressions(entityDescriptor.TypeAccessor, recordType, columns);

			var parameters       = ctor.GetParameters();
			var parms            = new Expression[parameters.Length];
			using var enumerator = exprs.GetEnumerator();

			for (var i = 0; i < parameters.Length; i++)
			{
				Expression? e = null;
				if (enumerator.MoveNext())
					e = enumerator.Current;

				parms[i] = e ?? Expression.Constant(MappingSchema.GetDefaultValue(parameters[i].ParameterType), parameters[i].ParameterType);
			}

			var expr = Expression.New(ctor, parms);

			return expr;
		}

		protected virtual Expression ProcessExpression(Expression expression)
		{
			return expression;
		}

		class ReadColumnInfo
		{
			public int              ReaderIndex;
			public ColumnDescriptor Column = null!;
		}


		public Func<IDataReader, T> BuildReaderFunction<T>()
		{
			var expr   = BuildReaderExpression();

			var lambda = Expression.Lambda<Func<IDataReader,T>>(BuildBlock(expr), DataReaderParam);

			if (Configuration.OptimizeForSequentialAccess)
				lambda = (Expression<Func<IDataReader, T>>)SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(lambda, Reader.FieldCount, reduce: true);

			return lambda.CompileExpression();
		}

		private Expression BuildReaderExpression()
		{
			if (MappingSchema.IsScalarType(ObjectType))
			{
				return new ConvertFromDataReaderExpression(ObjectType, 0, null, DataReaderLocal, DataContext);
			}

			var entityDescriptor   = MappingSchema.GetEntityDescriptor(ObjectType);
			var inheritanceMapping = entityDescriptor.InheritanceMapping;

			if (inheritanceMapping.Count == 0)
			{
				return BuildReadExpression(true, ObjectType);
			}

			Expression? expr = null;

			var defaultMapping = inheritanceMapping.SingleOrDefault(static m => m.IsDefault);

			if (defaultMapping != null)
			{
				expr = Expression.Convert(
					BuildReadExpression(false, defaultMapping.Type),
					ObjectType);
			}
			else
			{
				var dindex          = GetReaderIndex(entityDescriptor, null, inheritanceMapping[0].DiscriminatorName);

				if (dindex >= 0)
				{
					expr = Expression.Convert(
						Expression.Call(
							null,
							Methods.LinqToDB.Exceptions.DefaultInheritanceMappingException,
							new ConvertFromDataReaderExpression(typeof(object), dindex, null, DataReaderLocal, DataContext),
							Expression.Constant(ObjectType)),
						ObjectType);
				}
			}

			if (expr == null)
			{
				return BuildReadExpression(true, ObjectType);
			}

			foreach (var mapping in inheritanceMapping.Select(static (m,i) => new { m, i }))
			{
				if (mapping.m == defaultMapping)
					continue;

				var ed     = MappingSchema.GetEntityDescriptor(mapping.m.Type);
				var dindex = GetReaderIndex(ed, null, mapping.m.DiscriminatorName);

				if (dindex >= 0)
				{
					Expression testExpr;
					
					var isNullExpr = Expression.Call(
						DataReaderLocal,
						ReflectionHelper.DataReader.IsDBNull,
						ExpressionInstances.Int32Array(dindex));

					if (mapping.m.Code == null)
					{
						testExpr = isNullExpr;
					}
					else
					{
						var codeType = mapping.m.Code.GetType();

						testExpr = ExpressionBuilder.Equal(
							MappingSchema,
							new ConvertFromDataReaderExpression(codeType, dindex, mapping.m.Discriminator.ValueConverter, DataReaderLocal, DataContext),
							Expression.Constant(mapping.m.Code));

						if (mapping.m.Discriminator.CanBeNull)
						{
							testExpr =
								Expression.AndAlso(
									Expression.Not(isNullExpr),
									testExpr);
						}
					}

					expr = Expression.Condition(
						testExpr,
						Expression.Convert(BuildReadExpression(false, mapping.m.Type),
							ObjectType),
						expr);
				}
			}

			return expr;
		}

		private Expression BuildBlock(Expression expression)
		{
			if (BlockExpressions.Count == 0)
				return expression;

			BlockExpressions.Add(expression);

			expression = Expression.Block(BlockVariables, BlockExpressions);

			while (BlockVariables.  Count > 1) BlockVariables.  RemoveAt(BlockVariables.  Count - 1);
			while (BlockExpressions.Count > 1) BlockExpressions.RemoveAt(BlockExpressions.Count - 1);

			return expression;
		}

	}
}

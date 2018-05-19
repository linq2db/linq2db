using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	using Expressions;
	using Extensions;
	using Linq;
	using Linq.Builder;
	using Mapping;
	using Reflection;

	class RecordReaderBuilder
	{
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(IDataReader),  "rd");
		public        readonly ParameterExpression DataReaderLocal;

		public readonly List<ParameterExpression>  BlockVariables   = new List<ParameterExpression>();
		public readonly List<Expression>           BlockExpressions = new List<Expression>();

		public IDataContext           DataContext   { get; }
		public MappingSchema          MappingSchema { get; }
		public Type                   ObjectType    { get; }
		public Type                   OriginalType  { get; }
		public IDataReader            Reader        { get; }
		public Dictionary<string,int> ReaderIndexes { get; }

		int                 _varIndex;
		ParameterExpression _variable;

		public RecordReaderBuilder(IDataContext dataContext, Type objectType, IDataReader reader)
		{
			DataContext   = dataContext;
			MappingSchema = dataContext.MappingSchema;
			OriginalType  = objectType;
			ObjectType    = objectType;
			Reader        = reader;
			ReaderIndexes = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => i, MappingSchema.ColumnNameComparer);

			if (Common.Configuration.AvoidSpecificDataProviderAPI)
			{
				DataReaderLocal = DataReaderParam;
			}
			else
			{
				DataReaderLocal = BuildVariable(Expression.Convert(DataReaderParam, dataContext.DataReaderType), "ldr");
			}
		}

		static object DefaultInheritanceMappingException(object value, Type type)
		{
			throw new LinqException("Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.", value, type);
		}

		static bool IsRecord(IEnumerable<Attribute> attrs)
		{
			return  attrs.Any(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute")
				&& !attrs.Any(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CLIMutableAttribute");
		}

		Expression BuildReadExpression(bool buildBlock, Type objectType)
		{
			if (buildBlock && _variable != null)
				return _variable;

			var entityDescriptor = MappingSchema.GetEntityDescriptor(objectType);

			bool isRecord = IsRecord(MappingSchema.GetAttributes<Attribute>(objectType));

			var expr = isRecord == false
				? BuildDefaultConstructor(entityDescriptor, objectType)
				: BuildRecordConstructor (entityDescriptor, objectType);

			expr = ProcessExpression(expr);

			if (!buildBlock)
				return expr;

			return _variable = BuildVariable(expr);
		}

		public ParameterExpression BuildVariable(Expression expr, string name = null)
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
			int value;
			if (!ReaderIndexes.TryGetValue(columnName, out value))
				return -1;
			return value;
		}

		int GetReaderIndex(EntityDescriptor entityDescriptor, Type objectType, string name)
		{
			if (!ReaderIndexes.TryGetValue(name, out var value))
			{
				var cd = entityDescriptor.Columns.Find(c =>
					(objectType == null || c.MemberAccessor.TypeAccessor.Type == objectType) && c.MemberName == name);

				if (cd != null)
					return GetReaderIndex(cd.ColumnName);

				return -1;
			}

			return value;
		}

		IEnumerable<ReadColumnInfo> GetReadIndexes(EntityDescriptor entityDescriptor)
		{
			var result = from c in entityDescriptor.Columns
				where c.MemberAccessor.TypeAccessor.Type == entityDescriptor.ObjectType
				let   index = GetReaderIndex(c.ColumnName)
				where index >= 0
				select new ReadColumnInfo {ReaderIndex = index, Column = c};
			return result;
		}

		Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, Type objectType)
		{
			var members =
			(
				from info in GetReadIndexes(entityDescriptor)
				where info.Column.Storage != null ||
				      !(info.Column.MemberAccessor.MemberInfo is PropertyInfo) ||
				      ((PropertyInfo) info.Column.MemberAccessor.MemberInfo).GetSetMethodEx(true) != null
				select new
				{
					Column = info.Column,
					Expr   = new ConvertFromDataReaderExpression(info.Column.StorageType, info.ReaderIndex, DataReaderLocal, DataContext)
				}
			).ToList();

			Expression expr = Expression.MemberInit(
				Expression.New(objectType),
				members
					.Where (m => !m.Column.MemberAccessor.IsComplex)
					.Select(m => (MemberBinding)Expression.Bind(m.Column.StorageInfo, m.Expr)));

			return expr;
		}

		class ColumnInfo
		{
			public bool       IsComplex;
			public string     Name;
			public Expression Expression;
		}

		IEnumerable<Expression> GetExpressions(TypeAccessor typeAccessor, bool isRecordType, List<ColumnInfo> columns)
		{
			var members = isRecordType ?
				typeAccessor.Members.Where(m =>
					IsRecord(MappingSchema.GetAttributes<Attribute>(typeAccessor.Type, m.MemberInfo))) :
				typeAccessor.Members;

			foreach (var member in members)
			{
				var column = columns.FirstOrDefault(c => !c.IsComplex && c.Name == member.Name);

				if (column != null)
				{
					yield return column.Expression;
				}
				else
				{
					// HERE was removed associations and LoadWith

					var name = member.Name + '.';
					var cols = columns.Where(c => c.IsComplex && c.Name.StartsWith(name)).ToList();

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

						var typeAcc  = TypeAccessor.GetAccessor(member.Type);
						var isRecord = IsRecord(MappingSchema.GetAttributes<Attribute>(member.Type));

						var exprs = GetExpressions(typeAcc, isRecord, cols).ToList();

						if (isRecord)
						{
							var ctor      = member.Type.GetConstructorsEx().Single();
							var ctorParms = ctor.GetParameters();

							var parms =
							(
								from p in ctorParms.Select((p, i) => new {p, i})
								join e in exprs.Select((e, i) => new {e, i}) on p.i equals e.i into j
								from e in j.DefaultIfEmpty()
								select
									(e == null ? null : e.e) ??
									Expression.Constant(p.p.DefaultValue ?? MappingSchema.GetDefaultValue(p.p.ParameterType),
										p.p.ParameterType)
							).ToList();

							yield return Expression.New(ctor, parms);
						}
						else
						{
							var expr = Expression.MemberInit(
								Expression.New(member.Type),
								from m in typeAcc.Members.Zip(exprs, (m, e) => new {m, e})
								where m.e != null
								select (MemberBinding) Expression.Bind(m.m.MemberInfo, m.e));

							yield return expr;
						}
					}
				}
			}
		}

		Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, Type objectType)
		{
			var ctor  = objectType.GetConstructorsEx().Single();

			var exprs = GetExpressions(entityDescriptor.TypeAccessor, true,
				(
					from info in GetReadIndexes(entityDescriptor)
					select new ColumnInfo
					{
						IsComplex  = info.Column.MemberAccessor.IsComplex,
						Name       = info.Column.MemberName,
						Expression = new ConvertFromDataReaderExpression(info.Column.MemberType, info.ReaderIndex, DataReaderLocal, DataContext)
					}
				).ToList()).ToList();

			var parms =
			(
				from p in ctor.GetParameters().Select((p,i) => new { p, i })
				join e in exprs.Select((e,i) => new { e, i }) on p.i equals e.i into j
				from e in j.DefaultIfEmpty()
				select
					(e == null ? null : e.e) ?? Expression.Constant(MappingSchema.GetDefaultValue(p.p.ParameterType), p.p.ParameterType)
			).ToList();

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
			public ColumnDescriptor Column;
		}


		public Func<IDataReader, T> BuildReaderFunction<T>()
		{
			var expr   = BuildReaderExpression();

			var lambda = Expression.Lambda<Func<IDataReader,T>>(BuildBlock(expr), DataReaderParam);

			return lambda.Compile();
		}

		public Expression BuildReaderExpression()
		{
			if (MappingSchema.IsScalarType(ObjectType))
			{
				return new ConvertFromDataReaderExpression(ObjectType, 0, DataReaderLocal, DataContext);
			}

			var entityDescriptor   = MappingSchema.GetEntityDescriptor(ObjectType);
			var inheritanceMapping = entityDescriptor.InheritanceMapping;

			if (inheritanceMapping.Count == 0)
			{
				return BuildReadExpression(true, ObjectType);
			}

			Expression expr = null;

			var defaultMapping = inheritanceMapping.SingleOrDefault(m => m.IsDefault);

			if (defaultMapping != null)
			{
				expr = Expression.Convert(
					BuildReadExpression(false, defaultMapping.Type),
					ObjectType);
			}
			else
			{
				var exceptionMethod = MemberHelper.MethodOf(() => DefaultInheritanceMappingException(null, null));
				var dindex          = GetReaderIndex(entityDescriptor, null, inheritanceMapping[0].DiscriminatorName);

				if (dindex >= 0)
				{
					expr = Expression.Convert(
						Expression.Call(null, exceptionMethod,
							Expression.Call(
								DataReaderLocal,
								ReflectionHelper.DataReader.GetValue,
								Expression.Constant(dindex)),
							Expression.Constant(ObjectType)),
						ObjectType);
				}
			}

			if (expr == null)
			{
				return BuildReadExpression(true, ObjectType);
			}

			foreach (var mapping in inheritanceMapping.Select((m,i) => new { m, i }).Where(m => m.m != defaultMapping))
			{
				var ed     = MappingSchema.GetEntityDescriptor(mapping.m.Type);
				var dindex = GetReaderIndex(ed, null, mapping.m.DiscriminatorName);

				if (dindex >= 0)
				{

					Expression testExpr;

					var isNullExpr = Expression.Call(
						DataReaderLocal,
						ReflectionHelper.DataReader.IsDBNull,
						Expression.Constant(dindex));

					if (mapping.m.Code == null)
					{
						testExpr = isNullExpr;
					}
					else
					{
						var codeType = mapping.m.Code.GetType();

						testExpr = ExpressionBuilder.Equal(
							MappingSchema,
							new ConvertFromDataReaderExpression(codeType, dindex, DataReaderLocal, DataContext),
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

		public Expression BuildBlock(Expression expression)
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

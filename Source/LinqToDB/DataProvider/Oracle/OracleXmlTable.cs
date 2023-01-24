﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.DataProvider.Oracle
{
	using Common.Internal;
	using Expressions;
	using SqlProvider;
	using Mapping;
	using SqlQuery;

	public static partial class OracleTools
	{
		sealed class OracleXmlTableAttribute : Sql.TableExpressionAttribute
		{
			public OracleXmlTableAttribute()
				: base("")
			{
			}

			static string GetDataTypeText(SqlDataType type)
			{
				switch (type.Type.DataType)
				{
					case DataType.DateTime   : return "timestamp";
					case DataType.DateTime2  : return "timestamp";
					case DataType.UInt32     :
					case DataType.Int64      : return "Number(19)";
					case DataType.SByte      :
					case DataType.Byte       : return "Number(3)";
					case DataType.Money      : return "Number(19,4)";
					case DataType.SmallMoney : return "Number(10,4)";
					case DataType.NVarChar   : return "VarChar2(" + (type.Type.Length ?? 100) + ")";
					case DataType.NChar      : return "Char2(" + (type.Type.Length ?? 100) + ")";
					case DataType.Double     : return "Float";
					case DataType.Single     : return "Real";
					case DataType.UInt16     : return "Int";
					case DataType.UInt64     : return "Decimal";
					case DataType.Int16      : return "SmallInt";
					case DataType.Int32      : return "Int";
					case DataType.Boolean    : return "Bit";
				}

				var text = !string.IsNullOrEmpty(type.Type.DbType) ? type.Type.DbType! : type.Type.DataType.ToString();

				if (type.Type.Length > 0)
					text += "(" + type.Type.Length + ")";
				else if (type.Type.Precision > 0)
					text += "(" + type.Type.Precision + "," + type.Type.Scale + ")";

				return text;
			}

			static string ValueConverter(IReadOnlyList<Action<StringBuilder,object>> converters, object obj)
			{
				using var sb = Pools.StringBuilder.Allocate();
				sb.Value.Append("<t>").AppendLine();

				foreach (var item in (IEnumerable)obj)
				{
					sb.Value.Append("<r>");

					for (var i = 0; i < converters.Count; i++)
					{
						sb.Value.Append("<c" + i + ">");
						converters[i](sb.Value, item!);
						sb.Value.Append("</c" + i + ">");
					}

					sb.Value.AppendLine("</r>");
				}

				return sb.Value.AppendLine("</t>").ToString();
			}

			internal static Func<object,string> GetXmlConverter(MappingSchema mappingSchema, SqlTable sqlTable)
			{
				var ed  = mappingSchema.GetEntityDescriptor(sqlTable.ObjectType);

				var converters = new Action<StringBuilder,object>[ed.Columns.Count];
				for (var i = 0; i < ed.Columns.Count; i++)
				{
					var c = ed.Columns[i];
					var conv = mappingSchema.ValueToSqlConverter;

					converters[i] = (sb, obj) =>
					{
						var value = c.GetProviderValue(obj);

						if (value is string && c.MemberType == typeof(string))
						{
							using var sbv = Pools.StringBuilder.Allocate();
							var str = conv.Convert(sbv.Value, mappingSchema, null!, value).ToString();

							if (str.Length > 2)
							{
								str = str.Substring(1);
								str = str.Substring(0, str.Length - 1);
								sb.Append(str);
							}
						}
						else
							conv.Convert(sb, mappingSchema, null!, value);
					};
				}

				return o => ValueConverter(converters, o);
			}

			public override void SetTable<TContext>(TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				var exp = methodCall.Arguments[1];
				var arg = converter(context, exp, null);
				var ed  = mappingSchema.GetEntityDescriptor(table.ObjectType);

				if (arg is SqlParameter p)
				{
					exp = exp.Unwrap();

					// TODO: ValueConverter contract nullability violations
					if (exp is ConstantExpression constExpr)
					{
						if (constExpr.Value is Func<string>)
							p.ValueConverter = static l => ((Func<string>)l!)();
						else
							p.ValueConverter = GetXmlConverter(mappingSchema, table)!;
					}
					else if (exp is LambdaExpression)
					{
						p.ValueConverter = static l => ((Func<string>)l!)();
					}
				}

				using var columns = Pools.StringBuilder.Allocate();
				for (var i = 0; i < ed.Columns.Count; i++)
				{
					if (i > 0)
						columns.Value.Append(", ");

					var  c= ed.Columns[i];

					columns.Value.AppendFormat(
						"{0} {1} path 'c{2}'",
						sqlBuilder.ConvertInline(c.ColumnName, ConvertType.NameToQueryField),
						string.IsNullOrEmpty(c.DbType)
							? GetDataTypeText(
								new SqlDataType(
									c.DataType == DataType.Undefined ? mappingSchema.GetDataType(c.MemberType).Type.DataType : c.DataType,
									c.MemberType,
									c.Length,
									c.Precision,
									c.Scale,
									c.DbType))
							: c.DbType,
						i);
				}

				table.SqlTableType   = SqlTableType.Expression;
				table.Expression     = $"XmlTable(\'/t/r\' PASSING XmlType({{2}}) COLUMNS {columns.Value}) {{1}}";
				table.TableArguments = new[] { arg };
			}
		}

		public static string GetXmlData<T>(MappingSchema mappingSchema, IEnumerable<T> data)
		{
			var sqlTable = new SqlTable(mappingSchema, typeof(T));
			return GetXmlData(mappingSchema, sqlTable, data);
		}

		static string GetXmlData<T>(MappingSchema mappingSchema, SqlTable sqlTable, IEnumerable<T> data)
		{
			var converter  = OracleXmlTableAttribute.GetXmlConverter(mappingSchema, sqlTable);
			return converter(data);
		}

		private static readonly MethodInfo OracleXmlTableIEnumerableT = MemberHelper.MethodOf(() => OracleXmlTable<object>(null!, (IEnumerable<object>)null!)).GetGenericMethodDefinition();
		private static readonly MethodInfo OracleXmlTableString       = MemberHelper.MethodOf(() => OracleXmlTable<object>(null!, (string)null!))             .GetGenericMethodDefinition();
		private static readonly MethodInfo OracleXmlTableFuncString   = MemberHelper.MethodOf(() => OracleXmlTable<object>(null!, (Func<string>)null!))       .GetGenericMethodDefinition();

		[OracleXmlTable]
		public static ITable<T> OracleXmlTable<T>(this IDataContext dataContext, IEnumerable<T> data)
			where T : class
		{
			return dataContext.GetTable<T>(
				null,
				OracleXmlTableIEnumerableT.MakeGenericMethod(typeof(T)),
				dataContext,
				data);
		}

		[OracleXmlTable]
		public static ITable<T> OracleXmlTable<T>(this IDataContext dataContext, string xmlData)
			where T : class
		{
			return dataContext.GetTable<T>(
				null,
				OracleXmlTableString.MakeGenericMethod(typeof(T)),
				dataContext,
				xmlData);
		}

		[OracleXmlTable]
		public static ITable<T> OracleXmlTable<T>(this IDataContext dataContext, Func<string> xmlData)
			where T : class
		{
			return dataContext.GetTable<T>(
				null,
				OracleXmlTableFuncString.MakeGenericMethod(typeof(T)),
				dataContext,
				xmlData);
		}
	}
}

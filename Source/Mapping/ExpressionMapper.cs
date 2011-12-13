using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Data.Linq;
using LinqToDB.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Mapping
{
	class Mapper<TS,TD>
	{
		public Func<TS,MappingContext,TD> Map;
	}

	class MappingContext
	{
		public Dictionary<object,object> Objects;
		public Func<object,object>       GetParent;
		public List<Action<object>>      CrossActions;
		public Dictionary<object,List<Action<object,object>>> Crosses;
	}

	class MappingParameters
	{
		public   MappingSchema             MappingSchema;
		public   bool                      DeepCopy              = true;
		public   bool                      HandleCrossReferences = true;

		public   Dictionary<object,object> MapperList     = new Dictionary<object,object>();

		public   bool                      UseContext;
		public   bool                      ContextParameterUsed;

		readonly ParameterExpression      _mappingContext = Expression.Parameter(typeof(MappingContext), "ctx");
		public   ParameterExpression       MappingContext
		{
			get
			{
				ContextParameterUsed = true;
				return _mappingContext;
			}
		}
	}

	public class ExpressionMapper<TSource,TDest>
	{
		readonly MappingParameters     _parameters;
		private  Func<object,object>   _getCurrent;
		private  Action<object,object> _setCurrent;

		public bool DeepCopy             { get { return _parameters.DeepCopy;              } set { _parameters.DeepCopy              = value; } }
		public bool HandleBackReferences { get { return _parameters.HandleCrossReferences; } set { _parameters.HandleCrossReferences = value; } }

		public ExpressionMapper()
			: this(Map.DefaultSchema)
		{
		}

		public ExpressionMapper(MappingSchema mappingSchema)
		{
			_parameters = new MappingParameters { MappingSchema = mappingSchema };
		}

		ExpressionMapper(MappingParameters parameters)
		{
			_parameters = parameters;
		}

		#region Value Converter

		interface IValueConvertHelper
		{
			Expression GetConverter   (Expression source);
			Expression CheckNull      (ExpressionMapper<TSource,TDest> mapper, Expression source, object nullValue, MapValue[] mapValues, object defaultValue, MapValue[] srcMapValues);
			Expression SourceMapValues(ExpressionMapper<TSource,TDest> mapper, Expression source, object nullValue, object defaultValue, MapValue[] srcMapValues);
			Expression DestMapValues  (ExpressionMapper<TSource,TDest> mapper, Expression source, object nullValue, MapValue[] mapValues, object defaultValue);
		}

		class ValueConvertHelper<TS,TD> : IValueConvertHelper
		{
			public Expression GetConverter(Expression source)
			{
				return Expression.Invoke(Expression.Constant(Convert<TD,TS>.From), source);
			}

			public Expression CheckNull(
				ExpressionMapper<TSource,TDest> mapper,
				Expression                      source,
				object                          nullValue,
				MapValue[]                      mapValues,
				object                          defaultValue,
				MapValue[]                      srcMapValues)
			{
				var param =
					source.NodeType != ExpressionType.MemberAccess &&
					source.NodeType != ExpressionType.Parameter    &&
					source.NodeType != ExpressionType.Constant?
						Expression.Parameter(typeof(TS), "p") :
						null;

				var nexpr = Expression.Constant(nullValue ?? default(TD));

				var expr =
					source.NodeType == ExpressionType.Constant && ((ConstantExpression)source).Value == null ?
						nexpr as Expression:
						Expression.Condition(
							Expression.Equal(param ?? source, Expression.Constant(null)),
							nexpr.Value == null ? Expression.Convert(nexpr, typeof(TD)) : nexpr as Expression,
							mapper.GetValueMapper(param ?? source, typeof(TD), false, null, mapValues, defaultValue, srcMapValues));

				return param == null ? expr : Expression.Invoke(Expression.Lambda<Func<TS,TD>>(expr, param), source);
			}

			public Expression SourceMapValues(
				ExpressionMapper<TSource,TDest> mapper,
				Expression                      source,
				object                          nullValue,
				object                          defaultValue,
				MapValue[]                      srcMapValues)
			{
				var param =
					//source.NodeType != ExpressionType.MemberAccess &&
					source.NodeType != ExpressionType.Parameter    &&
					source.NodeType != ExpressionType.Constant?
						Expression.Parameter(typeof(TS), "p") :
						null;

				var expr = mapper.GetValueMapper(Expression.Constant(defaultValue), typeof(TD), true, nullValue, null, null, null);

				for (var i = srcMapValues.Length - 1; i >= 0; i--)
				{
					var value = srcMapValues[i];

					expr = Expression.Condition(
						Expression.Equal(param ?? source, mapper.GetValueMapper(Expression.Constant(value.OrigValue), typeof(TS), false, null, null, null, null)),
						mapper.GetValueMapper(Expression.Constant(value.MapValues[0]), typeof(TD), true, nullValue, null, null, null),
						expr);
				}

				return param == null ? expr : Expression.Invoke(Expression.Lambda<Func<TS,TD>>(expr, param), source);
			}

			public Expression DestMapValues(
				ExpressionMapper<TSource,TDest> mapper,
				Expression                      source,
				object                          nullValue,
				MapValue[]                      mapValues,
				object                          defaultValue)
			{
				var param =
					//source.NodeType != ExpressionType.MemberAccess &&
					source.NodeType != ExpressionType.Parameter    &&
					source.NodeType != ExpressionType.Constant?
						Expression.Parameter(typeof(TS), "p") :
						null;

				var expr = mapper.GetValueMapper(Expression.Constant(defaultValue), typeof(TD), true, nullValue, null, null, null);

				for (var i = mapValues.Length - 1; i >= 0; i--)
				{
					var value = mapValues[i];
					var orex  = null as Expression;

					foreach (var mapValue in value.MapValues)
					{
						var ex = Expression.Equal(param ?? source, mapper.GetValueMapper(Expression.Constant(mapValue), typeof (TS), false, null, null, null, null));
						orex = orex == null ? ex : Expression.OrElse(orex, ex);
					}

					if (orex != null)
						expr = Expression.Condition(
							orex,
							mapper.GetValueMapper(Expression.Constant(value.OrigValue), typeof(TD), true, nullValue, null, null, null),
							expr);
				}

				return param == null ? expr : Expression.Invoke(Expression.Lambda<Func<TS,TD>>(expr, param), source);
			}
		}

		static IValueConvertHelper GetValueHelper(Type stype, Type dtype)
		{
			var type = typeof(ValueConvertHelper<,>).MakeGenericType(typeof(TSource), typeof(TDest), stype, dtype);
			return ((IValueConvertHelper)Activator.CreateInstance(type));
		}

		#endregion

		#region Object Converter

		interface IConvertHelper
		{
			Expression MapObjects(ExpressionMapper<TSource,TDest> mapper, Expression source);
			Expression MapLists  (ExpressionMapper<TSource,TDest> mapper, Expression source);
		}

		class ConvertHelper<TS,TD> : IConvertHelper
			where TS : class
			where TD : class
		{
			static TD MapCrossReferences(
				MappingContext        ctx,
				TS                    source,
				Func<TS,TD>           func,
				Func<object,object>   getCurrent,
				Action<object,object> setCurrent)
			{
				if (source == null)
					return null;

				object dest;
				List<Action<object,object>> list;

				if (ctx.Objects.TryGetValue(source, out dest))
				{
					if (dest == null)
					{
						if (ctx.Crosses == null)
							ctx.Crosses = new Dictionary<object,List<Action<object,object>>>();

						if (!ctx.Crosses.TryGetValue(source, out list))
							ctx.Crosses[source] = list = new List<Action<object,object>>();

						var getParent = ctx.GetParent;

						Action<object,object> setter = (obj,value) => setCurrent(getParent(obj), value);

						list.Add(setter);
					}

					return (TD)dest;
				}

				var currParent = ctx.GetParent;

				ctx.GetParent = p => getCurrent(currParent(p));
				ctx.Objects.Add(source, null);
				ctx.Objects[source] = dest = func(source);
				ctx.GetParent = currParent;

				if (ctx.Crosses != null && ctx.Crosses.TryGetValue(source, out list))
				{
					if (ctx.CrossActions == null)
						ctx.CrossActions = new List<Action<object>>();

					foreach (var action in list)
					{
						var setValue = action;

						Action<object> f = parent => setValue(parent, dest);
						ctx.CrossActions.Add(f);
					}

					ctx.Crosses.Remove(source);
				}

				return (TD)dest;
			}

			static TD MapObjects(TS source, Func<TS,TD> func)
			{
				return source == null ? null : func(source);
			}

			public Expression MapObjects(ExpressionMapper<TSource,TDest> mapper, Expression source)
			{
				var param = mapper._getCurrent == null ? (ParameterExpression)source : Expression.Parameter(source.Type, "source");

				Expression expr;
				object     m;

				if (mapper._parameters.MapperList.TryGetValue(new { S = typeof(TS), D = typeof(TD) }, out m))
				{
					var map = (Mapper<TS,TD>)m;

					if (map.Map == null)
					{
						expr = Expression.Invoke(
							Expression.PropertyOrField(Expression.Constant(map), "Map"),
							source, mapper._parameters.MappingContext);
					}
					else
					{
						expr = Expression.Invoke(Expression.Constant(map.Map), source, mapper._parameters.MappingContext);
					}
				}
				else
				{
					var exmap = new ExpressionMapper<TS,TD>(mapper._parameters);
					expr = exmap.GetMemberInit(param);
				}

				if (mapper._getCurrent == null)
					return expr;

				if (!mapper.HandleBackReferences)
				{
					Expression<Func<object>> func = () => MapObjects((TS)null, null);
					return Expression.Call((MethodInfo)ReflectionHelper.MemeberInfo(func), source, Expression.Lambda<Func<TS,TD>>(expr, param));
				}
				else
				{
					mapper._parameters.UseContext = true;

					Expression<Func<object>> func = () => MapCrossReferences(null, null, null, null, null);

					return Expression.Call(
						(MethodInfo)ReflectionHelper.MemeberInfo(func),
						mapper._parameters.MappingContext,
						source,
						Expression.Lambda<Func<TS,TD>>(expr, param),
						Expression.Constant(mapper._getCurrent),
						Expression.Constant(mapper._setCurrent));
				}
			}

			interface IItemHelper
			{
				Expression MapLists(ExpressionMapper<TSource,TDest> mapper, Expression source);
			}

			interface IClassItemHelper
			{
				MethodInfo GetObjectArrayInfo();
				MethodInfo GetObjectListInfo(bool isList);
			}

			class ClassItemHelper<TSourceItem,TDestItem> : IClassItemHelper
				where TSourceItem : class
				where TDestItem   : class
			{
				static TDestItem[] MapObjectArray(MappingContext ctx, IEnumerable<TSourceItem> source, Func<TSourceItem,TDestItem> itemMapper)
				{
					if (source == null)
						return null;

					if (source is ICollection)
					{
						var col  = (ICollection)source;
						var dest = new TDestItem[col.Count];
						var n    = 0;

						foreach (var item in source)
						{
							var current = n;
							dest[n++] = ConvertHelper<TSourceItem,TDestItem>.MapCrossReferences(
								ctx, item, itemMapper,
								_ => dest[current],
								(_,v) => { dest[current] = (TDestItem)v; });
						}

						return dest;
					}
					else
					{
						TDestItem[] dest = null;

						var list = new List<TDestItem>();
						var n    = 0;

						foreach (var item in source)
						{
							var current = n;
							list.Add(null);
							list[n++] = ConvertHelper<TSourceItem,TDestItem>.MapCrossReferences(
								ctx, item, itemMapper,
								_ => dest == null ? list[current] : dest[current],
								(_,v) => { if (dest == null) list[current] = (TDestItem)v; else dest[current] = (TDestItem)v; });
						}

						return dest = list.ToArray();
					}
				}

				[UsedImplicitly]
				static TList MapObjectList<TList>(MappingContext ctx, IEnumerable<TSourceItem> source, Func<TSourceItem,TDestItem> itemMapper)
					where TList : class, IList<TDestItem>, new()
				{
					if (source == null)
						return null;

					var n    = 0;
					var dest = source is ICollection && typeof(TList) == typeof(List<TDestItem>) ?
						(TList)(IList<TDestItem>)new List<TDestItem>(((ICollection)source).Count) : new TList();

					foreach (var item in source)
					{
						var current = n;
						dest.Add(null);
						dest[n++] = ConvertHelper<TSourceItem,TDestItem>.MapCrossReferences(
							ctx, item, itemMapper,
							_ => dest[current],
							(_,v) => { dest[current] = (TDestItem)v; });
					}

					return dest;
				}

				public MethodInfo GetObjectArrayInfo()
				{
					Expression<Func<object>> arrMapper = () => MapObjectArray(null, null, null);
					return (MethodInfo)ReflectionHelper.MemeberInfo(arrMapper);
				}

				public MethodInfo GetObjectListInfo(bool isList)
				{
					var method = typeof(ClassItemHelper<TSourceItem,TDestItem>).GetMethod("MapObjectList", BindingFlags.NonPublic | BindingFlags.Static);
					return method.MakeGenericMethod(isList ? typeof (List<TDestItem>) : typeof (TD));
				}
			}

			class ItemHelper<TSourceItem,TDestItem> : IItemHelper
			{
				static TDestItem[] MapScalarArray(IEnumerable<TSourceItem> source, Func<TSourceItem,TDestItem> itemMapper)
				{
					if (source == null)
						return null;

					if (source is ICollection)
					{
						var col  = (ICollection)source;
						var dest = new TDestItem[col.Count];
						var n    = 0;

						foreach (var item in source)
							dest[n++] = itemMapper(item);

						return dest;
					}

					return source.Select(itemMapper).ToArray();
				}

				[UsedImplicitly]
				static TList MapScalarList<TList>(IEnumerable<TSourceItem> source, Func<TSourceItem,TDestItem> itemMapper)
					where TList : class, IList<TDestItem>, new()
				{
					if (source == null)
						return null;

					var dest = new TList();
					var list = dest as List<TDestItem>;

					if (list != null)
						list.AddRange(source.Select(itemMapper));
					else
						foreach (var item in source)
							dest.Add(itemMapper(item));

					return dest;
				}

				public Expression MapLists(ExpressionMapper<TSource,TDest> mapper, Expression source)
				{
					var itemMapper =
						new ExpressionMapper<TSourceItem,TDestItem>(mapper._parameters);

					var itemParam = Expression.Parameter(typeof(TSourceItem), "item");
					var itemExpr  = itemMapper.GetValueMapper(
						itemParam,
						typeof(TDestItem),
						true,
						mapper._parameters.MappingSchema.GetNullValue   (typeof(TDestItem)),
						mapper._parameters.MappingSchema.GetMapValues   (typeof(TDestItem)),
						mapper._parameters.MappingSchema.GetDefaultValue(typeof(TDestItem)),
						mapper._parameters.MappingSchema.GetMapValues   (typeof(TSourceItem)));

					var itemLambda = Expression.Lambda<Func<TSourceItem,TDestItem>>(itemExpr, itemParam);

					var isSourceScalar = !typeof(TSourceItem).IsArray && TypeHelper.IsScalar(typeof(TSourceItem));
					var isDestScalar   = !typeof(TDestItem).  IsArray && TypeHelper.IsScalar(typeof(TDestItem));

					if (!mapper.HandleBackReferences || isSourceScalar || isDestScalar)
					{
						if (typeof (TD).IsArray)
						{
							Expression<Func<object>> arrMapper = () => MapScalarArray(null, null);
							return Expression.Call((MethodInfo)ReflectionHelper.MemeberInfo(arrMapper), source, itemLambda);
						}

						var isList =
							typeof (TD) == typeof (IEnumerable<TDestItem>) || typeof (TD) == typeof (ICollection<TDestItem>) ||
							typeof (TD) == typeof (IList<TDestItem>)       || typeof (TD) == typeof (List<TDestItem>);

						var method = typeof (ItemHelper<TSourceItem, TDestItem>).GetMethod("MapScalarList", BindingFlags.NonPublic | BindingFlags.Static);

						method = method.MakeGenericMethod(isList ? typeof (List<TDestItem>) : typeof (TD));

						return Expression.Call(method, source, itemLambda);
					}
					else
					{
						mapper._parameters.UseContext = true;

						var type = typeof (ClassItemHelper<,>).MakeGenericType(
							typeof (TSource), typeof (TDest),
							typeof (TS), typeof (TD),
							typeof (TSourceItem), typeof (TDestItem));

						var helper = ((IClassItemHelper)Activator.CreateInstance(type));

						if (typeof (TD).IsArray)
							return Expression.Call(helper.GetObjectArrayInfo(), mapper._parameters.MappingContext, source, itemLambda);

						var isList =
							typeof (TD) == typeof (IEnumerable<TDestItem>) || typeof (TD) == typeof (ICollection<TDestItem>) ||
							typeof (TD) == typeof (IList<TDestItem>) || typeof (TD) == typeof (List<TDestItem>);

						return Expression.Call(helper.GetObjectListInfo(isList), mapper._parameters.MappingContext, source, itemLambda);
					}
				}
			}

			public Expression MapLists(ExpressionMapper<TSource,TDest> mapper, Expression source)
			{
				var ts = TypeHelper.GetGenericType(typeof(IEnumerable<>), typeof(TS)).GetGenericArguments()[0];
				var td = TypeHelper.GetGenericType(typeof(IEnumerable<>), typeof(TD)).GetGenericArguments()[0];

				var type = typeof(ItemHelper<,>).MakeGenericType(typeof(TSource), typeof(TDest), typeof(TS), typeof(TD), ts, td);
				return ((IItemHelper)Activator.CreateInstance(type)).MapLists(mapper, source);
			}
		}

		static IConvertHelper GetHelper(Type stype, Type dtype)
		{
			var type = typeof(ConvertHelper<,>).MakeGenericType(typeof(TSource), typeof(TDest), stype, dtype);
			return ((IConvertHelper)Activator.CreateInstance(type));
		}

		#endregion

		Expression GetValueMapper(
			Expression source,
			Type       dtype,
			bool       checkNull,
			object     nullValue,
			MapValue[] destMapValues,
			object     defaultValue,
			MapValue[] srcMapValues)
		{
			var stype = source.Type;

			var isSourceScalar = !stype.IsArray && TypeHelper.IsScalar(stype);
			var isDestScalar   = !dtype.IsArray && TypeHelper.IsScalar(dtype);

			if (dtype == typeof(object) || dtype == stype && (!DeepCopy || isSourceScalar))
				return source;

			var isSourceNullable = TypeHelper.IsNullableType(stype) || stype.IsClass;

			if (checkNull && isSourceNullable && !TypeHelper.IsNullableType(dtype) && (isDestScalar || isSourceScalar))
				return GetValueHelper(stype, dtype).CheckNull(this, source, nullValue, destMapValues, defaultValue, srcMapValues);

			if (srcMapValues != null)
				return GetValueHelper(stype, dtype).SourceMapValues(this, source, nullValue, defaultValue, srcMapValues);

			if (destMapValues != null)
				return GetValueHelper(stype, dtype).DestMapValues(this, source, nullValue, destMapValues, defaultValue);

			if (dtype == typeof (string))
				return
					isSourceNullable
						?
							Expression.Condition(
								Expression.Equal(source, Expression.Constant(null)),
								Expression.Constant(null),
								Expression.Call(source, "ToString", Array<Type>.Empty)) as Expression
						:
							Expression.Call(source, "ToString", Array<Type>.Empty);

			if (!isDestScalar && !isSourceScalar)
			{
				if (TypeHelper.GetGenericType(typeof(IEnumerable<>), dtype) != null &&
				    TypeHelper.GetGenericType(typeof(IEnumerable<>), stype) != null)
					return GetHelper(stype, dtype).MapLists(this, source);

				return GetHelper(stype, dtype).MapObjects(this, source);
			}

			try
			{
				return Expression.Convert(source, dtype);
			}
			catch (InvalidOperationException)
			{
			}

			return GetValueHelper(stype, dtype).GetConverter(source);
		}

		IEnumerable<MemberBinding> GetBindings(Expression source)
		{
			var dest = _parameters.MappingSchema.GetObjectMapper(typeof(TDest));
			var src  = _parameters.MappingSchema.GetObjectMapper(typeof(TSource));

			foreach (MemberMapper dmm in dest)
			{
				if (dmm is MemberMapper.ComplexMapper)
					continue;

				var dma = dmm.MemberAccessor;

				if (!dma.HasSetter)
					continue;

				var smm = src[dmm.Name];

				if (smm == null || smm is MemberMapper.ComplexMapper)
					continue;

				var sma = smm.MemberAccessor;

				if (!sma.HasGetter)
					continue;

				_getCurrent = dma.GetValue;
				_setCurrent = dma.SetValue;

				var bind = Expression.Bind(
					dma.MemberInfo,
					GetValueMapper(
						Expression.MakeMemberAccess(source, sma.MemberInfo),
						dma.Type,
						true,
						dmm.MapMemberInfo.NullValue,
						dmm.MapMemberInfo.MapValues,
						dmm.MapMemberInfo.DefaultValue,
						smm.MapMemberInfo.MapValues));

				yield return bind;
			}

			var destMembers = from m in ((IEnumerable<MemberAccessor>)dest.TypeAccessor) select m;

			destMembers = destMembers.Except(dest.Select(mm => mm.MemberAccessor));

			var srcMembers = from m in ((IEnumerable<MemberAccessor>)src.TypeAccessor) select m;

			srcMembers = srcMembers.Except(src.Select(mm => mm.MemberAccessor));

			foreach (var dma in destMembers)
			{
				if (!dma.HasSetter)
					continue;

				var sma = srcMembers.FirstOrDefault(mi => mi.Name == dma.Name);

				if (sma == null || !sma.HasGetter)
					continue;

				_getCurrent = dma.GetValue;
				_setCurrent = dma.SetValue;

				var bind = Expression.Bind(
					dma.MemberInfo,
					GetValueMapper(
						Expression.MakeMemberAccess(source, sma.MemberInfo),
						dma.Type,
						true,
						_parameters.MappingSchema.GetNullValue   (dma.Type),
						_parameters.MappingSchema.GetMapValues   (dma.Type),
						_parameters.MappingSchema.GetDefaultValue(dma.Type),
						_parameters.MappingSchema.GetMapValues   (sma.Type)));

				yield return bind;
			}
		}

		Expression GetMemberInit(ParameterExpression source)
		{
			var mapper = new Mapper<TSource,TDest>();

			_parameters.MapperList.Add(new { S = typeof(TSource), D = typeof(TDest) }, mapper);

			var dest = TypeAccessor<TDest>.Instance;
			var expr = Expression.MemberInit(Expression.New(dest.Type), GetBindings(source));

			mapper.Map = Expression.Lambda<Func<TSource,MappingContext,TDest>>(expr, source, _parameters.MappingContext).Compile();

			return expr;
		}

		public Func<TSource,TDest> GetMapper()
		{
			if (typeof(TSource) == typeof(TDest) && !DeepCopy)
				return s => (TDest)(object)s;

			var parm = Expression.Parameter(typeof(TSource), "src");
			var expr = GetValueMapper(
				parm,
				typeof(TDest),
				true,
				_parameters.MappingSchema.GetNullValue   (typeof(TDest)),
				_parameters.MappingSchema.GetMapValues   (typeof(TDest)),
				_parameters.MappingSchema.GetDefaultValue(typeof(TDest)),
				_parameters.MappingSchema.GetMapValues   (typeof(TSource)));

			if (_parameters.ContextParameterUsed)
			{
				var l = Expression.Lambda<Func<TSource,MappingContext,TDest>>(expr, parm, _parameters.MappingContext);
				var f = l.Compile();

				if (!_parameters.UseContext)
					return s => f(s, null);

				return s =>
				{
					var ctx  = new MappingContext
					{
						Objects   = new Dictionary<object,object>(10) { { s, null } },
						GetParent = p => p,
					};

					var dest = f(s, ctx);

					if (ctx.CrossActions != null)
						foreach (var circle in ctx.CrossActions)
							circle(dest);

					if (ctx.Crosses != null)
					{
						List<Action<object,object>> list;

						if (ctx.Crosses.TryGetValue(s, out list))
							foreach (var action in list)
								action(dest, dest);
					}

					return dest;
				};
			}

			var lambda = Expression.Lambda<Func<TSource,TDest>>(expr, parm);

			return lambda.Compile();
		}
	}
}

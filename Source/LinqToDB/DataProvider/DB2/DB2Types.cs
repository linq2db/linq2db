using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.DB2
{
	using Configuration;
	using Data;
	using Extensions;

	public abstract class TypeCreatorBase
	{
		public Type Type;

		protected Func<T,object> GetCreator<T>()
		{
			var ctor = Type.GetConstructorEx(new[] { typeof(T) });
			var parm = Expression.Parameter(typeof(T));
			var expr = Expression.Lambda<Func<T,object>>(
				Expression.Convert(Expression.New(ctor, parm), typeof(object)),
				parm);

			return expr.Compile();
		}

		protected Func<T,object> GetCreator<T>(Type paramType)
		{
			var ctor = Type.GetConstructorEx(new[] { paramType });
			var parm = Expression.Parameter(typeof(T));
			var expr = Expression.Lambda<Func<T,object>>(
				Expression.Convert(Expression.New(ctor, Expression.Convert(parm, paramType)), typeof(object)),
				parm);

			return expr.Compile();
		}

		public static implicit operator Type(TypeCreatorBase typeCreator)
		{
			return typeCreator.Type;
		}

		public bool IsSupported { get { return Type != null; } }
	}

	public class TypeCreator : TypeCreatorBase
	{
		Func<object> _creator;

		public dynamic CreateInstance()
		{
			if (_creator == null)
			{
				var expr = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(Type), typeof(object)));
				_creator = expr.Compile();
			}

			return _creator();
		}
	}

	public class TypeCreator<T> : TypeCreator
	{
		Func<T,object> _creator;

		public dynamic CreateInstance(T value)
		{
			return (_creator ?? (_creator = GetCreator<T>()))(value);
		}
	}

	public class TypeCreator<T1,T> : TypeCreator<T1>
	{
		Func<T,object> _creator;

		public dynamic CreateInstance(T value)
		{
			return (_creator ?? (_creator = GetCreator<T>()))(value);
		}
	}

	public class TypeCreator<T1,T2,T> : TypeCreator<T1,T2>
	{
		Func<T,object> _paramCreator;

		public dynamic CreateInstance(T value)
		{
			return (_paramCreator ?? (_paramCreator = GetCreator<T>()))(value);
		}
	}

	public class TypeCreatorNoDefault<T> : TypeCreatorBase
	{
		Func<T,object> _creator;

		public dynamic CreateInstance(T value)
		{
			return (_creator ?? (_creator = GetCreator<T>()))(value);
		}
	}

	public class ConnectionTypeTypeCreator<T> : TypeCreatorNoDefault<T>
	{
		Func<IDbConnection,object> _creator;

		public dynamic CreateInstance(DataConnection value)
		{
			return (_creator ?? (_creator = GetCreator<IDbConnection>(DB2Types.ConnectionType)))(Proxy.GetUnderlyingObject((DbConnection)value.Connection));
		}
	}

	public class DB2Types
	{
		public static readonly TypeCreator<long>                 DB2Int64        = new TypeCreator<long>    ();
		public static readonly TypeCreator<int>                  DB2Int32        = new TypeCreator<int>     ();
		public static readonly TypeCreator<short>                DB2Int16        = new TypeCreator<short>   ();
		public static readonly TypeCreator<decimal>              DB2Decimal      = new TypeCreator<decimal> ();
		public static readonly TypeCreator<decimal,double,long>  DB2DecimalFloat = new TypeCreator<decimal,double,long>();
		public static readonly TypeCreator<float>                DB2Real         = new TypeCreator<float>   ();
		public static readonly TypeCreator<double>               DB2Real370      = new TypeCreator<double>  ();
		public static readonly TypeCreator<double>               DB2Double       = new TypeCreator<double>  ();
		public static readonly TypeCreator<string>               DB2String       = new TypeCreator<string>  ();
		public static readonly ConnectionTypeTypeCreator<string> DB2Clob         = new ConnectionTypeTypeCreator<string>();
		public static readonly TypeCreator<byte[]>               DB2Binary       = new TypeCreator<byte[]>  ();
		public static readonly ConnectionTypeTypeCreator<byte[]> DB2Blob         = new ConnectionTypeTypeCreator<byte[]>();
		public static readonly TypeCreator<DateTime>             DB2Date         = new TypeCreator<DateTime>();
		public static readonly TypeCreator<DateTime,long>        DB2DateTime     = new TypeCreator<DateTime,long>();
		public static readonly TypeCreator<TimeSpan>             DB2Time         = new TypeCreator<TimeSpan>();
		public static readonly TypeCreator<DateTime>             DB2TimeStamp    = new TypeCreator<DateTime>();
		public static Type DB2Xml;
		public static readonly TypeCreator                       DB2RowId        = new TypeCreator          ();

		public static Type ConnectionType { get; internal set; }
	}
}

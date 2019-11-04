using System;
using System.Data;

namespace LinqToDB.DataProvider.SqlCe
{
	using LinqToDB.Expressions;

	internal static class SqlCeWrappers
	{
		private static readonly object _syncRoot = new object();
		private static TypeMapper? _typeMapper;

		internal static Type? ParameterType;
		internal static Type? ConnectionType;
		internal static Action<IDbDataParameter, SqlDbType>? TypeSetter;
		internal static Func<IDbDataParameter, SqlDbType>? TypeGetter;

		internal static void Initialize(Type connectionType)
		{
			if (_typeMapper == null)
			{
				lock (_syncRoot)
				{
					if (_typeMapper == null)
					{
						ConnectionType = connectionType;
						ParameterType  = connectionType.Assembly.GetType("System.Data.SqlServerCe.SqlCeParameter", true);

						_typeMapper = new TypeMapper(ConnectionType, ParameterType);

						var dbTypeBuilder = _typeMapper.Type<SqlCeParameter>().Member(p => p.SqlDbType);
						TypeSetter        = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						TypeGetter        = dbTypeBuilder.BuildGetter<IDbDataParameter>();
					}
				}
			}
		}

		public static SqlCeEngine NewSqlCeEngine(string connectionString) => _typeMapper!.CreateAndWrap(() => new SqlCeEngine(connectionString));

		internal class SqlCeEngine : TypeWrapper, IDisposable
		{
			public SqlCeEngine(string connectionString) => throw new NotImplementedException();

			public void CreateDatabase() => this.WrapAction(t => t.CreateDatabase());
			public void Dispose()        => this.WrapAction(t => t.Dispose());
		}

		[Wrapper]
		internal class SqlCeParameter
		{
			public SqlDbType SqlDbType { get; set; }
		}
	}
}

using System;
using System.Data;

namespace LinqToDB.DataProvider.SqlCe
{
	using System.Data.Common;
	using System.Reflection;
	using LinqToDB.Expressions;

	internal static class SqlCeWrappers
	{
		private static readonly object _syncRoot = new object();
		private static TypeMapper? _typeMapper;

		internal static Type                                ParameterType  = null!;
		internal static Action<IDbDataParameter, SqlDbType> TypeSetter = null!;
		internal static Func<IDbDataParameter, SqlDbType>   TypeGetter = null!;

		internal static void Initialize()
		{
			if (_typeMapper == null)
			{
				lock (_syncRoot)
				{
					if (_typeMapper == null)
					{
						var assembly = Type.GetType("System.Data.SqlServerCe.SqlCeConnection, System.Data.SqlServerCe", false)?.Assembly
#if !NETSTANDARD2_0
							?? DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0").GetType().Assembly
#endif
							;

						if (assembly == null)
							throw new InvalidOperationException("Cannot load assembly System.Data.SqlServerCe");

						var connectionType  = assembly.GetType("System.Data.SqlServerCe.SqlCeConnection", true);
						ParameterType       = assembly.GetType("System.Data.SqlServerCe.SqlCeParameter", true);
						var sqlCeEngine     = assembly.GetType("System.Data.SqlServerCe.SqlCeEngine", true);

						var typeMapper = new TypeMapper(ParameterType, sqlCeEngine);

						var dbTypeBuilder = typeMapper.Type<SqlCeParameter>().Member(p => p.SqlDbType);
						TypeSetter        = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						TypeGetter        = dbTypeBuilder.BuildGetter<IDbDataParameter>();
						_typeMapper       = typeMapper;

						_typeMapper.RegisterWrapper<SqlCeEngine>();
						_typeMapper.RegisterWrapper<SqlCeParameter>();
					}
				}
			}
		}

		public static SqlCeEngine NewSqlCeEngine(string connectionString) => _typeMapper!.CreateAndWrap(() => new SqlCeEngine(connectionString))!;

		[Wrapper]
		internal class SqlCeEngine : TypeWrapper, IDisposable
		{
			public SqlCeEngine(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

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

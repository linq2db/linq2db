namespace LinqToDB
{
	/// <summary>Defines how null values are compared in generated SQL queries.</summary>
	public enum CompareNulls
	{
		/// <summary>CLR semantics: nulls values are comparable/equal.</summary>
		/// <note>This option may add significant complexity in generated SQL, possibly preventing index usage.</note>
		/// <example>
		/// <code>
		/// public class MyEntity
		/// {
		///     public int? Value;
		/// }
		///
		/// db.MyEntity.Where(e => e.Value != 10)
		///
		/// from e1 in db.MyEntity
		/// join e2 in db.MyEntity on e1.Value equals e2.Value
		/// select e1
		///
		/// var filter = new [] {1, 2, 3};
		/// db.MyEntity.Where(e => ! filter.Contains(e.Value))
		/// </code>
		///
		/// Would be translated into:
		/// <code>
		/// SELECT Value FROM MyEntity WHERE Value IS NULL OR Value != 10
		///
		/// SELECT e1.Value
		/// FROM MyEntity e1
		/// INNER JOIN MyEntity e2 ON e1.Value = e2.Value OR (e1.Value IS NULL AND e2.Value IS NULL)
		///
		/// SELECT Value FROM MyEntity WHERE Value IS NULL OR NOT Value IN (1, 2, 3)
		/// </code>
		/// </example>
		LikeClr,
		/// <summary>SQL semantics: nulls values compare as UNKNOWN (three-valued logic).</summary>
		/// <note>This translates C# straight to equivalent SQL, which has different semantics when comapring NULLs.</note>
		LikeSql,
		/// <summary>SQL semantics, except null parameters are treated like null constants (they compile to <c>IS NULL</c>).</summary>
		/// <note>This value exists mostly for backward compatibility with versions before 6.0. <see cref="LikeSql"/> is recommended instead.</note>
		LikeSqlExceptParameters,
	}
}

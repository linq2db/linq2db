using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using Microsoft.Data.Sqlite;

using NUnit.Framework;

using Shouldly;

namespace Tests.Playground
{
	// Reference copy of the generator behind the landing page demo (docs/source/index.md).
	// Not compiled here: copy it into Tests/Tests.Playground to run it, see README.md next to it.
	//
	// Each tab is rendered for every dialect of the demo's SQL switcher, offline: a query through
	// ToSqlQuery, an executing call (Upsert) through a connection that records the commands it is sent
	// instead of running them. UpdateLandingPage writes the result into index.md, LandingPageIsCurrent
	// fails while index.md shows anything else.
	// TempTable records the statements CreateTempTable and the join really execute on SQLite in memory;
	// that pane is formatted by hand and not checked.
	[TestFixture]
	public class SqlDemoGenerator
	{
		#region Models

		[Table("Products")]
		public class Product
		{
			[PrimaryKey, Identity] public int     Id         { get; set; }
			[Column, NotNull]      public string  Name       { get; set; } = null!;
			[Column]               public decimal Price      { get; set; }
			[Column]               public int     CategoryId { get; set; }

			[Association(ThisKey = nameof(CategoryId), OtherKey = nameof(Category.Id))]
			public Category Category { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OrderItem.ProductId))]
			public IQueryable<OrderItem> OrderItems { get; set; } = null!;
		}

		[Table("Categories")]
		public class Category
		{
			[PrimaryKey, Identity] public int    Id   { get; set; }
			[Column, NotNull]      public string Name { get; set; } = null!;
		}

		[Table("OrderItems")]
		public class OrderItem
		{
			[PrimaryKey, Identity] public int     Id        { get; set; }
			[Column]               public int     ProductId { get; set; }
			[Column]               public int     Quantity  { get; set; }
			[Column]               public decimal UnitPrice { get; set; }
		}

		// Upsert needs the key in the INSERT, so not Product's identity column
		[Table("Stock")]
		public class Stock
		{
			[PrimaryKey] public int ProductId { get; set; }
			[Column]     public int Quantity  { get; set; }
		}

		#endregion

		#region Tabs: the C# of each tab, as the landing page shows it

		static string Query(DataConnection db)
		{
			var query =
				(from p in db.GetTable<Product>()
				 where p.Price > 100
					&& p.CategoryId == 1
				 orderby p.Name
				 select new
				 {
					 p.Name,
					 p.Price
				 })
				.Take(10);

			return query.ToSqlQuery(_sqlOptions).Sql;
		}

		static string Associations(DataConnection db)
		{
			var query =
				from p in db.GetTable<Product>()
				select new
				{
					Product  = p.Name,
					Category = p.Category.Name,
					Orders   = p.OrderItems.Count()
				};

			return query.ToSqlQuery(_sqlOptions).Sql;
		}

		[ExpressionMethod(nameof(Impl))]
		static decimal TotalRevenue(Product p)
			=> throw new InvalidOperationException();

		static Expression<Func<Product, decimal>> Impl() => p => p.OrderItems
			.Sum(oi => oi.Quantity * oi.UnitPrice);

		static string ExpressionMethod(DataConnection db)
		{
			var query =
				from p in db.GetTable<Product>()
				let total = TotalRevenue(p)
				where total > 1000
				select new
				{
					p.Name,
					Revenue = total
				};

			return query.ToSqlQuery(_sqlOptions).Sql;
		}

		static string Cte(DataConnection db)
		{
			var topProducts =
				db.GetTable<Product>()
				.Where(p => p.Price > 50)
				.Select(p => new
				{
					p.Id, p.Name,
					p.Price, p.CategoryId
				})
				.AsCte("TopProducts");

			var query =
				from tp in topProducts
				join c in db.GetTable<Category>()
					on tp.CategoryId equals c.Id
				select new
				{
					tp.Name, tp.Price,
					Category = c.Name,
					SameCategory = topProducts
						.Count(x => x.CategoryId
							== tp.CategoryId)
				};

			return query.ToSqlQuery(_sqlOptions).Sql;
		}

		static string WindowFunctions(DataConnection db)
		{
			var query =
				from p in db.GetTable<Product>()
				select new
				{
					p.Name,
					p.Price,
					Category = p.Category.Name,
					RowNum = Sql.Window.RowNumber(w => w
						.PartitionBy(p.CategoryId)
						.OrderByDesc(p.Price)),
					Total  = Sql.Window.Sum(p.Price, w => w
						.PartitionBy(p.CategoryId))
				};

			return query.ToSqlQuery(_sqlOptions).Sql;
		}

		static string Merge(DataConnection db)
		{
			var source = new[]
			{
				new Product { Id = 1, Name = "Laptop", Price = 999, CategoryId = 1 },
				new Product { Id = 2, Name = "Tablet", Price = 499, CategoryId = 1 },
			};

			// The tab finishes this chain with .Merge(), which executes it; ToSqlQuery renders the same
			// statement (checked against SQL Server 2022) without a server.
			var merge = db.GetTable<Product>()
				.Merge()
				.Using(source)
				.OnTargetKey()
				.UpdateWhenMatched(
					(target, src) => new Product
					{
						Name  = src.Name,
						Price = src.Price,
					})
				.InsertWhenNotMatched(
					src => new Product
					{
						Id         = src.Id,
						Name       = src.Name,
						Price      = src.Price,
						CategoryId = src.CategoryId,
					});

			return merge.ToSqlQuery(_sqlOptions).Sql;
		}

		// Executes: the SQL is what the capturing connection was sent (see Render).
		static void Upsert(DataConnection db)
		{
			// add to the stock, or start it
			var item = new Stock
			{
				ProductId = 1,
				Quantity  = 5
			};

			db.GetTable<Stock>()
				.Upsert(item, u => u.Update(v => v.Set(
					x => x.Quantity,
					(t, s) => t.Quantity + s.Quantity)));
		}

		/// <param name="Sql">A query tab: the SQL its ToSqlQuery renders.</param>
		/// <param name="Execute">An executing tab: the call to run against the capturing connection.</param>
		/// <param name="UnsupportedOn">Dialects that must refuse the tab; each gets <paramref name="Unsupported"/> as its note.</param>
		sealed record Tab(
			string                       Id,
			Func<DataConnection, string>? Sql           = null,
			Action<DataConnection>?       Execute       = null,
			string[]?                     UnsupportedOn = null,
			string?                       Unsupported   = null);

		static readonly Tab[] _tabs =
		[
			new("query",             Query),
			new("associations",      Associations),
			new("expression-method", ExpressionMethod),
			new("cte",               Cte),
			new("window-functions",  WindowFunctions),
			new("merge",             Merge, UnsupportedOn: ["mysql", "sqlite"], Unsupported: "{0} has no MERGE statement."),
			new("upsert",            Execute: Upsert),
		];

		#endregion

		#region Dialects: the demo's SQL switcher, in its order

		sealed record Dialect(string Id, string Name, Func<DataOptions, DataOptions> Use);

		// No server is needed: ToSqlQuery never opens the connection, and an executing tab runs against
		// CaptureConnection. The SQL equals what these calls send to SQL Server 2022, PostgreSQL 16,
		// MySQL 8, Oracle 23 and SQLite (compared on live databases, 2026-11-26).
		static readonly Dialect[] _dialects =
		[
			new("sqlserver",  "SQL Server", o => o.UseSqlServer ("Server=offline",       SqlServerVersion.v2022, SqlServerProvider.MicrosoftDataSqlClient)),
			new("postgresql", "PostgreSQL", o => o.UsePostgreSQL("Host=offline",         PostgreSQLVersion.v15)),
			new("mysql",      "MySQL",      o => o.UseMySql     ("Server=offline",       MySqlVersion.MySql80,   MySqlProvider.MySqlConnector)),
			new("oracle",     "Oracle",     o => o.UseOracle    ("Data Source=offline",  OracleVersion.v12,      OracleProvider.Managed)),
			new("sqlite",     "SQLite",     o => o.UseSQLite    ("Data Source=:memory:", SQLiteProvider.Microsoft)),
		];

		#endregion

		static readonly SqlGenerationOptions _sqlOptions = new() { InlineParameters = true };

		// null: the dialect is one the tab names as unsupported. It must still refuse the tab, or its note
		// would be false; any other dialect's failure propagates.
		static string? Generate(Tab tab, Dialect dialect)
		{
			if (tab.UnsupportedOn?.Contains(dialect.Id) != true)
				return Render(tab, dialect);

			try
			{
				Render(tab, dialect);
			}
			catch (LinqToDBException)
			{
				return null;
			}

			throw new InvalidOperationException($"{dialect.Name} now runs the {tab.Id} tab: drop it from UnsupportedOn");
		}

		static string Render(Tab tab, Dialect dialect)
		{
			if (tab.Sql != null)
			{
				using var db = new DataConnection(dialect.Use(new DataOptions()));
				return tab.Sql(db);
			}

			var capture = new CaptureConnection();

			using (var db = new DataConnection(dialect.Use(new DataOptions()).UseConnectionFactory(_ => capture)))
			{
				db.InlineParameters = true;
				tab.Execute!(db);
			}

			return string.Join("\n\n", capture.Commands);
		}

		/// <summary>Records each command's text and reports one affected row; never connects.</summary>
		sealed class CaptureConnection : DbConnection
		{
			ConnectionState _state;

			public readonly List<string> Commands = new();

			[System.Diagnostics.CodeAnalysis.AllowNull]
			public override string          ConnectionString { get; set; } = "";
			public override string          Database      => "";
			public override string          DataSource    => "";
			public override string          ServerVersion => "";
			public override ConnectionState State         => _state;

			public override void ChangeDatabase(string databaseName) { }
			public override void Close() => _state = ConnectionState.Closed;
			public override void Open()  => _state = ConnectionState.Open;

			protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
			protected override DbCommand     CreateDbCommand()                                => new CaptureCommand(this);
		}

		sealed class CaptureCommand(CaptureConnection connection) : DbCommand
		{
			// parameters are inlined; the collection only has to exist
			readonly DbParameterCollection _parameters = new SqliteCommand().Parameters;

			[System.Diagnostics.CodeAnalysis.AllowNull]
			public override string          CommandText       { get; set; } = "";
			public override int             CommandTimeout    { get; set; }
			public override CommandType     CommandType       { get; set; }
			public override bool            DesignTimeVisible { get; set; }
			public override UpdateRowSource UpdatedRowSource  { get; set; }

			protected override DbConnection?         DbConnection          { get; set; } = connection;
			protected override DbParameterCollection DbParameterCollection => _parameters;
			protected override DbTransaction?        DbTransaction         { get; set; }

			public override void    Cancel()          { }
			public override void    Prepare()         { }
			public override object? ExecuteScalar()   => throw new NotSupportedException(CommandText);

			public override int ExecuteNonQuery()
			{
				connection.Commands.Add(CommandText);
				return 1;
			}

			protected override DbParameter  CreateDbParameter()                             => new SqliteParameter();
			protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException(CommandText);
		}

		/// <summary>The SQL pane of one tab: a block per dialect, ready to paste into index.md.</summary>
		static string RenderTab(Tab tab)
		{
			var sb = new StringBuilder();

			foreach (var dialect in _dialects)
			{
				var sql = Generate(tab, dialect);

				sb.Append($"<div class=\"demo-sql-variant\" data-dialect=\"{dialect.Id}\">\n");
				sb.Append($"<div class=\"demo-pane-label\">Generated SQL · {dialect.Name}</div>\n");

				if (sql == null)
					sb.Append($"<p class=\"demo-na\">{string.Format(tab.Unsupported!, dialect.Name)}</p>\n");
				else
					sb.Append($"<pre class=\"demo-code demo-sql\" data-sql=\"{tab.Id}/{dialect.Id}\">{SqlHtml.Render(sql)}</pre>\n");

				sb.Append("</div>\n");
			}

			return sb.ToString();
		}

		// index.md marks each tab's SQL pane: <!-- sql-demo:query --> ... <!-- /sql-demo:query -->
		static readonly Regex _region = new(@"(?<open><!-- sql-demo:(?<id>[a-z-]+) -->\n)(?<body>.*?)(?<close><!-- /sql-demo:\k<id> -->)", RegexOptions.Singleline);

		/// <summary>Rewrites the SQL panes of docs/source/index.md from the current linq2db.</summary>
		[Test, Explicit("writes docs/source/index.md")]
		public void UpdateLandingPage()
		{
			var path    = FindLandingPage();
			var page    = File.ReadAllText(path).Replace("\r\n", "\n");
			var tabs    = _tabs.ToDictionary(t => t.Id);
			var updated = _region.Replace(page, m =>
			{
				var id = m.Groups["id"].Value;

				if (!tabs.TryGetValue(id, out var tab))
					throw new InvalidOperationException($"index.md has a <!-- sql-demo:{id} --> region but the generator has no '{id}' tab");

				return m.Groups["open"].Value + RenderTab(tab) + m.Groups["close"].Value;
			});

			File.WriteAllText(path, updated);

			foreach (var tab in _tabs)
			foreach (var dialect in _dialects)
			{
				var sql    = Generate(tab, dialect);
				var widest = sql == null ? 0 : SqlHtml.Layout(sql).Max(l => l.Length);

				if (widest > SqlHtml.MaxWidth)
					TestContext.Out.WriteLine($"WARNING: {tab.Id}/{dialect.Id} has a {widest}-character line (limit {SqlHtml.MaxWidth})");
			}
		}

		[Test]
		public void LandingPageIsCurrent()
		{
			var page  = File.ReadAllText(FindLandingPage()).Replace("\r\n", "\n");
			var shown = _region.Matches(page).ToDictionary(m => m.Groups["id"].Value, m => m.Groups["body"].Value);
			var stale = new List<string>();

			foreach (var tab in _tabs)
			{
				if (!shown.TryGetValue(tab.Id, out var body))
					stale.Add($"{tab.Id}: no <!-- sql-demo:{tab.Id} --> region");
				else if (body != RenderTab(tab))
					stale.Add($"{tab.Id}: differs");
			}

			foreach (var id in shown.Keys.Except(_tabs.Select(t => t.Id)))
				stale.Add($"{id}: region with no generator tab");

			stale.ShouldBeEmpty("docs/source/index.md is out of date, run UpdateLandingPage: " + string.Join("; ", stale));
		}

		static string FindLandingPage()
		{
			for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
			{
				var path = Path.Combine(dir.FullName, "docs", "source", "index.md");
				if (File.Exists(path))
					return path;
			}

			throw new FileNotFoundException("docs/source/index.md not found above the test directory");
		}

		[Test]
		public void TempTable()
		{
			var executed = new StringBuilder();

			var options = new DataOptions()
				.UseSQLite("Data Source=:memory:", SQLiteProvider.Microsoft)
				.UseTracing(System.Diagnostics.TraceLevel.Info, ti =>
				{
					if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
						executed.AppendLine(ti.SqlText);
				});

			using var db = new DataConnection(options);

			// the join needs Products; its CREATE is not part of the tab
			db.CreateTable<Product>();
			executed.Clear();

			// Populate temp table from data
			var ids = Enumerable
				.Range(1, 500)
				.Select(i => new { Id = i });

			using (var tmp =
				db.CreateTempTable(
					"FilterIds", ids))
			{
				// Join temp table in LINQ query
				var query =
					from p in db.GetTable<Product>()
					join t in tmp
						on p.Id equals t.Id
					orderby p.Name
					select new
					{
						p.Name,
						p.Price
					};

				query.ToList();
			}

			// CREATE, the bulk copy INSERT, the join and the DROP issued by Dispose, in execution order
			TestContext.Out.WriteLine("=== Temp Table (executed) ===");
			TestContext.Out.WriteLine(executed.ToString());
		}

		/// <summary>
		/// Lays linq2db's SQL out for a narrow pane and highlights it with the landing page's span classes.
		/// Only whitespace changes: tabs become four spaces, a JOIN's ON moves to its own line, a short
		/// parenthesized list, UPDATE SET and a nested clause with a single item each share a line when
		/// they fit, and a line wider than <see cref="MaxWidth"/> breaks after a comma or an opening
		/// parenthesis, or before an operator.
		/// </summary>
		static class SqlHtml
		{
			public const int MaxWidth = 56;

			static readonly string[] _clauses = ["SELECT", "FROM", "WHERE", "ORDER BY", "GROUP BY"];

			static readonly HashSet<string> _keywords = new(StringComparer.OrdinalIgnoreCase)
			{
				"ALL", "AND", "AS", "ASC", "BY", "CASE", "CONFLICT", "CROSS", "DESC", "DISTINCT", "DO", "DUPLICATE",
				"ELSE", "END", "EXISTS", "FETCH", "FIRST", "FROM", "FULL", "GROUP", "HAVING", "IDENTITY_INSERT", "IN",
				"INNER", "INSERT", "INTO", "IS", "JOIN", "KEY", "LEFT", "LIMIT", "MATCHED", "MERGE", "NEXT", "NOT",
				"NULL", "OFF", "OFFSET",
				"ON", "ONLY", "OR", "ORDER", "OUTER", "OVER", "OVERRIDING", "PARTITION", "RIGHT", "ROW", "ROWS",
				"SELECT", "SET", "SYSTEM", "THEN", "TOP", "UNION", "UPDATE", "USING", "VALUE", "VALUES", "WHEN",
				"WHERE", "WITH",
			};

			public static List<string> Layout(string sql)
			{
				var lines = new List<string>();

				foreach (var raw in sql.Replace("\r\n", "\n").Trim('\n').Split('\n'))
				{
					var trimmed = raw.TrimEnd();
					var tabs    = trimmed.Length - trimmed.TrimStart('\t').Length;
					var text    = trimmed.TrimStart('\t');
					var indent  = tabs * 4;

					// "LEFT JOIN t ON cond": the join at its table's level, the condition under it
					var on = text.IndexOf(" ON ", StringComparison.Ordinal);
					if (text.Contains("JOIN ", StringComparison.Ordinal) && on > 0)
					{
						indent = Math.Max(0, indent - 4);
						lines.Add(new string(' ', indent) + text[..on]);
						lines.Add(new string(' ', indent + 4) + text[(on + 1)..]);
					}
					else
						lines.Add(new string(' ', indent) + text);
				}

				// "(" + one simple item per line + ")" on one line when it fits: "([Id], [Name], [Price])"
				for (var i = 0; i < lines.Count; i++)
				{
					if (lines[i].Trim() != "(")
						continue;

					var indent = Indent(lines[i]);
					var items  = new List<string>();
					var j      = i + 1;

					for (; j < lines.Count && lines[j].Length > 0 && Indent(lines[j]) > indent && !lines[j].Contains('('); j++)
						items.Add(lines[j].Trim());

					if (items.Count == 0 || j == lines.Count || Indent(lines[j]) != indent || !lines[j].TrimStart().StartsWith(')'))
						continue;

					var joined = new string(' ', indent) + "(" + string.Join(" ", items) + lines[j].Trim();
					if (joined.Length > MaxWidth)
						continue;

					lines[i] = joined;
					lines.RemoveRange(i + 1, j - i);
				}

				// "UPDATE" / "SET" share a line
				for (var i = 0; i + 1 < lines.Count; i++)
				{
					if (lines[i].Trim() == "UPDATE" && lines[i + 1].Trim() == "SET" && Indent(lines[i]) == Indent(lines[i + 1]))
					{
						lines[i] += " SET";
						lines.RemoveAt(i + 1);
					}
				}

				// a nested clause with one item: "SELECT COUNT(*)", "FROM [OrderItems] [o]"
				for (var i = 0; i + 1 < lines.Count; i++)
				{
					var indent = Indent(lines[i]);

					if (indent > 0
						&& _clauses.Contains(lines[i].Trim())
						&& Indent(lines[i + 1]) > indent
						&& (i + 2 == lines.Count || lines[i + 2].Length == 0 || Indent(lines[i + 2]) <= indent)
						&& lines[i].Length + 1 + lines[i + 1].Trim().Length <= MaxWidth)
					{
						lines[i] += " " + lines[i + 1].Trim();
						lines.RemoveAt(i + 1);
					}
				}

				var result = new List<string>();

				foreach (var line in lines)
					Wrap(line, result);

				return result;
			}

			static int Indent(string line) => line.Length - line.TrimStart(' ').Length;

			static readonly string[] _breaksBefore = ["= ", "AND ", "OR ", "* ", "+ ", "AS ", "FROM ", "ORDER BY ", "PARTITION BY ", "UNION "];

			// Breaks a line wider than MaxWidth; every continuation of one source line shares one indent.
			static void Wrap(string line, List<string> result, int continuation = -1)
			{
				var indent = Indent(line);

				if (continuation < 0)
					continuation = indent + 4;

				if (line.Length <= MaxWidth)
				{
					result.Add(line);
					return;
				}

				// Break points outside quoted names and strings: after a top-level ", ", after a "(" that opens
				// something, before an operator or a clause keyword. A comma that fits wins, then the rightmost
				// other point that fits, then the first point at all.
				var comma = -1;
				var other = -1;
				var first = -1;
				var quote = '\0';
				var depth = 0;

				for (var i = indent; i < line.Length; i++)
				{
					var c = line[i];

					if (quote != '\0')
					{
						if (c == quote)
							quote = '\0';
						continue;
					}

					var cut     = -1;
					var isComma = false;

					switch (c)
					{
						case '[' : quote = ']';  continue;
						case '"' : quote = '"';  continue;
						case '`' : quote = '`';  continue;
						case '\'': quote = '\''; continue;

						case '(':
							depth++;
							if (i + 1 < line.Length && line[i + 1] != ')' && !Starts(line, i + 1, "*)"))
								cut = i + 1;
							break;

						case ')':
							depth--;
							break;

						case ',' when depth == 0 && i + 1 < line.Length && line[i + 1] == ' ':
							cut     = i + 1;
							isComma = true;
							break;

						case ' ' when _breaksBefore.Any(b => Starts(line, i + 1, b)):
							cut = i;
							break;
					}

					// the first part has to say something, and the rest has to get shorter
					if (cut <= indent + 4 || cut <= continuation)
						continue;

					if (cut <= MaxWidth)
					{
						if (isComma)
							comma = cut;
						else
							other = cut;
					}
					else if (first < 0)
						first = cut;
				}

				var at = comma >= 0 ? comma : other >= 0 ? other : first;

				if (at < 0)
				{
					result.Add(line);
					return;
				}

				result.Add(line[..at].TrimEnd());
				Wrap(new string(' ', continuation) + line[at..].TrimStart(), result, continuation);
			}

			static bool Starts(string line, int index, string value)
				=> index + value.Length <= line.Length && string.CompareOrdinal(line, index, value, 0, value.Length) == 0;

			static readonly Regex _token = new(
				@"(?<comment>--.*$)|(?<str>'(?:[^']|'')*')|(?<name>\[[^\]]*\]|""[^""]*""|`[^`]*`)|(?<num>\b\d+(?:\.\d+)?\b)|(?<word>[A-Za-z_][A-Za-z0-9_]*)|(?<ws>\s+)|(?<other>.)",
				RegexOptions.Compiled);

			public static string Render(string sql)
			{
				var sb    = new StringBuilder();
				var blank = false;

				foreach (var line in Layout(sql))
				{
					if (line.Length == 0)
					{
						// a blank line inside <pre> would end the HTML block in markdown
						blank = true;
						continue;
					}

					if (sb.Length > 0)
						sb.Append('\n');
					if (blank)
						sb.Append("&#10;");

					blank = false;
					sb.Append(Highlight(line));
				}

				return sb.ToString();
			}

			static string Highlight(string line)
			{
				var tokens = _token.Matches(line).Select(m => (Kind: m.Groups.Keys.First(k => k != "0" && m.Groups[k].Success), m.Value)).ToList();
				var sb     = new StringBuilder();
				var lastWord = "";

				for (var i = 0; i < tokens.Count; i++)
				{
					var (kind, text) = tokens[i];

					switch (kind)
					{
						case "comment": sb.Append(Span("code-comment", text)); break;
						case "str"    : sb.Append(Span("code-string",  text)); break;
						case "num"    : sb.Append(Span("code-number",  text)); break;

						case "word" when _keywords.Contains(text):
						{
							// a run of keywords is one span: "ORDER BY", "WHEN NOT MATCHED THEN"
							var run = text;
							while (i + 2 < tokens.Count && tokens[i + 1].Value == " " && tokens[i + 2].Kind == "word" && _keywords.Contains(tokens[i + 2].Value))
							{
								run += " " + tokens[i + 2].Value;
								i   += 2;
							}

							sb.Append(Span("code-keyword", run));
							lastWord = tokens[i].Value;
							break;
						}

						// a function call, but not the type of a CAST: "AS Decimal(3, 0)"
						case "word" when i + 1 < tokens.Count && tokens[i + 1].Value == "(" && !lastWord.Equals("AS", StringComparison.OrdinalIgnoreCase):
							sb.Append(Span("code-type", text));
							lastWord = text;
							break;

						case "word":
							sb.Append(Escape(text));
							lastWord = text;
							break;

						default:
							sb.Append(Escape(text));
							break;
					}
				}

				return sb.ToString();
			}

			static string Span(string cls, string text) => $"<span class=\"{cls}\">{Escape(text)}</span>";

			// only what HTML needs inside <pre>; quotes stay readable in the page source
			static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}
	}
}

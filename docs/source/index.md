---
title: Linq To DB
_disableToc: true
_disableAffix: true
_disableBreadcrumb: true
_disableNextArticle: true
_disableContribution: true
---

<section class="landing-hero">
<div class="landing-hero-inner">
<div class="container-xxl">
<div class="row align-items-center g-5">
<div class="col-lg-6">
<h1 class="landing-title">Data access<br/>made <span class="landing-accent">simple</span></h1>
<p class="landing-subtitle">A fast, lightweight LINQ database access library: a type-safe layer between your POCO objects and your database, with full SQL support.</p>
<div class="landing-actions">
<a href="documentation/get-started/overview.md" class="btn btn-primary btn-lg landing-btn-primary">Get Started <i class="bi bi-arrow-right"></i></a>
<a href="https://github.com/linq2db/linq2db" class="btn btn-outline-light btn-lg landing-btn-secondary"><i class="bi bi-github"></i> GitHub</a>
</div>
<div class="landing-install mt-4">
<code>dotnet add package linq2db</code>
</div>
</div>
<div class="col-lg-6">
<div class="demo-container">
<div class="demo-tabs" role="radiogroup" aria-label="Code examples">
<input type="radio" name="demo-tab" id="demo-tab-1" class="demo-radio" checked /><label for="demo-tab-1" class="demo-tab">Query</label>
<input type="radio" name="demo-tab" id="demo-tab-2" class="demo-radio" /><label for="demo-tab-2" class="demo-tab">Associations</label>
<input type="radio" name="demo-tab" id="demo-tab-3" class="demo-radio" /><label for="demo-tab-3" class="demo-tab">ExpressionMethod</label>
<input type="radio" name="demo-tab" id="demo-tab-4" class="demo-radio" /><label for="demo-tab-4" class="demo-tab">CTE</label>
<input type="radio" name="demo-tab" id="demo-tab-5" class="demo-radio" /><label for="demo-tab-5" class="demo-tab">Window Functions</label>
<input type="radio" name="demo-tab" id="demo-tab-6" class="demo-radio" /><label for="demo-tab-6" class="demo-tab">Merge</label>
<input type="radio" name="demo-tab" id="demo-tab-7" class="demo-radio" /><label for="demo-tab-7" class="demo-tab">Temp Table</label>
</div>
<div class="demo-dialects" role="radiogroup" aria-labelledby="demo-dialects-label">
<span id="demo-dialects-label" class="demo-dialects-label">SQL for</span>
<input type="radio" name="demo-dialect" id="demo-dialect-sqlserver" class="demo-radio" checked /><label for="demo-dialect-sqlserver" class="demo-dialect">SQL Server</label>
<input type="radio" name="demo-dialect" id="demo-dialect-postgresql" class="demo-radio" /><label for="demo-dialect-postgresql" class="demo-dialect">PostgreSQL</label>
<input type="radio" name="demo-dialect" id="demo-dialect-mysql" class="demo-radio" /><label for="demo-dialect-mysql" class="demo-dialect">MySQL</label>
<input type="radio" name="demo-dialect" id="demo-dialect-oracle" class="demo-radio" /><label for="demo-dialect-oracle" class="demo-dialect">Oracle</label>
<input type="radio" name="demo-dialect" id="demo-dialect-sqlite" class="demo-radio" /><label for="demo-dialect-sqlite" class="demo-dialect">SQLite</label>
<span class="demo-dialects-note">Run for real on SQLite</span>
</div>
<div class="demo-panels">
<!-- Query -->
<div class="demo-panel demo-panel-1">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code"><span class="code-keyword">var</span> query =
    (<span class="code-keyword">from</span> p <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Product</span>&gt;()
     <span class="code-keyword">where</span> p.Price &gt; <span class="code-number">100</span>
        &amp;&amp; p.CategoryId == <span class="code-number">1</span>
     <span class="code-keyword">orderby</span> p.Name
     <span class="code-keyword">select new</span>
     {
         p.Name,
         p.Price
     })
    .Take(<span class="code-number">10</span>);</pre>
</div>
<div class="demo-pane">
<!-- sql-demo:query -->
<div class="demo-sql-variant" data-dialect="sqlserver">
<div class="demo-pane-label">Generated SQL · SQL Server</div>
<pre class="demo-code demo-sql" data-sql="query/sqlserver"><span class="code-keyword">SELECT TOP</span> (<span class="code-number">10</span>)
    [p].[Name],
    [p].[Price]
<span class="code-keyword">FROM</span>
    [Products] [p]
<span class="code-keyword">WHERE</span>
    [p].[Price] &gt; <span class="code-number">100</span> <span class="code-keyword">AND</span> [p].[CategoryId] = <span class="code-number">1</span>
<span class="code-keyword">ORDER BY</span>
    [p].[Name]</pre>
</div>
<div class="demo-sql-variant" data-dialect="postgresql">
<div class="demo-pane-label">Generated SQL · PostgreSQL</div>
<pre class="demo-code demo-sql" data-sql="query/postgresql"><span class="code-keyword">SELECT</span>
    p."Name",
    p."Price"
<span class="code-keyword">FROM</span>
    "Products" p
<span class="code-keyword">WHERE</span>
    p."Price" &gt; <span class="code-number">100</span> <span class="code-keyword">AND</span> p."CategoryId" = <span class="code-number">1</span>
<span class="code-keyword">ORDER BY</span>
    p."Name"
<span class="code-keyword">LIMIT</span> <span class="code-number">10</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="mysql">
<div class="demo-pane-label">Generated SQL · MySQL</div>
<pre class="demo-code demo-sql" data-sql="query/mysql"><span class="code-keyword">SELECT</span>
    `p`.`Name`,
    `p`.`Price`
<span class="code-keyword">FROM</span>
    `Products` `p`
<span class="code-keyword">WHERE</span>
    `p`.`Price` &gt; <span class="code-number">100</span> <span class="code-keyword">AND</span> `p`.`CategoryId` = <span class="code-number">1</span>
<span class="code-keyword">ORDER BY</span>
    `p`.`Name`
<span class="code-keyword">LIMIT</span> <span class="code-number">10</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="oracle">
<div class="demo-pane-label">Generated SQL · Oracle</div>
<pre class="demo-code demo-sql" data-sql="query/oracle"><span class="code-keyword">SELECT</span>
    p."Name",
    p."Price"
<span class="code-keyword">FROM</span>
    "Products" p
<span class="code-keyword">WHERE</span>
    p."Price" &gt; <span class="code-number">100</span> <span class="code-keyword">AND</span> p."CategoryId" = <span class="code-number">1</span>
<span class="code-keyword">ORDER BY</span>
    p."Name"
<span class="code-keyword">FETCH NEXT</span> <span class="code-number">10</span> <span class="code-keyword">ROWS ONLY</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="sqlite">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<pre class="demo-code demo-sql" data-sql="query/sqlite"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price]
<span class="code-keyword">FROM</span>
    [Products] [p]
<span class="code-keyword">WHERE</span>
    [p].[Price] &gt; <span class="code-number">100</span> <span class="code-keyword">AND</span> [p].[CategoryId] = <span class="code-number">1</span>
<span class="code-keyword">ORDER BY</span>
    [p].[Name]
<span class="code-keyword">LIMIT</span> <span class="code-number">10</span></pre>
</div>
<!-- /sql-demo:query -->
</div>
</div>
<a class="demo-more" href="documentation/get-started/overview.md#queries">Queries in the docs <i class="bi bi-arrow-right"></i></a>
</div>
<!-- Associations -->
<div class="demo-panel demo-panel-2">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code"><span class="code-keyword">var</span> query =
    <span class="code-keyword">from</span> p <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    <span class="code-keyword">select new</span>
    {
        Product  = p.Name,
        Category = p.Category.Name,
        Orders   = p.OrderItems.Count()
    };</pre>
</div>
<div class="demo-pane">
<!-- sql-demo:associations -->
<div class="demo-sql-variant" data-dialect="sqlserver">
<div class="demo-pane-label">Generated SQL · SQL Server</div>
<pre class="demo-code demo-sql" data-sql="associations/sqlserver"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [a_Category].[Name],
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> [OrderItems] [a_OrderItems]
        <span class="code-keyword">WHERE</span> [p].[Id] = [a_OrderItems].[ProductId]
    )
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">LEFT JOIN</span> [Categories] [a_Category]
        <span class="code-keyword">ON</span> [p].[CategoryId] = [a_Category].[Id]</pre>
</div>
<div class="demo-sql-variant" data-dialect="postgresql">
<div class="demo-pane-label">Generated SQL · PostgreSQL</div>
<pre class="demo-code demo-sql" data-sql="associations/postgresql"><span class="code-keyword">SELECT</span>
    p."Name",
    "a_Category"."Name",
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> "OrderItems" "a_OrderItems"
        <span class="code-keyword">WHERE</span> p."Id" = "a_OrderItems"."ProductId"
    )
<span class="code-keyword">FROM</span>
    "Products" p
    <span class="code-keyword">LEFT JOIN</span> "Categories" "a_Category"
        <span class="code-keyword">ON</span> p."CategoryId" = "a_Category"."Id"</pre>
</div>
<div class="demo-sql-variant" data-dialect="mysql">
<div class="demo-pane-label">Generated SQL · MySQL</div>
<pre class="demo-code demo-sql" data-sql="associations/mysql"><span class="code-keyword">SELECT</span>
    `p`.`Name`,
    `a_Category`.`Name`,
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> `OrderItems` `a_OrderItems`
        <span class="code-keyword">WHERE</span> `p`.`Id` = `a_OrderItems`.`ProductId`
    )
<span class="code-keyword">FROM</span>
    `Products` `p`
    <span class="code-keyword">LEFT JOIN</span> `Categories` `a_Category`
        <span class="code-keyword">ON</span> `p`.`CategoryId` = `a_Category`.`Id`</pre>
</div>
<div class="demo-sql-variant" data-dialect="oracle">
<div class="demo-pane-label">Generated SQL · Oracle</div>
<pre class="demo-code demo-sql" data-sql="associations/oracle"><span class="code-keyword">SELECT</span>
    p."Name",
    a_Category."Name",
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> "OrderItems" a_OrderItems
        <span class="code-keyword">WHERE</span> p."Id" = a_OrderItems."ProductId"
    )
<span class="code-keyword">FROM</span>
    "Products" p
    <span class="code-keyword">LEFT JOIN</span> "Categories" a_Category
        <span class="code-keyword">ON</span> p."CategoryId" = a_Category."Id"</pre>
</div>
<div class="demo-sql-variant" data-dialect="sqlite">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<pre class="demo-code demo-sql" data-sql="associations/sqlite"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [a_Category].[Name],
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> [OrderItems] [a_OrderItems]
        <span class="code-keyword">WHERE</span> [p].[Id] = [a_OrderItems].[ProductId]
    )
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">LEFT JOIN</span> [Categories] [a_Category]
        <span class="code-keyword">ON</span> [p].[CategoryId] = [a_Category].[Id]</pre>
</div>
<!-- /sql-demo:associations -->
</div>
</div>
<a class="demo-more" href="documentation/get-started/overview.md#configuration-using-mapping-attributes">Mapping and associations in the docs <i class="bi bi-arrow-right"></i></a>
</div>
<!-- ExpressionMethod -->
<div class="demo-panel demo-panel-3">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code"><span class="code-comment">// Reusable SQL expression</span>
[<span class="code-type">ExpressionMethod</span>(<span class="code-keyword">nameof</span>(Impl))]
<span class="code-keyword">static decimal</span> TotalRevenue(<span class="code-type">Product</span> p)
    =&gt; <span class="code-keyword">throw new</span> <span class="code-type">InvalidOperationException</span>();
&#10;<span class="code-keyword">static</span> Expression&lt;Func&lt;<span class="code-type">Product</span>, <span class="code-keyword">decimal</span>&gt;&gt;
    Impl() =&gt; p =&gt; p.OrderItems
        .Sum(oi =&gt; oi.Quantity * oi.UnitPrice);
&#10;<span class="code-comment">// let avoids duplicate subqueries</span>
<span class="code-keyword">var</span> query =
    <span class="code-keyword">from</span> p <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    <span class="code-keyword">let</span> total = TotalRevenue(p)
    <span class="code-keyword">where</span> total &gt; <span class="code-number">1000</span>
    <span class="code-keyword">select new</span>
    {
        p.Name,
        Revenue = total
    };</pre>
</div>
<div class="demo-pane">
<!-- sql-demo:expression-method -->
<div class="demo-sql-variant" data-dialect="sqlserver">
<div class="demo-pane-label">Generated SQL · SQL Server</div>
<pre class="demo-code demo-sql" data-sql="expression-method/sqlserver"><span class="code-keyword">SELECT</span>
    [t1].[Name],
    [t1].[total]
<span class="code-keyword">FROM</span>
    (
        <span class="code-keyword">SELECT</span>
            <span class="code-type">Coalesce</span>((
                <span class="code-keyword">SELECT</span>
                    <span class="code-type">SUM</span>(<span class="code-type">CAST</span>([a_OrderItems].[Quantity]
                        <span class="code-keyword">AS</span> Decimal(<span class="code-number">18</span>, <span class="code-number">10</span>))
                        * [a_OrderItems].[UnitPrice])
                <span class="code-keyword">FROM</span> [OrderItems] [a_OrderItems]
                <span class="code-keyword">WHERE</span>
                    [p].[Id]
                        = [a_OrderItems].[ProductId]
            ), <span class="code-number">0</span>) <span class="code-keyword">as</span> [total],
            [p].[Name]
        <span class="code-keyword">FROM</span> [Products] [p]
    ) [t1]
<span class="code-keyword">WHERE</span>
    [t1].[total] &gt; <span class="code-number">1000</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="postgresql">
<div class="demo-pane-label">Generated SQL · PostgreSQL</div>
<pre class="demo-code demo-sql" data-sql="expression-method/postgresql"><span class="code-keyword">SELECT</span>
    t1."Name",
    t1.total
<span class="code-keyword">FROM</span>
    (
        <span class="code-keyword">SELECT</span>
            <span class="code-type">Coalesce</span>((
                <span class="code-keyword">SELECT</span>
                    <span class="code-type">SUM</span>("a_OrderItems"."Quantity"::decimal
                        * "a_OrderItems"."UnitPrice")
                <span class="code-keyword">FROM</span> "OrderItems" "a_OrderItems"
                <span class="code-keyword">WHERE</span>
                    p."Id" = "a_OrderItems"."ProductId"
            ), <span class="code-number">0</span>) <span class="code-keyword">as</span> total,
            p."Name"
        <span class="code-keyword">FROM</span> "Products" p
    ) t1
<span class="code-keyword">WHERE</span>
    t1.total &gt; <span class="code-number">1000</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="mysql">
<div class="demo-pane-label">Generated SQL · MySQL</div>
<pre class="demo-code demo-sql" data-sql="expression-method/mysql"><span class="code-keyword">SELECT</span>
    `t1`.`Name`,
    `t1`.`total`
<span class="code-keyword">FROM</span>
    (
        <span class="code-keyword">SELECT</span>
            <span class="code-type">Coalesce</span>((
                <span class="code-keyword">SELECT</span>
                    <span class="code-type">SUM</span>(<span class="code-type">CAST</span>(`a_OrderItems`.`Quantity`
                        <span class="code-keyword">AS</span> DECIMAL(<span class="code-number">29</span>, <span class="code-number">10</span>))
                        * `a_OrderItems`.`UnitPrice`)
                <span class="code-keyword">FROM</span> `OrderItems` `a_OrderItems`
                <span class="code-keyword">WHERE</span>
                    `p`.`Id`
                        = `a_OrderItems`.`ProductId`
            ), <span class="code-number">0</span>) <span class="code-keyword">as</span> `total`,
            `p`.`Name`
        <span class="code-keyword">FROM</span> `Products` `p`
    ) `t1`
<span class="code-keyword">WHERE</span>
    `t1`.`total` &gt; <span class="code-number">1000</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="oracle">
<div class="demo-pane-label">Generated SQL · Oracle</div>
<pre class="demo-code demo-sql" data-sql="expression-method/oracle"><span class="code-keyword">SELECT</span>
    t1."Name",
    t1."total"
<span class="code-keyword">FROM</span>
    (
        <span class="code-keyword">SELECT</span>
            <span class="code-type">Coalesce</span>((
                <span class="code-keyword">SELECT</span>
                    <span class="code-type">SUM</span>(<span class="code-type">CAST</span>(a_OrderItems."Quantity"
                        <span class="code-keyword">AS</span> Decimal(<span class="code-number">28</span>, <span class="code-number">10</span>))
                        * a_OrderItems."UnitPrice")
                <span class="code-keyword">FROM</span> "OrderItems" a_OrderItems
                <span class="code-keyword">WHERE</span> p."Id" = a_OrderItems."ProductId"
            ), <span class="code-number">0</span>) <span class="code-keyword">as</span> "total",
            p."Name"
        <span class="code-keyword">FROM</span> "Products" p
    ) t1
<span class="code-keyword">WHERE</span>
    t1."total" &gt; <span class="code-number">1000</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="sqlite">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<pre class="demo-code demo-sql" data-sql="expression-method/sqlite"><span class="code-keyword">SELECT</span>
    [t1].[Name],
    [t1].[total]
<span class="code-keyword">FROM</span>
    (
        <span class="code-keyword">SELECT</span>
            <span class="code-type">Coalesce</span>((
                <span class="code-keyword">SELECT</span>
                    <span class="code-type">SUM</span>(<span class="code-type">CAST</span>([a_OrderItems].[Quantity]
                        <span class="code-keyword">AS</span> Decimal)
                        * [a_OrderItems].[UnitPrice])
                <span class="code-keyword">FROM</span> [OrderItems] [a_OrderItems]
                <span class="code-keyword">WHERE</span>
                    [p].[Id]
                        = [a_OrderItems].[ProductId]
            ), <span class="code-number">0</span>) <span class="code-keyword">as</span> [total],
            [p].[Name]
        <span class="code-keyword">FROM</span> [Products] [p]
    ) [t1]
<span class="code-keyword">WHERE</span>
    [t1].[total] &gt; <span class="code-number">1000</span></pre>
</div>
<!-- /sql-demo:expression-method -->
</div>
</div>
<a class="demo-more" href="documentation/how-to/teach-linq2db-convert-custom-net-code-to-sql.md#expressionmethod">ExpressionMethod in the docs <i class="bi bi-arrow-right"></i></a>
</div>
<!-- CTE -->
<div class="demo-panel demo-panel-4">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code"><span class="code-keyword">var</span> topProducts =
    db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    .Where(p =&gt; p.Price &gt; <span class="code-number">50</span>)
    .Select(p =&gt; <span class="code-keyword">new</span>
    {
        p.Id, p.Name,
        p.Price, p.CategoryId
    })
    .AsCte(<span class="code-string">"TopProducts"</span>);
&#10;<span class="code-comment">// CTE reused in join + subquery</span>
<span class="code-keyword">var</span> query =
    <span class="code-keyword">from</span> tp <span class="code-keyword">in</span> topProducts
    <span class="code-keyword">join</span> c <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Category</span>&gt;()
        <span class="code-keyword">on</span> tp.CategoryId <span class="code-keyword">equals</span> c.Id
    <span class="code-keyword">select new</span>
    {
        tp.Name, tp.Price,
        Category = c.Name,
        SameCategory = topProducts
            .Count(x =&gt; x.CategoryId
                == tp.CategoryId)
    };</pre>
</div>
<div class="demo-pane">
<!-- sql-demo:cte -->
<div class="demo-sql-variant" data-dialect="sqlserver">
<div class="demo-pane-label">Generated SQL · SQL Server</div>
<pre class="demo-code demo-sql" data-sql="cte/sqlserver"><span class="code-keyword">WITH</span> [TopProducts] ([CategoryId], [Name], [Price])
<span class="code-keyword">AS</span>
(
    <span class="code-keyword">SELECT</span>
        [p].[CategoryId],
        [p].[Name],
        [p].[Price]
    <span class="code-keyword">FROM</span> [Products] [p]
    <span class="code-keyword">WHERE</span> [p].[Price] &gt; <span class="code-number">50</span>
)
<span class="code-keyword">SELECT</span>
    [tp].[Name],
    [tp].[Price],
    [c_1].[Name],
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> [TopProducts] [t1]
        <span class="code-keyword">WHERE</span> [t1].[CategoryId] = [tp].[CategoryId]
    )
<span class="code-keyword">FROM</span>
    [TopProducts] [tp]
    <span class="code-keyword">INNER JOIN</span> [Categories] [c_1]
        <span class="code-keyword">ON</span> [tp].[CategoryId] = [c_1].[Id]</pre>
</div>
<div class="demo-sql-variant" data-dialect="postgresql">
<div class="demo-pane-label">Generated SQL · PostgreSQL</div>
<pre class="demo-code demo-sql" data-sql="cte/postgresql"><span class="code-keyword">WITH</span> "TopProducts" ("CategoryId", "Name", "Price")
<span class="code-keyword">AS</span>
(
    <span class="code-keyword">SELECT</span>
        p."CategoryId",
        p."Name",
        p."Price"
    <span class="code-keyword">FROM</span> "Products" p
    <span class="code-keyword">WHERE</span> p."Price" &gt; <span class="code-number">50</span>
)
<span class="code-keyword">SELECT</span>
    tp."Name",
    tp."Price",
    c_1."Name",
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> "TopProducts" t1
        <span class="code-keyword">WHERE</span> t1."CategoryId" = tp."CategoryId"
    )
<span class="code-keyword">FROM</span>
    "TopProducts" tp
    <span class="code-keyword">INNER JOIN</span> "Categories" c_1
        <span class="code-keyword">ON</span> tp."CategoryId" = c_1."Id"</pre>
</div>
<div class="demo-sql-variant" data-dialect="mysql">
<div class="demo-pane-label">Generated SQL · MySQL</div>
<pre class="demo-code demo-sql" data-sql="cte/mysql"><span class="code-keyword">WITH</span> `TopProducts` (`CategoryId`, `Name`, `Price`)
<span class="code-keyword">AS</span>
(
    <span class="code-keyword">SELECT</span>
        `p`.`CategoryId`,
        `p`.`Name`,
        `p`.`Price`
    <span class="code-keyword">FROM</span> `Products` `p`
    <span class="code-keyword">WHERE</span> `p`.`Price` &gt; <span class="code-number">50</span>
)
<span class="code-keyword">SELECT</span>
    `tp`.`Name`,
    `tp`.`Price`,
    `c_1`.`Name`,
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> `TopProducts` `t1`
        <span class="code-keyword">WHERE</span> `t1`.`CategoryId` = `tp`.`CategoryId`
    )
<span class="code-keyword">FROM</span>
    `TopProducts` `tp`
    <span class="code-keyword">INNER JOIN</span> `Categories` `c_1`
        <span class="code-keyword">ON</span> `tp`.`CategoryId` = `c_1`.`Id`</pre>
</div>
<div class="demo-sql-variant" data-dialect="oracle">
<div class="demo-pane-label">Generated SQL · Oracle</div>
<pre class="demo-code demo-sql" data-sql="cte/oracle"><span class="code-keyword">WITH</span> "TopProducts" ("CategoryId", "Name", "Price")
<span class="code-keyword">AS</span>
(
    <span class="code-keyword">SELECT</span>
        p."CategoryId",
        p."Name",
        p."Price"
    <span class="code-keyword">FROM</span> "Products" p
    <span class="code-keyword">WHERE</span> p."Price" &gt; <span class="code-number">50</span>
)
<span class="code-keyword">SELECT</span>
    tp."Name",
    tp."Price",
    c_1."Name",
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> "TopProducts" t1
        <span class="code-keyword">WHERE</span> t1."CategoryId" = tp."CategoryId"
    )
<span class="code-keyword">FROM</span>
    "TopProducts" tp
    <span class="code-keyword">INNER JOIN</span> "Categories" c_1
        <span class="code-keyword">ON</span> tp."CategoryId" = c_1."Id"</pre>
</div>
<div class="demo-sql-variant" data-dialect="sqlite">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<pre class="demo-code demo-sql" data-sql="cte/sqlite"><span class="code-keyword">WITH</span> [TopProducts] ([CategoryId], [Name], [Price])
<span class="code-keyword">AS</span>
(
    <span class="code-keyword">SELECT</span>
        [p].[CategoryId],
        [p].[Name],
        [p].[Price]
    <span class="code-keyword">FROM</span> [Products] [p]
    <span class="code-keyword">WHERE</span> [p].[Price] &gt; <span class="code-number">50</span>
)
<span class="code-keyword">SELECT</span>
    [tp].[Name],
    [tp].[Price],
    [c_1].[Name],
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> [TopProducts] [t1]
        <span class="code-keyword">WHERE</span> [t1].[CategoryId] = [tp].[CategoryId]
    )
<span class="code-keyword">FROM</span>
    [TopProducts] [tp]
    <span class="code-keyword">INNER JOIN</span> [Categories] [c_1]
        <span class="code-keyword">ON</span> [tp].[CategoryId] = [c_1].[Id]</pre>
</div>
<!-- /sql-demo:cte -->
</div>
</div>
<a class="demo-more" href="documentation/sql/cte.md">Common table expressions in the docs <i class="bi bi-arrow-right"></i></a>
</div>
<!-- Window Functions -->
<div class="demo-panel demo-panel-5">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code"><span class="code-keyword">var</span> query =
    <span class="code-keyword">from</span> p <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    <span class="code-keyword">select new</span>
    {
        p.Name,
        p.Price,
        Category = p.Category.Name,
        RowNum = <span class="code-type">Sql</span>.Ext.RowNumber()
            .Over()
            .PartitionBy(p.CategoryId)
            .OrderByDesc(p.Price)
            .ToValue(),
        Total  = <span class="code-type">Sql</span>.Ext.Sum(p.Price)
            .Over()
            .PartitionBy(p.CategoryId)
            .ToValue()
    };</pre>
</div>
<div class="demo-pane">
<!-- sql-demo:window-functions -->
<div class="demo-sql-variant" data-dialect="sqlserver">
<div class="demo-pane-label">Generated SQL · SQL Server</div>
<pre class="demo-code demo-sql" data-sql="window-functions/sqlserver"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price],
    [a_Category].[Name],
    <span class="code-type">ROW_NUMBER</span>() <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> [p].[CategoryId]
        <span class="code-keyword">ORDER BY</span> [p].[Price] <span class="code-keyword">DESC</span>),
    <span class="code-type">SUM</span>([p].[Price]) <span class="code-keyword">OVER</span> (
        <span class="code-keyword">PARTITION BY</span> [p].[CategoryId])
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">LEFT JOIN</span> [Categories] [a_Category]
        <span class="code-keyword">ON</span> [p].[CategoryId] = [a_Category].[Id]</pre>
</div>
<div class="demo-sql-variant" data-dialect="postgresql">
<div class="demo-pane-label">Generated SQL · PostgreSQL</div>
<pre class="demo-code demo-sql" data-sql="window-functions/postgresql"><span class="code-keyword">SELECT</span>
    p."Name",
    p."Price",
    "a_Category"."Name",
    <span class="code-type">ROW_NUMBER</span>() <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> p."CategoryId"
        <span class="code-keyword">ORDER BY</span> p."Price" <span class="code-keyword">DESC</span>),
    <span class="code-type">SUM</span>(p."Price") <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> p."CategoryId")
<span class="code-keyword">FROM</span>
    "Products" p
    <span class="code-keyword">LEFT JOIN</span> "Categories" "a_Category"
        <span class="code-keyword">ON</span> p."CategoryId" = "a_Category"."Id"</pre>
</div>
<div class="demo-sql-variant" data-dialect="mysql">
<div class="demo-pane-label">Generated SQL · MySQL</div>
<pre class="demo-code demo-sql" data-sql="window-functions/mysql"><span class="code-keyword">SELECT</span>
    `p`.`Name`,
    `p`.`Price`,
    `a_Category`.`Name`,
    <span class="code-type">ROW_NUMBER</span>() <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> `p`.`CategoryId`
        <span class="code-keyword">ORDER BY</span> `p`.`Price` <span class="code-keyword">DESC</span>),
    <span class="code-type">SUM</span>(`p`.`Price`) <span class="code-keyword">OVER</span> (
        <span class="code-keyword">PARTITION BY</span> `p`.`CategoryId`)
<span class="code-keyword">FROM</span>
    `Products` `p`
    <span class="code-keyword">LEFT JOIN</span> `Categories` `a_Category`
        <span class="code-keyword">ON</span> `p`.`CategoryId` = `a_Category`.`Id`</pre>
</div>
<div class="demo-sql-variant" data-dialect="oracle">
<div class="demo-pane-label">Generated SQL · Oracle</div>
<pre class="demo-code demo-sql" data-sql="window-functions/oracle"><span class="code-keyword">SELECT</span>
    p."Name",
    p."Price",
    a_Category."Name",
    <span class="code-type">ROW_NUMBER</span>() <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> p."CategoryId"
        <span class="code-keyword">ORDER BY</span> p."Price" <span class="code-keyword">DESC</span>),
    <span class="code-type">SUM</span>(p."Price") <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> p."CategoryId")
<span class="code-keyword">FROM</span>
    "Products" p
    <span class="code-keyword">LEFT JOIN</span> "Categories" a_Category
        <span class="code-keyword">ON</span> p."CategoryId" = a_Category."Id"</pre>
</div>
<div class="demo-sql-variant" data-dialect="sqlite">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<pre class="demo-code demo-sql" data-sql="window-functions/sqlite"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price],
    [a_Category].[Name],
    <span class="code-type">ROW_NUMBER</span>() <span class="code-keyword">OVER</span> (<span class="code-keyword">PARTITION BY</span> [p].[CategoryId]
        <span class="code-keyword">ORDER BY</span> [p].[Price] <span class="code-keyword">DESC</span>),
    <span class="code-type">SUM</span>([p].[Price]) <span class="code-keyword">OVER</span> (
        <span class="code-keyword">PARTITION BY</span> [p].[CategoryId])
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">LEFT JOIN</span> [Categories] [a_Category]
        <span class="code-keyword">ON</span> [p].[CategoryId] = [a_Category].[Id]</pre>
</div>
<!-- /sql-demo:window-functions -->
</div>
</div>
<a class="demo-more" href="documentation/sql/window-functions-(analytic-functions).md">Window functions in the docs <i class="bi bi-arrow-right"></i></a>
</div>
<!-- Merge -->
<div class="demo-panel demo-panel-6">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code">db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    .Merge()
    .Using(source)
    .OnTargetKey()
    .UpdateWhenMatched(
        (target, src) =&gt; <span class="code-keyword">new</span> <span class="code-type">Product</span>
        {
            Name  = src.Name,
            Price = src.Price,
        })
    .InsertWhenNotMatched(
        src =&gt; <span class="code-keyword">new</span> <span class="code-type">Product</span>
        {
            Id         = src.Id,
            Name       = src.Name,
            Price      = src.Price,
            CategoryId = src.CategoryId,
        })
    .Merge();</pre>
</div>
<div class="demo-pane">
<!-- sql-demo:merge -->
<div class="demo-sql-variant" data-dialect="sqlserver">
<div class="demo-pane-label">Generated SQL · SQL Server</div>
<pre class="demo-code demo-sql" data-sql="merge/sqlserver"><span class="code-keyword">SET IDENTITY_INSERT</span> [Products] <span class="code-keyword">ON</span>
<span class="code-keyword">MERGE INTO</span> [Products] [Target]
<span class="code-keyword">USING</span> (<span class="code-keyword">VALUES</span>
    (<span class="code-number">1</span>,N<span class="code-string">'Laptop'</span>,<span class="code-type">CAST</span>(<span class="code-number">999</span> <span class="code-keyword">AS</span> Decimal(<span class="code-number">3</span>, <span class="code-number">0</span>)),<span class="code-number">1</span>),
    (<span class="code-number">2</span>,N<span class="code-string">'Tablet'</span>,<span class="code-number">499</span>,<span class="code-number">1</span>)
) [Source]
([Id], [Name], [Price], [CategoryId])
<span class="code-keyword">ON</span> ([Target].[Id] = [Source].[Id])
&#10;<span class="code-keyword">WHEN MATCHED THEN</span>
<span class="code-keyword">UPDATE SET</span>
    [Name] = [Source].[Name],
    [Price] = [Source].[Price]
&#10;<span class="code-keyword">WHEN NOT MATCHED THEN</span>
<span class="code-keyword">INSERT</span>
([Id], [Name], [Price], [CategoryId])
<span class="code-keyword">VALUES</span>
(
    [Source].[Id],
    [Source].[Name],
    [Source].[Price],
    [Source].[CategoryId]
)
;
<span class="code-keyword">SET IDENTITY_INSERT</span> [Products] <span class="code-keyword">OFF</span></pre>
</div>
<div class="demo-sql-variant" data-dialect="postgresql">
<div class="demo-pane-label">Generated SQL · PostgreSQL</div>
<pre class="demo-code demo-sql" data-sql="merge/postgresql"><span class="code-keyword">MERGE INTO</span> "Products" "Target"
<span class="code-keyword">USING</span> (<span class="code-keyword">VALUES</span>
    (<span class="code-number">1</span>,<span class="code-string">'Laptop'</span>,<span class="code-number">999</span>::decimal,<span class="code-number">1</span>), (<span class="code-number">2</span>,<span class="code-string">'Tablet'</span>,<span class="code-number">499</span>,<span class="code-number">1</span>)
) "Source"
("Id", "Name", "Price", "CategoryId")
<span class="code-keyword">ON</span> ("Target"."Id" = "Source"."Id")
&#10;<span class="code-keyword">WHEN MATCHED THEN</span>
<span class="code-keyword">UPDATE SET</span>
    "Name" = "Source"."Name",
    "Price" = "Source"."Price"
&#10;<span class="code-keyword">WHEN NOT MATCHED THEN</span>
<span class="code-keyword">INSERT</span>
("Id", "Name", "Price", "CategoryId")
<span class="code-keyword">VALUES</span>
(
    "Source"."Id",
    "Source"."Name",
    "Source"."Price",
    "Source"."CategoryId"
)</pre>
</div>
<div class="demo-sql-variant" data-dialect="mysql">
<div class="demo-pane-label">Generated SQL · MySQL</div>
<p class="demo-na">MySQL has no MERGE statement.</p>
</div>
<div class="demo-sql-variant" data-dialect="oracle">
<div class="demo-pane-label">Generated SQL · Oracle</div>
<pre class="demo-code demo-sql" data-sql="merge/oracle"><span class="code-keyword">MERGE INTO</span> "Products" Target
<span class="code-keyword">USING</span> (
    <span class="code-keyword">SELECT</span> <span class="code-number">1</span> <span class="code-keyword">AS</span> "Id", <span class="code-string">'Laptop'</span> <span class="code-keyword">AS</span> "Name",
        <span class="code-number">999</span> <span class="code-keyword">AS</span> "Price", <span class="code-number">1</span> <span class="code-keyword">AS</span> "CategoryId" <span class="code-keyword">FROM</span> sys.dual
    <span class="code-keyword">UNION ALL</span>
    <span class="code-keyword">SELECT</span> <span class="code-number">2</span>, <span class="code-string">'Tablet'</span>, <span class="code-number">499</span>, <span class="code-number">1</span> <span class="code-keyword">FROM</span> sys.dual) "Source"
<span class="code-keyword">ON</span> (Target."Id" = "Source"."Id")
&#10;<span class="code-keyword">WHEN MATCHED THEN</span>
<span class="code-keyword">UPDATE SET</span>
    "Name" = "Source"."Name",
    "Price" = "Source"."Price"
&#10;<span class="code-keyword">WHEN NOT MATCHED THEN</span>
<span class="code-keyword">INSERT</span>
("Id", "Name", "Price", "CategoryId")
<span class="code-keyword">VALUES</span>
(
    "Source"."Id",
    "Source"."Name",
    "Source"."Price",
    "Source"."CategoryId"
)</pre>
</div>
<div class="demo-sql-variant" data-dialect="sqlite">
<div class="demo-pane-label">Generated SQL · SQLite</div>
<p class="demo-na">SQLite has no MERGE statement.</p>
</div>
<!-- /sql-demo:merge -->
</div>
</div>
<a class="demo-more" href="documentation/sql/merge/merge-api.md">Merge API in the docs <i class="bi bi-arrow-right"></i></a>
</div>
<!-- Temp Table -->
<div class="demo-panel demo-panel-7">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# Code</div>
<pre class="demo-code"><span class="code-comment">// Populate temp table from data</span>
<span class="code-keyword">var</span> ids = <span class="code-type">Enumerable</span>
    .Range(<span class="code-number">1</span>, <span class="code-number">500</span>)
    .Select(i =&gt; <span class="code-keyword">new</span> { Id = i });
&#10;<span class="code-keyword">using var</span> tmp =
    db.CreateTempTable(
        <span class="code-string">"FilterIds"</span>, ids);
&#10;<span class="code-comment">// Join temp table in LINQ query</span>
<span class="code-keyword">var</span> query =
    <span class="code-keyword">from</span> p <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    <span class="code-keyword">join</span> t <span class="code-keyword">in</span> tmp
        <span class="code-keyword">on</span> p.Id <span class="code-keyword">equals</span> t.Id
    <span class="code-keyword">orderby</span> p.Name
    <span class="code-keyword">select new</span>
    {
        p.Name,
        p.Price
    };</pre>
</div>
<div class="demo-pane">
<div class="demo-pane-label">Executed SQL · SQLite</div>
<pre class="demo-code demo-sql"><span class="code-comment">-- 1. CREATE TABLE</span>
<span class="code-keyword">CREATE TEMPORARY TABLE</span> temp.[FilterIds]
(
    [Id] <span class="code-type">INTEGER</span> <span class="code-keyword">NOT NULL</span>
)
&#10;<span class="code-comment">-- 2. BulkCopy 500 rows (abbreviated)</span>
<span class="code-keyword">INSERT INTO</span> temp.[FilterIds] ([Id])
<span class="code-keyword">VALUES</span>
    (<span class="code-number">1</span>), (<span class="code-number">2</span>), …, (<span class="code-number">500</span>)
&#10;<span class="code-comment">-- 3. Query joins temp table</span>
<span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price]
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">INNER JOIN</span> temp.[FilterIds] [t]
        <span class="code-keyword">ON</span> [p].[Id] = [t].[Id]
<span class="code-keyword">ORDER BY</span>
    [p].[Name]
&#10;<span class="code-comment">-- 4. Dispose drops table</span>
<span class="code-keyword">DROP TABLE IF EXISTS</span> temp.[FilterIds]</pre>
</div>
</div>
<a class="demo-more" href="documentation/sql/bulk-copy.md">Bulk copy in the docs <i class="bi bi-arrow-right"></i></a>
</div>
</div>
</div>
</div>
</div>
</div>
</div>
</section>

<section class="landing-dbs" aria-labelledby="landing-dbs-title">
<h2 id="landing-dbs-title" class="landing-dbs-title no-anchor">One API, 16 databases</h2>
<ul class="landing-dbs-list">
<li><a href="documentation/general/databases.md">SQL Server</a></li>
<li><a href="documentation/general/databases.md">PostgreSQL</a></li>
<li><a href="documentation/general/databases.md">MySQL</a></li>
<li><a href="documentation/general/databases.md">MariaDB</a></li>
<li><a href="documentation/general/databases.md">SQLite</a></li>
<li><a href="documentation/general/databases.md">Oracle</a></li>
<li><a href="documentation/general/databases.md">DB2</a></li>
<li><a href="documentation/general/databases.md">Informix</a></li>
<li><a href="documentation/general/databases.md">Firebird</a></li>
<li><a href="documentation/general/databases.md">SAP HANA</a></li>
<li><a href="documentation/general/databases.md">SAP ASE</a></li>
<li><a href="documentation/general/databases.md">ClickHouse</a></li>
<li><a href="documentation/general/databases.md">DuckDB</a></li>
<li><a href="documentation/general/databases.md">YDB</a></li>
<li><a href="documentation/general/databases.md">Access</a></li>
<li><a href="documentation/general/databases.md">SQL CE</a></li>
</ul>
</section>

<section class="landing-steps" aria-labelledby="landing-steps-title">
<div class="container-xxl">
<h2 id="landing-steps-title" class="landing-steps-title no-anchor">Get started in three steps</h2>
<ol class="row g-4 landing-steps-list">

<li class="col-lg-4">
<div class="landing-step">
<div class="landing-step-number" aria-hidden="true">1</div>
<h3 class="no-anchor">Install</h3>
<p>Add linq2db and the ADO.NET provider of your database.</p>
<pre class="landing-step-code">dotnet add package linq2db
dotnet add package Microsoft.Data.SqlClient</pre>
<a class="landing-step-link" href="documentation/get-started/install/index.md">Installing <i class="bi bi-arrow-right"></i></a>
</div>
</li>

<li class="col-lg-4">
<div class="landing-step">
<div class="landing-step-number" aria-hidden="true">2</div>
<h3 class="no-anchor">Connect</h3>
<p>Point a connection at your database; it opens on demand.</p>
<pre class="landing-step-code"><span class="code-keyword">using var</span> db = <span class="code-keyword">new</span> <span class="code-type">DataConnection</span>(
    <span class="code-keyword">new</span> <span class="code-type">DataOptions</span>()
        .UseSqlServer(connectionString));</pre>
<a class="landing-step-link" href="documentation/general/managing-data-connection.md">Managing connections <i class="bi bi-arrow-right"></i></a>
</div>
</li>

<li class="col-lg-4">
<div class="landing-step">
<div class="landing-step-number" aria-hidden="true">3</div>
<h3 class="no-anchor">Query</h3>
<p>Write LINQ against your classes and get SQL you can read.</p>
<pre class="landing-step-code"><span class="code-keyword">var</span> products = <span class="code-keyword">await</span> db
    .GetTable&lt;<span class="code-type">Product</span>&gt;()
    .Where(p =&gt; p.Price &gt; <span class="code-number">100</span>)
    .OrderBy(p =&gt; p.Name)
    .ToListAsync();</pre>
<a class="landing-step-link" href="documentation/get-started/overview.md#queries">Queries <i class="bi bi-arrow-right"></i></a>
</div>
</li>

</ol>
</div>
</section>

<section class="landing-features">
<div class="container-xxl">
<h2 class="visually-hidden no-anchor">Features</h2>
<div class="row g-4 g-lg-5">

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-lightning-charge-fill"></i></div>
<h3 class="no-anchor">High Performance</h3>
<p>Generates optimized SQL with minimal overhead. Materializes results with compiled expressions and does no change tracking.</p>
</div>
</div>

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-database-fill"></i></div>
<h3 class="no-anchor"><a href="documentation/general/databases.md">Multi-Database</a></h3>
<p>SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Firebird, ClickHouse, and many more.</p>
</div>
</div>

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-braces"></i></div>
<h3 class="no-anchor"><a href="documentation/sql/join-operators.md">Full LINQ</a></h3>
<p>Type-safe queries with advanced SQL: CTEs, Window Functions, MERGE, Bulk Copy, and more.</p>
</div>
</div>

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-puzzle-fill"></i></div>
<h3 class="no-anchor"><a href="https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB.EntityFrameworkCore/README.md">Extensible</a></h3>
<p>EF Core integration, ASP.NET Identity, gRPC, SignalR, and HTTP remoting out of the box.</p>
</div>
</div>

</div>
</div>
</section>

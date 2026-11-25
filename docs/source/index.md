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
<a href="https://dotnetfoundation.org/" class="landing-badge mb-3"><i class="bi bi-patch-check-fill"></i> .NET Foundation Member</a>
<h1 class="landing-title">Data access<br/>made <span class="landing-accent">simple</span></h1>
<p class="landing-subtitle">The fastest LINQ database access library. A lightweight, type-safe layer between your POCO objects and your database with full SQL support.</p>
<div class="landing-actions">
<a href="documentation/get-started/install/index.md" class="btn btn-primary btn-lg landing-btn-primary">Get Started <i class="bi bi-arrow-right"></i></a>
<a href="https://github.com/linq2db/linq2db" class="btn btn-outline-light btn-lg landing-btn-secondary"><i class="bi bi-github"></i> GitHub</a>
</div>
<div class="landing-install mt-4">
<code>dotnet add package linq2db</code>
</div>
</div>
<div class="col-lg-6">
<div class="demo-container">
<input type="radio" name="demo-tab" id="demo-tab-1" class="demo-radio" checked />
<input type="radio" name="demo-tab" id="demo-tab-2" class="demo-radio" />
<input type="radio" name="demo-tab" id="demo-tab-3" class="demo-radio" />
<input type="radio" name="demo-tab" id="demo-tab-4" class="demo-radio" />
<input type="radio" name="demo-tab" id="demo-tab-5" class="demo-radio" />
<input type="radio" name="demo-tab" id="demo-tab-6" class="demo-radio" />
<input type="radio" name="demo-tab" id="demo-tab-7" class="demo-radio" />
<div class="demo-tabs">
<label for="demo-tab-1" class="demo-tab">Query</label>
<label for="demo-tab-2" class="demo-tab">Associations</label>
<label for="demo-tab-3" class="demo-tab">ExpressionMethod</label>
<label for="demo-tab-4" class="demo-tab">CTE</label>
<label for="demo-tab-5" class="demo-tab">Window Functions</label>
<label for="demo-tab-6" class="demo-tab">Merge</label>
<label for="demo-tab-7" class="demo-tab">Temp Table</label>
</div>
<div class="demo-panels">
<!-- Basic Query -->
<div class="demo-panel demo-panel-1">
<div class="demo-split">
<div class="demo-pane">
<div class="demo-pane-label">C# LINQ</div>
<pre class="demo-code"><span class="code-keyword">var</span> query =
    <span class="code-keyword">from</span> p <span class="code-keyword">in</span> db.GetTable&lt;<span class="code-type">Product</span>&gt;()
    <span class="code-keyword">where</span> p.Price &gt; <span class="code-number">100</span>
       &amp;&amp; p.CategoryId == <span class="code-number">1</span>
    <span class="code-keyword">orderby</span> p.Name
    <span class="code-keyword">select new</span>
    {
        p.Name,
        p.Price
    };</pre>
</div>
<div class="demo-pane">
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price]
<span class="code-keyword">FROM</span>
    [Products] [p]
<span class="code-keyword">WHERE</span>
    [p].[Price] &gt; <span class="code-number">100</span>
    <span class="code-keyword">AND</span> [p].[CategoryId] = <span class="code-number">1</span>
<span class="code-keyword">ORDER BY</span>
    [p].[Name]</pre>
</div>
</div>
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
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [a_Category].[Name],
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> [OrderItems] [a_OrderItems]
        <span class="code-keyword">WHERE</span>
            [p].[Id] = [a_OrderItems].[ProductId]
    )
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">LEFT JOIN</span> [Categories] [a_Category]
        <span class="code-keyword">ON</span> [p].[CategoryId] = [a_Category].[Id]</pre>
</div>
</div>
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
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-keyword">SELECT</span>
    [t1].[Name],
    [t1].[total]
<span class="code-keyword">FROM</span>
    (
        <span class="code-keyword">SELECT</span>
            (
                <span class="code-keyword">SELECT</span> <span class="code-type">SUM</span>(
                    <span class="code-type">CAST</span>([oi].[Quantity]
                      <span class="code-keyword">AS</span> Decimal)
                    * [oi].[UnitPrice])
                <span class="code-keyword">FROM</span> [OrderItems] [oi]
                <span class="code-keyword">WHERE</span>
                    [p].[Id] = [oi].[ProductId]
            ) <span class="code-keyword">as</span> [total],
            [p].[Name]
        <span class="code-keyword">FROM</span>
            [Products] [p]
    ) [t1]
<span class="code-keyword">WHERE</span>
    [t1].[total] &gt; <span class="code-number">1000</span></pre>
</div>
</div>
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
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-keyword">WITH</span> [TopProducts]
    ([CategoryId], [Name], [Price])
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
    [c].[Name],
    (
        <span class="code-keyword">SELECT</span> <span class="code-type">COUNT</span>(*)
        <span class="code-keyword">FROM</span> [TopProducts] [t1]
        <span class="code-keyword">WHERE</span> [t1].[CategoryId]
            = [tp].[CategoryId]
    )
<span class="code-keyword">FROM</span>
    [TopProducts] [tp]
    <span class="code-keyword">INNER JOIN</span> [Categories] [c]
        <span class="code-keyword">ON</span> [tp].[CategoryId] = [c].[Id]</pre>
</div>
</div>
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
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price],
    [a_Category].[Name],
    <span class="code-type">ROW_NUMBER</span>() <span class="code-keyword">OVER</span>(
        <span class="code-keyword">PARTITION BY</span> [p].[CategoryId]
        <span class="code-keyword">ORDER BY</span> [p].[Price] <span class="code-keyword">DESC</span>),
    <span class="code-type">SUM</span>([p].[Price]) <span class="code-keyword">OVER</span>(
        <span class="code-keyword">PARTITION BY</span> [p].[CategoryId])
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">LEFT JOIN</span> [Categories] [a_Category]
        <span class="code-keyword">ON</span> [p].[CategoryId]
            = [a_Category].[Id]</pre>
</div>
</div>
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
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-keyword">MERGE INTO</span> [Products] [Target]
<span class="code-keyword">USING</span> (<span class="code-keyword">VALUES</span>
    (<span class="code-number">1</span>,N<span class="code-string">'Laptop'</span>,<span class="code-number">999</span>,<span class="code-number">1</span>),
    (<span class="code-number">2</span>,N<span class="code-string">'Tablet'</span>,<span class="code-number">499</span>,<span class="code-number">1</span>)
) [Source]
([Id],[Name],[Price],[CategoryId])
<span class="code-keyword">ON</span> ([Target].[Id] = [Source].[Id])
&#10;<span class="code-keyword">WHEN MATCHED THEN</span>
<span class="code-keyword">UPDATE SET</span>
    [Name]  = [Source].[Name],
    [Price] = [Source].[Price]
&#10;<span class="code-keyword">WHEN NOT MATCHED THEN</span>
<span class="code-keyword">INSERT</span>
    ([Id],[Name],[Price],[CategoryId])
<span class="code-keyword">VALUES</span>
    ([Source].[Id],
     [Source].[Name],
     [Source].[Price],
     [Source].[CategoryId])
;</pre>
</div>
</div>
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
        <span class="code-string">"#FilterIds"</span>, ids);
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
<div class="demo-pane-label">Generated SQL</div>
<pre class="demo-code demo-sql"><span class="code-comment">-- 1. CREATE TABLE</span>
<span class="code-keyword">CREATE TABLE</span> [#FilterIds]
(
    [Id] int <span class="code-keyword">NOT NULL</span>
)
&#10;<span class="code-comment">-- 2. BulkCopy 500 rows</span>
&#10;<span class="code-comment">-- 3. Query joins temp table</span>
<span class="code-keyword">SELECT</span>
    [p].[Name],
    [p].[Price]
<span class="code-keyword">FROM</span>
    [Products] [p]
    <span class="code-keyword">INNER JOIN</span> [#FilterIds] [t]
        <span class="code-keyword">ON</span> [p].[Id] = [t].[Id]
<span class="code-keyword">ORDER BY</span>
    [p].[Name]
&#10;<span class="code-comment">-- 4. Dispose drops table</span></pre>
</div>
</div>
</div>
</div>
</div>
</div>
</div>
</div>
</div>
</section>

<section class="landing-cards">
<div class="container-xxl">
<div class="row g-4">

<div class="col-md-4">
<a href="documentation/index.md" class="landing-card">
<div class="landing-card-icon-wrap"><i class="bi bi-book"></i></div>
<h3>Documentation</h3>
<p>Installation, core concepts, SQL features, configuration guides, and best practices.</p>
<span class="landing-card-link">Browse docs <i class="bi bi-arrow-right"></i></span>
</a>
</div>

<div class="col-md-4">
<a href="articles/index.md" class="landing-card">
<div class="landing-card-icon-wrap"><i class="bi bi-newspaper"></i></div>
<h3>Articles</h3>
<p>Release notes, announcements, and news from the Linq To DB project.</p>
<span class="landing-card-link">Read articles <i class="bi bi-arrow-right"></i></span>
</a>
</div>

<div class="col-md-4">
<a href="api/linq2db/index.md" class="landing-card">
<div class="landing-card-icon-wrap"><i class="bi bi-code-slash"></i></div>
<h3>API Reference</h3>
<p>Complete API documentation for all packages, extensions, and integrations.</p>
<span class="landing-card-link">Explore API <i class="bi bi-arrow-right"></i></span>
</a>
</div>

</div>
</div>
</section>

<section class="landing-features">
<div class="container-xxl">
<div class="row g-4 g-lg-5">

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-lightning-charge-fill"></i></div>
<h4>High Performance</h4>
<p>Generates optimized SQL with minimal overhead. No reflection at runtime, no heavy object tracking.</p>
</div>
</div>

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-database-fill"></i></div>
<h4>Multi-Database</h4>
<p>SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Firebird, ClickHouse, and many more.</p>
</div>
</div>

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-braces"></i></div>
<h4>Full LINQ</h4>
<p>Type-safe queries with advanced SQL: CTEs, Window Functions, MERGE, Bulk Copy, and more.</p>
</div>
</div>

<div class="col-md-6 col-lg-3">
<div class="landing-feature">
<div class="landing-feature-icon"><i class="bi bi-puzzle-fill"></i></div>
<h4>Extensible</h4>
<p>EF Core integration, ASP.NET Identity, gRPC, SignalR, and HTTP remoting out of the box.</p>
</div>
</div>

</div>
</div>
</section>

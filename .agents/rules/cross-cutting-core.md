---
paths:
  - "Source/LinqToDB/**/SqlQuery/**"
  - "Source/LinqToDB/**/Translation/**"
---

# Cross-cutting core: SQL AST / translator invariants

You are editing shared engine code (SQL AST, member translators) that **every** provider consumes — the blast radius of a signature or namespace change here is the whole product. Before changing signatures, namespaces, or reshaping these types, read the matching sections of [`../docs/code-design.md`](../docs/code-design.md):

- **SQL AST types live in `LinqToDB.Internal.SqlQuery`** — new AST nodes go in the `Internal.SqlQuery` namespace, never the public `LinqToDB.SqlQuery`. A legacy public AST type whose signature you change should be *moved* to `Internal.SqlQuery` in the same PR, not baseline-suppressed as a public break.
- **Cross-cutting internals are shared** — don't reshape the SQL AST, `IDataProvider`, or translator interfaces for a local/provider-scoped fix; surface the question first.
- **Provider-called core methods are public, not internal** — a new shared-core method that a provider `*SqlBuilder`/`*SqlOptimizer`/translator calls must be `public` (external providers consume it) and needs a `PublicAPI.Unshipped.txt` entry.
- **Version-aware translators: derive a subclass, don't parameterize** — dispatch on `Version` in `CreateMemberTranslator()`; don't add a feature-flag ctor param.

This rule only points at the canonical invariants; `code-design.md` is the source of truth.

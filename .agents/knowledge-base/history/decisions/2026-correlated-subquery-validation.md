---
area: GLOBAL
kind: decision
sources: [git]
confidence: high
last_verified: 2026-06-15
last_verified_sha: b3340aa9ded15ffc626983fd242e6399daa081ca
---

# Correlated subquery validation for level-0 providers

## Context

ClickHouse and YDB (SupportedCorrelatedSubqueriesLevel == 0) cannot execute correlated subqueries. SqlQueryValidatorVisitor only detected correlated subqueries in column position via _columnSubqueryLevel; correlated subqueries in predicate or expression position (EXISTS, IN, scalar predicates) passed validation and reached the server, failing with a raw provider error instead of a clean LinqToDBException.

## Decision

SqlQueryValidatorVisitor was extended to track _inExpression and reject correlated subqueries in predicate/expression position for level-0 non-APPLY providers (commit b12201c, Jun 4, #5574). A ForErrorReporting flag gates the rejection to the final ExpressionBuilder validation pass only, so optimizer and eager-load capability probes are unaffected. YDB got IsSupportedSimpleCorrelatedSubqueries = false. A ThrowsRequiresCorrelatedSubquery(simple: bool) attribute replaces ~78 YdbMemberNotFoundAttribute usages. Separately, 48771ed (Jun 4, #5582) fixed NOT IN/IN emulation null-safety by filtering NULLs from the membership subquery on level-0 providers.

## Why

The ForErrorReporting flag was necessary because the initial implementation changed optimizer/eager-load behavior: a correlated subquery in an ORDER BY context the optimizer would normally flatten was rejected during capability probes, dropping the ORDER BY silently (Issue4596 regression on ClickHouse). Gating on ForErrorReporting restores the original probe behavior while still throwing the clean exception at the final gate.

## Consequences

- YdbMemberNotFoundAttribute removed; ~78 test attributes updated to ThrowsRequiresCorrelatedSubquery.
- SqlQueryValidatorVisitor gains ForErrorReporting flag and _inExpression tracking.
- IsSupportedSimpleCorrelatedSubqueries = false on YdbDataProvider.
- File anchors: Source/LinqToDB/SqlQuery/SqlQueryValidatorVisitor.cs

## Sources

- Commit b12201c -- Reject unsupported correlated subqueries in expression position (#5574) (MaceWindu, 2026-06-04)
- Commit 48771ed -- Make non-correlated NOT IN/IN emulation null-safe in the subquery (#5582) (MaceWindu, 2026-06-04)

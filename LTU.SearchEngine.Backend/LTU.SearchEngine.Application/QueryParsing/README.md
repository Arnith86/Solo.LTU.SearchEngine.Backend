# QueryParsing (UC-3001)

This module handles parsing of user search queries.

## Responsibility

Convert raw query strings into a structured `ParsedQuery` object
that can be consumed by the search/index layer.

## Files

- QueryParser.cs: Parses raw query strings (FRQ-3001–FRQ-3008)
- IQueryParser.cs: Interface for query parsing
- ParsedQuery.cs: Structured representation of a query
- QueryMode.cs: Defines AND / OR behavior

## Notes

- Query parsing is independent of crawler and index parsing
- Operators must be uppercase (AND, OR, NOT)
- Parentheses and escaping are handled in UC-3002

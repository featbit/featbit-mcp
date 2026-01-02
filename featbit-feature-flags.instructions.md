---
description: FeatBit Feature Flag Architecture Guide
applyTo: 'FeatBit/**/*.cs'
---

## Feature Flag Definition Location

All feature flags are defined in [FeatBit/FeatBit.FeatureFlags/FeatoureFlag.cs](FeatBit/FeatBit.FeatureFlags/FeatureFlag.cs) as static readonly fields.

## Finding Feature Flag Usage Locations

**Important**: This is a .NET program. When searching, ensure you're finding actual code references, not keyword matches in comments or strings.

### Method 1: Direct Semantic Analysis (Recommended for Quick Search)

Use the `list_code_usages` tool directly for semantic code analysis:
- Pass in the feature flag field name (e.g., `DocNotFound`)
- Pass in the file path `c:\Code\featbit\featbit-mcp\FeatBit\FeatBit.FeatureFlags\FeatureFlag.cs`
- The tool will analyze code semantics using AST, compilation checks, and C# syntax analysis
- Returns only actual code references, filtering out comments, strings, and false positives

### Method 2: Two-Step Approach (Recommended for Diff Analysis or Detailed Investigation)

1. **Initial Search**: Use `grep_search` to find potential matches of `FeatureFlag.{Feature_Flag_Variable}` pattern
   - Search for the pattern like `FeatureFlag.DocNotFound`
   - Note: The pattern might span across lines with whitespace or newlines, but when normalized (whitespace/newlines removed), it will match the pattern
   - Example: Search for `FeatureFlag.DocNotFound` in diffs or code files

2. **Verification**: Use `list_code_usages` tool to confirm references are actual code usage
   - Pass in the feature flag field name (e.g., `DocNotFound`)
   - Pass in the file path `c:\Code\featbit\featbit-mcp\FeatBit\FeatBit.FeatureFlags\FeatureFlag.cs`
   - This leverages AST (Abstract Syntax Tree), compilation checks, and C# syntax analysis
   - Filters out false positives from comments, strings, or other non-reference contexts

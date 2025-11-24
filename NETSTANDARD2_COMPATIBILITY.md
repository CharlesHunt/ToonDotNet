# NetStandard 2.0 Compatibility Changes

This document summarizes the changes made to ensure the ToonFormat library compiles for NetStandard2.0 (C# 7.3) while maintaining modern C# features for newer frameworks.

## Overview

The project now uses conditional compilation (`#if NETSTANDARD2_0`) to provide C# 7.3-compatible code paths for NetStandard2.0 while keeping modern C# 11 features for .NET 8+.

## Changes Made

### 1. Required Property Modifier

**Files Modified:**
- `src/ToonFormat/Types.cs`
- `src/ToonFormat/Decode/ToonScanner.cs`
- `src/ToonFormat/Decode/ToonParser.cs`

**Changes:**
- Replaced `required` properties with conditional compilation
- For NETSTANDARD2_0: Regular properties without `required` modifier
- For .NET 8+: Properties with `required` modifier

**Example:**
```csharp
#if NETSTANDARD2_0
    public string Raw { get; set; }
#else
    public required string Raw { get; set; }
#endif
```

### 2. Pattern Matching with 'or'

**File Modified:** `src/ToonFormat/Types.cs`

**Changes:**
- Replaced `is ... or ...` pattern with traditional boolean logic for NETSTANDARD2_0
- Modern pattern matching retained for .NET 8+

**Example:**
```csharp
#if NETSTANDARD2_0
    var vk = element.ValueKind;
    return vk == JsonValueKind.String || vk == JsonValueKind.Number || ...;
#else
    return element.ValueKind is JsonValueKind.String or JsonValueKind.Number or ...;
#endif
```

### 3. Switch Expressions

**File Modified:** `src/ToonFormat/Types.cs`

**Changes:**
- Replaced switch expressions with traditional switch statements for NETSTANDARD2_0
- Switch expressions retained for .NET 8+

**Example:**
```csharp
#if NETSTANDARD2_0
    switch (element.ValueKind)
    {
        case JsonValueKind.String:
            return element.GetString();
        // ...
    }
#else
    return element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        // ...
    };
#endif
```

### 4. Range and Index Operators

**Files Modified:**
- `src/ToonFormat/Decode/ToonScanner.cs`
- `src/ToonFormat/Decode/ToonParser.cs`
- `src/ToonFormat/Decode/ToonDecoder.cs`
- `src/ToonFormat/Shared/StringUtils.cs`

**Changes:**
- Replaced range operators (`..`, `[start..end]`, `[^1]`) with `Substring()` calls
- Index-from-end operators (`[^1]`) replaced with `[length - 1]`

**Examples:**
```csharp
// Range operator
#if NETSTANDARD2_0
    string content = raw.Substring(indent);
#else
    string content = raw[indent..];
#endif

// Index from end
#if NETSTANDARD2_0
    return value[0] == Constants.DoubleQuote && value[value.Length - 1] == Constants.DoubleQuote;
#else
    return value[0] == Constants.DoubleQuote && value[^1] == Constants.DoubleQuote;
#endif

// Substring with range
#if NETSTANDARD2_0
    string content = quotedValue.Substring(1, quotedValue.Length - 2);
#else
    string content = quotedValue[1..^1];
#endif
```

### 5. StartsWith/EndsWith/Contains with Char Argument

**Files Modified:**
- `src/ToonFormat/Decode/ToonParser.cs`
- `src/ToonFormat/Decode/ToonDecoder.cs`
- `src/ToonFormat/Shared/LiteralUtils.cs`

**Changes:**
- Replaced `StartsWith(char)`, `EndsWith(char)`, and `Contains(char)` with string overloads
- These methods only accept char arguments in .NET Core 2.1+, not NetStandard2.0

**Example:**
```csharp
#if NETSTANDARD2_0
    if (trimmed.StartsWith(Constants.DoubleQuote.ToString()))
#else
    if (trimmed.StartsWith(Constants.DoubleQuote))
#endif
```

## Build Verification

All target frameworks compile successfully:
- ✅ netstandard2.0 (C# 7.3)
- ✅ net8.0 (C# 12)
- ✅ net9.0 (C# 13)
- ✅ net10.0 (C# 13)

All unit tests pass for all frameworks.

## Performance Considerations

- For NetStandard2.0: Minor performance differences due to `Substring()` allocations vs range operators
- For .NET 8+: Full performance benefits of modern C# features (range operators, pattern matching)
- The conditional compilation ensures optimal code is generated for each target framework

## Maintenance Notes

When adding new code:
1. Avoid C# 8+ features in shared code paths unless wrapped in conditional compilation
2. For NetStandard2.0, use:
   - Traditional switch statements instead of switch expressions
   - Boolean operators (`||`, `&&`) instead of pattern matching with `or`/`and`
   - `Substring()` instead of range operators
   - `.ToString()` on char constants for `StartsWith`/`EndsWith`/`Contains`
   - Regular properties instead of `required` properties

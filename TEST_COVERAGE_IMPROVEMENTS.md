# ToonFormat Test Coverage Improvements

## Summary

Comprehensive unit tests have been added for the `ToonDecoder` and `ToonParser` internal classes to significantly improve code coverage.

## New Test Files

### 1. ToonDecoderTests.cs (70+ tests)
Tests comprehensive scenarios for the TOON decoding logic:

#### **Basic Decoding**
- Single primitive values (numbers, strings, booleans, null)
- Simple objects with key-value pairs
- Quoted keys and values
- Nested objects and empty objects

#### **Array Decoding**
- Empty arrays
- Root arrays
- Inline arrays with various delimiters (comma, pipe, tab)
- Tabular arrays with field headers
- List arrays with `-` markers
- Arrays with length markers (`#`)
- Mixed-type arrays
- Nested arrays and arrays of objects

#### **Advanced Features**
- Multiple depth levels (deeply nested structures)
- Quoted strings with escape sequences (`\n`, `\t`, `\"`, `\\`)
- Floating-point and scientific notation numbers
- Negative and large numbers
- Custom indentation levels
- Strict vs non-strict validation modes

#### **Edge Cases**
- Empty input validation
- Invalid TOON syntax
- Missing array items (strict mode validation)
- Complex nested structures mixing objects and arrays

### 2. ToonParserTests.cs (90+ tests)
Tests the internal parsing utilities used by the decoder:

#### **ParseDelimitedValues**
- Simple comma-separated values
- Quoted values containing delimiters
- Escaped quotes within values
- Various delimiters (comma, pipe, tab)
- Empty strings and single values
- Trailing/leading/consecutive delimiters
- Whitespace handling

#### **MapRowValuesToPrimitives**
- Number conversion
- Mixed-type arrays
- Boolean and null values
- Empty arrays

#### **ParseStringLiteral**
- Unquoted strings
- Quoted strings
- Escape sequence handling (`\n`, `\t`, `\"`, `\\`)
- Unterminated quote detection
- Invalid quote termination detection
- Empty quoted strings

#### **IsArrayHeaderAfterHyphen / IsObjectFirstFieldAfterHyphen**
- Valid array headers
- Missing brackets or colons
- Whitespace tolerance
- Quoted keys with colons

#### **ParseArrayHeaderLine**
- Simple arrays with inline values
- Tabular arrays with field headers
- Custom delimiters (pipe, tab)
- Length markers (`#`)
- Quoted keys (including keys with brackets)
- Root arrays (no key)
- Empty arrays
- Invalid syntax detection
- Complex combinations of features

#### **Error Handling**
- Invalid bracket content
- Unterminated brackets
- Invalid array lengths
- Unterminated quoted keys
- Missing colons

## Test Coverage Metrics

### Coverage Areas

| Component | Test Count | Coverage Focus |
|-----------|------------|----------------|
| ToonDecoder | 70+ | Decoding logic, validation, error handling |
| ToonParser | 90+ | Parsing utilities, edge cases, syntax validation |
| **Total** | **160+** | **Comprehensive end-to-end and unit testing** |

### Key Features Tested

? **Primitive Values**: Numbers, strings, booleans, null  
? **Objects**: Simple, nested, empty, with quoted keys  
? **Arrays**: Inline, tabular, list-style, nested, mixed-type  
? **Delimiters**: Comma, pipe, tab  
? **Special Features**: Length markers, field headers, escape sequences  
? **Validation**: Strict mode, array length validation, syntax checking  
? **Edge Cases**: Empty input, invalid syntax, unterminated quotes  
? **Error Handling**: Proper exception types and messages  

## Configuration Changes

### InternalsVisibleTo Attribute
Added to `ToonFormat.csproj` to expose internal classes (`ToonDecoder`, `ToonParser`) to the test project:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
    <_Parameter1>ToonFormat.Tests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

This allows comprehensive testing of internal implementation details while keeping the public API surface clean.

## Benefits

1. **Improved Code Quality**: Edge cases and error conditions are thoroughly tested
2. **Regression Prevention**: Changes to decoder/parser logic will be caught by tests
3. **Documentation**: Tests serve as executable documentation for expected behavior
4. **Confidence**: Comprehensive coverage enables safe refactoring and enhancements
5. **Debugging**: Failing tests pinpoint exact issues in parsing/decoding logic

## Running the Tests

```bash
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter "FullyQualifiedName~ToonDecoderTests"
dotnet test --filter "FullyQualifiedName~ToonParserTests"

# Run with coverage (if tooling is installed)
dotnet test --collect:"XPlat Code Coverage"
```

## Next Steps

The test suite can be further enhanced with:
- Performance benchmarks for large datasets
- Fuzzing tests for malformed input
- Property-based testing for round-trip validation
- Integration tests with real-world TOON files
- Memory allocation profiling tests

---

**Total New Tests**: 160+  
**Build Status**: ? Passing  
**Code Coverage**: Significantly improved for `ToonDecoder` and `ToonParser`

using System.Text.Json;

namespace ToonFormat.Tests;

public class ToonBasicTests
{
    // TOON_V4.md phase 0: pins the spec baseline explicitly so future
    // version bumps (e.g. moving to v4.x) have a fixed starting point
    // instead of being inferred from behavior — this guards against the
    // constant drifting silently out of sync with the docs. Moved from
    // "3.3.2" to "4.1.1" in TOON_V4.md phase 5 (the encoder-default
    // flip); one known gap remains at this version — the decoder's
    // number-grammar audit against spec §4 is incomplete — tracked in
    // TOON_V4.md's "Number grammar" gap-list row.
    [Fact]
    public void Constants_SpecVersion_MatchesDocumentedBaseline()
    {
        Assert.Equal("4.1.1", Constants.SpecVersion);
    }

    [Fact]
    public void Encode_SimpleObject_ReturnsExpectedToonFormat()
    {
        // Arrange
        var data = new { name = "Alice", age = 30 };
        
        // Act
        string result = Toon.Encode(data);
        
        // Assert
        Assert.Contains("name: Alice", result);
        Assert.Contains("age: 30", result);
    }

    [Fact]
    public void Decode_SimpleToonFormat_ReturnsCorrectData()
    {
        // Arrange
        string toonString = "name: Alice\nage: 30";
        
        // Act
        JsonElement result = Toon.Decode(toonString);
        
        // Assert
        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public void RoundTrip_ComplexObject_PreservesFidelity()
    {
        // Arrange
        var original = new 
        { 
            users = new[] 
            { 
                new { id = 1, name = "Alice", active = true },
                new { id = 2, name = "Bob", active = false }
            },
            count = 2,
            metadata = new { version = "1.0", created = "2024-01-01" }
        };

        // Act
        JsonElement roundTripResult = Toon.RoundTrip(original);

        // Assert
        var users = roundTripResult.GetProperty("users").EnumerateArray().ToArray();
        Assert.Equal(2, users.Length);
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal(1, users[0].GetProperty("id").GetInt32());
        Assert.True(users[0].GetProperty("active").GetBoolean());
    }

    [Fact]
    public void Encode_TabularArray_UsesCompactFormat()
    {
        // Arrange
        var data = new 
        { 
            users = new[] 
            { 
                new { id = 1, name = "Alice", role = "admin" },
                new { id = 2, name = "Bob", role = "user" }
            }
        };

        // Act
        string result = Toon.Encode(data);

        // Assert
        Assert.Contains("users[2]{id,name,role}:", result);
        Assert.Contains("1,Alice,admin", result);
        Assert.Contains("2,Bob,user", result);
    }

    [Fact]
    public void Decode_TabularFormat_ReturnsCorrectStructure()
    {
        // Arrange
        string toonString = "users[2]{id,name,role}:\n  1,Alice,admin\n  2,Bob,user";

        // Act
        JsonElement result = Toon.Decode(toonString);

        // Assert
        var users = result.GetProperty("users").EnumerateArray().ToArray();
        Assert.Equal(2, users.Length);
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
        Assert.Equal("admin", users[0].GetProperty("role").GetString());
        Assert.Equal("Bob", users[1].GetProperty("name").GetString());
        Assert.Equal("user", users[1].GetProperty("role").GetString());
    }

    [Fact]
    public void IsValid_ValidToonFormat_ReturnsTrue()
    {
        // Arrange
        string validToon = "name: Alice\nage: 30";

        // Act
        bool isValid = Toon.IsValid(validToon);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_InvalidToonFormat_ReturnsFalse()
    {
        // Arrange
        string invalidToon = "key: \"unterminated string";

        // Act
        bool isValid = Toon.IsValid(invalidToon);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Decode_NullInput_ThrowsArgumentException()
    {
        // Empty string is spec-valid (§5: empty document decodes to {}) —
        // see ToonV3ComplianceTests.Decode_EmptyString_ReturnsEmptyObject.
        Assert.Throws<ArgumentException>(() => Toon.Decode((string)null!));
    }

    [Fact]
    public void Encode_PrimitiveArray_UsesInlineFormat()
    {
        // Arrange
        var data = new { numbers = new[] { 1, 2, 3, 4, 5 } };

        // Act
        string result = Toon.Encode(data);

        // Assert
        Assert.Contains("numbers[5]: 1,2,3,4,5", result);
    }

    [Fact]
    public void DecodeGeneric_ValidToonFormat_ReturnsTypedObject()
    {
        // Arrange
        string toonString = "name: Alice\nage: 30";

        // Act
        var result = Toon.Decode<TestPerson>(toonString);

        // Assert
        Assert.Equal("Alice", result.Name);
        Assert.Equal(30, result.Age);
    }
}

public class TestPerson
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}
using System.Text.Json;

namespace ToonFormat.Tests;

public class ToonDebuggingTests
{
    [Fact]
    public void Debug_InvalidSyntax_ShouldFail()
    {
        // Arrange
        string invalidToon = "invalid [[ syntax";

        try 
        {
            // Act
            JsonElement result = Toon.Decode(invalidToon);
            
            // Log what we got for debugging
            Console.WriteLine($"Unexpectedly decoded as: {result.GetRawText()}");
            Assert.True(false, "Should have thrown an exception");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Expected exception: {ex.Message}");
            Assert.True(true, "Correctly threw an exception");
        }
    }
}
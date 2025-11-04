using System.Text.Json;
using ToonFormat;

Console.WriteLine("=== ToonFormat .NET Example ===\n");

// Example 1: Simple object encoding
var person = new { name = "Alice", age = 30, isActive = true };
string toonString = Toon.Encode(person);
Console.WriteLine("1. Simple Object:");
Console.WriteLine($"Original: {JsonSerializer.Serialize(person)}");
Console.WriteLine($"TOON:     {toonString}");
Console.WriteLine();

// Example 2: Tabular data (most efficient for uniform arrays)
var userData = new 
{
    users = new[]
    {
        new { id = 1, name = "Alice", role = "admin", active = true },
        new { id = 2, name = "Bob", role = "user", active = false },
        new { id = 3, name = "Charlie", role = "moderator", active = true }
    }
};

string tabularToon = Toon.Encode(userData);
Console.WriteLine("2. Tabular Data (Uniform Array):");
Console.WriteLine($"JSON ({JsonSerializer.Serialize(userData).Length} chars):");
Console.WriteLine(JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"\nTOON ({tabularToon.Length} chars - {100 - (tabularToon.Length * 100 / JsonSerializer.Serialize(userData).Length)}% smaller):");
Console.WriteLine(tabularToon);
Console.WriteLine();

// Example 3: Decoding back to objects
Console.WriteLine("3. Round-trip Decoding:");
JsonElement decoded = Toon.Decode(tabularToon);
var users = decoded.GetProperty("users").EnumerateArray().ToArray();
Console.WriteLine($"Decoded {users.Length} users:");
foreach (var user in users)
{
    Console.WriteLine($"  - {user.GetProperty("name").GetString()}: {user.GetProperty("role").GetString()}");
}
Console.WriteLine();

// Example 4: Strongly-typed decoding
Console.WriteLine("4. Strongly-typed Decoding:");
var typedResult = Toon.Decode<UserData>(tabularToon);
Console.WriteLine($"Typed result has {typedResult.Users.Length} users:");
foreach (var user in typedResult.Users)
{
    Console.WriteLine($"  - {user.Name} (ID: {user.Id}, Role: {user.Role}, Active: {user.Active})");
}
Console.WriteLine();

// Example 5: Different delimiters and options
Console.WriteLine("5. Custom Options (Pipe delimiter, Length markers):");
var options = new EncodeOptions 
{ 
    Delimiter = '|', 
    LengthMarker = '#',
    Indent = 4 
};
string customToon = Toon.Encode(userData, options);
Console.WriteLine(customToon);

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public bool Active { get; set; }
}

public class UserData
{
    public User[] Users { get; set; } = Array.Empty<User>();
}

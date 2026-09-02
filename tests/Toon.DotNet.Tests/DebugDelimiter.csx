using System;
using System.Text.Json;
using ToonFormat;

var toon = "users[2|]{id,name}:\n  1|Alice\n  2|Bob";
Console.WriteLine("Input TOON:");
Console.WriteLine(toon);
Console.WriteLine();

try
{
    var result = Toon.Decode(toon);
    Console.WriteLine("Decoded successfully!");
    Console.WriteLine("Result JSON:");
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine();
    
    if (result.ValueKind == JsonValueKind.Object)
    {
        Console.WriteLine("Properties:");
        foreach (var prop in result.EnumerateObject())
        {
            Console.WriteLine($"  - {prop.Name}: {prop.Value.ValueKind}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

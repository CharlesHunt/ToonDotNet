using System.Text.Json;

namespace ToonFormat.Tests;

public class FileOperationAsyncTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string GetTempFilePath(string extension = ".toon")
    {
        var path = Path.Combine(Path.GetTempPath(), $"toon_async_test_{Guid.NewGuid()}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    private string CreateTempFile(string content, string extension = ".toon")
    {
        var path = GetTempFilePath(extension);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                try { File.Delete(file); } catch { }
        }
    }

    // =========================================================================
    // SaveAsync
    // =========================================================================

    [Fact]
    public async Task SaveAsync_SimpleObject_CreatesFile()
    {
        // Arrange
        var data = new { name = "Alice", age = 30, role = "admin" };
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(data, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.NotEmpty(File.ReadAllText(filePath));
    }

    [Fact]
    public async Task SaveAsync_NullInput_CreatesFile()
    {
        // Arrange
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync((object?)null, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveAsync_WithCustomOptions_UsesOptions()
    {
        // Arrange
        var data = new { users = new[] { new { id = 1, name = "Alice" } } };
        var filePath = GetTempFilePath();
        var options = new EncodeOptions { Delimiter = '|' };

        // Act
        await Toon.SaveAsync(data, filePath, options);

        // Assert
        Assert.Contains("|", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task SaveAsync_WithCancellationToken_Completes()
    {
        // Arrange
        var data = new { name = "Alice" };
        var filePath = GetTempFilePath();
        using var cts = new CancellationTokenSource();

        // Act
        await Toon.SaveAsync(data, filePath, cancellationToken: cts.Token);

        // Assert
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveAsync_ProducesSameContentAsSyncSave()
    {
        // Arrange
        var data = new { users = new[] { new { id = 1, name = "Alice" }, new { id = 2, name = "Bob" } } };
        var syncPath  = GetTempFilePath();
        var asyncPath = GetTempFilePath();

        // Act
        Toon.Save(data, syncPath);
        await Toon.SaveAsync(data, asyncPath);

        // Assert
        Assert.Equal(File.ReadAllText(syncPath), File.ReadAllText(asyncPath));
    }

    // =========================================================================
    // LoadAsync (JsonElement overload)
    // =========================================================================

    [Fact]
    public async Task LoadAsync_ValidFile_ReturnsDecodedElement()
    {
        // Arrange
        var filePath = CreateTempFile("name: Alice\nage: 30");

        // Act
        var result = await Toon.LoadAsync(filePath);

        // Assert
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.Equal("Alice", result.GetProperty("name").GetString());
        Assert.Equal(30, result.GetProperty("age").GetInt32());
    }

    [Fact]
    public async Task LoadAsync_WithDecodeOptions_UsesOptions()
    {
        // Arrange
        var data = new { values = new[] { 1, 2, 3 } };
        var filePath = GetTempFilePath();
        await Toon.SaveAsync(data, filePath, new EncodeOptions { Indent = 4 });

        // Act
        var result = await Toon.LoadAsync(filePath, new DecodeOptions { Indent = 4 });

        // Assert
        Assert.Equal(3, result.GetProperty("values").GetArrayLength());
    }

    [Fact]
    public async Task LoadAsync_ProducesSameResultAsSyncLoad()
    {
        // Arrange
        var data = new { id = 1, name = "Alice", active = true };
        var filePath = GetTempFilePath();
        Toon.Save(data, filePath);

        // Act
        var syncResult  = Toon.Load(filePath);
        var asyncResult = await Toon.LoadAsync(filePath);

        // Assert
        Assert.Equal(syncResult.GetRawText(), asyncResult.GetRawText());
    }

    // =========================================================================
    // LoadAsync<T> (typed overload)
    // =========================================================================

    [Fact]
    public async Task LoadAsyncTyped_ValidFile_ReturnsDeserializedObject()
    {
        // Arrange
        var original = new TestAsyncUser { Id = 1, Name = "Alice", Role = "admin" };
        var filePath = GetTempFilePath();
        Toon.Save(original, filePath);

        // Act
        var result = await Toon.LoadAsync<TestAsyncUser>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Role, result.Role);
    }

    [Fact]
    public async Task LoadAsyncTyped_WithCustomJsonOptions_UsesOptions()
    {
        // Arrange
        var data = new { userName = "Alice" };
        var filePath = GetTempFilePath();
        Toon.Save(data, filePath);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = await Toon.LoadAsync<Dictionary<string, string>>(filePath, jsonOptions: jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("userName", result.Keys);
    }

    [Fact]
    public async Task LoadAsyncTyped_ProducesSameResultAsSyncLoad()
    {
        // Arrange
        var original = new TestAsyncUser { Id = 42, Name = "Bob", Role = "user" };
        var filePath = GetTempFilePath();
        Toon.Save(original, filePath);

        // Act
        var syncResult  = Toon.Load<TestAsyncUser>(filePath);
        var asyncResult = await Toon.LoadAsync<TestAsyncUser>(filePath);

        // Assert
        Assert.Equal(syncResult.Id,   asyncResult.Id);
        Assert.Equal(syncResult.Name, asyncResult.Name);
        Assert.Equal(syncResult.Role, asyncResult.Role);
    }

    // =========================================================================
    // FromJsonFileAsync
    // =========================================================================

    [Fact]
    public async Task FromJsonFileAsync_ValidFile_ReturnsToonString()
    {
        // Arrange
        var json = """{"users":[{"id":1,"name":"Alice"},{"id":2,"name":"Bob"}]}""";
        var filePath = CreateTempFile(json, ".json");

        // Act
        var result = await Toon.FromJsonFileAsync(filePath);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(Toon.IsValid(result));
    }

    [Fact]
    public async Task FromJsonFileAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Toon.FromJsonFileAsync(""));
    }

    [Fact]
    public async Task FromJsonFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            Toon.FromJsonFileAsync("does_not_exist.json"));
    }

    [Fact]
    public async Task FromJsonFileAsync_ProducesSameResultAsSyncMethod()
    {
        // Arrange
        var json = """{"name":"Alice","age":30}""";
        var filePath = CreateTempFile(json, ".json");

        // Act
        var syncResult  = Toon.FromJsonFile(filePath);
        var asyncResult = await Toon.FromJsonFileAsync(filePath);

        // Assert
        Assert.Equal(syncResult, asyncResult);
    }

    // =========================================================================
    // ToJsonFileAsync
    // =========================================================================

    [Fact]
    public async Task ToJsonFileAsync_ValidFile_ReturnsJsonString()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var filePath = CreateTempFile(toon);

        // Act
        var result = await Toon.ToJsonFileAsync(filePath);

        // Assert
        Assert.NotEmpty(result);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("Alice", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ToJsonFileAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Toon.ToJsonFileAsync(""));
    }

    [Fact]
    public async Task ToJsonFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            Toon.ToJsonFileAsync("missing.toon"));
    }

    [Fact]
    public async Task ToJsonFileAsync_ProducesSameResultAsSyncMethod()
    {
        // Arrange
        var toon = "users[2]{id,name}:\n  1,Alice\n  2,Bob";
        var filePath = CreateTempFile(toon);

        // Act
        var syncResult  = Toon.ToJsonFile(filePath);
        var asyncResult = await Toon.ToJsonFileAsync(filePath);

        // Assert
        Assert.Equal(syncResult, asyncResult);
    }

    // =========================================================================
    // SaveAsJsonAsync
    // =========================================================================

    [Fact]
    public async Task SaveAsJsonAsync_ValidInput_CreatesJsonFile()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var filePath = GetTempFilePath(".json");

        // Act
        await Toon.SaveAsJsonAsync(toon, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Alice", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task SaveAsJsonAsync_EmptyToonString_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Toon.SaveAsJsonAsync("", GetTempFilePath(".json")));
    }

    [Fact]
    public async Task SaveAsJsonAsync_EmptyFilePath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Toon.SaveAsJsonAsync("name: Alice", ""));
    }

    [Fact]
    public async Task SaveAsJsonAsync_ProducesSameContentAsSyncMethod()
    {
        // Arrange
        var toon = "users[2]{id,name}:\n  1,Alice\n  2,Bob";
        var syncPath  = GetTempFilePath(".json");
        var asyncPath = GetTempFilePath(".json");

        // Act
        Toon.SaveAsJson(toon, syncPath);
        await Toon.SaveAsJsonAsync(toon, asyncPath);

        // Assert
        Assert.Equal(File.ReadAllText(syncPath), File.ReadAllText(asyncPath));
    }

    [Fact]
    public async Task SaveAsJsonAsync_WithCustomJsonOptions_UsesOptions()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";
        var filePath = GetTempFilePath(".json");
        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        // Act
        await Toon.SaveAsJsonAsync(toon, filePath, jsonOptions: jsonOptions);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.DoesNotContain("\n", content);
    }

    // =========================================================================
    // Round-trip
    // =========================================================================

    [Fact]
    public async Task RoundTrip_SaveAsyncLoadAsyncTyped_PreservesData()
    {
        // Arrange
        var original = new TestAsyncUser { Id = 7, Name = "Charlie", Role = "moderator" };
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(original, filePath);
        var result = await Toon.LoadAsync<TestAsyncUser>(filePath);

        // Assert
        Assert.Equal(original.Id,   result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Role, result.Role);
    }

    [Fact]
    public async Task RoundTrip_SaveAsyncLoadAsync_PreservesData()
    {
        // Arrange
        var original = new { values = new[] { 10, 20, 30 }, label = "test" };
        var filePath = GetTempFilePath();

        // Act
        await Toon.SaveAsync(original, filePath);
        var result = await Toon.LoadAsync(filePath);

        // Assert
        Assert.Equal("test", result.GetProperty("label").GetString());
        Assert.Equal(3, result.GetProperty("values").GetArrayLength());
    }

    [Fact]
    public async Task RoundTrip_JsonAsync_PreservesData()
    {
        // Arrange
        var json = """{"users":[{"id":1,"name":"Alice"},{"id":2,"name":"Bob"}]}""";
        var jsonIn  = CreateTempFile(json, ".json");
        var toonPath = GetTempFilePath(".toon");
        var jsonOut  = GetTempFilePath(".json");

        // Act — JSON ? TOON file ? JSON file
        var toon = await Toon.FromJsonFileAsync(jsonIn);
        await Toon.SaveAsJsonAsync(toon, jsonOut);

        // Assert
        var result = File.ReadAllText(jsonOut);
        using var doc = JsonDocument.Parse(result);
        var users = doc.RootElement.GetProperty("users").EnumerateArray().ToArray();
        Assert.Equal(2, users.Length);
        Assert.Equal("Alice", users[0].GetProperty("name").GetString());
    }
}

// ---------------------------------------------------------------------------
// Test fixture model
// ---------------------------------------------------------------------------
file class TestAsyncUser
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

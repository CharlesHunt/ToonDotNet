public class UserData
{
    public User[] Users { get; set; } = Array.Empty<User>();
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public bool Active { get; set; }
}
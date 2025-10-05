namespace Echo.Services.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
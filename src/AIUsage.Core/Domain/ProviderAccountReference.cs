namespace AIUsage.Core.Domain;

public sealed record ProviderAccountReference
{
    private ProviderAccountReference(
        ProviderAccountId? id,
        string? email,
        string? name,
        string? login,
        string? plan)
    {
        Id = id;
        Email = email;
        Name = name;
        Login = login;
        Plan = plan;
    }

    public ProviderAccountId? Id { get; }

    public string? Email { get; }

    public string? Name { get; }

    public string? Login { get; }

    public string? Plan { get; }

    public static ProviderAccountReference Create(
        ProviderAccountId? id = null,
        string? email = null,
        string? name = null,
        string? login = null,
        string? plan = null)
    {
        if (id is null && email is null && name is null && login is null && plan is null)
        {
            throw new ArgumentException("A provider account reference requires at least one account fact.");
        }

        return new ProviderAccountReference(id, email, name, login, plan);
    }
}

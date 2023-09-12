namespace AnonymousPhotoBin
{
    public record AdminPassword(string Password) : IAdminPasswordProvider
    {
        bool IAdminPasswordProvider.IsValid(string pw) => pw == Password;
    }
}

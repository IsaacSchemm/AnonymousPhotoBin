namespace AnonymousPhotoBin
{
    public interface IAdminPasswordProvider
    {
        bool IsValid(string password);
    }
}

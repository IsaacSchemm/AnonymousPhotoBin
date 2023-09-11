namespace AnonymousPhotoBin {
    // This password gives users access to list the IDs of uploaded files.
    // Operations on files only need the ID, no other credentials.
    public record FileManagementCredentials(string Password);
}

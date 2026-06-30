using System.DirectoryServices.AccountManagement;

namespace DentalCollegeManagementSystem_AAU.Services
{
    public sealed class ActiveDirectoryUserInfo
    {
        public string UserName { get; init; } = string.Empty;
        public string GivenName { get; init; } = string.Empty;
        public string Surname { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }

    public sealed class ActiveDirectoryUnavailableException : Exception
    {
        public ActiveDirectoryUnavailableException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class ActiveDirectoryValidator
    {
        private readonly string _domain;

        public ActiveDirectoryValidator(IConfiguration configuration)
        {
            _domain = configuration["ActiveDirectory:Domain"]
                ?? throw new InvalidOperationException(
                    "ActiveDirectory:Domain is missing from appsettings.json.");
        }

        public ActiveDirectoryUserInfo? Authenticate(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(password))
                return null;

            string normalizedUserName = NormalizeUserName(userName);

            try
            {
                using var context = new PrincipalContext(ContextType.Domain, _domain);

                bool valid = context.ValidateCredentials(
                    normalizedUserName,
                    password,
                    ContextOptions.Negotiate);

                if (!valid)
                    return null;

                using var user = UserPrincipal.FindByIdentity(
                    context,
                    IdentityType.SamAccountName,
                    normalizedUserName);

                if (user == null || user.Enabled == false)
                    return null;

                return new ActiveDirectoryUserInfo
                {
                    UserName = user.SamAccountName ?? normalizedUserName,
                    GivenName = user.GivenName ?? string.Empty,
                    Surname = user.Surname ?? string.Empty,
                    DisplayName = user.DisplayName ?? normalizedUserName,
                    Email = user.EmailAddress ?? string.Empty
                };
            }
            catch (PrincipalServerDownException ex)
            {
                throw new ActiveDirectoryUnavailableException(
                    "The Active Directory server cannot be reached.", ex);
            }
            catch (PrincipalOperationException ex)
            {
                throw new ActiveDirectoryUnavailableException(
                    "Active Directory could not complete authentication.", ex);
            }
        }

        private static string NormalizeUserName(string userName)
        {
            string value = userName.Trim();

            int slashIndex = value.LastIndexOf('\\');
            if (slashIndex >= 0 && slashIndex < value.Length - 1)
                value = value[(slashIndex + 1)..];

            int atIndex = value.IndexOf('@');
            if (atIndex > 0)
                value = value[..atIndex];

            return value;
        }
    }
}

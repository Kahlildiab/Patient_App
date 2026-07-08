using Microsoft.AspNetCore.Authentication;
using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

public class ActiveDirectoryValidator
{
    private string _ldapPath;
    private string _filterAttribute;

    public ActiveDirectoryValidator(string ldapPath)
    {
        _ldapPath = ldapPath;
    }

    public bool IsAuthenticated(string domainName, string userName, string password)
    {
        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName))
            {
                bool isValid = context.ValidateCredentials(userName, password);
                if (!isValid) return false;

                // Optionally, you can get the user details
                //using (UserPrincipal user = UserPrincipal.FindByIdentity(context, userName))
                //{
                //    if (user == null)
                //        return false;

                //    _filterAttribute = user.DisplayName; // or user.Name, user.GivenName, etc.
                //    _ldapPath = user.DistinguishedName;
                //}
                return true;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Login Error: " + ex.Message);
        }
    }

    public string GetGivenName(string domainName, string userName, string password)
    {
        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName, userName, password))
            using (UserPrincipal user = UserPrincipal.FindByIdentity(context, userName))
            {
                return user?.GivenName ?? "No given name found";
            }
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string GetLastName(string domainName, string userName, string password)
    {
        try
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domainName, userName, password))
            using (UserPrincipal user = UserPrincipal.FindByIdentity(context, userName))
            {
                return user?.Surname ?? "No surname found";
            }
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string FilterAttribute => _filterAttribute;
    public string LdapPath => _ldapPath;
}



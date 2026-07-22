using System;
using System.DirectoryServices.AccountManagement;

namespace DentalCollegeManagementSystem_AAU.Services
{
    public class ActiveDirectoryValidator
    {
        private readonly string _ldapPath;
        private string? _filterAttribute;

        public ActiveDirectoryValidator(string ldapPath)
        {
            if (string.IsNullOrWhiteSpace(ldapPath))
            {
                throw new ArgumentException(
                    "Active Directory server/path cannot be empty.",
                    nameof(ldapPath));
            }

            _ldapPath = ldapPath;
        }

        // =====================================================
        // AUTHENTICATE USER
        // =====================================================

        public bool IsAuthenticated(
            string domainName,
            string userName,
            string password)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException(
                    "Active Directory domain name is empty.",
                    nameof(domainName));
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException(
                    "Active Directory username is empty.",
                    nameof(userName));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            string normalizedUserName =
                NormalizeUserName(userName);

            try
            {
                /*
                 * نفس المشروع الذي يعمل لديك:
                 * الاتصال باستخدام اسم الدومين.
                 */
                using (PrincipalContext context =
                       new PrincipalContext(
                           ContextType.Domain,
                           domainName))
                {
                    return context.ValidateCredentials(
                        normalizedUserName,
                        password);
                }
            }
            catch (PrincipalServerDownException)
            {
                /*
                 * مهم:
                 * لا نخفي نوع الخطأ الأصلي.
                 * Controller سيعرض الخطأ الحقيقي.
                 */
                throw;
            }
            catch (PrincipalOperationException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // =====================================================
        // GET GIVEN NAME
        // =====================================================

        public string GetGivenName(
            string domainName,
            string userName,
            string password)
        {
            string normalizedUserName =
                NormalizeUserName(userName);

            using (PrincipalContext context =
                   new PrincipalContext(
                       ContextType.Domain,
                       domainName,
                       normalizedUserName,
                       password))
            using (UserPrincipal? user =
                   UserPrincipal.FindByIdentity(
                       context,
                       IdentityType.SamAccountName,
                       normalizedUserName))
            {
                if (user == null)
                {
                    return string.Empty;
                }

                _filterAttribute =
                    user.DisplayName;

                return user.GivenName
                    ?? string.Empty;
            }
        }

        // =====================================================
        // GET LAST NAME
        // =====================================================

        public string GetLastName(
            string domainName,
            string userName,
            string password)
        {
            string normalizedUserName =
                NormalizeUserName(userName);

            using (PrincipalContext context =
                   new PrincipalContext(
                       ContextType.Domain,
                       domainName,
                       normalizedUserName,
                       password))
            using (UserPrincipal? user =
                   UserPrincipal.FindByIdentity(
                       context,
                       IdentityType.SamAccountName,
                       normalizedUserName))
            {
                if (user == null)
                {
                    return string.Empty;
                }

                return user.Surname
                    ?? string.Empty;
            }
        }

        // =====================================================
        // NORMALIZE USERNAME
        // =====================================================

        private static string NormalizeUserName(
            string userName)
        {
            string result =
                userName?.Trim() ?? string.Empty;

            /*
             * AMMAN\2141
             * becomes:
             * 2141
             */
            int slashIndex =
                result.LastIndexOf('\\');

            if (slashIndex >= 0 &&
                slashIndex < result.Length - 1)
            {
                result =
                    result.Substring(
                        slashIndex + 1);
            }

            /*
             * 2141@AMMAN.LOCAL
             * becomes:
             * 2141
             */
            int atIndex =
                result.IndexOf('@');

            if (atIndex > 0)
            {
                result =
                    result.Substring(
                        0,
                        atIndex);
            }

            return result.Trim();
        }

        // =====================================================
        // PROPERTIES
        // =====================================================

        public string FilterAttribute =>
            _filterAttribute ?? string.Empty;

        public string LdapPath =>
            _ldapPath;
    }
}
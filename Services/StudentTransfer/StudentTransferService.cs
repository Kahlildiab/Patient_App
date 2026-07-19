using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using DentalCollegeManagementSystem_AAU.Models.StudentTransfer;
using DentalCollegeManagementSystem_AAU.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace DentalCollegeManagementSystem_AAU.Services.StudentTransfer;

public sealed class StudentTransferService : IStudentTransferService
{
    private const string UsersTable =
        "[PatientDB].[dbo].[Users]";

    private static readonly Regex OracleIdentifierRegex =
        new(
            @"^[A-Za-z][A-Za-z0-9_$#]*(\.[A-Za-z][A-Za-z0-9_$#]*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly StudentTransferOptions _options;
    private readonly string _oracleConnectionString;
    private readonly string _sqlConnectionString;
    private readonly ILogger<StudentTransferService> _logger;

    public StudentTransferService(
        IConfiguration configuration,
        IOptions<StudentTransferOptions> options,
        ILogger<StudentTransferService> logger)
    {
        _options = options.Value;
        _logger = logger;

        ValidateOptions(_options);

        _oracleConnectionString =
            configuration.GetConnectionString(
                _options.OracleConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{_options.OracleConnectionStringName}' was not found.");

        _sqlConnectionString =
            configuration.GetConnectionString(
                _options.SqlConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{_options.SqlConnectionStringName}' was not found.");
    }

    public async Task<StudentTransferPageViewModel> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);

        if (pageSize <= 0)
        {
            pageSize = _options.DefaultPageSize;
        }

        pageSize = Math.Min(
            pageSize,
            _options.MaximumPageSize);

        List<OracleStudentDto> allStudents =
            await LoadOracleStudentsAsync(
                cancellationToken);

        HashSet<string> existingUsernames =
            await LoadExistingUsernamesAsync(
                cancellationToken);

        foreach (OracleStudentDto student in allStudents)
        {
            student.IsTransferred =
                existingUsernames.Contains(
                    student.Username);
        }

        int oracleTotalCount =
            allStudents.Count;

        int existingCount =
            allStudents.Count(
                student => student.IsTransferred);

        int newCount =
            oracleTotalCount - existingCount;

        IEnumerable<OracleStudentDto> filtered =
            allStudents;

        string normalizedSearch =
            (search ?? string.Empty).Trim();

        if (!string.IsNullOrWhiteSpace(
                normalizedSearch))
        {
            filtered = filtered.Where(
                student =>
                    ContainsIgnoreCase(
                        student.Username,
                        normalizedSearch)
                    ||
                    ContainsIgnoreCase(
                        student.FullName,
                        normalizedSearch)
                    ||
                    ContainsIgnoreCase(
                        student.Email,
                        normalizedSearch));
        }

        List<OracleStudentDto> filteredList =
            filtered.ToList();

        int totalItems =
            filteredList.Count;

        int totalPages =
            totalItems == 0
                ? 1
                : (int)Math.Ceiling(
                    totalItems /
                    (double)pageSize);

        if (page > totalPages)
        {
            page = totalPages;
        }

        List<OracleStudentDto> students =
            filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

        return new StudentTransferPageViewModel
        {
            Students = students,
            Search = normalizedSearch,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            OracleTotalCount = oracleTotalCount,
            ExistingCount = existingCount,
            NewCount = newCount
        };
    }

    public async Task<TransferOperationResult> TransferAsync(
        CancellationToken cancellationToken = default)
    {
        List<OracleStudentDto> students =
            await LoadOracleStudentsAsync(
                cancellationToken);

        TransferOperationResult result =
            new()
            {
                SourceCount = students.Count
            };

        if (students.Count == 0)
        {
            return result;
        }

        const string sql = """
            SET NOCOUNT ON;

            DECLARE @ExistingUserId INT;

            SELECT TOP (1)
                @ExistingUserId = [UserID]
            FROM [PatientDB].[dbo].[Users] WITH
            (
                UPDLOCK,
                HOLDLOCK
            )
            WHERE LTRIM(RTRIM([Username])) =
                  @Username
            ORDER BY [UserID];

            IF @ExistingUserId IS NULL
            BEGIN
                INSERT INTO [PatientDB].[dbo].[Users]
                (
                    [Username],
                    [Email],
                    [FullName],
                    [UserRole],
                    [IsActive],
                    [CreatedDate],
                    [LastLoginDate],
                    [ModifiedDate],
                    [Password],
                    [PhoneNumber]
                )
                VALUES
                (
                    @Username,
                    @Email,
                    @FullName,
                    N'Student',
                    1,
                    SYSDATETIME(),
                    NULL,
                    SYSDATETIME(),
                    @Password,
                    NULL
                );

                SELECT CAST(1 AS INT);
            END
            ELSE
            BEGIN
                UPDATE [PatientDB].[dbo].[Users]
                SET
                    [Email] = @Email,
                    [FullName] = @FullName,
                    [UserRole] = N'Student',
                    [IsActive] = 1,
                    [ModifiedDate] = SYSDATETIME(),
                    [PhoneNumber] = NULL,
                    [Password] =
                        CASE
                            WHEN @ResetExistingPassword = 1
                                THEN @Password
                            ELSE [Password]
                        END
                WHERE [UserID] =
                      @ExistingUserId;

                SELECT CAST(0 AS INT);
            END;
            """;

        await using SqlConnection connection =
            new(_sqlConnectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            foreach (OracleStudentDto student in students)
            {
                await using SqlCommand command =
                    new(
                        sql,
                        connection,
                        transaction)
                    {
                        CommandTimeout = 180
                    };

                command.Parameters.Add(
                    "@Username",
                    SqlDbType.NVarChar,
                    100)
                    .Value = student.Username;

                command.Parameters.Add(
                    "@Email",
                    SqlDbType.NVarChar,
                    256)
                    .Value = student.Email;

                command.Parameters.Add(
                    "@FullName",
                    SqlDbType.NVarChar,
                    500)
                    .Value = student.FullName;

                command.Parameters.Add(
                    "@Password",
                    SqlDbType.NVarChar,
                    500)
                    .Value = _options.DefaultPassword;

                command.Parameters.Add(
                    "@ResetExistingPassword",
                    SqlDbType.Bit)
                    .Value = _options.ResetExistingPasswords;

                object? scalar =
                    await command.ExecuteScalarAsync(
                        cancellationToken);

                int inserted =
                    Convert.ToInt32(
                        scalar,
                        CultureInfo.InvariantCulture);

                if (inserted == 1)
                {
                    result.InsertedCount++;
                }
                else
                {
                    result.UpdatedCount++;
                }
            }

            await transaction.CommitAsync(
                cancellationToken);

            return result;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _logger.LogError(
                exception,
                "An error occurred while transferring students from Oracle to SQL Server.");

            throw;
        }
    }

    private async Task<List<OracleStudentDto>>
        LoadOracleStudentsAsync(
            CancellationToken cancellationToken)
    {
        string oracleQuery = $"""
            SELECT DISTINCT
                TO_CHAR({_options.StudentNumberColumn})
                    AS STUDENT_NUMBER,

                {_options.StudentNameColumn}
                    AS STUDENT_NAME,

                {_options.LevelColumn}
                    AS LEVEL_DESCRIPTION

            FROM {_options.OracleViewName}

            WHERE TRIM({_options.LevelColumn}) =
                  :LevelDescription

            ORDER BY
                STUDENT_NUMBER
            """;

        Dictionary<string, OracleStudentDto> students =
            new(
                StringComparer.OrdinalIgnoreCase);

        await using OracleConnection connection =
            new(_oracleConnectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using OracleCommand command =
            connection.CreateCommand();

        command.BindByName = true;
        command.CommandText = oracleQuery;
        command.CommandTimeout = 180;

        command.Parameters.Add(
            "LevelDescription",
            OracleDbType.NVarchar2)
            .Value = _options.LevelValue;

        await using OracleDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        int studentNumberOrdinal =
            reader.GetOrdinal(
                "STUDENT_NUMBER");

        int studentNameOrdinal =
            reader.GetOrdinal(
                "STUDENT_NAME");

        int levelOrdinal =
            reader.GetOrdinal(
                "LEVEL_DESCRIPTION");

        while (await reader.ReadAsync(
            cancellationToken))
        {
            string username =
                ReadString(
                    reader,
                    studentNumberOrdinal);

            string fullName =
                ReadString(
                    reader,
                    studentNameOrdinal);

            string levelDescription =
                ReadString(
                    reader,
                    levelOrdinal);

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            username =
                username.Trim();

            if (students.ContainsKey(username))
            {
                continue;
            }

            students.Add(
                username,
                new OracleStudentDto
                {
                    Username = username,
                    Email =
                        username +
                        NormalizeEmailDomain(
                            _options.EmailDomain),
                    FullName = fullName.Trim(),
                    LevelDescription =
                        levelDescription.Trim()
                });
        }

        return students.Values
            .OrderBy(
                student => student.Username,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<HashSet<string>>
        LoadExistingUsernamesAsync(
            CancellationToken cancellationToken)
    {
        string sql = $"""
            SELECT
                LTRIM(RTRIM([Username]))
                    AS [Username]

            FROM {UsersTable}

            WHERE [Username] IS NOT NULL
              AND LTRIM(RTRIM([Username])) <> N'';
            """;

        HashSet<string> usernames =
            new(
                StringComparer.OrdinalIgnoreCase);

        await using SqlConnection connection =
            new(_sqlConnectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using SqlCommand command =
            new(
                sql,
                connection)
            {
                CommandTimeout = 180
            };

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken);

        int usernameOrdinal =
            reader.GetOrdinal(
                "Username");

        while (await reader.ReadAsync(
            cancellationToken))
        {
            string username =
                ReadString(
                    reader,
                    usernameOrdinal);

            if (!string.IsNullOrWhiteSpace(username))
            {
                usernames.Add(
                    username.Trim());
            }
        }

        return usernames;
    }

    private static string ReadString(
        IDataRecord record,
        int ordinal)
    {
        if (record.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        return Convert.ToString(
                   record.GetValue(ordinal),
                   CultureInfo.InvariantCulture)
               ?.Trim()
               ?? string.Empty;
    }

    private static bool ContainsIgnoreCase(
        string source,
        string search)
    {
        return source.Contains(
            search,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEmailDomain(
        string emailDomain)
    {
        string domain =
            (emailDomain ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new InvalidOperationException(
                "StudentTransfer:EmailDomain cannot be empty.");
        }

        return domain.StartsWith(
            '@')
            ? domain
            : "@" + domain;
    }

    private static void ValidateOptions(
        StudentTransferOptions options)
    {
        ValidateOracleIdentifier(
            options.OracleViewName,
            nameof(options.OracleViewName));

        ValidateOracleIdentifier(
            options.StudentNumberColumn,
            nameof(options.StudentNumberColumn));

        ValidateOracleIdentifier(
            options.StudentNameColumn,
            nameof(options.StudentNameColumn));

        ValidateOracleIdentifier(
            options.LevelColumn,
            nameof(options.LevelColumn));

        if (string.IsNullOrWhiteSpace(
                options.LevelValue))
        {
            throw new InvalidOperationException(
                "StudentTransfer:LevelValue cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(
                options.DefaultPassword))
        {
            throw new InvalidOperationException(
                "StudentTransfer:DefaultPassword cannot be empty.");
        }

        if (options.DefaultPageSize <= 0)
        {
            throw new InvalidOperationException(
                "StudentTransfer:DefaultPageSize must be greater than zero.");
        }

        if (options.MaximumPageSize <
            options.DefaultPageSize)
        {
            throw new InvalidOperationException(
                "StudentTransfer:MaximumPageSize must be greater than or equal to DefaultPageSize.");
        }
    }

    private static void ValidateOracleIdentifier(
        string identifier,
        string optionName)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            !OracleIdentifierRegex.IsMatch(identifier))
        {
            throw new InvalidOperationException(
                $"StudentTransfer:{optionName} contains an invalid Oracle identifier.");
        }
    }
}

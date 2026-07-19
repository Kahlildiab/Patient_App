namespace DentalCollegeManagementSystem_AAU.Options;

public sealed class StudentTransferOptions
{
    public const string SectionName = "StudentTransfer";

    public string OracleConnectionStringName { get; set; } =
        "OracleConnection";

    public string SqlConnectionStringName { get; set; } =
        "PatientDb";

    public string OracleViewName { get; set; } =
        "Doctor";

    /*
        غيّر هذا الاسم حسب اسم عمود رقم الطالب الحقيقي داخل View Doctor.
        أمثلة محتملة:
        STUDENT_NO
        STUDENT_ID
        STUD_NO
    */
    public string StudentNumberColumn { get; set; } =
        "STUDENT_NO";

    public string StudentNameColumn { get; set; } =
        "NAME";

    public string LevelColumn { get; set; } =
        "LEVEL_DESC";

    public string LevelValue { get; set; } =
        "السنة الثالثة";

    public string EmailDomain { get; set; } =
        "@ammanu.edu.jo";

    public string DefaultPassword { get; set; } =
        "1";

    /*
        false: لا يتم تغيير كلمة مرور الطالب الموجود مسبقاً.
        true: يتم إعادة كلمة مرور جميع الطلاب الموجودين إلى DefaultPassword.
    */
    public bool ResetExistingPasswords { get; set; } =
        false;

    public int DefaultPageSize { get; set; } =
        20;

    public int MaximumPageSize { get; set; } =
        100;
}

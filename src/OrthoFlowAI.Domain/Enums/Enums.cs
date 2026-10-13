namespace OrthoFlowAI.Domain.Enums;

public enum UserRole
{
    SuperAdmin,
    ClinicOwner,
    Doctor,
    Receptionist,
    Assistant,
    Patient
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum PatientStatus
{
    Active,
    Inactive,
    Completed,
    OnHold,
    Cancelled
}

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    CheckedIn,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled
}

public enum AppointmentType
{
    Consultation,
    Adjustment,
    Emergency,
    FollowUp,
    Cleaning,
    Extraction,
    Bonding,
    Debonding,
    XRay,
    Photo,
    Other
}

public enum PaymentStatus
{
    Pending,
    Partial,
    Paid,
    Overdue,
    Refunded,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    BankTransfer,
    Insurance,
    Online,
    Other
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Partial,
    Overdue,
    Cancelled
}

public enum SubscriptionPlan
{
    Starter,
    Professional,
    Enterprise
}

public enum SubscriptionStatus
{
    Active,
    Trial,
    PastDue,
    Expired,
    Cancelled,
    Suspended,
    Pending
}

public enum BillingPeriod
{
    Monthly,
    Yearly
}

public enum NotificationType
{
    AppointmentReminder,
    AppointmentConfirmed,
    AppointmentCancelled,
    AppointmentRescheduled,
    PaymentDue,
    PaymentReceived,
    TreatmentUpdate,
    MonthlyReport,
    Emergency,
    General,
    Welcome,
    Birthday,
    Holiday
}

public enum NotificationChannel
{
    InApp,
    Email,
    WhatsApp,
    SMS,
    Push
}

public enum DocumentType
{
    ConsentForm,
    TreatmentAgreement,
    MedicalHistory,
    Insurance,
    XRay,
    Scan,
    Photo,
    Video,
    Report,
    Invoice,
    Other
}

public enum PhotoCategory
{
    Initial,
    Month1,
    Month2,
    Month3,
    Month4,
    Month5,
    Month6,
    Month7,
    Month8,
    Month9,
    Month10,
    Month11,
    Month12,
    PreTreatment,
    PostTreatment,
    Progress,
    XRay,
    Scan,
    Other
}

public enum TreatmentType
{
    Traditional,
    Ceramic,
    Lingual,
    Invisalign,
    Damon,
    SelfLigating,
    Combined
}

public enum EmergencySeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum StaffStatus
{
    Active,
    Inactive,
    OnLeave,
    Terminated
}

public enum PainLevel
{
    None = 0,
    Minimal = 1,
    Mild = 2,
    Moderate = 3,
    Severe = 4,
    VerySevere = 5,
    Extreme = 6,
    VeryExtreme = 7,
    Unbearable = 8,
    Worst = 9,
    WorstPossible = 10
}

public enum InventoryCategory
{
    Brackets,
    Wires,
    Elastics,
    Adhesive,
    RubberBands,
    Tools,
    Consumables,
    Other
}

public enum FeedbackType
{
    Rating,
    Survey,
    NPS,
    Comment,
    Complaint,
    Suggestion
}

public enum Language
{
    Arabic,
    English
}

public enum ReferralStatus
{
    Pending,
    Completed,
    Expired,
    Cancelled
}

public enum FeedbackSentiment
{
    Positive,
    Neutral,
    Negative
}

public enum DocumentStatus
{
    Draft,
    PendingSignature,
    Signed,
    Expired,
    Revoked,
    Rejected
}

namespace Clinic.Contracts.Enums;

public enum SharedAppointmentStatus
{
    Scheduled,
    Waiting,
    InConsultation,
    Completed,
    Cancelled
}

public enum SharedBillingStatus
{
    Draft,
    Pending,
    Paid,
    Overdue,
    Cancelled
}

public enum SharedUserRole
{
    Admin,
    Doctor,
    Assistant,
    Patient
}

public enum SharedToothStatus
{
    Healthy,
    Decayed,
    Missing,
    Filled,
    Crowned,
    Implant,
    Bridge,
    RootCanal
}

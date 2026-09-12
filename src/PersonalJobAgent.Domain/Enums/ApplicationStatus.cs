namespace PersonalJobAgent.Domain.Enums;

public enum ApplicationStatus
{
    Unknown = 0,
    Discovered = 1,
    Shortlisted = 2,
    ReadyToApply = 3,
    Applied = 4,
    Assessment = 5,
    RecruiterContacted = 6,
    Interview = 7,
    Offer = 8,
    Rejected = 9,
    Withdrawn = 10,
    Closed = 11
}

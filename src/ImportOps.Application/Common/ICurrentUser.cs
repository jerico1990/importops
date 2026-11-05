namespace ImportOps.Application.Common;

public interface ICurrentUser
{
    long UserId { get; }
    string UserType { get; }
    long? ClientId { get; }
    bool IsClientUser { get; }
}

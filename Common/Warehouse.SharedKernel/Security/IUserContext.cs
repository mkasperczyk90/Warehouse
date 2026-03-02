namespace Warehouse.SharedKernel.Security;

public interface IUserContext
{
    string Username { get; }
    bool IsAuthenticated { get; }
}

namespace Warehouse.SharedKernel.Exceptions;

public class DomainException(string message) : Exception(message)
{
	public virtual string Code => "domain_error";
}

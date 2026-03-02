namespace Warehouse.SharedKernel.Exceptions;

public class BusinessLogicException(string message) : Exception(message)
{
	public virtual string Code => "business_logic_error";
}

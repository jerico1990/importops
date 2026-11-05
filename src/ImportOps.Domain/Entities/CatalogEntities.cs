namespace ImportOps.Domain.Entities;

public sealed class Currency
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public byte DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Country
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string? Iso3Code { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Port
{
    public long PortId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? PortCode { get; set; }
    public string PortName { get; set; } = string.Empty;
    public string? CityName { get; set; }
    public string PortType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TransportMode
{
    public int TransportModeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class Incoterm
{
    public string IncotermCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PaymentMethod
{
    public int PaymentMethodId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class DocumentType
{
    public int DocumentTypeId { get; set; }
    public long? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsRequiredDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ExpenseCategory
{
    public int ExpenseCategoryId { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRebillableDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ServiceCategory
{
    public int ServiceCategoryId { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WorkflowStage
{
    public int WorkflowStageId { get; set; }
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsFinal { get; set; }
    public bool IsActive { get; set; }
}

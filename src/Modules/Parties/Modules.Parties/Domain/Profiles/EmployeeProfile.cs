using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.Profiles;

/// <summary>
/// Faceta empleado — mínimo M1 (SPEC §6.5). Solo lo necesario para que un empleado funcione en M1
/// (aparecer como vendedor/cobrador). Contrato, salario, seguridad social → módulo Payroll (M2).
///
/// PR-A: entidad independiente con <c>PartyId</c> Guid. Activación idempotente.
/// </summary>
public sealed class EmployeeProfile : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public string? EmployeeCode { get; private set; }
    public string? JobTitle { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? ManagerPartyId { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public bool IsSalesperson { get; private set; }
    public bool IsCollector { get; private set; }
    public bool IsActive { get; private set; }

    private EmployeeProfile() { }

    public static EmployeeProfile Create(
        Guid partyId,
        string? employeeCode = null,
        string? jobTitle = null,
        Guid? branchId = null,
        Guid? costCenterId = null,
        Guid? managerPartyId = null,
        DateOnly? hireDate = null,
        bool isSalesperson = false,
        bool isCollector = false)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        return new EmployeeProfile
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            EmployeeCode = employeeCode?.Trim(),
            JobTitle = jobTitle?.Trim(),
            BranchId = branchId,
            CostCenterId = costCenterId,
            ManagerPartyId = managerPartyId,
            HireDate = hireDate,
            IsSalesperson = isSalesperson,
            IsCollector = isCollector,
            IsActive = true,
        };
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
    }
}

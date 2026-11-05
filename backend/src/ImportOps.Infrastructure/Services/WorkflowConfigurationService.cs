using ImportOps.Application.Common;
using ImportOps.Application.Workflow;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class WorkflowConfigurationService(
    ImportOpsDbContext db,
    ICurrentCompany currentCompany) : IWorkflowConfigurationService
{
    public async Task<PagedResult<WorkflowTaskTemplateDto>> SearchTaskTemplatesAsync(
        WorkflowTaskTemplateQuery query,
        CancellationToken ct)
    {
        var (page, pageSize) = ValidatePage(query.Page, query.PageSize);
        var companyId = currentCompany.CompanyId;

        var rows =
            from t in db.StageTaskTemplates.AsNoTracking()
            join s in db.WorkflowStages.AsNoTracking() on t.WorkflowStageId equals s.WorkflowStageId
            where t.CompanyId == companyId && s.CompanyId == companyId
            select new { Template = t, Stage = s };

        if (query.WorkflowStageId.HasValue)
            rows = rows.Where(x => x.Template.WorkflowStageId == query.WorkflowStageId.Value);

        var status = (query.Status ?? "ALL").Trim().ToUpperInvariant();
        rows = status switch
        {
            "ACTIVE" => rows.Where(x => x.Template.IsActive),
            "INACTIVE" => rows.Where(x => !x.Template.IsActive),
            "ALL" => rows,
            _ => throw Validation("El estado debe ser ALL, ACTIVE o INACTIVE.")
        };

        var search = (query.Search ?? string.Empty).Trim();
        if (search.Length > 0)
            rows = rows.Where(x => x.Template.Title.Contains(search) ||
                                   (x.Template.Description != null && x.Template.Description.Contains(search)));

        var total = await rows.CountAsync(ct);

        var pageRows = await rows
            .OrderBy(x => x.Stage.DisplayOrder)
            .ThenBy(x => x.Template.DisplayOrder)
            .ThenBy(x => x.Template.StageTaskTemplateId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Template.StageTaskTemplateId,
                x.Template.WorkflowStageId,
                StageCode = x.Stage.Code,
                StageName = x.Stage.Name,
                x.Template.Title,
                x.Template.Description,
                x.Template.IsMandatory,
                x.Template.DueDaysOffset,
                x.Template.DisplayOrder,
                x.Template.IsActive
            })
            .ToListAsync(ct);

        var ids = pageRows.Select(x => x.StageTaskTemplateId).ToArray();
        var usage = ids.Length == 0
            ? new Dictionary<long, int>()
            : await db.OperationTasks.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.StageTaskTemplateId.HasValue && ids.Contains(x.StageTaskTemplateId.Value))
                .GroupBy(x => x.StageTaskTemplateId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var items = pageRows.Select(x => new WorkflowTaskTemplateDto(
            x.StageTaskTemplateId,
            x.WorkflowStageId,
            x.StageCode,
            x.StageName,
            x.Title,
            x.Description,
            x.IsMandatory,
            x.DueDaysOffset,
            x.DisplayOrder,
            x.IsActive,
            usage.GetValueOrDefault(x.StageTaskTemplateId)))
            .ToList();

        return new PagedResult<WorkflowTaskTemplateDto>(items, page, pageSize, total);
    }

    public async Task<WorkflowTaskTemplateDto> CreateTaskTemplateAsync(
        SaveWorkflowTaskTemplateRequest request,
        CancellationToken ct)
    {
        var values = await ValidateRequestAsync(request, null, ct);
        var entity = new StageTaskTemplate
        {
            CompanyId = currentCompany.CompanyId,
            WorkflowStageId = request.WorkflowStageId,
            Title = values.Title,
            Description = values.Description,
            IsMandatory = request.IsMandatory,
            DueDaysOffset = request.DueDaysOffset,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        db.StageTaskTemplates.Add(entity);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(entity.StageTaskTemplateId, ct);
    }

    public async Task<WorkflowTaskTemplateDto> UpdateTaskTemplateAsync(
        long id,
        SaveWorkflowTaskTemplateRequest request,
        CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, ct);
        var values = await ValidateRequestAsync(request, id, ct);

        entity.WorkflowStageId = request.WorkflowStageId;
        entity.Title = values.Title;
        entity.Description = values.Description;
        entity.IsMandatory = request.IsMandatory;
        entity.DueDaysOffset = request.DueDaysOffset;
        entity.DisplayOrder = request.DisplayOrder;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task SetTaskTemplateActiveAsync(long id, bool active, CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, ct);
        if (entity.IsActive == active) return;

        if (active)
            await EnsureStageActiveAsync(entity.WorkflowStageId, ct);

        entity.IsActive = active;
        await db.SaveChangesAsync(ct);
    }

    public async Task<WorkflowTaskTemplateDeleteCheckDto> CheckTaskTemplateDeleteAsync(long id, CancellationToken ct)
    {
        _ = await FindOwnedAsync(id, ct);
        var companyId = currentCompany.CompanyId;
        var generatedTasksCount = await db.OperationTasks.IgnoreQueryFilters().AsNoTracking()
            .CountAsync(x => x.CompanyId == companyId && x.StageTaskTemplateId == id, ct);

        if (generatedTasksCount > 0)
            return new WorkflowTaskTemplateDeleteCheckDto(
                false,
                generatedTasksCount,
                $"No se puede eliminar la plantilla porque ya generó {generatedTasksCount} tarea(s) en operaciones. Desactívala para que no se genere en nuevas operaciones.");

        return new WorkflowTaskTemplateDeleteCheckDto(
            true,
            0,
            "La plantilla nunca ha generado tareas y puede eliminarse definitivamente.");
    }

    public async Task DeleteTaskTemplateAsync(long id, CancellationToken ct)
    {
        var entity = await FindOwnedAsync(id, ct);
        var check = await CheckTaskTemplateDeleteAsync(id, ct);
        if (!check.CanDelete)
            throw new BusinessException(ErrorCodes.BusinessRule, check.Message, 409);

        db.StageTaskTemplates.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task<(string Title, string? Description)> ValidateRequestAsync(
        SaveWorkflowTaskTemplateRequest request,
        long? selfId,
        CancellationToken ct)
    {
        var title = (request.Title ?? string.Empty).Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (title.Length == 0) throw Validation("El título es obligatorio.");
        if (title.Length > 180) throw Validation("El título no puede exceder 180 caracteres.");
        if (description?.Length > 500) throw Validation("La descripción no puede exceder 500 caracteres.");
        if (request.DueDaysOffset is < 0 or > 3650) throw Validation("El plazo en días debe estar entre 0 y 3650.");
        if (request.DisplayOrder < 0 || request.DisplayOrder > 100000) throw Validation("El orden debe estar entre 0 y 100000.");

        await EnsureStageActiveAsync(request.WorkflowStageId, ct);

        var companyId = currentCompany.CompanyId;
        var duplicate = await db.StageTaskTemplates.AsNoTracking().AnyAsync(x =>
            x.CompanyId == companyId &&
            x.WorkflowStageId == request.WorkflowStageId &&
            x.StageTaskTemplateId != selfId &&
            x.Title == title,
            ct);

        if (duplicate)
            throw new BusinessException(
                ErrorCodes.BusinessRule,
                "Ya existe una plantilla con el mismo título en esta etapa.",
                409);

        return (title, description);
    }

    private async Task EnsureStageActiveAsync(int stageId, CancellationToken ct)
    {
        var companyId = currentCompany.CompanyId;
        var exists = await db.WorkflowStages.AsNoTracking().AnyAsync(x =>
            x.WorkflowStageId == stageId && x.CompanyId == companyId && x.IsActive,
            ct);
        if (!exists)
            throw Validation("La etapa seleccionada no existe o está inactiva.");
    }

    private async Task<StageTaskTemplate> FindOwnedAsync(long id, CancellationToken ct)
    {
        var companyId = currentCompany.CompanyId;
        return await db.StageTaskTemplates.SingleOrDefaultAsync(x =>
                   x.StageTaskTemplateId == id && x.CompanyId == companyId,
                   ct)
               ?? throw new BusinessException(ErrorCodes.NotFound, "Plantilla de tarea no encontrada.", 404);
    }

    private async Task<WorkflowTaskTemplateDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var companyId = currentCompany.CompanyId;
        var row = await (
            from t in db.StageTaskTemplates.AsNoTracking()
            join s in db.WorkflowStages.AsNoTracking() on t.WorkflowStageId equals s.WorkflowStageId
            where t.StageTaskTemplateId == id && t.CompanyId == companyId && s.CompanyId == companyId
            select new
            {
                t.StageTaskTemplateId,
                t.WorkflowStageId,
                StageCode = s.Code,
                StageName = s.Name,
                t.Title,
                t.Description,
                t.IsMandatory,
                t.DueDaysOffset,
                t.DisplayOrder,
                t.IsActive
            }).SingleAsync(ct);

        var count = await db.OperationTasks.IgnoreQueryFilters().AsNoTracking().CountAsync(x => x.CompanyId == companyId && x.StageTaskTemplateId == id, ct);
        return new WorkflowTaskTemplateDto(
            row.StageTaskTemplateId,
            row.WorkflowStageId,
            row.StageCode,
            row.StageName,
            row.Title,
            row.Description,
            row.IsMandatory,
            row.DueDaysOffset,
            row.DisplayOrder,
            row.IsActive,
            count);
    }

    private static (int Page, int PageSize) ValidatePage(int page, int pageSize)
    {
        if (page < 1) throw Validation("La página debe ser mayor o igual a 1.");
        if (pageSize is < 1 or > 100) throw Validation("El tamaño de página debe estar entre 1 y 100.");
        return (page, pageSize);
    }

    private static BusinessException Validation(string message) =>
        new(ErrorCodes.Validation, message, 400);
}

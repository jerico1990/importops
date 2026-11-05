using ImportOps.Application.Alerts;
using ImportOps.Application.Common;
using ImportOps.Application.Security;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class AlertService(ImportOpsDbContext db, ICurrentCompany company, ICurrentUser currentUser) : IAlertService
{
    private sealed record Candidate(string Type,string Title,string Message,string SourceType,long? SourceId,long? OperationId,long? ClientId,long? ResponsibleUserId,string? RequiredPermission,string? ActionPath,string Key,DateTime TriggeredAt);

    public async Task EvaluateAllCompaniesAsync(CancellationToken ct)
    {
        var ids=await db.Companies.AsNoTracking().Select(x=>x.CompanyId).ToListAsync(ct);
        foreach(var id in ids) await EvaluateCompanyAsync(id,ct);
    }

    public async Task EvaluateCompanyAsync(long companyId,CancellationToken ct)
    {
        var now=DateTime.UtcNow;
        var rules=await db.AlertRules.Where(x=>x.CompanyId==companyId && x.IsActive).ToDictionaryAsync(x=>x.AlertType,ct);
        var candidates=new List<Candidate>();
        var operations=await db.ImportOperations.AsNoTracking().Where(x=>x.CompanyId==companyId && x.OperationalStatus!=OperationStatuses.Closed && x.OperationalStatus!=OperationStatuses.Cancelled).Select(x=>new{x.OperationId,x.OperationNo,x.ClientId,x.ClientNameSnapshot,x.ResponsibleUserId,x.CurrentStageId,x.OperationalStatus,x.DocumentaryStatus,x.FinancialStatus,x.OpenedAt}).ToListAsync(ct);
        var opMap=operations.ToDictionary(x=>x.OperationId);

        // Solicitudes: una alerta nace cuando el cliente realmente la envía a revisión,
        // no cuando simplemente guarda un borrador.
        if(rules.ContainsKey(AlertTypes.RequestPendingReview) || rules.ContainsKey(AlertTypes.RequestObserved))
        {
            var requests=await db.ImportRequests.AsNoTracking().Where(x=>x.CompanyId==companyId && !x.IsDeleted && (x.Status==ImportRequestStatuses.UnderReview || x.Status==ImportRequestStatuses.Observed)).Select(x=>new{x.RequestId,x.RequestNo,x.ClientId,x.AssignedUserId,x.Status}).ToListAsync(ct);
            var requestIds=requests.Select(x=>x.RequestId).ToArray();
            var history=await db.RequestStatusHistories.AsNoTracking().Where(x=>requestIds.Contains(x.RequestId) && (x.NewStatus==ImportRequestStatuses.UnderReview || x.NewStatus==ImportRequestStatuses.Observed)).GroupBy(x=>new{x.RequestId,x.NewStatus}).Select(g=>new{g.Key.RequestId,g.Key.NewStatus,ChangedAt=g.Max(x=>x.ChangedAt)}).ToListAsync(ct);
            foreach(var r in requests)
            {
                var changed=history.Where(x=>x.RequestId==r.RequestId && x.NewStatus==r.Status).Select(x=>x.ChangedAt).DefaultIfEmpty(now).Max();
                if(r.Status==ImportRequestStatuses.UnderReview && rules.ContainsKey(AlertTypes.RequestPendingReview))
                    candidates.Add(new(AlertTypes.RequestPendingReview,"Nueva solicitud pendiente de revisión",$"La solicitud {r.RequestNo} fue enviada para revisión.","REQUEST",r.RequestId,null,r.ClientId,r.AssignedUserId,Permissions.RequestsManage,$"/solicitudes/{r.RequestId}",$"REQUEST_PENDING_REVIEW:{r.RequestId}",changed));
                if(r.Status==ImportRequestStatuses.Observed && rules.ContainsKey(AlertTypes.RequestObserved))
                    candidates.Add(new(AlertTypes.RequestObserved,"Solicitud observada",$"La solicitud {r.RequestNo} tiene observaciones y requiere subsanación.","REQUEST",r.RequestId,null,r.ClientId,r.AssignedUserId,Permissions.RequestsView,$"/solicitudes/{r.RequestId}",$"REQUEST_OBSERVED:{r.RequestId}",changed));
            }
        }

        if(rules.TryGetValue(AlertTypes.QuotationPendingApproval,out var qr))
        {
            var rows=await (from q in db.Quotations.AsNoTracking() join r in db.ImportRequests.AsNoTracking() on q.RequestId equals r.RequestId where q.CompanyId==companyId && q.Status==QuotationStatuses.Sent select new{q.QuotationId,q.QuotationNo,q.ClientId,q.SentAt,r.AssignedUserId}).ToListAsync(ct);
            foreach(var x in rows) candidates.Add(new(AlertTypes.QuotationPendingApproval,"Cotización pendiente de aprobación",$"La cotización {x.QuotationNo} está pendiente de aprobación del cliente.","QUOTATION",x.QuotationId,null,x.ClientId,x.AssignedUserId,Permissions.QuotationsView,$"/cotizaciones/{x.QuotationId}",$"QUOTATION_PENDING_APPROVAL:{x.QuotationId}",x.SentAt??now));
        }

        if(rules.ContainsKey(AlertTypes.QuotationRejected) || rules.ContainsKey(AlertTypes.QuotationApprovedOperationPending))
        {
            var quotations=await (from q in db.Quotations.AsNoTracking() join r in db.ImportRequests.AsNoTracking() on q.RequestId equals r.RequestId where q.CompanyId==companyId && !q.IsDeleted && (q.Status==QuotationStatuses.Rejected || q.Status==QuotationStatuses.Approved) select new{q.QuotationId,q.QuotationNo,q.ClientId,q.Status,q.RejectedAt,q.ApprovedAt,r.AssignedUserId}).ToListAsync(ct);
            var quotationIds=quotations.Select(x=>x.QuotationId).ToArray();
            var withOperation=(await db.ImportOperations.AsNoTracking().Where(x=>x.CompanyId==companyId && x.QuotationId!=null && quotationIds.Contains(x.QuotationId.Value)).Select(x=>x.QuotationId!.Value).ToListAsync(ct)).ToHashSet();
            foreach(var q in quotations)
            {
                if(q.Status==QuotationStatuses.Rejected && rules.ContainsKey(AlertTypes.QuotationRejected))
                    candidates.Add(new(AlertTypes.QuotationRejected,"Cotización rechazada por el cliente",$"El cliente rechazó la cotización {q.QuotationNo}. Se requiere revisar el caso y, de corresponder, generar una nueva versión.","QUOTATION",q.QuotationId,null,q.ClientId,q.AssignedUserId,Permissions.QuotationsManage,$"/cotizaciones/{q.QuotationId}",$"QUOTATION_REJECTED:{q.QuotationId}",q.RejectedAt??now));
                if(q.Status==QuotationStatuses.Approved && !withOperation.Contains(q.QuotationId) && rules.ContainsKey(AlertTypes.QuotationApprovedOperationPending))
                    candidates.Add(new(AlertTypes.QuotationApprovedOperationPending,"Cotización aprobada — operación pendiente",$"La cotización {q.QuotationNo} fue aprobada y todavía no se ha creado la operación.","QUOTATION",q.QuotationId,null,q.ClientId,q.AssignedUserId,Permissions.OperationsManage,$"/cotizaciones/{q.QuotationId}",$"QUOTATION_APPROVED_OPERATION_PENDING:{q.QuotationId}",q.ApprovedAt??now));
            }
        }

        // Estados operativos que requieren comunicación o acción.
        if(rules.ContainsKey(AlertTypes.OperationOnHold))
            foreach(var o in operations.Where(x=>x.OperationalStatus==OperationStatuses.OnHold))
                candidates.Add(new(AlertTypes.OperationOnHold,"Operación puesta en espera",$"La operación {o.OperationNo} se encuentra temporalmente en espera.","OPERATION",o.OperationId,o.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.OperationsView,$"/operaciones/{o.OperationId}",$"OPERATION_ON_HOLD:{o.OperationId}",now));

        if(rules.TryGetValue(AlertTypes.OperationCancelled,out var cancelledRule))
        {
            var cancelled=await (from o in db.ImportOperations.AsNoTracking() join h in db.OperationStatusHistories.AsNoTracking() on o.OperationId equals h.OperationId where o.CompanyId==companyId && o.OperationalStatus==OperationStatuses.Cancelled && h.StatusDimension=="OPERATIONAL" && h.NewStatus==OperationStatuses.Cancelled select new{o.OperationId,o.OperationNo,o.ClientId,o.ResponsibleUserId,h.ChangedAt}).ToListAsync(ct);
            var keepHours=cancelledRule.ThresholdHours??72;
            foreach(var o in cancelled.GroupBy(x=>new{x.OperationId,x.OperationNo,x.ClientId,x.ResponsibleUserId}).Select(g=>new{g.Key.OperationId,g.Key.OperationNo,g.Key.ClientId,g.Key.ResponsibleUserId,ChangedAt=g.Max(x=>x.ChangedAt)}).Where(x=>x.ChangedAt>=now.AddHours(-keepHours)))
                candidates.Add(new(AlertTypes.OperationCancelled,"Operación cancelada",$"La operación {o.OperationNo} fue cancelada.","OPERATION",o.OperationId,o.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.OperationsView,$"/operaciones/{o.OperationId}",$"OPERATION_CANCELLED:{o.OperationId}:{o.ChangedAt:yyyyMMddHHmmss}",o.ChangedAt));
        }

        if(rules.TryGetValue(AlertTypes.TaskDueSoon,out var taskSoon) || rules.ContainsKey(AlertTypes.TaskOverdue))
        {
            var tasks=await db.OperationTasks.AsNoTracking().Where(x=>x.CompanyId==companyId && x.DueAt!=null && x.TaskStatus!=OperationTaskStatuses.Completed && x.TaskStatus!=OperationTaskStatuses.Cancelled).ToListAsync(ct);
            foreach(var t in tasks.Where(x=>opMap.ContainsKey(x.OperationId)))
            {
                var o=opMap[t.OperationId];
                if(t.DueAt!.Value<now && rules.ContainsKey(AlertTypes.TaskOverdue)) candidates.Add(new(AlertTypes.TaskOverdue,"Tarea vencida",$"{t.Title} venció el {t.DueAt:dd/MM/yyyy HH:mm}.","TASK",t.OperationTaskId,t.OperationId,o.ClientId,t.AssignedUserId??o.ResponsibleUserId,Permissions.OperationsView,$"/operaciones/{t.OperationId}",$"TASK_OVERDUE:{t.OperationTaskId}",t.DueAt.Value));
                else if(taskSoon is not null && t.DueAt.Value>=now && t.DueAt.Value<=now.AddHours(taskSoon.ThresholdHours??24)) candidates.Add(new(AlertTypes.TaskDueSoon,"Tarea próxima a vencer",$"{t.Title} vence el {t.DueAt:dd/MM/yyyy HH:mm}.","TASK",t.OperationTaskId,t.OperationId,o.ClientId,t.AssignedUserId??o.ResponsibleUserId,Permissions.OperationsView,$"/operaciones/{t.OperationId}",$"TASK_DUE_SOON:{t.OperationTaskId}",now));
            }
        }

        var shipments=await db.Shipments.AsNoTracking().Where(x=>x.CompanyId==companyId && !x.IsDeleted).ToListAsync(ct);
        foreach(var sh in shipments.Where(x=>opMap.ContainsKey(x.OperationId)))
        {
            var o=opMap[sh.OperationId];
            if(sh.ActualArrivalAt.HasValue && rules.ContainsKey(AlertTypes.ShipmentArrived)) candidates.Add(new(AlertTypes.ShipmentArrived,"Arribo confirmado",$"El embarque de {o.OperationNo} arribó el {sh.ActualArrivalAt:dd/MM/yyyy HH:mm}.","SHIPMENT",sh.ShipmentId,sh.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.ShipmentsView,$"/embarques/{sh.ShipmentId}",$"SHIPMENT_ARRIVED:{sh.ShipmentId}:{sh.ActualArrivalAt:yyyyMMddHHmm}",sh.ActualArrivalAt.Value));
            if(!sh.ActualArrivalAt.HasValue && sh.Eta.HasValue)
            {
                if(sh.Eta.Value<now && rules.ContainsKey(AlertTypes.ShipmentEtaOverdue)) candidates.Add(new(AlertTypes.ShipmentEtaOverdue,"Embarque con ETA vencida",$"La ETA de {o.OperationNo} venció el {sh.Eta:dd/MM/yyyy} y aún no registra arribo.","SHIPMENT",sh.ShipmentId,sh.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.ShipmentsView,$"/embarques/{sh.ShipmentId}",$"SHIPMENT_ETA_OVERDUE:{sh.ShipmentId}",sh.Eta.Value));
                else if(rules.TryGetValue(AlertTypes.ShipmentEtaSoon,out var etaRule) && sh.Eta.Value>=now && sh.Eta.Value<=now.AddHours(etaRule.ThresholdHours??48)) candidates.Add(new(AlertTypes.ShipmentEtaSoon,"Embarque próximo a arribar",$"El embarque de {o.OperationNo} tiene ETA {sh.Eta:dd/MM/yyyy HH:mm}.","SHIPMENT",sh.ShipmentId,sh.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.ShipmentsView,$"/embarques/{sh.ShipmentId}",$"SHIPMENT_ETA_SOON:{sh.ShipmentId}",now));
            }
        }

        if(rules.ContainsKey(AlertTypes.DocumentObserved))
        {
            var obs=await (from d in db.Documents.AsNoTracking() join ob in db.DocumentObservations.AsNoTracking() on d.DocumentId equals ob.DocumentId where d.CompanyId==companyId && !d.IsDeleted && d.OperationId!=null && ob.Status==DocumentObservationStatuses.Open select new{d.DocumentId,d.OperationId,d.Visibility,ob.DocumentObservationId,ob.Description,ob.ObservedAt}).ToListAsync(ct);
            foreach(var x in obs.Where(x=>opMap.ContainsKey(x.OperationId!.Value))) {var o=opMap[x.OperationId!.Value]; candidates.Add(new(AlertTypes.DocumentObserved,"Documento observado",x.Description,"DOCUMENT",x.DocumentId,x.OperationId,(x.Visibility==RecordVisibilities.Client||x.Visibility==RecordVisibilities.Both)?o.ClientId:null,o.ResponsibleUserId,Permissions.DocumentsView,$"/documentos/{x.DocumentId}",$"DOCUMENT_OBSERVED:{x.DocumentObservationId}",x.ObservedAt));}
        }

        if(rules.ContainsKey(AlertTypes.DocumentRequiredMissing))
        {
            var reqs=await db.StageDocumentRequirements.AsNoTracking().Where(x=>x.CompanyId==companyId && x.IsActive && x.IsMandatory).ToListAsync(ct);
            var docs=await db.Documents.AsNoTracking().Where(x=>x.CompanyId==companyId && !x.IsDeleted && x.OperationId!=null && x.DocumentStatus==DocumentStatuses.Valid).Select(x=>new{x.OperationId,x.DocumentTypeId}).ToListAsync(ct);
            var valid=docs.Select(x=>(x.OperationId!.Value,x.DocumentTypeId)).ToHashSet();
            var names=await db.DocumentTypes.AsNoTracking().ToDictionaryAsync(x=>x.DocumentTypeId,x=>x.Name,ct);
            foreach(var o in operations) foreach(var r in reqs.Where(x=>x.WorkflowStageId==o.CurrentStageId && !valid.Contains((o.OperationId,x.DocumentTypeId)))) candidates.Add(new(AlertTypes.DocumentRequiredMissing,"Documento obligatorio pendiente",$"Falta documento obligatorio: {names.GetValueOrDefault(r.DocumentTypeId,"Documento requerido")}.","DOCUMENT_REQUIREMENT",r.StageDocumentRequirementId,o.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.DocumentsView,$"/operaciones/{o.OperationId}",$"DOCUMENT_REQUIRED_MISSING:{o.OperationId}:{r.StageDocumentRequirementId}",now));
        }

        var customs=await db.CustomsProcesses.AsNoTracking().Where(x=>x.CompanyId==companyId).ToListAsync(ct);
        foreach(var cp in customs.Where(x=>opMap.ContainsKey(x.OperationId)))
        {
            var o=opMap[cp.OperationId];
            if(cp.CustomsStatus==CustomsStatuses.Released && cp.ReleaseDate.HasValue && rules.ContainsKey(AlertTypes.CustomsReleased)) candidates.Add(new(AlertTypes.CustomsReleased,"Levante autorizado",$"{o.OperationNo} obtuvo levante el {cp.ReleaseDate:dd/MM/yyyy HH:mm}.","CUSTOMS",cp.CustomsProcessId,cp.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.CustomsView,$"/aduanas/{cp.CustomsProcessId}",$"CUSTOMS_RELEASED:{cp.CustomsProcessId}:{cp.ReleaseDate:yyyyMMddHHmm}",cp.ReleaseDate.Value));
            if(cp.CustomsStatus!=CustomsStatuses.Released && cp.CustomsStatus!=CustomsStatuses.Cancelled && rules.TryGetValue(AlertTypes.CustomsReleasePending,out var cr) && cp.CreatedAt<=now.AddHours(-(cr.ThresholdHours??48))) candidates.Add(new(AlertTypes.CustomsReleasePending,"Levante pendiente",$"{o.OperationNo} continúa sin levante aduanero.","CUSTOMS",cp.CustomsProcessId,cp.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.CustomsView,$"/aduanas/{cp.CustomsProcessId}",$"CUSTOMS_RELEASE_PENDING:{cp.CustomsProcessId}",cp.CreatedAt));
        }
        if(rules.ContainsKey(AlertTypes.CustomsObservationOpen))
        {
            var obs=await (from ob in db.CustomsObservations.AsNoTracking() join cp in db.CustomsProcesses.AsNoTracking() on ob.CustomsProcessId equals cp.CustomsProcessId where cp.CompanyId==companyId && ob.ObservationStatus==CustomsObservationStatuses.Open select new{ob,cp.OperationId}).ToListAsync(ct);
            foreach(var x in obs.Where(x=>opMap.ContainsKey(x.OperationId))){var o=opMap[x.OperationId];candidates.Add(new(AlertTypes.CustomsObservationOpen,"Observación aduanera abierta",x.ob.Description,"CUSTOMS_OBSERVATION",x.ob.CustomsObservationId,x.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.CustomsView,$"/aduanas/{x.ob.CustomsProcessId}",$"CUSTOMS_OBSERVATION_OPEN:{x.ob.CustomsObservationId}",x.ob.ObservedAt));}
        }

        var settlements=await db.OperationSettlements.AsNoTracking().Where(x=>x.CompanyId==companyId).ToListAsync(ct);
        foreach(var o in operations)
        {
            var st=settlements.Where(x=>x.OperationId==o.OperationId).OrderByDescending(x=>x.OperationSettlementId).FirstOrDefault();
            if(rules.TryGetValue(AlertTypes.SettlementPending,out var sr) && o.OperationalStatus==OperationStatuses.Delivered && (st is null || st.SettlementStatus!=FinanceStatuses.Approved) && o.OpenedAt<=now.AddHours(-(sr.ThresholdHours??24))) candidates.Add(new(AlertTypes.SettlementPending,"Rendición pendiente",$"{o.OperationNo} fue entregada y aún no tiene rendición aprobada.","SETTLEMENT",st?.OperationSettlementId,o.OperationId,o.ClientId,null,Permissions.FinanceView,$"/finanzas/{o.OperationId}",$"SETTLEMENT_PENDING:{o.OperationId}",now));
            if(rules.ContainsKey(AlertTypes.OperationReadyToClose) && o.OperationalStatus!=OperationStatuses.Closed && o.DocumentaryStatus==DocumentaryStatuses.Complete && o.FinancialStatus==FinancialStatuses.Closed && st?.SettlementStatus==FinanceStatuses.Approved) candidates.Add(new(AlertTypes.OperationReadyToClose,"Operación lista para cierre",$"{o.OperationNo} tiene documentación completa y finanzas cerradas.","OPERATION",o.OperationId,o.OperationId,o.ClientId,o.ResponsibleUserId,Permissions.OperationsClose,$"/operaciones/{o.OperationId}",$"OPERATION_READY_TO_CLOSE:{o.OperationId}",now));
        }

        var keys=candidates.Select(x=>x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var active=await db.Alerts.Where(x=>x.CompanyId==companyId && x.Status==AlertStatuses.Active).ToListAsync(ct);
        foreach(var a in active.Where(x=>!keys.Contains(x.DedupKey))){a.Status=AlertStatuses.Resolved;a.ResolvedAt=now;a.ModifiedAt=now;}
        foreach(var c in candidates)
        {
            if(!rules.TryGetValue(c.Type,out var rule)) continue;
            var existing=active.FirstOrDefault(x=>x.DedupKey==c.Key);
            if(existing is not null){existing.LastDetectedAt=now;existing.Title=c.Title;existing.Message=c.Message;existing.Severity=rule.Severity;existing.Audience=rule.Audience;existing.ModifiedAt=now;continue;}
            db.Alerts.Add(new AlertRecord{CompanyId=companyId,AlertRuleId=rule.AlertRuleId,AlertType=c.Type,Severity=rule.Severity,Audience=rule.Audience,OperationId=c.OperationId,ClientId=c.ClientId,ResponsibleUserId=c.ResponsibleUserId,RequiredPermission=c.RequiredPermission,Title=c.Title,Message=c.Message,SourceType=c.SourceType,SourceId=c.SourceId,ActionPath=c.ActionPath,DedupKey=c.Key,Status=AlertStatuses.Active,TriggeredAt=c.TriggeredAt,LastDetectedAt=now,CreatedAt=now});
        }
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<AlertRecord> VisibleQuery()
    {
        var q=db.Alerts.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId);
        if(currentUser.IsClientUser) return q.Where(x=>x.ClientId==currentUser.ClientId && (x.Audience==AlertAudiences.Client || x.Audience==AlertAudiences.Both));
        return q.Where(x=>x.Audience==AlertAudiences.Internal || x.Audience==AlertAudiences.Both);
    }
    private async Task<HashSet<string>> UserPermissions(CancellationToken ct){var roleIds=await db.UserRoles.AsNoTracking().Where(x=>x.UserId==currentUser.UserId).Select(x=>x.RoleId).ToListAsync(ct);return (await(from rp in db.RolePermissions.AsNoTracking() join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.PermissionId where roleIds.Contains(rp.RoleId)&&p.IsActive select p.Code).Distinct().ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);}
    private async Task<List<AlertRecord>> VisibleRows(CancellationToken ct){var rows=await VisibleQuery().ToListAsync(ct);if(currentUser.IsClientUser)return rows;var perms=await UserPermissions(ct);return rows.Where(x=>x.ResponsibleUserId==currentUser.UserId || x.RequiredPermission==null || perms.Contains(x.RequiredPermission)).ToList();}

    public async Task<AlertPageDto> GetAsync(AlertQuery query,CancellationToken ct){await EvaluateCompanyAsync(company.CompanyId,ct);var rows=await VisibleRows(ct);var states=await db.AlertUserStates.AsNoTracking().Where(x=>x.UserId==currentUser.UserId).ToDictionaryAsync(x=>x.AlertId,ct);var q=rows.Where(x=>!states.TryGetValue(x.AlertId,out var st)||!st.IsDismissed);if(!string.IsNullOrWhiteSpace(query.Status))q=q.Where(x=>x.Status.Equals(query.Status,StringComparison.OrdinalIgnoreCase));if(!string.IsNullOrWhiteSpace(query.Severity))q=q.Where(x=>x.Severity.Equals(query.Severity,StringComparison.OrdinalIgnoreCase));if(query.Unread==true)q=q.Where(x=>!states.TryGetValue(x.AlertId,out var st)||!st.IsRead);var list=q.OrderByDescending(x=>x.TriggeredAt).ToList();var total=list.Count;var page=Math.Max(1,query.Page);var size=Math.Clamp(query.PageSize,1,100);var slice=list.Skip((page-1)*size).Take(size).ToList();var opIds=slice.Where(x=>x.OperationId.HasValue).Select(x=>x.OperationId!.Value).Distinct().ToArray();var ops=await db.ImportOperations.AsNoTracking().Where(x=>opIds.Contains(x.OperationId)).Select(x=>new{x.OperationId,x.OperationNo,x.ClientNameSnapshot}).ToDictionaryAsync(x=>x.OperationId,ct);return new AlertPageDto(slice.Select(x=>new AlertDto(x.AlertId,x.AlertType,x.Severity,x.Status,x.Title,x.Message,x.OperationId,x.OperationId.HasValue&&ops.TryGetValue(x.OperationId.Value,out var o)?o.OperationNo:null,x.ClientId,x.OperationId.HasValue&&ops.TryGetValue(x.OperationId.Value,out var o2)?o2.ClientNameSnapshot:null,x.SourceType,x.SourceId,x.ActionPath,x.TriggeredAt,states.TryGetValue(x.AlertId,out var st)&&st.IsRead)).ToList(),total,page,size);}
    public async Task<AlertSummaryDto> GetSummaryAsync(CancellationToken ct){await EvaluateCompanyAsync(company.CompanyId,ct);var rows=(await VisibleRows(ct)).Where(x=>x.Status==AlertStatuses.Active).ToList();var states=await db.AlertUserStates.AsNoTracking().Where(x=>x.UserId==currentUser.UserId).ToDictionaryAsync(x=>x.AlertId,ct);rows=rows.Where(x=>!states.TryGetValue(x.AlertId,out var st)||!st.IsDismissed).ToList();return new(rows.Count,rows.Count(x=>!states.TryGetValue(x.AlertId,out var st)||!st.IsRead),rows.Count(x=>x.Severity==AlertSeverities.Critical),rows.Count(x=>x.Severity==AlertSeverities.Warning));}
    public async Task MarkReadAsync(long alertId,CancellationToken ct){if(!(await VisibleRows(ct)).Any(x=>x.AlertId==alertId))throw new BusinessException(ErrorCodes.NotFound,"Alerta no encontrada.",404);var s=await db.AlertUserStates.SingleOrDefaultAsync(x=>x.AlertId==alertId&&x.UserId==currentUser.UserId,ct);if(s is null){s=new AlertUserState{AlertId=alertId,UserId=currentUser.UserId};db.AlertUserStates.Add(s);}s.IsRead=true;s.ReadAt=DateTime.UtcNow;await db.SaveChangesAsync(ct);}
    public async Task MarkAllReadAsync(CancellationToken ct){var ids=(await VisibleRows(ct)).Where(x=>x.Status==AlertStatuses.Active).Select(x=>x.AlertId).ToArray();var existing=await db.AlertUserStates.Where(x=>x.UserId==currentUser.UserId&&ids.Contains(x.AlertId)).ToDictionaryAsync(x=>x.AlertId,ct);foreach(var id in ids){if(!existing.TryGetValue(id,out var s)){s=new AlertUserState{AlertId=id,UserId=currentUser.UserId};db.AlertUserStates.Add(s);}s.IsRead=true;s.ReadAt=DateTime.UtcNow;}await db.SaveChangesAsync(ct);}
    public async Task DismissAsync(long alertId,CancellationToken ct){if(!(await VisibleRows(ct)).Any(x=>x.AlertId==alertId))throw new BusinessException(ErrorCodes.NotFound,"Alerta no encontrada.",404);var s=await db.AlertUserStates.SingleOrDefaultAsync(x=>x.AlertId==alertId&&x.UserId==currentUser.UserId,ct);if(s is null){s=new AlertUserState{AlertId=alertId,UserId=currentUser.UserId};db.AlertUserStates.Add(s);}s.IsDismissed=true;s.DismissedAt=DateTime.UtcNow;await db.SaveChangesAsync(ct);}
    public async Task<IReadOnlyList<AlertRuleDto>> GetRulesAsync(CancellationToken ct)=>await db.AlertRules.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId).OrderBy(x=>x.Name).Select(x=>new AlertRuleDto(x.AlertRuleId,x.AlertType,x.Name,x.Description,x.Severity,x.Audience,x.ThresholdHours,x.IsActive)).ToListAsync(ct);
    public async Task<AlertRuleDto> UpdateRuleAsync(int id,UpdateAlertRuleRequest r,CancellationToken ct){var x=await db.AlertRules.SingleOrDefaultAsync(x=>x.AlertRuleId==id&&x.CompanyId==company.CompanyId,ct)??throw new BusinessException(ErrorCodes.NotFound,"Regla no encontrada.",404);if(!AlertSeverities.All.Contains(r.Severity))throw new BusinessException(ErrorCodes.Validation,"Severidad inválida.",400);var aud=r.Audience.ToUpperInvariant();if(aud is not (AlertAudiences.Internal or AlertAudiences.Client or AlertAudiences.Both))throw new BusinessException(ErrorCodes.Validation,"Audiencia inválida.",400);if((aud==AlertAudiences.Client||aud==AlertAudiences.Both)&&!AlertTypes.ClientVisible.Contains(x.AlertType))throw new BusinessException(ErrorCodes.BusinessRule,"Esta alerta contiene información interna y no puede publicarse al cliente.",400);x.Severity=r.Severity.ToUpperInvariant();x.Audience=aud;x.ThresholdHours=r.ThresholdHours;x.IsActive=r.IsActive;x.ModifiedAt=DateTime.UtcNow;await db.SaveChangesAsync(ct);return new(x.AlertRuleId,x.AlertType,x.Name,x.Description,x.Severity,x.Audience,x.ThresholdHours,x.IsActive);}
}

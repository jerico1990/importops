using ImportOps.Application.Common;
using ImportOps.Application.Documents;
using ImportOps.Application.Operations;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class DocumentService(
    ImportOpsDbContext db,
    IFileStorage storage,
    IImportOperationService operationService,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser) : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".xls", ".docx", ".doc", ".txt"
    };

    private const long MaxFileBytes = 25L * 1024 * 1024;

    public async Task<PagedResult<DocumentDto>> GetAsync(DocumentQuery q, CancellationToken ct)
    {
        var page=Math.Max(q.Page,1); var pageSize=Math.Clamp(q.PageSize,1,100);
        var query=db.Documents.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId);
        if(currentUser.IsClientUser)
        {
            if(!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden,"Usuario cliente sin client_id.",403);

            var clientId=currentUser.ClientId.Value;
            query=query.Where(x=>
                (x.RequestId.HasValue && db.ImportRequests.Any(r=>r.RequestId==x.RequestId.Value&&r.ClientId==clientId)) ||
                (x.QuotationId.HasValue && db.Quotations.Any(q=>q.QuotationId==x.QuotationId.Value&&q.ClientId==clientId)) ||
                (x.OperationId.HasValue && db.ImportOperations.Any(o=>o.OperationId==x.OperationId.Value&&o.ClientId==clientId)));

            query=query.Where(x=>x.Visibility==RecordVisibilities.Client||x.Visibility==RecordVisibilities.Both);
        }
        if(q.RequestId.HasValue) query=query.Where(x=>x.RequestId==q.RequestId);
        if(q.QuotationId.HasValue) query=query.Where(x=>x.QuotationId==q.QuotationId);
        if(q.OperationId.HasValue) query=query.Where(x=>x.OperationId==q.OperationId);
        if(q.DocumentTypeId.HasValue) query=query.Where(x=>x.DocumentTypeId==q.DocumentTypeId);
        if(!string.IsNullOrWhiteSpace(q.Status)){var s=q.Status.Trim().ToUpperInvariant(); query=query.Where(x=>x.DocumentStatus==s);}
        if(!string.IsNullOrWhiteSpace(q.Search)){var s=q.Search.Trim(); query=query.Where(x=>x.OriginalFileName.Contains(s)||(x.Notes!=null&&x.Notes.Contains(s)));}

        var total=await query.CountAsync(ct);
        var entities=await query.OrderByDescending(x=>x.UploadedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        var items=new List<DocumentDto>();
        foreach(var e in entities) items.Add(await MapAsync(e,ct));
        return new(items,page,pageSize,total);
    }

    public async Task<DocumentDto?> GetByIdAsync(long id,CancellationToken ct)
    {
        var e=await db.Documents.AsNoTracking().SingleOrDefaultAsync(x=>
            x.DocumentId==id&&x.CompanyId==currentCompany.CompanyId&&
            (!currentUser.IsClientUser ||
             ((x.Visibility==RecordVisibilities.Client||x.Visibility==RecordVisibilities.Both) &&
              ((x.RequestId.HasValue&&db.ImportRequests.Any(r=>r.RequestId==x.RequestId.Value&&r.ClientId==currentUser.ClientId)) ||
               (x.QuotationId.HasValue&&db.Quotations.Any(q=>q.QuotationId==x.QuotationId.Value&&q.ClientId==currentUser.ClientId)) ||
               (x.OperationId.HasValue&&db.ImportOperations.Any(o=>o.OperationId==x.OperationId.Value&&o.ClientId==currentUser.ClientId))))),ct);
        return e is null?null:await MapAsync(e,ct);
    }

    public async Task<IReadOnlyCollection<DocumentObservationDto>> GetObservationsAsync(long documentId,CancellationToken ct)
    {
        var document = await GetByIdAsync(documentId,ct);
        if(document is null)
            throw new BusinessException(ErrorCodes.NotFound,"Documento no encontrado.",404);

        var rows = await db.DocumentObservations.AsNoTracking()
            .Where(x=>x.DocumentId==documentId)
            .OrderByDescending(x=>x.ObservedAt)
            .ToListAsync(ct);

        return rows.Select(x=>new DocumentObservationDto(
            x.DocumentObservationId,
            x.DocumentId,
            x.Description,
            x.Status,
            x.ObservedAt,
            x.ObservedBy,
            x.ResolvedAt,
            x.ResolvedBy,
            x.ResolutionNote,
            currentUser.IsClientUser ? null : x.OperationTaskId,
            ConcurrencyToken.Encode(x.RowVer)))
            .ToList();
    }

    public async Task<(Stream Stream,string FileName,string ContentType)?> DownloadAsync(long id,CancellationToken ct)
    {
        var dto=await GetByIdAsync(id,ct);
        if(dto is null) return null;
        var e=await GetTrackedAsync(id,ct);
        if(e is null) return null;
        return (await storage.OpenReadAsync(e.StorageKey,ct),e.OriginalFileName,e.MimeType??"application/octet-stream");
    }

    public async Task<DocumentDto> UploadAsync(UploadDocumentRequest r,Stream stream,string originalFileName,string contentType,long fileSize,CancellationToken ct)
    {
        await ValidateContextAsync(r.RequestId,r.QuotationId,r.OperationId,ct);
        if(currentUser.IsClientUser && string.Equals(NormalizeVisibility(r.Visibility),RecordVisibilities.Internal,StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(ErrorCodes.Forbidden,"Un usuario cliente no puede cargar documentos INTERNAL.",403);
        await ValidateTypeAsync(r.DocumentTypeId,ct);
        await ValidateFileAsync(stream,originalFileName,contentType,fileSize,ct);
        ValidateDates(r.DocumentDate,r.ExpirationDate);

        var maxVersion=await db.Documents.IgnoreQueryFilters().AsNoTracking()
            .Where(x=>x.CompanyId==currentCompany.CompanyId&&x.RequestId==r.RequestId&&x.QuotationId==r.QuotationId&&x.OperationId==r.OperationId&&x.DocumentTypeId==r.DocumentTypeId)
            .Select(x=>(int?)x.VersionNo).MaxAsync(ct)??0;

        var stored=await storage.SaveAsync(stream,originalFileName,contentType,ct);
        var e=new DocumentRecord{
            CompanyId=currentCompany.CompanyId,RequestId=r.RequestId,QuotationId=r.QuotationId,OperationId=r.OperationId,
            DocumentTypeId=r.DocumentTypeId,FileName=stored.FileName,OriginalFileName=Path.GetFileName(originalFileName),
            FileExtension=Path.GetExtension(originalFileName).ToLowerInvariant(),MimeType=contentType,StorageProvider=stored.Provider,
            StorageKey=stored.StorageKey,FileSizeBytes=stored.SizeBytes,Sha256Hash=stored.Sha256,VersionNo=maxVersion+1,
            DocumentStatus=DocumentStatuses.PendingReview,Visibility=NormalizeVisibility(r.Visibility),DocumentDate=r.DocumentDate?.Date,ExpirationDate=r.ExpirationDate?.Date,
            Notes=Clean(r.Notes),UploadedAt=DateTime.UtcNow,UploadedBy=currentUser.UserId,IsDeleted=false
        };
        db.Documents.Add(e); await db.SaveChangesAsync(ct);

        if(e.OperationId.HasValue){
            AddEvent(e.OperationId.Value,"DOCUMENT_UPLOADED","Documento cargado",$"{e.OriginalFileName} V{e.VersionNo} pendiente de validación.");
            await db.SaveChangesAsync(ct);
            await RefreshOperationDocumentaryStatusAsync(e.OperationId.Value,ct);
        }

        return await MapAsync(e,ct);
    }

    public async Task<DocumentDto?> ReplaceAsync(long documentId,Stream stream,string originalFileName,string contentType,long fileSize,string? notes,CancellationToken ct)
    {
        var old=await GetTrackedAsync(documentId,ct);
        if(old is null) return null;
        if(old.DocumentStatus is DocumentStatuses.Void or DocumentStatuses.Replaced)
            throw new BusinessException("DOCUMENT_REPLACE_NOT_ALLOWED","El documento no puede reemplazarse en su estado actual.",409);

        await ValidateFileAsync(stream,originalFileName,contentType,fileSize,ct);
        var stored=await storage.SaveAsync(stream,originalFileName,contentType,ct);

        await using var tx=await db.Database.BeginTransactionAsync(ct);
        old.DocumentStatus=DocumentStatuses.Replaced;

        var e=new DocumentRecord{
            CompanyId=old.CompanyId,RequestId=old.RequestId,QuotationId=old.QuotationId,OperationId=old.OperationId,
            DocumentTypeId=old.DocumentTypeId,FileName=stored.FileName,OriginalFileName=Path.GetFileName(originalFileName),
            FileExtension=Path.GetExtension(originalFileName).ToLowerInvariant(),MimeType=contentType,StorageProvider=stored.Provider,
            StorageKey=stored.StorageKey,FileSizeBytes=stored.SizeBytes,Sha256Hash=stored.Sha256,VersionNo=old.VersionNo+1,
            DocumentStatus=DocumentStatuses.PendingReview,Visibility=old.Visibility,DocumentDate=old.DocumentDate,ExpirationDate=old.ExpirationDate,
            Notes=Clean(notes)??old.Notes,UploadedAt=DateTime.UtcNow,UploadedBy=currentUser.UserId,ReplacedDocumentId=old.DocumentId,IsDeleted=false
        };
        db.Documents.Add(e);

        var openObs=await db.DocumentObservations.Where(x=>x.DocumentId==old.DocumentId&&x.Status==DocumentObservationStatuses.Open).ToListAsync(ct);
        foreach(var o in openObs){
            o.Status=DocumentObservationStatuses.Resolved;o.ResolvedAt=DateTime.UtcNow;o.ResolvedBy=currentUser.UserId;o.ResolutionNote="Documento reemplazado por nueva versión.";
            if(o.OperationTaskId.HasValue){
                var task=await db.OperationTasks.SingleOrDefaultAsync(x=>x.OperationTaskId==o.OperationTaskId.Value,ct);
                if(task is not null && task.TaskStatus!=OperationTaskStatuses.Completed){task.TaskStatus=OperationTaskStatuses.Completed;task.CompletedAt=DateTime.UtcNow;task.CompletedByUserId=currentUser.UserId;}
            }
        }

        await db.SaveChangesAsync(ct);
        if(e.OperationId.HasValue){
            AddEvent(e.OperationId.Value,"DOCUMENT_REPLACED","Documento reemplazado",$"{old.OriginalFileName} V{old.VersionNo} → V{e.VersionNo}.");
            await db.SaveChangesAsync(ct);
            await RefreshOperationDocumentaryStatusAsync(e.OperationId.Value,ct);
        }
        await tx.CommitAsync(ct);
        return await MapAsync(e,ct);
    }

    public async Task<DocumentDto?> ValidateAsync(long id,ValidateDocumentRequest r,CancellationToken ct)
    {
        var e=await GetTrackedAsync(id,ct); if(e is null)return null;
        db.Entry(e).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(r.RowVersion);
        if(e.DocumentStatus is DocumentStatuses.Void or DocumentStatuses.Replaced) throw new BusinessException("DOCUMENT_VALIDATE_NOT_ALLOWED","No puede validar un documento VOID/REPLACED.",409);
        if(e.ExpirationDate.HasValue&&e.ExpirationDate.Value.Date<DateTime.Today) throw new BusinessException("DOCUMENT_EXPIRED","El documento está vencido.",409);

        e.DocumentStatus=DocumentStatuses.Valid;
        var obs=await db.DocumentObservations.Where(x=>x.DocumentId==id&&x.Status==DocumentObservationStatuses.Open).ToListAsync(ct);
        foreach(var o in obs){o.Status=DocumentObservationStatuses.Resolved;o.ResolvedAt=DateTime.UtcNow;o.ResolvedBy=currentUser.UserId;o.ResolutionNote=Clean(r.Comment)??"Documento validado.";}
        await db.SaveChangesAsync(ct);

        if(e.OperationId.HasValue){AddEvent(e.OperationId.Value,"DOCUMENT_VALIDATED","Documento validado",Clean(r.Comment)??e.OriginalFileName);await db.SaveChangesAsync(ct);await RefreshOperationDocumentaryStatusAsync(e.OperationId.Value,ct);}
        return await MapAsync(e,ct);
    }

    public async Task<DocumentObservationDto?> ObserveAsync(long id,ObserveDocumentRequest r,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(r.Observation)) throw new BusinessException(ErrorCodes.Validation,"La observación es obligatoria.",400);
        var e=await GetTrackedAsync(id,ct); if(e is null)return null;
        db.Entry(e).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(r.RowVersion);
        if(e.DocumentStatus is DocumentStatuses.Void or DocumentStatuses.Replaced) throw new BusinessException("DOCUMENT_OBSERVE_NOT_ALLOWED","No puede observar un documento VOID/REPLACED.",409);

        await using var tx=await db.Database.BeginTransactionAsync(ct);
        long? taskId=null;
        if(e.OperationId.HasValue){
            var op=await db.ImportOperations.AsNoTracking().SingleAsync(x=>x.OperationId==e.OperationId.Value,ct);
            var task=new OperationTask{CompanyId=currentCompany.CompanyId,OperationId=op.OperationId,WorkflowStageId=op.CurrentStageId,
                Title=$"Corregir documento: {e.OriginalFileName}",Description=r.Observation.Trim(),TaskStatus=OperationTaskStatuses.Pending,
                Priority=TaskPriorities.High,AssignedUserId=op.ResponsibleUserId,IsMandatory=true,CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
            db.OperationTasks.Add(task);await db.SaveChangesAsync(ct);taskId=task.OperationTaskId;
        }

        var o=new DocumentObservation{DocumentId=id,Description=r.Observation.Trim(),Status=DocumentObservationStatuses.Open,
            ObservedAt=DateTime.UtcNow,ObservedBy=currentUser.UserId,OperationTaskId=taskId};
        db.DocumentObservations.Add(o);e.DocumentStatus=DocumentStatuses.Observed;await db.SaveChangesAsync(ct);

        if(e.OperationId.HasValue){AddEvent(e.OperationId.Value,"DOCUMENT_OBSERVED","Documento observado",r.Observation.Trim());await db.SaveChangesAsync(ct);await RefreshOperationDocumentaryStatusAsync(e.OperationId.Value,ct);}
        await tx.CommitAsync(ct);
        return MapObservation(o);
    }

    public async Task<DocumentObservationDto?> ResolveObservationAsync(long id,long observationId,ResolveDocumentObservationRequest r,CancellationToken ct)
    {
        var e=await GetTrackedAsync(id,ct);if(e is null)return null;
        var o=await db.DocumentObservations.SingleOrDefaultAsync(x=>x.DocumentId==id&&x.DocumentObservationId==observationId,ct);if(o is null)return null;
        db.Entry(o).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(r.RowVersion);
        if(o.Status!=DocumentObservationStatuses.Open) throw new BusinessException("DOCUMENT_OBSERVATION_CLOSED","La observación ya fue cerrada.",409);
        if(string.IsNullOrWhiteSpace(r.ResolutionNote)) throw new BusinessException(ErrorCodes.Validation,"Debe indicar la resolución.",400);

        o.Status=DocumentObservationStatuses.Resolved;o.ResolvedAt=DateTime.UtcNow;o.ResolvedBy=currentUser.UserId;o.ResolutionNote=r.ResolutionNote.Trim();
        if(o.OperationTaskId.HasValue){
            var task=await db.OperationTasks.SingleOrDefaultAsync(x=>x.OperationTaskId==o.OperationTaskId.Value,ct);
            if(task is not null){task.TaskStatus=OperationTaskStatuses.Completed;task.CompletedAt=DateTime.UtcNow;task.CompletedByUserId=currentUser.UserId;}
        }
        await db.SaveChangesAsync(ct);
        return MapObservation(o);
    }

    public async Task<DocumentDto?> VoidAsync(long id,VoidDocumentRequest r,CancellationToken ct)
    {
        var e=await GetTrackedAsync(id,ct);if(e is null)return null;
        db.Entry(e).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(r.RowVersion);
        if(string.IsNullOrWhiteSpace(r.Reason)) throw new BusinessException(ErrorCodes.Validation,"Motivo obligatorio.",400);
        e.DocumentStatus=DocumentStatuses.Void;await db.SaveChangesAsync(ct);
        if(e.OperationId.HasValue){AddEvent(e.OperationId.Value,"DOCUMENT_VOID","Documento anulado",r.Reason.Trim());await db.SaveChangesAsync(ct);await RefreshOperationDocumentaryStatusAsync(e.OperationId.Value,ct);}
        return await MapAsync(e,ct);
    }

    public async Task<OperationDocumentStatusDto> GetOperationRequirementsAsync(long operationId,CancellationToken ct)
    {
        var op=await db.ImportOperations.AsNoTracking().SingleOrDefaultAsync(x=>
            x.OperationId==operationId&&
            x.CompanyId==currentCompany.CompanyId&&
            (!currentUser.IsClientUser||x.ClientId==currentUser.ClientId),ct)
            ?? throw new BusinessException(ErrorCodes.NotFound,"Operación no encontrada.",404);
        var stage=await db.WorkflowStages.AsNoTracking().SingleAsync(x=>x.WorkflowStageId==op.CurrentStageId,ct);

        var reqs=await (from r in db.StageDocumentRequirements.AsNoTracking()
                        join dt in db.DocumentTypes.AsNoTracking() on r.DocumentTypeId equals dt.DocumentTypeId
                        where r.CompanyId==currentCompany.CompanyId&&r.WorkflowStageId==op.CurrentStageId&&r.IsActive
                        select new {r,dt}).ToListAsync(ct);

        var results=new List<StageDocumentRequirementDto>();
        foreach(var x in reqs){
            var valid=await db.Documents.AsNoTracking()
                .Where(d=>d.CompanyId==currentCompany.CompanyId&&d.OperationId==operationId&&d.DocumentTypeId==x.r.DocumentTypeId&&d.DocumentStatus==DocumentStatuses.Valid&&(!d.ExpirationDate.HasValue||d.ExpirationDate.Value.Date>=DateTime.Today))
                .OrderByDescending(d=>d.VersionNo).FirstOrDefaultAsync(ct);
            results.Add(new(op.CurrentStageId,stage.Code,stage.Name,x.r.DocumentTypeId,x.dt.Code,x.dt.Name,x.r.IsMandatory,valid!=null,valid?.DocumentId));
        }

        var required=results.Count(x=>x.IsMandatory);
        var validCount=results.Count(x=>x.IsMandatory&&x.IsSatisfied);

        var mandatoryTypeIds=results
            .Where(x=>x.IsMandatory)
            .Select(x=>x.DocumentTypeId)
            .Distinct()
            .ToList();

        var observed=mandatoryTypeIds.Count==0
            ? 0
            : await db.Documents.AsNoTracking().CountAsync(x=>
                x.CompanyId==currentCompany.CompanyId&&
                x.OperationId==operationId&&
                !x.IsDeleted&&
                mandatoryTypeIds.Contains(x.DocumentTypeId)&&
                x.DocumentStatus==DocumentStatuses.Observed,ct);

        return new(operationId,op.DocumentaryStatus,required,validCount,required-validCount,observed,results);
    }

    public async Task<OperationDocumentStatusDto> RefreshOperationDocumentaryStatusAsync(long operationId,CancellationToken ct)
    {
        var expired=await db.Documents
            .Where(x=>
                x.CompanyId==currentCompany.CompanyId&&
                x.OperationId==operationId&&
                !x.IsDeleted&&
                x.DocumentStatus==DocumentStatuses.Valid&&
                x.ExpirationDate.HasValue&&
                x.ExpirationDate.Value.Date<DateTime.Today)
            .ToListAsync(ct);
        foreach(var d in expired)d.DocumentStatus=DocumentStatuses.Expired;
        if(expired.Count>0)await db.SaveChangesAsync(ct);

        var status=await GetOperationRequirementsAsync(operationId,ct);

        var newStatus=status.ObservedCount>0
            ? DocumentaryStatuses.Observed
            : status.MissingCount==0
                ? DocumentaryStatuses.Complete
                : DocumentaryStatuses.Pending;

        await operationService.SetDocumentaryStatusAsync(
            operationId,
            newStatus,
            $"Recalculado por Documents: {status.ValidCount}/{status.RequiredCount} requisitos obligatorios válidos.",
            ct);
        return await GetOperationRequirementsAsync(operationId,ct);
    }

    private async Task ValidateContextAsync(long? requestId,long? quotationId,long? operationId,CancellationToken ct)
    {
        var count=(requestId.HasValue?1:0)+(quotationId.HasValue?1:0)+(operationId.HasValue?1:0);
        if(count!=1) throw new BusinessException(ErrorCodes.Validation,"El documento debe pertenecer exactamente a REQUEST, QUOTATION u OPERATION.",400);
        if(requestId.HasValue&&!await db.ImportRequests.AsNoTracking().AnyAsync(x=>x.RequestId==requestId&&x.CompanyId==currentCompany.CompanyId&&(!currentUser.IsClientUser||x.ClientId==currentUser.ClientId),ct)) throw new BusinessException(ErrorCodes.NotFound,"Solicitud no encontrada.",404);
        if(quotationId.HasValue&&!await db.Quotations.AsNoTracking().AnyAsync(x=>x.QuotationId==quotationId&&x.CompanyId==currentCompany.CompanyId&&(!currentUser.IsClientUser||x.ClientId==currentUser.ClientId),ct)) throw new BusinessException(ErrorCodes.NotFound,"Cotización no encontrada.",404);
        if(operationId.HasValue&&!await db.ImportOperations.AsNoTracking().AnyAsync(x=>x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId&&(!currentUser.IsClientUser||x.ClientId==currentUser.ClientId),ct)) throw new BusinessException(ErrorCodes.NotFound,"Operación no encontrada.",404);
    }

    private async Task ValidateTypeAsync(int id,CancellationToken ct)
    {
        if(!await db.DocumentTypes.AsNoTracking().AnyAsync(x=>x.DocumentTypeId==id&&x.IsActive&&(x.CompanyId==null||x.CompanyId==currentCompany.CompanyId),ct))
            throw new BusinessException(ErrorCodes.Validation,"Tipo de documento inválido.",400);
    }

    private static async Task ValidateFileAsync(Stream stream,string name,string contentType,long size,CancellationToken ct)
    {
        var ext=Path.GetExtension(Path.GetFileName(name)).ToLowerInvariant();
        if(!AllowedExtensions.Contains(ext)) throw new BusinessException("FILE_EXTENSION_NOT_ALLOWED",$"Extensión {ext} no permitida.",400);
        if(size<=0||size>MaxFileBytes) throw new BusinessException("FILE_SIZE_INVALID","El archivo debe tener tamaño mayor a 0 y máximo 25 MB.",400);
        if(string.IsNullOrWhiteSpace(contentType)) throw new BusinessException("MIME_REQUIRED","Content-Type obligatorio.",400);

        var expectedMime=ext switch{
            ".pdf"=>"application/pdf",
            ".jpg" or ".jpeg"=>"image/jpeg",
            ".png"=>"image/png",
            ".xlsx"=>"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx"=>"application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls"=>"application/vnd.ms-excel",
            ".doc"=>"application/msword",
            ".txt"=>"text/plain",
            _=>"application/octet-stream"
        };

        if(!string.Equals(contentType,expectedMime,StringComparison.OrdinalIgnoreCase) &&
           !(ext==".txt" && contentType.StartsWith("text/",StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException("MIME_MISMATCH",$"El MIME {contentType} no corresponde a la extensión {ext}.",400);

        if(!stream.CanSeek) return;
        var originalPosition=stream.Position;
        var header=new byte[Math.Min(8,(int)Math.Min(size,8))];
        var read=await stream.ReadAsync(header.AsMemory(0,header.Length),ct);
        stream.Position=originalPosition;

        bool Match(params byte[] signature)=>read>=signature.Length&&signature.Select((b,i)=>header[i]==b).All(x=>x);
        var signatureOk=ext switch{
            ".pdf"=>Match(0x25,0x50,0x44,0x46),
            ".jpg" or ".jpeg"=>Match(0xFF,0xD8,0xFF),
            ".png"=>Match(0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A),
            ".xlsx" or ".docx"=>Match(0x50,0x4B,0x03,0x04),
            ".xls" or ".doc"=>Match(0xD0,0xCF,0x11,0xE0,0xA1,0xB1,0x1A,0xE1),
            ".txt"=>true,
            _=>false
        };
        if(!signatureOk) throw new BusinessException("FILE_SIGNATURE_MISMATCH","La firma binaria del archivo no corresponde al tipo declarado.",400);
    }

    private static void ValidateDates(DateTime? date,DateTime? exp){if(date.HasValue&&exp.HasValue&&exp.Value.Date<date.Value.Date)throw new BusinessException("INVALID_DOCUMENT_DATES","ExpirationDate no puede ser anterior a DocumentDate.",400);}
    private Task<DocumentRecord?> GetTrackedAsync(long id,CancellationToken ct)=>
        db.Documents.SingleOrDefaultAsync(x=>
            x.DocumentId==id&&
            x.CompanyId==currentCompany.CompanyId&&
            (!currentUser.IsClientUser ||
             ((x.Visibility==RecordVisibilities.Client||x.Visibility==RecordVisibilities.Both) &&
              ((x.RequestId.HasValue&&db.ImportRequests.Any(r=>r.RequestId==x.RequestId.Value&&r.ClientId==currentUser.ClientId)) ||
               (x.QuotationId.HasValue&&db.Quotations.Any(q=>q.QuotationId==x.QuotationId.Value&&q.ClientId==currentUser.ClientId)) ||
               (x.OperationId.HasValue&&db.ImportOperations.Any(o=>o.OperationId==x.OperationId.Value&&o.ClientId==currentUser.ClientId))))),ct);

    private async Task<DocumentDto> MapAsync(DocumentRecord e,CancellationToken ct)
    {
        var t=await db.DocumentTypes.AsNoTracking().SingleAsync(x=>x.DocumentTypeId==e.DocumentTypeId,ct);
        return new(e.DocumentId,e.RequestId,e.QuotationId,e.OperationId,e.DocumentTypeId,t.Code,t.Name,e.OriginalFileName,e.MimeType,e.FileSizeBytes,e.Sha256Hash,e.VersionNo,e.DocumentStatus,e.Visibility,e.DocumentDate,e.ExpirationDate,e.Notes,e.UploadedAt,e.UploadedBy,e.ReplacedDocumentId,ConcurrencyToken.Encode(e.RowVer));
    }

    private static DocumentObservationDto MapObservation(DocumentObservation o)=>new(o.DocumentObservationId,o.DocumentId,o.Description,o.Status,o.ObservedAt,o.ObservedBy,o.ResolvedAt,o.ResolvedBy,o.ResolutionNote,o.OperationTaskId,ConcurrencyToken.Encode(o.RowVer));

    private void AddEvent(long operationId,string type,string title,string description)=>db.OperationEvents.Add(new OperationEvent{CompanyId=currentCompany.CompanyId,OperationId=operationId,EventType=type,EventDate=DateTime.UtcNow,Title=title,Description=description,SourceType=OperationEventSources.System,CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId});

    private static string NormalizeVisibility(string? value)
    {
        var normalized=string.IsNullOrWhiteSpace(value)?RecordVisibilities.Both:value.Trim().ToUpperInvariant();
        if(normalized is not(RecordVisibilities.Internal or RecordVisibilities.Client or RecordVisibilities.Both))
            throw new BusinessException(ErrorCodes.Validation,"Visibility inválida.",400);
        return normalized;
    }

    private static string? Clean(string? x)=>string.IsNullOrWhiteSpace(x)?null:x.Trim();
}

import fs from 'node:fs';
import assert from 'node:assert/strict';

const read = (p) => fs.readFileSync(p, 'utf8');

const controller = read('src/ImportOps.Api/Controllers/WorkflowConfigurationController.cs');
const models = read('src/ImportOps.Application/Workflow/WorkflowConfigurationModels.cs');
const iface = read('src/ImportOps.Application/Workflow/IWorkflowConfigurationService.cs');
const service = read('src/ImportOps.Infrastructure/Services/WorkflowConfigurationService.cs');
const di = read('src/ImportOps.Infrastructure/DependencyInjection.cs');
const operationService = read('src/ImportOps.Infrastructure/Services/ImportOperationService.cs');

assert.match(controller, /Route\("api\/configuration\/workflow"\)/);
assert.match(controller, /RequirePermission\(Permissions\.ConfigurationView\)/);
assert.match(controller, /HttpGet\("task-templates"\)/);
assert.match(controller, /HttpPost\("task-templates"\)/);
assert.match(controller, /HttpPut\("task-templates\/\{id:long\}"\)/);
assert.match(controller, /HttpPatch\("task-templates\/\{id:long\}\/active"\)/);
assert.match(controller, /HttpGet\("task-templates\/\{id:long\}\/delete-check"\)/);
assert.match(controller, /HttpDelete\("task-templates\/\{id:long\}"\)/);
assert.match(models, /WorkflowStageId/);
assert.match(models, /DueDaysOffset/);
assert.match(models, /DisplayOrder/);
assert.match(models, /IsMandatory/);
assert.match(iface, /SearchTaskTemplatesAsync/);
assert.match(service, /OperationTasks/);
assert.match(service, /StageTaskTemplateId/);
assert.match(service, /No se puede eliminar/);
assert.match(service, /DueDaysOffset.*< 0/s);
assert.match(di, /IWorkflowConfigurationService, WorkflowConfigurationService/);
assert.match(operationService, /AssignedUserId = operation\.ResponsibleUserId/);
assert.match(operationService, /Title = template\.Title/);
assert.match(operationService, /Description = template\.Description/);
console.log('workflow task administration contract: PASS');

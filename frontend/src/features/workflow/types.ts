export type WorkflowTaskTemplate = {
  stageTaskTemplateId: number;
  workflowStageId: number;
  stageCode: string;
  stageName: string;
  title: string;
  description?: string | null;
  isMandatory: boolean;
  dueDaysOffset?: number | null;
  displayOrder: number;
  isActive: boolean;
  generatedTasksCount: number;
};

export type WorkflowTaskTemplatePage = {
  items: WorkflowTaskTemplate[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
};

export type SaveWorkflowTaskTemplateRequest = {
  workflowStageId: number;
  title: string;
  description?: string | null;
  isMandatory: boolean;
  dueDaysOffset?: number | null;
  displayOrder: number;
};

export type WorkflowTaskTemplateDeleteCheck = {
  canDelete: boolean;
  generatedTasksCount: number;
  message: string;
};

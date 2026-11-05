import { api, apiRequest } from "@/lib/api/http";
import type {
  SaveWorkflowTaskTemplateRequest,
  WorkflowTaskTemplate,
  WorkflowTaskTemplateDeleteCheck,
  WorkflowTaskTemplatePage
} from "./types";

export const workflowConfigurationApi = {
  taskTemplates: (params: {
    page: number;
    pageSize: number;
    workflowStageId?: number;
    search?: string;
    status?: string;
  }) => {
    const q = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== "") q.set(key, String(value));
    });
    return api.get<WorkflowTaskTemplatePage>(`/api/configuration/workflow/task-templates?${q}`);
  },
  createTaskTemplate: (request: SaveWorkflowTaskTemplateRequest) =>
    api.post<WorkflowTaskTemplate>("/api/configuration/workflow/task-templates", request),
  updateTaskTemplate: (id: number, request: SaveWorkflowTaskTemplateRequest) =>
    api.put<WorkflowTaskTemplate>(`/api/configuration/workflow/task-templates/${id}`, request),
  setTaskTemplateActive: (id: number, isActive: boolean) =>
    apiRequest<void>(`/api/configuration/workflow/task-templates/${id}/active`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isActive })
    }),
  deleteCheck: (id: number) =>
    api.get<WorkflowTaskTemplateDeleteCheck>(`/api/configuration/workflow/task-templates/${id}/delete-check`),
  deleteTaskTemplate: (id: number) =>
    api.delete<void>(`/api/configuration/workflow/task-templates/${id}`)
};

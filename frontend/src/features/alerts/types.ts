export type AlertSeverity="INFO"|"WARNING"|"CRITICAL";
export type AlertStatus="ACTIVE"|"RESOLVED";
export type AlertItem={alertId:number;alertType:string;severity:AlertSeverity;status:AlertStatus;title:string;message:string;operationId?:number|null;operationNo?:string|null;clientId?:number|null;clientName?:string|null;sourceType:string;sourceId?:number|null;actionPath?:string|null;triggeredAt:string;isRead:boolean};
export type AlertPage={items:AlertItem[];total:number;page:number;pageSize:number};
export type AlertSummary={active:number;unread:number;critical:number;warning:number};
export type AlertRule={alertRuleId:number;alertType:string;name:string;description?:string|null;severity:AlertSeverity;audience:"INTERNAL"|"CLIENT"|"BOTH";thresholdHours?:number|null;isActive:boolean};

export type UpdateAlertRuleRequest = { severity: AlertSeverity; audience: AlertRule["audience"]; thresholdHours?: number | null; isActive: boolean };

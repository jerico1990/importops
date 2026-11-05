import { api } from "@/lib/api/http";
import type { AlertPage,AlertRule,AlertSeverity,AlertSummary } from "./types";
export const alertsApi={
 list:(p?:{status?:string;severity?:string;unread?:boolean;page?:number;pageSize?:number})=>{const q=new URLSearchParams();Object.entries(p??{}).forEach(([k,v])=>v!==undefined&&q.set(k,String(v)));return api.get<AlertPage>(`/api/alerts?${q}`)},
 summary:()=>api.get<AlertSummary>("/api/alerts/summary"),
 read:(id:number)=>api.post<void>(`/api/alerts/${id}/read`),readAll:()=>api.post<void>("/api/alerts/read-all"),dismiss:(id:number)=>api.post<void>(`/api/alerts/${id}/dismiss`),
 rules:()=>api.get<AlertRule[]>("/api/alerts/rules"),updateRule:(id:number,r:{severity:AlertSeverity;audience:"INTERNAL"|"CLIENT"|"BOTH";thresholdHours?:number|null;isActive:boolean})=>api.put<AlertRule>(`/api/alerts/rules/${id}`,r),evaluate:()=>api.post<void>("/api/alerts/evaluate")
};

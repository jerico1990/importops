import { api, apiRequest } from "@/lib/api/http";
import type { CatalogDeleteCheck, CatalogKey, CatalogPage, CatalogSaveRequest, CatalogAdminItem } from "./types";
export const configurationApi={
 list:(catalog:CatalogKey,p:{page:number;pageSize:number;search?:string;status?:string;countryCode?:string;portType?:string})=>{const q=new URLSearchParams();Object.entries(p).forEach(([k,v])=>{if(v!==undefined&&v!=="")q.set(k,String(v))});return api.get<CatalogPage>(`/api/configuration/catalogs/${catalog}?${q}`)},
 create:(catalog:CatalogKey,r:CatalogSaveRequest)=>api.post<CatalogAdminItem>(`/api/configuration/catalogs/${catalog}`,r),
 update:(catalog:CatalogKey,id:string,r:CatalogSaveRequest)=>api.put<CatalogAdminItem>(`/api/configuration/catalogs/${catalog}/${encodeURIComponent(id)}`,r),
 active:(catalog:CatalogKey,id:string,isActive:boolean)=>apiRequest<void>(`/api/configuration/catalogs/${catalog}/${encodeURIComponent(id)}/active`,{method:"PATCH",headers:{"Content-Type":"application/json"},body:JSON.stringify({isActive})}),
 deleteCheck:(catalog:CatalogKey,id:string)=>api.get<CatalogDeleteCheck>(`/api/configuration/catalogs/${catalog}/${encodeURIComponent(id)}/delete-check`),
 delete:(catalog:CatalogKey,id:string)=>api.delete<void>(`/api/configuration/catalogs/${catalog}/${encodeURIComponent(id)}`)
};

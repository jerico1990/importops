import {
  Bell,
  Building2,
  ChartNoAxesCombined,
  ClipboardList,
  FileText,
  Landmark,
  PackageSearch,
  ReceiptText,
  Settings,
  Ship,
  Users,
  Warehouse
} from "lucide-react";
import { Permissions } from "@/types/auth";

export type NavItem = {
  label: string;
  path?: string;
  icon?: typeof Users;
  permissions?: string[];
  children?: NavItem[];
};

export type NavGroup = {
  label: string;
  items: NavItem[];
};

export const navigation: NavGroup[] = [
  { label: "Principal", items: [
    { label: "Inicio", path: "/", icon: ChartNoAxesCombined },
    { label: "Alertas", path: "/alertas", icon: Bell, permissions: [Permissions.AlertsView] }
  ]},
  { label: "Comercial", items: [
    { label: "Solicitudes", path: "/solicitudes", icon: ClipboardList, permissions: [Permissions.RequestsView] },
    { label: "Cotizaciones", path: "/cotizaciones", icon: ReceiptText, permissions: [Permissions.QuotationsView] }
  ]},
  { label: "Operaciones", items: [
    { label: "Importaciones", path: "/operaciones", icon: PackageSearch, permissions: [Permissions.OperationsView] },
    { label: "Embarques", path: "/embarques", icon: Ship, permissions: [Permissions.ShipmentsView] },
    { label: "Aduanas", path: "/aduanas", icon: Landmark, permissions: [Permissions.CustomsView] },
    /*{ label: "Documentos", path: "/documentos", icon: FileText, permissions: [Permissions.DocumentsView] },*/
    { label: "Finanzas", path: "/finanzas", icon: ReceiptText, permissions: [Permissions.FinanceView] }
  ]},
  { label: "Administración", items: [
    { label: "Usuarios y accesos", path: "/seguridad", icon: Users, permissions: [Permissions.SecurityManage] },
    { label: "Maestros", icon: Building2, permissions: [Permissions.ClientsView, Permissions.SuppliersView], children: [
      { label: "Clientes", path: "/maestros/clientes", icon: Users, permissions: [Permissions.ClientsView] },
      { label: "Proveedores", path: "/maestros/proveedores", icon: Warehouse, permissions: [Permissions.SuppliersView] }
    ]},
    { label: "Configuración", icon: Settings, permissions: [Permissions.ConfigurationView], children: [
      { label: "Catálogos", path: "/configuracion/catalogos", icon: Settings, permissions: [Permissions.ConfigurationView] },
      { label: "Tareas por etapa", path: "/configuracion/workflow/tareas", icon: ClipboardList, permissions: [Permissions.ConfigurationView] }
    ]}
  ]}
];

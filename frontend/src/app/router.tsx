import {
  createBrowserRouter
} from "react-router-dom";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { PermissionRoute } from "@/features/auth/PermissionRoute";
import { AppShell } from "@/components/layout/AppShell";
import { LoginPage } from "@/pages/auth/LoginPage";
import { HomePage } from "@/pages/dashboard/HomePage";
import { OperationsPage } from "@/pages/operations/OperationsPage";
import { OperationDetailPage } from "@/pages/operations/OperationDetailPage";
import { ClientsPage } from "@/pages/clients/ClientsPage";
import { ClientDetailPage } from "@/pages/clients/ClientDetailPage";
import { SuppliersPage } from "@/pages/suppliers/SuppliersPage";
import { SupplierDetailPage } from "@/pages/suppliers/SupplierDetailPage";
import { RequestsPage } from "@/pages/requests/RequestsPage";
import { RequestDetailPage } from "@/pages/requests/RequestDetailPage";
import { QuotationsPage } from "@/pages/quotations/QuotationsPage";
import { QuotationDetailPage } from "@/pages/quotations/QuotationDetailPage";
import { ShipmentsPage } from "@/pages/shipments/ShipmentsPage";
import { ShipmentDetailPage } from "@/pages/shipments/ShipmentDetailPage";
import { DocumentsPage } from "@/pages/documents/DocumentsPage";
import { DocumentDetailPage } from "@/pages/documents/DocumentDetailPage";
import { CustomsPage } from "@/pages/customs/CustomsPage";
import { CustomsDetailPage } from "@/pages/customs/CustomsDetailPage";
import { FinancePage } from "@/pages/finance/FinancePage";
import { FinanceDetailPage } from "@/pages/finance/FinanceDetailPage";
import { ClientLiquidationPage } from "@/pages/finance/ClientLiquidationPage";
function OperationEntry(){ return <OperationDetailPage/>; }
import { UsersPage } from "@/pages/security/UsersPage";
import { ProfilePage } from "@/pages/profile/ProfilePage";
import { ForbiddenPage } from "@/pages/shared/ForbiddenPage";
import { AlertsPage } from "@/pages/alerts/AlertsPage";
import { Permissions } from "@/types/auth";
import { CatalogsPage } from "@/pages/configuration/CatalogsPage";
import { WorkflowTasksPage } from "@/pages/configuration/WorkflowTasksPage";

export const router = createBrowserRouter([
  {
    path: "/login",
    element: <LoginPage />
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppShell />,
        children: [
          {
            index: true,
            element: <HomePage />
          },
          {
            path: "403",
            element: <ForbiddenPage />
          },
          {
            element: (
              <PermissionRoute
                anyOf={[Permissions.OperationsView]}
              />
            ),
            children: [
              {
                path: "operaciones",
                element: <OperationsPage />
              },
              {
                path: "operaciones/:id",
                element: <OperationEntry />
              }
            ]
          },
          {
            element: (
              <PermissionRoute
                anyOf={[Permissions.ClientsView]}
              />
            ),
            children: [
              {
                path: "maestros/clientes",
                element: <ClientsPage />
              },
              {
                path: "maestros/clientes/:id",
                element: <ClientDetailPage />
              }
            ]
          },
          {
            element: (
              <PermissionRoute
                anyOf={[Permissions.SuppliersView]}
              />
            ),
            children: [
              {
                path: "maestros/proveedores",
                element: <SuppliersPage />
              },
              {
                path: "maestros/proveedores/:id",
                element: <SupplierDetailPage />
              }
            ]
          },
          {
            element: (
              <PermissionRoute
                anyOf={[Permissions.RequestsView]}
              />
            ),
            children: [
              {
                path: "solicitudes",
                element: <RequestsPage />
              },
              {
                path: "solicitudes/:id",
                element: <RequestDetailPage />
              }
            ]
          },
          {
            element: (
              <PermissionRoute
                anyOf={[Permissions.QuotationsView]}
              />
            ),
            children: [
              {
                path: "cotizaciones",
                element: <QuotationsPage />
              },
              {
                path: "cotizaciones/:id",
                element: <QuotationDetailPage />
              }
            ]
          },
          {
            element: (
              <PermissionRoute anyOf={[Permissions.ShipmentsView]} />
            ),
            children: [
              {
                path: "embarques",
                element: <ShipmentsPage />
              },
              {
                path: "embarques/:id",
                element: <ShipmentDetailPage />
              }
            ]
          },
          {
            element: <PermissionRoute anyOf={[Permissions.CustomsView]} />,
            children: [
              { path: "aduanas", element: <CustomsPage /> },
              { path: "aduanas/:id", element: <CustomsDetailPage /> }
            ]
          },
          {
            element: (
              <PermissionRoute anyOf={[Permissions.DocumentsView]} />
            ),
            children: [
              {
                path: "documentos",
                element: <DocumentsPage />
              },
              {
                path: "documentos/:id",
                element: <DocumentDetailPage />
              }
            ]
          },
          { path: "mi-liquidacion/:id", element: <ClientLiquidationPage /> },
          {
            element: <PermissionRoute anyOf={[Permissions.FinanceView]} />,
            children: [
              { path: "finanzas", element: <FinancePage /> },
              { path: "finanzas/:id", element: <FinanceDetailPage /> }
            ]
          },
          {
            element: (
              <PermissionRoute anyOf={[Permissions.SecurityManage]} />
            ),
            children: [
              {
                path: "seguridad",
                element: <UsersPage />
              }
            ]
          },
          {
            element: <PermissionRoute anyOf={[Permissions.AlertsView]} />,
            children: [{ path: "alertas", element: <AlertsPage /> }]
          },
          {
            element: <PermissionRoute anyOf={[Permissions.ConfigurationView]} />,
            children: [
              { path: "configuracion/catalogos", element: <CatalogsPage /> },
              { path: "configuracion/workflow/tareas", element: <WorkflowTasksPage /> }
            ]
          },
          {
            path: "perfil",
            element: <ProfilePage />
          }
        ]
      }
    ]
  }
]);

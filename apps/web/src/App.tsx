import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { hasSession } from "./auth/session";
import { AdminPage } from "./pages/AdminPage";
import { AuthForm } from "./pages/AuthForm";
import { InvestigationDetailPage } from "./pages/InvestigationDetailPage";
import { InvestigationHistoryPage } from "./pages/InvestigationHistoryPage";
import { ReviewQueuePage } from "./pages/ReviewQueuePage";
import { LandingPage } from "./pages/LandingPage";
import { NewInvestigationPage } from "./pages/NewInvestigationPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { WorkspaceHomePage } from "./pages/WorkspaceHomePage";
import { WorkspaceLayout } from "./pages/WorkspaceLayout";

function GuestOnly({ children }: { children: ReactNode }) {
  if (hasSession()) {
    return <Navigate to="/app" replace />;
  }

  return children;
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route
        path="/login"
        element={
          <GuestOnly>
            <AuthForm mode="login" />
          </GuestOnly>
        }
      />
      <Route
        path="/register"
        element={
          <GuestOnly>
            <AuthForm mode="register" />
          </GuestOnly>
        }
      />
      <Route path="/app" element={<WorkspaceLayout />}>
        <Route index element={<WorkspaceHomePage />} />
        <Route path="investigations" element={<InvestigationHistoryPage />} />
        <Route path="investigations/new" element={<NewInvestigationPage />} />
        <Route path="investigations/:id" element={<InvestigationDetailPage />} />
        <Route path="review" element={<ReviewQueuePage />} />
        <Route path="admin" element={<AdminPage />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

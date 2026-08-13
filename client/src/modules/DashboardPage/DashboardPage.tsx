import { Suspense, lazy } from 'react';
import { Loader2 } from 'lucide-react';

import { MetabaseDashboard } from './MetabaseDashboard';

// Lazy so Metabase deployments never download the in-app dashboard chunk
// (recharts and all).
const InAppDashboard = lazy(() =>
  import('./InAppDashboard').then((m) => ({ default: m.InAppDashboard }))
);

// Deployment-selected dashboard provider, mirroring the VITE_AUTH_PROVIDER
// pattern: "metabase" (default) keeps the existing iframe embed; "in_app"
// renders the built-in dashboard against /api/v1/dashboards/*.
export const DashboardPage = () => {
  const provider = import.meta.env.VITE_DASHBOARD_PROVIDER ?? 'metabase';

  if (provider === 'in_app') {
    return (
      <Suspense
        fallback={
          <div className="flex h-full w-full items-center justify-center p-6">
            <Loader2 className="h-10 w-10 animate-spin" />
          </div>
        }
      >
        <InAppDashboard />
      </Suspense>
    );
  }

  return <MetabaseDashboard />;
};

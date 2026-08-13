import { useState } from 'react';

import { PageContainer } from '@/components/PageContainer';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth } from '@/providers/GlobalProvider';

import { DeduplicationTab } from './DeduplicationTab';
import { OverviewTab } from './OverviewTab';
import { FilterBar } from './components/FilterBar';
import { DashboardApiParams, DashboardPeriod, DisplayCurrency } from './types';

export const InAppDashboard = () => {
  const { organization, deploymentSettings } = useAuth();

  // Default: rolling last 30 days (committee-confirmed)
  const [period, setPeriod] = useState<DashboardPeriod>('30d');
  const [myOrganizationOnly, setMyOrganizationOnly] = useState(false);
  // null = follow the deployment default until the user overrides for the session
  const [currencyOverride, setCurrencyOverride] =
    useState<DisplayCurrency | null>(null);

  const deploymentCurrency: DisplayCurrency =
    deploymentSettings?.dashboardDisplayCurrency === 'EUR' ? 'EUR' : 'USD';
  const currency = currencyOverride ?? deploymentCurrency;

  const params: DashboardApiParams = {
    period,
    displayCurrency: currency,
    // Own-org only; the server enforces that any other org id degrades to the
    // platform-wide aggregate (committee 3.1 — no cross-org drill).
    ...(myOrganizationOnly && organization?.id
      ? { organizationId: organization.id }
      : {}),
  };

  return (
    <PageContainer
      pageTitle="Dashboard"
      pageSubtitle="Programme overview and deduplication effectiveness."
    >
      <FilterBar
        period={period}
        onPeriodChange={setPeriod}
        myOrganizationOnly={myOrganizationOnly}
        onMyOrganizationOnlyChange={setMyOrganizationOnly}
        currency={currency}
        onCurrencyChange={setCurrencyOverride}
      />

      {/* Filter state persists across tab switches by design. */}
      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="deduplication">Deduplication</TabsTrigger>
        </TabsList>
        <TabsContent value="overview">
          <OverviewTab params={params} />
        </TabsContent>
        <TabsContent value="deduplication">
          <DeduplicationTab params={params} />
        </TabsContent>
      </Tabs>
    </PageContainer>
  );
};

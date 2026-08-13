import { useState } from 'react';

import { PageContainer } from '@/components/PageContainer';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth } from '@/providers/GlobalProvider';

import { DeduplicationTab } from './DeduplicationTab';
import { OverviewTab } from './OverviewTab';
import { FilterBar } from './components/FilterBar';
import { DashboardApiParams, DashboardPeriod, DisplayCurrency } from './types';

// SSO-only deployments (UNICEF) can't reach superadmin settings, so the
// default currency is baked in. Users flip the view with the FilterBar toggle.
const DEFAULT_DISPLAY_CURRENCY: DisplayCurrency = 'USD';

export const InAppDashboard = () => {
  const { organization } = useAuth();

  const [period, setPeriod] = useState<DashboardPeriod>('all-time');
  const [myOrganizationOnly, setMyOrganizationOnly] = useState(false);
  const [currency, setCurrency] = useState<DisplayCurrency>(
    DEFAULT_DISPLAY_CURRENCY
  );

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
        onCurrencyChange={setCurrency}
      />

      {/* Filter state persists across tab switches by design. */}
      {/* ring-0 overrides: no focus rings on dashboard chrome (user preference) */}
      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger
            value="overview"
            className="focus-visible:ring-0 focus-visible:ring-offset-0"
          >
            Overview
          </TabsTrigger>
          <TabsTrigger
            value="deduplication"
            className="focus-visible:ring-0 focus-visible:ring-offset-0"
          >
            Deduplication
          </TabsTrigger>
        </TabsList>
        <TabsContent
          value="overview"
          className="focus-visible:ring-0 focus-visible:ring-offset-0"
        >
          <OverviewTab params={params} />
        </TabsContent>
        <TabsContent
          value="deduplication"
          className="focus-visible:ring-0 focus-visible:ring-offset-0"
        >
          <DeduplicationTab params={params} />
        </TabsContent>
      </Tabs>
    </PageContainer>
  );
};

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import { PaginationContext, usePagination } from '@/helpers/pagination';
import { APP_ROUTE } from '@/helpers/constants';
import { ConfirmationDialog } from '@/components/ConfirmationDialog';
import { toast } from '@/components/ui/use-toast';
import {
  getReferralsExport,
  useReferralMutation,
  useReferrals,
  useReferralUsers,
} from '@/services/referrals/api';
import { Referral } from '@/services/referrals';
import { FilterDropdown } from '@/components/DataTable/FilterDropdown';
import { ReferralStatusDisplayNames } from '@/services/referrals/const';
import { useOrganizations } from '@/services/organizations/api';
import { DateRangePickerFilter } from '@/components/DataTable/DateRangePickerFilter';
import { OrgActivityFilterMap } from '@/services/organizations';
import { FilterByUrgencyButton } from '@/components/FilterByUrgencyButton';
import { UserPermission } from '@/services/users';
import { AdminRegionsFilter } from '@/components/DataTable/AdminRegionsFilter';

import { columns } from './columns';
import { downloadFile } from '@/helpers/common';
import { Button } from '@/components/ui/button';
import { FileDownIcon } from 'lucide-react';

export const ReceivedReferralsPage = () => {
  const navigate = useNavigate();
  const pagination = usePagination();
  const {
    currentPage,
    onPageChange,
    onPageSizeChange,
    onSortChange,
    onSearchChange,
  } = pagination;

  const [receivedReferralToDelete, setReceivedReferralToDelete] =
    useState<Referral | null>(null);
  const [receivedReferralsFilters, setReceivedReferralsFilters] = useState<
    Record<string, string>
  >({
    isDraft: 'false',
  });
  const [exportLoading, setExportLoading] = useState<boolean>(false);

  const { data: receivedReferralsData, isLoading: queryLoading } = useReferrals(
    {
      ...pagination,
      received: true,
      filters: receivedReferralsFilters,
    }
  );
  const { data: organizations, isFetched } = useOrganizations({
    currentPage: 1,
    pageSize: 999,
  });

  const { data: users, isFetched: usersFetched } = useReferralUsers({
    currentPage: 1,
    pageSize: 999,
    queryFilters: { permission: UserPermission.Referrals },
  });
  const { removeReferral } = useReferralMutation();

  const handleDeleteReferral = async () => {
    if (!receivedReferralToDelete) return;

    try {
      await removeReferral.mutateAsync(receivedReferralToDelete.id);
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Referral successfully deleted!',
      });
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Failed to delete referral.',
      });
    }
    setReceivedReferralToDelete(null);
  };

  const onExportReferralsClick = async (pagination: PaginationContext) => {
    setExportLoading(true);
    try {
      const exportedData = await getReferralsExport(
        {
          ...pagination,
          pageSize: 999,
          filters: receivedReferralsFilters,
        },
        true
      );
      downloadFile(exportedData, 'referrals-export');
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'An error has occured. Please try again.',
      });
    }
    setExportLoading(false);
  };

  const onReceivedReferralTableRowClick = (referralRow: Referral) =>
    navigate(`${APP_ROUTE.ReceivedReferrals}/${referralRow.id}`);

  return (
    <PageContainer
      pageTitle="Manage Received Referrals"
      pageSubtitle="On this page you can view the referrals that you have received from other organisations. You should review each referral and decide whether to accept or reject it."
      breadcrumbs={[
        { href: `${APP_ROUTE.ReceivedReferrals}`, name: 'Received Referrals' },
      ]}
      headerNode={
        <Button
          type="button"
          variant="outline"
          isLoading={exportLoading}
          disabled={exportLoading}
          onClick={() => onExportReferralsClick(pagination)}
        >
          <FileDownIcon className="mr-2 size-5" />
          Export Referrals
        </Button>
      }
    >
      <DataTable
        data={receivedReferralsData?.data ?? []}
        pagination={receivedReferralsData?.meta}
        isQueryLoading={queryLoading}
        currentPage={currentPage}
        pageClicked={onPageChange}
        pageSizeClicked={onPageSizeChange}
        headerClicked={onSortChange}
        onSearchChange={onSearchChange}
        columns={columns(setReceivedReferralToDelete)}
        onRowClick={onReceivedReferralTableRowClick}
        tableFilterNodes={
          <div className="flex flex-wrap gap-4">
            <FilterByUrgencyButton
              currentFilters={receivedReferralsFilters}
              filterName="isUrgent"
              setCurrentFilters={setReceivedReferralsFilters}
              label="Show only urgent"
            />
            <FilterDropdown
              currentFilters={receivedReferralsFilters}
              filterName="focalPointId[in]"
              setCurrentFilters={setReceivedReferralsFilters}
              title="Filter by Focal Point"
              options={
                usersFetched
                  ? users!.data.map((user) => ({
                      label: `${user.firstName} ${user.lastName}`,
                      value: user.id,
                    }))
                  : []
              }
            />
            <FilterDropdown
              currentFilters={receivedReferralsFilters}
              filterName="isRejected=false,status[in]"
              setCurrentFilters={setReceivedReferralsFilters}
              title="Filter by Step"
              options={Object.entries(ReferralStatusDisplayNames).map(
                ([value, label]) => ({
                  label,
                  value,
                })
              )}
            />
            <FilterDropdown
              currentFilters={receivedReferralsFilters}
              filterName="organizationCreatedId[in]"
              setCurrentFilters={setReceivedReferralsFilters}
              title="Filter by Sender"
              options={
                isFetched
                  ? organizations!.data.map((org) => ({
                      label: org.name,
                      value: org.id,
                    }))
                  : []
              }
            />
            <FilterDropdown
              currentFilters={receivedReferralsFilters}
              filterName="serviceCategory[in]"
              setCurrentFilters={setReceivedReferralsFilters}
              title="Filter by Activity"
              options={Object.entries(OrgActivityFilterMap).map(
                ([label, value]) => ({ label, value })
              )}
            />
            <AdminRegionsFilter
              currentFilters={receivedReferralsFilters}
              setCurrentFilters={setReceivedReferralsFilters}
            />
            <DateRangePickerFilter
              setCurrentFilters={setReceivedReferralsFilters}
              placeholder="Filter by Date"
            />
          </div>
        }
      />
      <ConfirmationDialog
        open={!!receivedReferralToDelete}
        title="Delete Referral"
        body={`Are you sure you want to delete the referral for "${receivedReferralToDelete?.firstName} ${receivedReferralToDelete?.surname}"?`}
        onAction={handleDeleteReferral}
        confirmButtonLoading={removeReferral.isLoading}
        actionButtonVariant="destructive"
        onCancel={() => setReceivedReferralToDelete(null)}
      />
    </PageContainer>
  );
};

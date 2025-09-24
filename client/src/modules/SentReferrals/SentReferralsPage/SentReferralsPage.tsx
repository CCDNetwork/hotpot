import { useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { VisibilityState } from '@tanstack/react-table';
import { Download, FileDown, FileUp } from 'lucide-react';

import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import {
  PaginationContext,
  SortDirection,
  usePagination,
} from '@/helpers/pagination';
import { APP_ROUTE } from '@/helpers/constants';
import { ConfirmationDialog } from '@/components/ConfirmationDialog';
import { toast } from '@/components/ui/use-toast';
import { Referral } from '@/services/referrals';
import {
  getReferralsExport,
  getReferralsTemplateFile,
  useReferralMutation,
  useReferrals,
  useReferralUsers,
} from '@/services/referrals/api';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { FilterDropdown } from '@/components/DataTable/FilterDropdown';
import { useOrganizations } from '@/services/organizations/api';
import { ReferralStatusDisplayNames } from '@/services/referrals/const';
import { DateRangePickerFilter } from '@/components/DataTable/DateRangePickerFilter';
import { OrgActivityFilterMap } from '@/services/organizations';
import { FilterByUrgencyButton } from '@/components/FilterByUrgencyButton';
import { UserPermission } from '@/services/users';
import { AdminRegionsFilter } from '@/components/DataTable/AdminRegionsFilter';
import { ButtonWithDropdown } from '@/components/ButtonWithDropdown';
import { downloadFile } from '@/helpers/common';

import { columns } from './columns';
import { useSentReferralsProvider } from '../SentReferralsProvider';
import { BatchCreateModal } from './components/BatchCreateModal';
import { ImpexLoadingDialog } from '../SentReferralPage/components';

export const SentReferralsPage = () => {
  const navigate = useNavigate();
  const pagination = usePagination({
    initialPagination: {
      sortBy: 'createdAt',
      sortDirection: SortDirection.Desc,
    },
  });
  const { setViewOnlyEnabled } = useSentReferralsProvider();
  const {
    currentPage,
    onPageChange,
    onPageSizeChange,
    onSortChange,
    onSearchChange,
  } = pagination;

  const [sentReferralsFilters, setSentReferralsFilters] = useState<
    Record<string, string>
  >({
    isDraft: 'false',
  });
  const [hiddenColumns, setHiddenColumns] = useState<
    VisibilityState | undefined
  >({ status: true });
  const [sentReferralToDelete, setSentReferralToDelete] =
    useState<Referral | null>(null);
  const [isBatchCreateModalOpen, setIsBatchCreateModalOpen] =
    useState<boolean>(false);
  const [impexActionLoading, setImpexActionLoading] = useState<boolean>(false);

  const { data: sentReferralsData, isLoading: queryLoading } = useReferrals({
    ...pagination,
    filters: sentReferralsFilters,
  });

  const { data: organizations, isFetched: isOrganizationsFetched } =
    useOrganizations({
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
    if (!sentReferralToDelete) return;

    try {
      await removeReferral.mutateAsync(sentReferralToDelete.id);
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
    setSentReferralToDelete(null);
  };

  const onSentReferralTableRowClick = useCallback(
    (referralRow: Referral) => {
      setViewOnlyEnabled(true);
      navigate(`${APP_ROUTE.SentReferrals}/${referralRow.id}`);
    },
    [navigate, setViewOnlyEnabled]
  );

  const onNewCaseClick = () => navigate(`${APP_ROUTE.SentReferrals}/new`);

  const onEditSentReferralClick = useCallback(
    async (referralRow: Referral) => {
      setViewOnlyEnabled(false);
      navigate(`${APP_ROUTE.SentReferrals}/${referralRow.id}`);
    },
    [navigate, setViewOnlyEnabled]
  );

  const onExportReferralsClick = async (pagination: PaginationContext) => {
    setImpexActionLoading(true);
    try {
      const exportedData = await getReferralsExport(
        {
          ...pagination,
          pageSize: 999,
          filters: sentReferralsFilters,
        },
        false
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
    setImpexActionLoading(false);
  };

  const onDownloadTemplateClick = async () => {
    setImpexActionLoading(true);
    try {
      const templateFile = await getReferralsTemplateFile();
      downloadFile(templateFile, 'referrals-templates', 'zip');
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'An error has occured. Please try again.',
      });
    }
    setImpexActionLoading(false);
  };

  return (
    <PageContainer
      pageTitle="Manage Sent Referrals"
      pageSubtitle="On this page you can view the referrals that you have sent to other organisations. The Sent tab will show you referrals that have already been sent. The Drafts tab will show you referrals which you have drafted but not sent."
      headerNode={
        <ButtonWithDropdown
          buttonLabel="New Case"
          onButtonClick={onNewCaseClick}
          dropdownOptions={[
            {
              icon: <FileUp className="size-4 mr-2" />,
              label: 'Import referrals',
              onClick: () => setIsBatchCreateModalOpen(true),
            },
            {
              icon: <FileDown className="size-4 mr-2" />,
              label: 'Export referrals',
              onClick: () => onExportReferralsClick(pagination),
            },
            {
              icon: <Download className="size-4 mr-2" />,
              label: 'Download templates',
              onClick: onDownloadTemplateClick,
            },
          ]}
        />
      }
      breadcrumbs={[
        { href: `${APP_ROUTE.SentReferrals}`, name: 'Sent Referrals' },
      ]}
    >
      <Tabs defaultValue="sent">
        <TabsList>
          <TabsTrigger
            value="sent"
            onClick={() => {
              setHiddenColumns({ status: true });
              setSentReferralsFilters(() => ({
                isDraft: 'false',
              }));
            }}
          >
            Sent
          </TabsTrigger>
          <TabsTrigger
            value="draft"
            onClick={() => {
              setHiddenColumns({
                status: false,
                organizationReferredTo: false,
              });
              setSentReferralsFilters(() => ({
                isDraft: 'true',
              }));
            }}
          >
            Drafts
          </TabsTrigger>
        </TabsList>
      </Tabs>
      <DataTable
        data={sentReferralsData?.data ?? []}
        pagination={sentReferralsData?.meta}
        isQueryLoading={queryLoading}
        currentPage={currentPage}
        pageClicked={onPageChange}
        pageSizeClicked={onPageSizeChange}
        headerClicked={onSortChange}
        onSearchChange={onSearchChange}
        columns={columns(setSentReferralToDelete, onEditSentReferralClick)}
        onRowClick={onSentReferralTableRowClick}
        hiddenColumns={hiddenColumns}
        tableFilterNodes={
          <div className="flex flex-wrap gap-4">
            <FilterByUrgencyButton
              currentFilters={sentReferralsFilters}
              filterName="isUrgent"
              setCurrentFilters={setSentReferralsFilters}
              label="Show only urgent"
            />
            <FilterDropdown
              currentFilters={sentReferralsFilters}
              filterName="userCreatedId[in]"
              setCurrentFilters={setSentReferralsFilters}
              title="Filter by Creator"
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
              currentFilters={sentReferralsFilters}
              filterName="isRejected=false,status[in]"
              setCurrentFilters={setSentReferralsFilters}
              title="Filter by Step"
              options={Object.entries(ReferralStatusDisplayNames).map(
                ([value, label]) => ({
                  label,
                  value,
                })
              )}
            />
            <FilterDropdown
              currentFilters={sentReferralsFilters}
              filterName="organizationReferredToId[in]"
              setCurrentFilters={setSentReferralsFilters}
              title="Filter by Recipient"
              options={
                isOrganizationsFetched
                  ? organizations!.data.map((org) => ({
                      label: org.name,
                      value: org.id,
                    }))
                  : []
              }
            />
            <FilterDropdown
              currentFilters={sentReferralsFilters}
              filterName="serviceCategory[in]"
              setCurrentFilters={setSentReferralsFilters}
              title="Filter by Activity"
              options={Object.entries(OrgActivityFilterMap).map(
                ([label, value]) => ({ label, value })
              )}
            />
            <AdminRegionsFilter
              currentFilters={sentReferralsFilters}
              setCurrentFilters={setSentReferralsFilters}
            />
            <DateRangePickerFilter
              setCurrentFilters={setSentReferralsFilters}
              placeholder="Filter by Date"
            />
          </div>
        }
      />
      <BatchCreateModal
        open={isBatchCreateModalOpen}
        setOpen={setIsBatchCreateModalOpen}
      />
      <ConfirmationDialog
        open={!!sentReferralToDelete}
        title="Delete Referral"
        body={`Are you sure you want to delete the referral for "${sentReferralToDelete?.firstName} ${sentReferralToDelete?.surname}"?`}
        onAction={handleDeleteReferral}
        confirmButtonLoading={removeReferral.isLoading}
        actionButtonVariant="destructive"
        onCancel={() => setSentReferralToDelete(null)}
      />
      <ImpexLoadingDialog open={impexActionLoading} />
    </PageContainer>
  );
};

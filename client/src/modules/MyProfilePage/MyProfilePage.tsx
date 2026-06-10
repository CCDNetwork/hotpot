import { PageContainer } from '@/components/PageContainer';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card';
import { useUserMe } from '@/services/users/api';
import { MyProfileForm } from '@/modules/MyProfilePage/components';
import { APP_ROUTE } from '@/helpers/constants';

export const MyProfilePage = () => {
  const { data: userProfileData, isLoading: userProfileQueryLoading } =
    useUserMe({ queryEnabled: true });

  return (
    <PageContainer
      pageTitle="My Profile"
      pageSubtitle="Manage your profile"
      isLoading={userProfileQueryLoading}
      breadcrumbs={[{ href: `${APP_ROUTE.MyProfile}`, name: 'My Profile' }]}
    >
      <div className="space-y-8 max-w-xl">
        <Card className="sm:bg-secondary/10 border-0 sm:border sm:dark:bg-secondary/10 shadow-none">
          <CardHeader>
            <CardTitle>Account Information</CardTitle>
            <CardDescription>
              Edit your account information here
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <MyProfileForm
              userProfileData={userProfileData}
              userProfileQueryLoading={userProfileQueryLoading}
            />
          </CardContent>
        </Card>
      </div>
    </PageContainer>
  );
};

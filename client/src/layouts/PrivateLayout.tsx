import { ReactNode, useCallback, useMemo, useState } from 'react';
import { Navigate, Outlet, useNavigate } from 'react-router-dom';
import { HamburgerMenuIcon } from '@radix-ui/react-icons';

import { useAuth } from '@/providers/GlobalProvider';
import { APP_ROUTE, getNavigationItems } from '@/helpers/constants';
import { UserRole } from '@/services/users';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { useDirectus } from '@/providers/DirectusProvider';
import { toast } from '@/components/ui/use-toast';
import { useIdleTimeout } from '@/hooks/useIdleTimeout';
import { SidebarContent } from './SidebarContent';

interface Props {
  children?: ReactNode;
}

const IDLE_TIMEOUT = 5 * 60 * 1000; // 5 minutes in milliseconds

export const PrivateLayout = ({ children = <Outlet /> }: Props) => {
  const { isLoggedIn, user, logoutUser } = useAuth();
  const navigate = useNavigate();
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState<boolean>(false);

  const handleIdle = useCallback(() => {
    logoutUser();
    navigate(APP_ROUTE.SignIn);
    toast({
      title: "You've been logged out due to inactivity.",
      variant: 'destructive',
    });
  }, [logoutUser, navigate]);

  useIdleTimeout(IDLE_TIMEOUT, handleIdle);

  const { useHomepageData } = useDirectus();

  const { data: homepageData, isLoading: isCmsDataLoading } = useHomepageData;

  const roleBasedNavigationItems = useMemo(() => {
    const navigationItems = getNavigationItems(homepageData);
    if (user.role === UserRole.User) {
      return navigationItems.filter((permissions) =>
        permissions?.userPermissions?.some((p) => user.permissions.includes(p))
      );
    }

    return navigationItems;
  }, [user.permissions, user.role, homepageData]);

  if (user.isSuperAdmin && isLoggedIn) {
    return <Navigate to={APP_ROUTE.Settings} replace />;
  }

  if (!user.id || !isLoggedIn) {
    return <Navigate to={APP_ROUTE.SignIn} />;
  }

  return (
    <div className="relative flex overflow-hidden h-[100svh]">
      {/* DESKTOP */}
      <div className="hidden md:flex w-[300px] z-10">
        <SidebarContent
          isCmsDataLoading={isCmsDataLoading}
          navigationItems={roleBasedNavigationItems}
          showHandbookRoute={user.role === UserRole.User}
          handbookRouteName={homepageData?.handbook ?? ''}
          dashboardRouteName={homepageData?.dashboard ?? ''}
        />
      </div>

      {/* MOBILE */}
      <Sheet open={mobileSidebarOpen}>
        <SheetTrigger
          onClick={() => setMobileSidebarOpen((old) => !old)}
          className="absolute top-1.5 left-3 bg-muted/50 p-1 rounded-md hover:bg-muted transition-colors duration-300 ease-in-out block md:hidden focus:outline-primary outline-none"
        >
          <HamburgerMenuIcon className="size-5 text-muted-foreground" />
        </SheetTrigger>
        <SheetContent
          onOverlayClick={() => setMobileSidebarOpen(false)}
          className="p-0 w-[300px] z-50"
          side="left"
        >
          <SidebarContent
            isCmsDataLoading={isCmsDataLoading}
            navigationItems={roleBasedNavigationItems}
            showHandbookRoute={user.role === UserRole.User}
            handbookRouteName={homepageData?.handbook ?? ''}
            dashboardRouteName={homepageData?.dashboard ?? ''}
            closeSidebar={() => setMobileSidebarOpen(false)}
          />
        </SheetContent>
      </Sheet>

      <div className="divide-y divide-border flex-1 flex flex-col overflow-hidden">
        <div className="px-4 pt-2 pb-4 flex-1 overflow-y-auto md:mt-0 mt-10 border-t md:border-t-0">
          {children}
        </div>
        {/* <div className="px-4 py-4 sm:px-6 min-h-[69px] flex items-center justify-center">
          <p className="font-medium tracking-tight text-muted-foreground">
            Footer Content
          </p>
        </div> */}
      </div>
    </div>
  );
};

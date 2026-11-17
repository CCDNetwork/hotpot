import { LucideIcon } from 'lucide-react';

export interface NavigationItem {
  categoryName: string;
  userPermissions?: string[];
  routes: {
    name: string;
    to: string;
    icon: LucideIcon;
    userPermissions?: string[];
    roles?: string[];
  }[];
}

export interface IBreadcrumb {
  href?: string;
  name: string;
}

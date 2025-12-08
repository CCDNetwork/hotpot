import { useLocation, useNavigate } from 'react-router-dom';
import {
  LanguagesIcon,
  LaptopIcon,
  LogOut,
  Moon,
  Sun,
  User,
} from 'lucide-react';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuPortal,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { APP_ROUTE } from '@/helpers/constants';
import { cn } from '@/helpers/utils';
import { useAuth } from '@/providers/GlobalProvider';
import { useTheme } from '@/providers/ThemeProvider';
import { useLanguage } from '@/providers/LanguageProvider.tsx';
import { CheckIcon } from '@radix-ui/react-icons';

export const MyProfileItemWithDropdown = ({
  closeSidebar,
}: {
  closeSidebar?: () => void;
}) => {
  const { user, logoutUser } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { setTheme } = useTheme();
  const { language, setLanguage } = useLanguage();

  const userInitials = `${user.firstName?.[0] ?? ''} ${user.lastName?.[0] ?? ''}`;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild className="border-t border-border">
        <div className="-mx-6 mt-auto">
          <div
            className={cn(
              'flex cursor-pointer hover:bg-muted-foreground/10 justify-start items-center gap-x-4 px-6 py-3 text-sm font-semibold leading-6 text-secondary-foreground transition-colors duration-150 ease-linear',
              {
                'bg-primary/90 text-gray-50 hover:bg-primary/90':
                  pathname.includes(APP_ROUTE.MyProfile),
              }
            )}
          >
            <Avatar>
              <AvatarImage src="profileimageurlgoeshere" />
              <AvatarFallback
                className={cn(
                  'bg-foreground/30 text-gray-50 tracking-tight text-lg uppercase',
                  {
                    'dark:bg-background/30': pathname.includes(
                      APP_ROUTE.MyProfile
                    ),
                  }
                )}
              >
                {userInitials}
              </AvatarFallback>
            </Avatar>
            <span className="sr-only">Your profile</span>
            <div className="flex flex-col truncate">
              <span aria-hidden="true">{`${user.firstName ?? '-'} ${user.lastName ?? '-'}`}</span>
              <span
                aria-hidden="true"
                className="text-sm truncate opacity-80 font-normal"
              >
                {user.organizations?.length
                  ? user.organizations?.[0].name
                  : user.email ?? '-'}
              </span>
            </div>
          </div>
        </div>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-56 mx-2 my-1">
        <DropdownMenuGroup>
          <DropdownMenuItem
            onClick={() => {
              closeSidebar?.();
              navigate(APP_ROUTE.MyProfile);
            }}
          >
            <User className="mr-2 h-[1.2rem] w-[1.2rem]" />
            My Profile
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            <Sun className="h-[1.2rem] w-[1.2rem] rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0 mr-2" />
            <Moon className="absolute h-[1.2rem] w-[1.2rem] rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100 mr-2" />
            Theme
          </DropdownMenuSubTrigger>
          <DropdownMenuPortal>
            <DropdownMenuSubContent>
              <DropdownMenuItem onClick={() => setTheme('light')}>
                <Sun className="mr-2 w-4 h-4" /> Light
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTheme('dark')}>
                <Moon className="mr-2 w-4 h-4" />
                Dark
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTheme('system')}>
                <LaptopIcon className="mr-2 h-4 w-4" />
                System
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuPortal>
        </DropdownMenuSub>
        <DropdownMenuSeparator />
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            <LanguagesIcon className="size-5 mr-2" />
            Language
          </DropdownMenuSubTrigger>
          <DropdownMenuPortal>
            <DropdownMenuSubContent>
              <DropdownMenuItem onClick={() => setLanguage('en-US')}>
                <CheckIcon
                  className={cn('mr-2 h-4 w-4 hidden', {
                    block: language === 'en-US',
                  })}
                />
                English
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setLanguage('fr-FR')}>
                <CheckIcon
                  className={cn('mr-2 h-4 w-4 hidden', {
                    block: language === 'fr-FR',
                  })}
                />
                French
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setLanguage('uk-UA')}>
                <CheckIcon
                  className={cn('mr-2 h-4 w-4 hidden', {
                    block: language === 'uk-UA',
                  })}
                />
                Ukrainian
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuPortal>
        </DropdownMenuSub>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="text-red-500 focus:text-red-500"
          onClick={logoutUser}
        >
          <LogOut className="mr-2 h-[1.2rem] w-[1.2rem]" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

const { VITE_DIRECTUS_URL, VITE_DIRECTUS_TOKEN } = import.meta.env;
import { createContext, ReactNode, useContext } from 'react';
import { createDirectus, readItems, rest, staticToken } from '@directus/sdk';
import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { Outlet } from 'react-router-dom';

import {
  DirectusSchema,
  HomepageData,
} from '@/providers/DirectusProvider/types.ts';
import { useLanguage } from '@/providers/LanguageProvider.tsx';

type DirectusProviderProps = {
  children: ReactNode;
};

/**
 * Directus is optional: it only supplies translated menu labels for
 * deployments that configure it (e.g. multi-language deployments). When
 * VITE_DIRECTUS_URL is not set, no request is made and the built-in English
 * labels are used (see getNavigationItems).
 */
export const isDirectusEnabled = (): boolean => {
  const url = (VITE_DIRECTUS_URL || '').trim();
  return url.startsWith('http://') || url.startsWith('https://');
};

type DirectusProviderState = {
  useHomepageData: UseQueryResult<HomepageData>;
};

const DirectusProviderContext = createContext<DirectusProviderState>(
  undefined!
);

export const DirectusProvider = ({
  children = <Outlet />,
}: DirectusProviderProps) => {
  const { language } = useLanguage();

  const getHomepageData = async () => {
    const directusClient = createDirectus<DirectusSchema>(VITE_DIRECTUS_URL)
      .with(staticToken(VITE_DIRECTUS_TOKEN))
      .with(rest());
    const data = await directusClient.request(
      readItems('homepage_translations', {
        filter: {
          languages_code: {
            _eq: language,
          },
        },
      })
    );
    return data[0];
  };

  const useHomepageData = useQuery(
    ['directus-homepage', language],
    getHomepageData,
    {
      enabled: isDirectusEnabled(),
    }
  );

  const value = {
    useHomepageData,
  };

  return (
    <DirectusProviderContext.Provider value={value}>
      {children}
    </DirectusProviderContext.Provider>
  );
};

export const useDirectus = () => {
  const context = useContext(DirectusProviderContext);

  if (context === undefined)
    throw new Error('useDirectus must be used within a DirectusProvider');

  return context;
};

import { Settings } from './types';

export const resToSettings = (res: any): Settings => {
  return {
    id: res.id ?? '',
    deploymentCountry: res.deploymentCountry ?? '',
    deploymentName: res.deploymentName ?? '',
    adminLevel1Name: res.adminLevel1Name ?? '',
    adminLevel2Name: res.adminLevel2Name ?? '',
    adminLevel3Name: res.adminLevel3Name ?? '',
    adminLevel4Name: res.adminLevel4Name ?? '',
    metabaseUrl: res.metabaseUrl ?? '',
    fundingSources: res.fundingSources ?? [],
  };
};

export const settingsToReq = (
  data: Omit<Settings, 'id'>
): Omit<Settings, 'id'> => {
  return {
    deploymentCountry: data.deploymentCountry ?? '',
    deploymentName: data.deploymentName ?? '',
    adminLevel1Name: data.adminLevel1Name ?? '',
    adminLevel2Name: data.adminLevel2Name ?? '',
    adminLevel3Name: data.adminLevel3Name ?? '',
    adminLevel4Name: data.adminLevel4Name ?? '',
    metabaseUrl: data.metabaseUrl ?? '',
    fundingSources: data.fundingSources.length
      ? data.fundingSources.map((i) => i.value)
      : [],
  };
};

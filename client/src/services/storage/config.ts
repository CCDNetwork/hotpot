import { StorageTypeId } from './types';

export const activeStorageTypeId: StorageTypeId = Number(
  import.meta.env.VITE_STORAGE_TYPE_ID ?? StorageTypeId.Assets
) as StorageTypeId;

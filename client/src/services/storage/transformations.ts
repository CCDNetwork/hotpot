import { activeStorageTypeId } from './config';
import { StorageFile, StorageTypeId, Image, StorageFileData } from './types';

export const resToStorageFile = (res: any): StorageFile => {
  return {
    id: res.id ?? '',
    name: res.name ?? '',
    storageTypeId: res.storageTypeId ?? 0,
    url: res.url ?? '',
  };
};

export const storageFileToPostReq = (data: {
  storageFileType: StorageTypeId;
  file: File;
}) => {
  return {
    storageFileType: data.storageFileType,
    file: data.file,
  };
};

export const imageToFormData = (image: Image): FormData => {
  const formData = new FormData();
  formData.append(
    'file',
    new File([image.buffer], image.name, { type: 'image/*' })
  );
  formData.append('storageTypeId', activeStorageTypeId.toString());
  return formData;
};

export const fileToFormData = (data: StorageFileData): FormData => {
  const formData = new FormData();
  formData.append('file', new File([data.file.buffer], data.file.name));
  formData.append('storageTypeId', data.type.toString());
  return formData;
};

export const fileToImage = async (file: File): Promise<Image> => {
  return {
    id: -1,
    name: file.name,
    url: URL.createObjectURL(file),
    buffer: await file.arrayBuffer(),
    alt: file.name,
  };
};

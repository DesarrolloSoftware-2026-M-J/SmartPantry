import type { ProductLookupStatus } from './product-lookup-status.enum';

export interface BarcodeLookupInputDto {
  barcode: string;
}

export interface ProductLookupResultDto {
  status?: ProductLookupStatus;
  barcode?: string;
  name?: string | null;
  brand?: string | null;
  imageUrl?: string | null;
}

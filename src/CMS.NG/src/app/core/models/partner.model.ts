/** Response model for the Partner (合作廠商) table. pkid is a smallint IDENTITY (DB-assigned). */
export interface Partner {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}

/** Write DTO for create/update. pkid is DB-assigned on create; used as the key on edit. */
export interface PartnerRequest {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}

/** Search DTO for POST /api/partners/query. */
export interface PartnerQuery {
  keyword?: string | null;
}

/** Response model for the PublishStatus (發布狀態) table. pkid is a user-assigned tinyint PK. */
export interface PublishStatus {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}

/** Write DTO for create/update. pkid is required on create, immutable on edit. */
export interface PublishStatusRequest {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}

/** Search DTO for POST /api/publish-statuses/query. */
export interface PublishStatusQuery {
  keyword?: string | null;
  isDraft?: boolean | null;
  isPublished?: boolean | null;
  isDiscontinued?: boolean | null;
}

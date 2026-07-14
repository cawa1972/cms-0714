/** Response model for the CourseGroup (課程分類) table. pkid is a database-assigned smallint IDENTITY PK. */
export interface CourseGroup {
  pkid: number;
  description: string;
}

/** Write DTO for create/update. pkid is ignored on create (DB-assigned); required on update. */
export interface CourseGroupRequest {
  pkid: number;
  description: string;
}

/** Search DTO for POST /api/course-groups/query. */
export interface CourseGroupQuery {
  keyword?: string | null;
}

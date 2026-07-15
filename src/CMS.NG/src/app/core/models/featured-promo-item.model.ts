/**
 * Response model for the FeaturedPromoItem (首頁上稿) table. pkid is an int IDENTITY.
 * One row = one promoted item on the home page for a day (scheduleOn, ISO 'yyyy-MM-dd'),
 * training center and slot (1–3); the triple is unique. promoCode / trainingCenterName
 * are resolved display labels from JOINs.
 */
export interface FeaturedPromoItem {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  topic: string;
  description: string;
  promoCode: string | null;
  trainingCenterName: string | null;
}

/** Write DTO for create/update. pkid is DB-assigned on create; used as the key on edit. */
export interface FeaturedPromoItemRequest {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  topic: string;
  description: string;
}

/** Search DTO for POST /api/featured-promo-items/query (week bounds + training-center tab). */
export interface FeaturedPromoItemQuery {
  scheduleOnFrom?: string | null;
  scheduleOnTo?: string | null;
  trainingCenterPkid?: number | null;
  slot?: number | null;
}

/** Result of resolving a Promotion2 PromoCode (GET /api/featured-promo-items/promo-lookup). */
export interface PromoCodeLookup {
  promotionPkid: number;
  promoCode: string;
  topic: string;
  description: string;
}

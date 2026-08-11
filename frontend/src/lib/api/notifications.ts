import { apiFetch } from "@/lib/api/client";
import { FULL_PAGE, toQueryString, type PageParams } from "@/lib/api/query";
import type { AppNotification, UnreadCount } from "@/types";

export interface NotificationListParams extends PageParams {
  unreadOnly?: boolean;
}

/** The caller's own notifications — the API has no route for reading anyone else's. */
export const getNotifications = (params: NotificationListParams = FULL_PAGE) =>
  apiFetch<AppNotification[]>(`/notifications${toQueryString({ ...FULL_PAGE, ...params })}`);

/** Badge count only, so the bell can poll without pulling a page of rows. */
export const getUnreadCount = () => apiFetch<UnreadCount>("/notifications/unread-count");

export const markNotificationRead = (id: string) =>
  apiFetch<AppNotification>(`/notifications/${id}/read`, { method: "PATCH" });

export const markAllNotificationsRead = () =>
  apiFetch<UnreadCount>("/notifications/read-all", { method: "POST" });

"use client";

import { useEffect, useRef, useState } from "react";
import {
  getNotifications,
  getUnreadCount,
  markAllNotificationsRead,
  markNotificationRead,
} from "@/lib/api/notifications";
import { formatDateTime } from "@/lib/datetime";
import { mutedTextClass, subtleButtonClass } from "@/components/ui/styles";
import type { AppNotification } from "@/types";

/** How often the badge re-checks. Long enough to stay cheap, short enough to feel live. */
const POLL_INTERVAL_MS = 30_000;

/** Newest first, capped — the panel is a glance, not an archive. */
const PANEL_PAGE_SIZE = 20;

/**
 * Bell with an unread badge and a dropdown panel.
 *
 * Polls the count endpoint rather than the list: the badge is the only thing that needs to
 * stay current, and it is a single integer instead of a page of rows. The list is fetched
 * once the panel is actually opened.
 */
export function NotificationBell() {
  const [unread, setUnread] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Subscription shape: the effect only starts and stops the poll. `isActive` prevents a
  // response that lands after unmount (or after a fast sign-out) from setting state on a
  // component that is gone, and the leading timeout keeps the first fetch off the render path.
  useEffect(() => {
    let isActive = true;

    const poll = async () => {
      try {
        const result = await getUnreadCount();
        if (isActive) setUnread(result.unread);
      } catch {
        // A failed poll is not worth interrupting the page for: the next tick retries and the
        // badge keeps its previous value rather than flashing an error at the user.
      }
    };

    const initial = setTimeout(poll, 0);
    const timer = setInterval(poll, POLL_INTERVAL_MS);

    return () => {
      isActive = false;
      clearTimeout(initial);
      clearInterval(timer);
    };
  }, []);

  // Close on outside click and on Escape, so the panel behaves like every other menu.
  useEffect(() => {
    if (!isOpen) return;

    const onPointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsOpen(false);
    };

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [isOpen]);

  const openPanel = async () => {
    setIsOpen(true);
    setIsLoading(true);
    setError(null);

    try {
      setNotifications(await getNotifications({ pageSize: PANEL_PAGE_SIZE }));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not load notifications.");
    } finally {
      setIsLoading(false);
    }
  };

  const togglePanel = () => {
    if (isOpen) {
      setIsOpen(false);
      return;
    }
    void openPanel();
  };

  const handleMarkRead = async (notification: AppNotification) => {
    if (notification.isRead) return;

    try {
      await markNotificationRead(notification.id);
      setNotifications((current) =>
        current.map((item) => (item.id === notification.id ? { ...item, isRead: true } : item)),
      );
      setUnread((current) => Math.max(0, current - 1));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not update the notification.");
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await markAllNotificationsRead();
      setNotifications((current) => current.map((item) => ({ ...item, isRead: true })));
      setUnread(0);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Could not update the notifications.");
    }
  };

  const badgeLabel = unread > 99 ? "99+" : String(unread);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={togglePanel}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-label={unread > 0 ? `Notifications, ${unread} unread` : "Notifications"}
        className="relative rounded px-2 py-1 text-sm hover:bg-black/5 dark:hover:bg-white/10"
      >
        <span aria-hidden="true">🔔</span>
        {unread > 0 && (
          <span
            // aria-hidden: the count is already announced through the button's label, so
            // exposing it again would read the number twice.
            aria-hidden="true"
            className="absolute -right-1 -top-1 min-w-4 rounded-full bg-red-600 px-1 text-center text-[10px] font-semibold leading-4 text-white"
          >
            {badgeLabel}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          role="menu"
          aria-label="Notifications"
          className="absolute right-0 z-20 mt-2 w-80 max-w-[calc(100vw-2rem)] rounded border border-black/10 bg-white shadow-lg dark:border-white/15 dark:bg-black"
        >
          <div className="flex items-center justify-between border-b border-black/10 px-3 py-2 dark:border-white/15">
            <span className="text-sm font-semibold">Notifications</span>
            {unread > 0 && (
              <button type="button" onClick={handleMarkAllRead} className={subtleButtonClass}>
                Mark all read
              </button>
            )}
          </div>

          <div className="max-h-96 overflow-y-auto">
            {isLoading && <p className={`px-3 py-4 ${mutedTextClass}`}>Loading…</p>}

            {error && (
              <p role="alert" className="px-3 py-4 text-sm text-red-600 dark:text-red-400">
                {error}
              </p>
            )}

            {!isLoading && !error && notifications.length === 0 && (
              <p className={`px-3 py-4 ${mutedTextClass}`}>You have no notifications.</p>
            )}

            <ul>
              {notifications.map((notification) => (
                <li
                  key={notification.id}
                  className="border-b border-black/5 last:border-b-0 dark:border-white/10"
                >
                  <button
                    type="button"
                    role="menuitem"
                    onClick={() => handleMarkRead(notification)}
                    className={`w-full px-3 py-2 text-left hover:bg-black/5 dark:hover:bg-white/10 ${
                      notification.isRead ? "" : "bg-black/[0.03] dark:bg-white/[0.06]"
                    }`}
                  >
                    <span className="flex items-start gap-2">
                      {!notification.isRead && (
                        <span
                          aria-hidden="true"
                          className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-blue-600 dark:bg-blue-400"
                        />
                      )}
                      <span className="min-w-0">
                        <span className="block text-sm font-medium">{notification.title}</span>
                        <span className="block text-sm text-black/70 dark:text-white/70">
                          {notification.message}
                        </span>
                        <span className={`block ${mutedTextClass}`}>
                          {formatDateTime(notification.createdAt)}
                        </span>
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </div>
  );
}

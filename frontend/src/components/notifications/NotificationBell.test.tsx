import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { NotificationBell } from "@/components/notifications/NotificationBell";
import {
  getNotifications,
  getUnreadCount,
  markAllNotificationsRead,
  markNotificationRead,
} from "@/lib/api/notifications";
import type { AppNotification } from "@/types";

vi.mock("@/lib/api/notifications", () => ({
  getNotifications: vi.fn(),
  getUnreadCount: vi.fn(),
  markNotificationRead: vi.fn(),
  markAllNotificationsRead: vi.fn(),
}));

const unreadNotification: AppNotification = {
  id: "n-1",
  type: "AssignmentPublished",
  title: "New assignment published",
  message: '"Algebra" is due 01 Sep 2026 17:00 UTC.',
  assignmentId: "a-1",
  submissionId: null,
  isRead: false,
  createdAt: "2026-08-11T10:00:00Z",
  readAt: null,
};

const readNotification: AppNotification = {
  ...unreadNotification,
  id: "n-2",
  title: "Your submission was graded",
  type: "SubmissionGraded",
  isRead: true,
  readAt: "2026-08-11T11:00:00Z",
};

describe("NotificationBell", () => {
  beforeEach(() => {
    // Without this, "not called" assertions see calls made by earlier tests.
    vi.clearAllMocks();
    vi.mocked(getUnreadCount).mockResolvedValue({ unread: 0 });
    vi.mocked(getNotifications).mockResolvedValue([]);
    vi.mocked(markNotificationRead).mockResolvedValue(readNotification);
    vi.mocked(markAllNotificationsRead).mockResolvedValue({ unread: 0 });
  });

  it("shows the unread count in the button label once the first poll lands", async () => {
    vi.mocked(getUnreadCount).mockResolvedValue({ unread: 3 });

    render(<NotificationBell />);

    expect(await screen.findByRole("button", { name: "Notifications, 3 unread" })).toBeInTheDocument();
  });

  it("does not render a badge when there is nothing unread", async () => {
    render(<NotificationBell />);

    const button = await screen.findByRole("button", { name: "Notifications" });
    expect(button).toHaveTextContent(/^🔔$/);
  });

  it("caps the badge at 99+ so a large count cannot break the layout", async () => {
    vi.mocked(getUnreadCount).mockResolvedValue({ unread: 250 });

    render(<NotificationBell />);

    expect(await screen.findByText("99+")).toBeInTheDocument();
  });

  it("loads the list only when the panel is opened", async () => {
    const user = userEvent.setup();
    vi.mocked(getUnreadCount).mockResolvedValue({ unread: 1 });
    vi.mocked(getNotifications).mockResolvedValue([unreadNotification]);

    render(<NotificationBell />);
    await screen.findByRole("button", { name: /Notifications/ });

    // Polling the count must not drag a page of rows along with it.
    expect(getNotifications).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: /Notifications/ }));

    expect(await screen.findByText("New assignment published")).toBeInTheDocument();
    expect(getNotifications).toHaveBeenCalledTimes(1);
  });

  it("marks a notification read and decrements the badge", async () => {
    const user = userEvent.setup();
    vi.mocked(getUnreadCount).mockResolvedValue({ unread: 1 });
    vi.mocked(getNotifications).mockResolvedValue([unreadNotification]);

    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: "Notifications, 1 unread" }));
    await user.click(await screen.findByRole("menuitem", { name: /New assignment published/ }));

    await waitFor(() => expect(markNotificationRead).toHaveBeenCalledWith("n-1"));
    expect(await screen.findByRole("button", { name: "Notifications" })).toBeInTheDocument();
  });

  it("does not re-send a read request for an already-read notification", async () => {
    const user = userEvent.setup();
    vi.mocked(getNotifications).mockResolvedValue([readNotification]);

    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: "Notifications" }));
    await user.click(await screen.findByRole("menuitem", { name: /Your submission was graded/ }));

    expect(markNotificationRead).not.toHaveBeenCalled();
  });

  it("clears everything through mark all read", async () => {
    const user = userEvent.setup();
    vi.mocked(getUnreadCount).mockResolvedValue({ unread: 2 });
    vi.mocked(getNotifications).mockResolvedValue([unreadNotification]);

    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: "Notifications, 2 unread" }));
    await user.click(await screen.findByRole("button", { name: "Mark all read" }));

    await waitFor(() => expect(markAllNotificationsRead).toHaveBeenCalled());
    expect(await screen.findByRole("button", { name: "Notifications" })).toBeInTheDocument();
  });

  it("reports a failure to load the list instead of showing an empty panel", async () => {
    const user = userEvent.setup();
    vi.mocked(getNotifications).mockRejectedValue(new Error("Service unavailable."));

    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: "Notifications" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Service unavailable.");
  });

  it("keeps the badge quiet when a poll fails", async () => {
    // A transient network error must not throw an error banner onto every page.
    vi.mocked(getUnreadCount).mockRejectedValue(new Error("offline"));

    render(<NotificationBell />);

    expect(await screen.findByRole("button", { name: "Notifications" })).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows an empty state when there is nothing to read", async () => {
    const user = userEvent.setup();

    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: "Notifications" }));

    expect(await screen.findByText("You have no notifications.")).toBeInTheDocument();
  });

  it("closes the panel on Escape", async () => {
    const user = userEvent.setup();
    vi.mocked(getNotifications).mockResolvedValue([unreadNotification]);

    render(<NotificationBell />);
    await user.click(await screen.findByRole("button", { name: "Notifications" }));
    expect(await screen.findByRole("menu")).toBeInTheDocument();

    await user.keyboard("{Escape}");

    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());
  });
});

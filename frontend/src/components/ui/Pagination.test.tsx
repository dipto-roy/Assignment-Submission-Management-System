import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Pagination, buildPageWindow } from "@/components/ui/Pagination";
import type { PageMeta } from "@/types";

const meta = (overrides: Partial<PageMeta> = {}): PageMeta => ({
  total: 45,
  page: 1,
  pageSize: 10,
  totalPages: 5,
  ...overrides,
});

describe("Pagination", () => {
  it("renders nothing for a single page when no size control is offered", () => {
    const { container } = render(
      <Pagination
        meta={meta({ total: 4, totalPages: 1 })}
        onPageChange={vi.fn()}
        label="users"
      />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it("still renders the size control on a single page", () => {
    render(
      <Pagination
        meta={meta({ total: 4, totalPages: 1 })}
        onPageChange={vi.fn()}
        onPageSizeChange={vi.fn()}
        label="users"
      />,
    );

    expect(screen.getByLabelText("users per page")).toBeInTheDocument();
  });

  it("describes the visible range", () => {
    render(<Pagination meta={meta({ page: 3 })} onPageChange={vi.fn()} label="users" />);

    const summary = screen.getByText(/Showing/);
    expect(summary).toHaveTextContent("Showing 21–30 of 45 users");
  });

  it("caps the range at the total on the last page", () => {
    render(<Pagination meta={meta({ page: 5 })} onPageChange={vi.fn()} label="users" />);

    expect(screen.getByText(/Showing/)).toHaveTextContent("Showing 41–45 of 45 users");
  });

  it("disables the previous control on the first page and the next on the last", () => {
    const { rerender } = render(
      <Pagination meta={meta()} onPageChange={vi.fn()} label="users" />,
    );

    expect(screen.getByLabelText("Previous page")).toBeDisabled();
    expect(screen.getByLabelText("Next page")).toBeEnabled();

    rerender(<Pagination meta={meta({ page: 5 })} onPageChange={vi.fn()} label="users" />);

    expect(screen.getByLabelText("Previous page")).toBeEnabled();
    expect(screen.getByLabelText("Next page")).toBeDisabled();
  });

  it("reports the page a control moves to", async () => {
    const onPageChange = vi.fn();
    render(<Pagination meta={meta({ page: 2 })} onPageChange={onPageChange} label="users" />);

    await userEvent.click(screen.getByLabelText("Next page"));
    expect(onPageChange).toHaveBeenLastCalledWith(3);

    await userEvent.click(screen.getByLabelText("Previous page"));
    expect(onPageChange).toHaveBeenLastCalledWith(1);

    await userEvent.click(screen.getByLabelText("Page 5"));
    expect(onPageChange).toHaveBeenLastCalledWith(5);
  });

  it("marks the current page for assistive technology", () => {
    render(<Pagination meta={meta({ page: 2 })} onPageChange={vi.fn()} label="users" />);

    expect(screen.getByLabelText("Page 2")).toHaveAttribute("aria-current", "page");
    expect(screen.getByLabelText("Page 1")).not.toHaveAttribute("aria-current");
  });

  it("reports the chosen rows-per-page as a number", async () => {
    const onPageSizeChange = vi.fn();
    render(
      <Pagination
        meta={meta()}
        onPageChange={vi.fn()}
        onPageSizeChange={onPageSizeChange}
        label="users"
      />,
    );

    await userEvent.selectOptions(screen.getByLabelText("users per page"), "25");
    expect(onPageSizeChange).toHaveBeenCalledWith(25);
  });

  it("disables the controls while a page is in flight", () => {
    render(<Pagination meta={meta({ page: 2 })} onPageChange={vi.fn()} label="users" isBusy />);

    expect(screen.getByLabelText("Next page")).toBeDisabled();
    expect(screen.getByLabelText("Page 1")).toBeDisabled();
  });
});

describe("buildPageWindow", () => {
  it("lists every page when they all fit", () => {
    expect(buildPageWindow(1, 3)).toEqual([1, 2, 3]);
  });

  it("collapses the pages far from the current one", () => {
    expect(buildPageWindow(10, 20)).toEqual([1, "gap", 9, 10, 11, "gap", 20]);
  });

  it("only collapses the side that needs it", () => {
    expect(buildPageWindow(2, 20)).toEqual([1, 2, 3, "gap", 20]);
    expect(buildPageWindow(19, 20)).toEqual([1, "gap", 18, 19, 20]);
  });

  it("handles a single page without duplicating it", () => {
    expect(buildPageWindow(1, 1)).toEqual([1]);
  });
});

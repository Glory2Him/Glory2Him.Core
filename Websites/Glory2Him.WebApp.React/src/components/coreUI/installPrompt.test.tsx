import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { InstallPrompt } from "./installPrompt";

describe("InstallPrompt", () => {
    it("should render nothing when there is nothing to offer", () => {
        // when
        render(<InstallPrompt variant="none" onInstall={vi.fn()} onDismiss={vi.fn()} />);

        // then
        expect(screen.queryByRole("region")).not.toBeInTheDocument();
    });

    it("should install through the browser when the reader accepts the offer", async () => {
        // given
        const onInstall = vi.fn();
        render(<InstallPrompt variant="install" onInstall={onInstall} onDismiss={vi.fn()} />);

        // when
        await userEvent.click(screen.getByRole("button", { name: "Install" }));

        // then
        expect(onInstall).toHaveBeenCalledTimes(1);
    });

    // iOS never fires the install event, so a button here would do nothing at all — the reader
    // has to be sent to the share sheet instead.
    it("should show the share-sheet steps rather than a button on iOS", () => {
        // when
        render(<InstallPrompt variant="iosShareSheet" onInstall={vi.fn()} onDismiss={vi.fn()} />);

        // then
        expect(screen.getByRole("region")).toHaveTextContent(/Add to Home Screen/);
        expect(screen.queryByRole("button", { name: "Install" })).not.toBeInTheDocument();
    });

    // An app's built-in browser has no Add to Home Screen at all, so telling this reader where
    // to tap would send them looking for something that is not there.
    it("should send a reader inside an app's browser to Safari first", () => {
        // when
        render(
            <InstallPrompt variant="iosInAppBrowser" onInstall={vi.fn()} onDismiss={vi.fn()} />);

        // then
        expect(screen.getByRole("region")).toHaveTextContent(/Open this page in Safari/);
        expect(screen.queryByRole("button", { name: "Install" })).not.toBeInTheDocument();
    });

    it("should let the reader dismiss any of the offers", async () => {
        // given
        const onDismiss = vi.fn();

        render(
            <InstallPrompt variant="iosShareSheet" onInstall={vi.fn()} onDismiss={onDismiss} />);

        // when
        await userEvent.click(screen.getByRole("button", { name: "Dismiss" }));

        // then
        expect(onDismiss).toHaveBeenCalledTimes(1);
    });

    // Regression guard: the offline banner already owns the top edge of the viewport and the two
    // can be on screen together, so this one is pinned to the bottom — and above the sticky
    // header's own z-index, the same way offlineBanner.tsx is.
    it("should sit pinned to the bottom of the viewport, above the sticky header", () => {
        // when
        render(<InstallPrompt variant="install" onInstall={vi.fn()} onDismiss={vi.fn()} />);

        // then
        const prompt = screen.getByRole("region");
        expect(prompt).toHaveClass("position-fixed", "bottom-0");
        expect(prompt).toHaveStyle({ zIndex: 1030 });
    });
});

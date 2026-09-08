import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useInstallPrompt } from "./useInstallPrompt";

describe("useInstallPrompt", () => {
    const desktopChromeAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        + "(KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36";

    const iphoneSafariAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) "
        + "AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1";

    // The same iPhone, inside an app's embedded webview: no "Safari/" token, and no Add to Home
    // Screen anywhere in its share sheet.
    const iphoneInAppAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) "
        + "AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";

    const macAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 "
        + "(KHTML, like Gecko) Version/18.0 Safari/605.1.15";

    const realUserAgent = Object.getOwnPropertyDescriptor(navigator, "userAgent");
    const realMaxTouchPoints = Object.getOwnPropertyDescriptor(navigator, "maxTouchPoints");
    const realStandalone = Object.getOwnPropertyDescriptor(navigator, "standalone");
    const realMatchMedia = Object.getOwnPropertyDescriptor(window, "matchMedia");

    const overwrite = (target: object, key: string, value: unknown) =>
        Object.defineProperty(target, key, { configurable: true, value });

    const restore = (target: object, key: string, descriptor?: PropertyDescriptor) => {
        if (descriptor) {
            Object.defineProperty(target, key, descriptor);
        } else {
            delete (target as Record<string, unknown>)[key];
        }
    };

    const setAgent = (agent: string) => overwrite(navigator, "userAgent", agent);
    const setTouchPoints = (points: number) => overwrite(navigator, "maxTouchPoints", points);
    const setIosStandalone = (standalone: boolean | undefined) =>
        overwrite(navigator, "standalone", standalone);

    const setDisplayMode = (isStandalone: boolean) =>
        overwrite(window, "matchMedia", (query: string) =>
            ({ matches: isStandalone && query.includes("standalone"), media: query }));

    const createInstallEvent = (outcome: "accepted" | "dismissed" = "accepted") => {
        const event = new Event("beforeinstallprompt", { cancelable: true }) as Event & {
            prompt: () => Promise<void>;
            userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
        };

        event.prompt = vi.fn(() => Promise.resolve());
        event.userChoice = Promise.resolve({ outcome });

        return event;
    };

    beforeEach(() => {
        window.localStorage.clear();
        setAgent(desktopChromeAgent);
        setTouchPoints(0);
        setIosStandalone(undefined);
        setDisplayMode(false);
    });

    afterEach(() => {
        vi.restoreAllMocks();
        window.localStorage.clear();
        restore(navigator, "userAgent", realUserAgent);
        restore(navigator, "maxTouchPoints", realMaxTouchPoints);
        restore(navigator, "standalone", realStandalone);
        restore(window, "matchMedia", realMatchMedia);
    });

    it("should offer nothing until the browser says the app can be installed", () => {
        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("none");
    });

    it("should offer the install button once the browser defers its own prompt", () => {
        // given
        const { result } = renderHook(() => useInstallPrompt());

        // when
        act(() => { window.dispatchEvent(createInstallEvent()); });

        // then
        expect(result.current.variant).toBe("install");
    });

    // Chromium shows its own mini-infobar unless the event is cancelled, which would leave the
    // reader asked for the same thing twice in two different places.
    it("should stop the browser from offering the install alongside ours", () => {
        // given
        renderHook(() => useInstallPrompt());
        const installEvent = createInstallEvent();

        // when
        act(() => { window.dispatchEvent(installEvent); });

        // then
        expect(installEvent.defaultPrevented).toBe(true);
    });

    it("should hand the install back to the browser when the reader accepts", async () => {
        // given
        const installEvent = createInstallEvent("accepted");
        const { result } = renderHook(() => useInstallPrompt());
        act(() => { window.dispatchEvent(installEvent); });

        // when
        act(() => result.current.install());

        // then
        expect(installEvent.prompt).toHaveBeenCalled();
        await waitFor(() => expect(result.current.variant).toBe("none"));
    });

    // Declining the browser's own dialog is a decline of the offer that raised it — otherwise
    // the card sits there afterwards, and comes back on the next visit.
    it("should remember a decline made in the browser's own dialog", async () => {
        // given
        const { result } = renderHook(() => useInstallPrompt());
        act(() => { window.dispatchEvent(createInstallEvent("dismissed")); });

        // when
        act(() => result.current.install());
        await waitFor(() => expect(result.current.variant).toBe("none"));

        // then
        const nextVisit = renderHook(() => useInstallPrompt());
        act(() => { window.dispatchEvent(createInstallEvent()); });
        expect(nextVisit.result.current.variant).toBe("none");
    });

    it("should stop offering once the app reports itself installed", () => {
        // given
        const { result } = renderHook(() => useInstallPrompt());
        act(() => { window.dispatchEvent(createInstallEvent()); });
        expect(result.current.variant).toBe("install");

        // when
        act(() => { window.dispatchEvent(new Event("appinstalled")); });

        // then
        expect(result.current.variant).toBe("none");
    });

    it("should offer nothing while the app is already running standalone", () => {
        // given
        setDisplayMode(true);
        const { result } = renderHook(() => useInstallPrompt());

        // when
        act(() => { window.dispatchEvent(createInstallEvent()); });

        // then
        expect(result.current.variant).toBe("none");
    });

    // iOS never fires beforeinstallprompt, so an installed iOS app is only distinguishable
    // through Safari's own non-standard navigator.standalone.
    it("should offer nothing on an iOS app already added to the home screen", () => {
        // given
        setAgent(iphoneSafariAgent);
        setIosStandalone(true);

        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("none");
    });

    it("should point an iOS browser at its share sheet", () => {
        // given
        setAgent(iphoneSafariAgent);

        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("iosShareSheet");
    });

    it("should tell a reader inside an app's browser to open Safari first", () => {
        // given
        setAgent(iphoneInAppAgent);

        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("iosInAppBrowser");
    });

    // iPadOS 13+ reports itself as a Mac; the touch points are the only thing separating it
    // from a desktop.
    it("should treat an iPad that claims to be a Mac as iOS", () => {
        // given
        setAgent(macAgent);
        setTouchPoints(5);

        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("iosShareSheet");
    });

    it("should offer nothing on a real Mac, which has no home screen to add to", () => {
        // given
        setAgent(macAgent);
        setTouchPoints(0);

        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("none");
    });

    it("should remember a dismissal across visits", () => {
        // given
        setAgent(iphoneSafariAgent);
        const { result } = renderHook(() => useInstallPrompt());

        // when
        act(() => result.current.dismiss());

        // then
        expect(result.current.variant).toBe("none");
        expect(renderHook(() => useInstallPrompt()).result.current.variant).toBe("none");
    });

    // A dismissal that lasted forever would mean one "not now" could never be revisited; one
    // that lasted a session would be an every-visit nag.
    it("should offer again once the dismissal has aged out", () => {
        // given
        setAgent(iphoneSafariAgent);
        const dismissedAt = Date.UTC(2026, 0, 1);
        const clock = vi.spyOn(Date, "now").mockReturnValue(dismissedAt);
        const { result } = renderHook(() => useInstallPrompt());
        act(() => result.current.dismiss());

        // when
        clock.mockReturnValue(dismissedAt + (31 * 24 * 60 * 60 * 1000));

        // then
        expect(renderHook(() => useInstallPrompt()).result.current.variant).toBe("iosShareSheet");
    });

    it("should keep the dismissal for this session when storage refuses the write", () => {
        // given
        setAgent(iphoneSafariAgent);
        vi.spyOn(window.localStorage, "setItem").mockImplementation(() => {
            throw new Error("site data is blocked");
        });

        const { result } = renderHook(() => useInstallPrompt());

        // when
        act(() => result.current.dismiss());

        // then
        expect(result.current.variant).toBe("none");
        expect(renderHook(() => useInstallPrompt()).result.current.variant).toBe("iosShareSheet");
    });

    it("should still offer when storage refuses the read", () => {
        // given
        setAgent(iphoneSafariAgent);
        vi.spyOn(window.localStorage, "getItem").mockImplementation(() => {
            throw new Error("site data is blocked");
        });

        // when
        const { result } = renderHook(() => useInstallPrompt());

        // then
        expect(result.current.variant).toBe("iosShareSheet");
    });
});

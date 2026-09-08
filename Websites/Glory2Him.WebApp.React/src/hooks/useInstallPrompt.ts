import { useCallback, useEffect, useState } from "react";

// Chromium fires this instead of installing on its own, and it is not in lib.dom.d.ts — the two
// members this hook actually uses are declared here rather than casting at every call site.
export interface BeforeInstallPromptEvent extends Event {
    prompt: () => Promise<void>;
    userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

// What the reader should be shown, if anything. The three offers are mutually exclusive because
// the platforms are: only Chromium can install from a button, and only iOS needs telling where
// the action lives.
export type InstallPromptVariant = "none" | "install" | "iosShareSheet" | "iosInAppBrowser";

export interface InstallPromptState {
    variant: InstallPromptVariant;
    install: () => void;
    dismiss: () => void;
}

// Declining has to stick, or the offer becomes an every-visit nag. It ages out rather than
// lasting forever: someone who said "not now" three months ago is a different reader from
// someone who said it this morning.
const dismissalKey = "g2h-install-prompt-dismissed";
const dismissalWindowMilliseconds = 30 * 24 * 60 * 60 * 1000;

// localStorage is guarded both ways — a private window, blocked site data, or a browser that
// throws on access all fall back to "not dismissed", which is the safe default because it is
// the one the reader can change from.
const readDismissedAt = (): number => {
    try {
        return Number(window.localStorage.getItem(dismissalKey)) || 0;
    } catch {
        return 0;
    }
};

const writeDismissedAt = (dismissedAt: number): void => {
    try {
        window.localStorage.setItem(dismissalKey, String(dismissedAt));
    } catch {
        // A reader whose browser cannot store the dismissal simply gets asked again next visit.
    }
};

const isDismissalCurrent = (): boolean =>
    Date.now() - readDismissedAt() < dismissalWindowMilliseconds;

// Two different signals because the platforms report it differently: everything modern answers
// the display-mode query, while an installed iOS app is only distinguishable through Safari's
// own non-standard navigator.standalone.
const isInstalled = (): boolean => {
    const iosStandalone = (window.navigator as Navigator & { standalone?: boolean }).standalone;

    return window.matchMedia?.("(display-mode: standalone)").matches === true
        || iosStandalone === true;
};

const isIos = (): boolean => {
    const agent = window.navigator.userAgent;

    // iPadOS 13+ reports itself as a Mac; the touch points are the only thing separating it
    // from a desktop Safari that has no home screen to add anything to.
    return /iPhone|iPod|iPad/.test(agent)
        || (/Macintosh/.test(agent) && window.navigator.maxTouchPoints > 1);
};

// Every browser on iOS is WebKit, but only a real browser tab carries the share sheet that has
// Add to Home Screen in it — Chrome and Edge on iOS both keep the "Safari/" token and both offer
// the action. An app's embedded webview (a link opened from WhatsApp, Teams, Gmail) drops that
// token and offers nothing, and it is exactly that reader who goes looking for the action,
// cannot find it, and concludes the site is not installable.
const isInAppBrowser = (): boolean => /Safari\//.test(window.navigator.userAgent) === false;

// Nothing in the UI otherwise tells a reader the app is installable, so installing it depends on
// them finding a browser menu item unaided. This decides whether to offer, and how — the
// component it feeds stays a pure renderer.
export const useInstallPrompt = (): InstallPromptState => {
    const [deferredEvent, setDeferredEvent] = useState<BeforeInstallPromptEvent | null>(null);
    const [isDismissed, setIsDismissed] = useState(isDismissalCurrent);
    const [isAlreadyInstalled, setIsAlreadyInstalled] = useState(isInstalled);

    useEffect(() => {
        const onBeforeInstallPrompt = (event: Event) => {
            // Chromium shows its own mini-infobar unless the event is cancelled, so without this
            // the reader gets asked for the same thing twice, in two different places.
            event.preventDefault();
            setDeferredEvent(event as BeforeInstallPromptEvent);
        };

        const onInstalled = () => {
            setDeferredEvent(null);
            setIsAlreadyInstalled(true);
        };

        // beforeinstallprompt fires once the manifest and worker have been checked, which is
        // well after this tree mounts — a listener attached here has not missed it.
        window.addEventListener("beforeinstallprompt", onBeforeInstallPrompt);
        window.addEventListener("appinstalled", onInstalled);

        return () => {
            window.removeEventListener("beforeinstallprompt", onBeforeInstallPrompt);
            window.removeEventListener("appinstalled", onInstalled);
        };
    }, []);

    const dismiss = useCallback(() => {
        writeDismissedAt(Date.now());
        setIsDismissed(true);
    }, []);

    const install = useCallback(() => {
        if (deferredEvent === null) {
            return;
        }

        // The event is single-use — Chromium refuses a second prompt() on the same one — so it
        // is dropped here whichever way the reader answers.
        setDeferredEvent(null);

        const promptForInstall = async () => {
            try {
                await deferredEvent.prompt();
                const { outcome } = await deferredEvent.userChoice;

                // Declining the browser's own dialog is a decline of the offer that raised it.
                // Without this the card would still be sitting there afterwards.
                if (outcome === "dismissed") {
                    dismiss();
                }
            } catch {
                // The browser refused or withdrew its prompt; the offer is gone either way.
            }
        };

        void promptForInstall();
    }, [deferredEvent, dismiss]);

    let variant: InstallPromptVariant = "none";

    if (isAlreadyInstalled === false && isDismissed === false) {
        if (deferredEvent !== null) {
            variant = "install";
        } else if (isIos()) {
            variant = isInAppBrowser() ? "iosInAppBrowser" : "iosShareSheet";
        }
    }

    return { variant, install, dismiss };
};

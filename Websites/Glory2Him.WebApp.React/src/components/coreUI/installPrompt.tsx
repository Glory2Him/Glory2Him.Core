import type { InstallPromptVariant } from "../../hooks/useInstallPrompt";

export interface InstallPromptProps {
    variant: InstallPromptVariant;
    onInstall: () => void;
    onDismiss: () => void;
}

// The app has been installable since the manifest and service worker shipped, but nothing said
// so — installing it meant finding a browser menu item unaided. This is the invitation.
//
// Pinned to the BOTTOM of the viewport rather than the top: the offline banner already owns the
// top edge (offlineBanner.tsx) and the two can be on screen together, and on a phone the bottom
// is also the end nearest the share button the iOS copy is pointing at. The z-index matches the
// banner's so neither disappears behind the sticky header.
//
// The extra bottom padding below sm is not decoration: the theme's back-to-top button is fixed
// 10px off the bottom-right corner (index.html's .back-top, z-index 99), and on a phone this
// card spans the full width, so at the default padding it covers the button's corner. From sm up
// the card is centred at 32rem and never reaches it, so the padding drops back.
//
// Which offer is shown is not this component's decision — useInstallPrompt.ts makes it, so the
// platform sniffing stays testable on its own and this stays a pure renderer.
export function InstallPrompt({ variant, onInstall, onDismiss }: InstallPromptProps) {
    if (variant === "none") {
        return null;
    }

    return (
        <div
            className="position-fixed bottom-0 start-0 end-0 p-3 pb-5 pb-sm-3"
            style={{ zIndex: 1030 }}
            role="region"
            aria-label="Install Glory 2 Him">
            <div
                className="alert alert-light border shadow-sm rounded-3 mb-0 mx-auto d-flex align-items-start gap-3"
                style={{ maxWidth: "32rem" }}>
                <i className="bi bi-phone fs-4 text-primary lh-1" aria-hidden="true"></i>

                <div className="flex-grow-1">
                    <h6 className="mb-1">Add Glory 2 Him to your home screen</h6>

                    {variant === "install" && (
                        <>
                            <p className="small mb-2">
                                Open it full screen, straight from your home screen, with no
                                browser bar in the way.
                            </p>

                            <button
                                type="button"
                                className="btn btn-sm btn-primary-soft mb-0"
                                onClick={onInstall}>
                                Install
                            </button>
                        </>
                    )}

                    {variant === "iosShareSheet" && (
                        <p className="small mb-0">
                            Tap <i className="bi bi-box-arrow-up" aria-hidden="true"></i>{" "}
                            <strong>Share</strong>, scroll down the list of actions, then choose{" "}
                            <strong>Add to Home Screen</strong>.
                        </p>
                    )}

                    {/* An app's built-in browser has no Add to Home Screen at all, so telling
                        this reader where to tap would send them looking for something that is
                        not there. */}
                    {variant === "iosInAppBrowser" && (
                        <p className="small mb-0">
                            Open this page in Safari first, using the menu in the corner. Then tap{" "}
                            <i className="bi bi-box-arrow-up" aria-hidden="true"></i>{" "}
                            <strong>Share</strong> and choose <strong>Add to Home Screen</strong>.
                        </p>
                    )}
                </div>

                <button
                    type="button"
                    className="btn-close"
                    aria-label="Dismiss"
                    onClick={onDismiss}></button>
            </div>
        </div>
    );
}

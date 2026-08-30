import { RefObject, useCallback, useEffect, useId, useRef, useState } from "react";

// Dismissal, labelling and focus for a menu this app drives itself.
//
// Every other dropdown in the codebase is Bootstrap's — userMenu, megaMenu, the theme switcher
// — and `data-bs-toggle` brings Escape, outside-click and focus handling with it. The review
// panel's three menus borrow the .dropdown classes but are driven by React state, so they get
// none of it: opened by keyboard, the only way out was to find the trigger and click it again.
//
// Adopting `data-bs-toggle` there instead was considered and rejected. Bootstrap's JS is loaded
// by a plain <script> tag in index.html, which vitest never evaluates, so its data API is absent
// under test — the panel's ~80 tests exist precisely because its menus are state-driven. A
// dismissal that could not be tested would be the wrong trade for a panel whose whole job is
// keyboard-heavy moderation work.
//
// So this is the shared home the behaviour lives in, once, rather than three times. It is the
// first such hook here; the next component that needs an in-app menu should use it rather than
// grow a fourth copy.
//
// WHAT THIS DELIBERATELY DOES NOT CLAIM. Consumers get `triggerId`/`menuId` for aria-controls
// and aria-labelledby, and `isOpen` for aria-expanded — all three are simply true of a
// disclosure. They do NOT get aria-haspopup, and a trigger using this hook must not add one.
// ARIA 1.1/1.2 define aria-haspopup="true" as synonymous with "menu", so asserting it promises
// assistive technology a role=menu with menuitem children and arrow-key navigation. None of
// that is here: the popups are role-less divs of plain buttons, and the reviewer picker is not
// even menu-SHAPED — it is a filter box over multi-select toggles, where this hook lands focus
// in the text field and the advertised Down-Arrow does nothing. Bootstrap 5 dropped
// aria-haspopup from its own dropdowns for exactly this reason.
//
// The honest options are a truthful disclosure (what this is) or the full menu pattern — roles,
// roving tabindex, Up/Down/Home/End, close-on-Tab. Announcing the second while implementing the
// first is the one thing worse than either.
export interface DismissableMenu {
    isOpen: boolean;

    // Wraps the trigger AND the menu. Outside-click is decided against this, so a click on the
    // trigger is never "outside" and cannot race the toggle into reopening what it just closed.
    containerRef: RefObject<HTMLDivElement | null>;

    triggerRef: RefObject<HTMLButtonElement | null>;
    menuRef: RefObject<HTMLDivElement | null>;

    // Paired so the menu can name its trigger as its accessible label, which is what tells a
    // screen-reader user which of three menus they have landed in.
    triggerId: string;
    menuId: string;

    toggle: () => void;
    close: (options?: { returnFocus?: boolean }) => void;
}

// Enough to find the first thing worth landing on, for consumers using initialFocus: "first".
// The picker's is its filter box, which is also where somebody opening it wants to be.
const focusableSelector = [
    "input:not([disabled])",
    "button:not([disabled])",
    "select:not([disabled])",
    "textarea:not([disabled])",
    "a[href]",
    "[tabindex]:not([tabindex='-1'])"
].join(",");

export interface DismissableMenuOptions {
    // "first" (default) focuses the first focusable control inside the menu, which is right for
    // the picker: its first control is the filter box the user came to type in. "container"
    // focuses the menu div itself instead, for menus whose first control is an action rather
    // than a field — the vote and decision dropdowns — so a stray second Enter on the trigger
    // does not fall through to that action. See #370.
    initialFocus?: "first" | "container";
}

export const useDismissableMenu = (options?: DismissableMenuOptions): DismissableMenu => {
    const initialFocus = options?.initialFocus ?? "first";
    const [isOpen, setIsOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement | null>(null);
    const triggerRef = useRef<HTMLButtonElement | null>(null);
    const menuRef = useRef<HTMLDivElement | null>(null);
    const triggerId = useId();
    const menuId = useId();

    // Whether the NEXT close should put focus back on the trigger. Escape and choosing an item
    // should — the user is still working the control. An outside click should not: they have
    // already moved somewhere else, and yanking focus back would undo their own click.
    const shouldReturnFocus = useRef(false);

    // Tracked so focus is only restored on a real open-to-closed transition, never on the first
    // render, which would steal focus from whatever the page had legitimately focused.
    const wasOpen = useRef(false);

    const close = useCallback((options?: { returnFocus?: boolean }) => {
        shouldReturnFocus.current = options?.returnFocus ?? true;
        setIsOpen(false);
    }, []);

    const toggle = useCallback(() => {
        // Focus is already on the trigger — the pointer or the keyboard just put it there — so
        // there is nothing to restore and trying would fight the browser.
        shouldReturnFocus.current = false;
        setIsOpen(current => current === false);
    }, []);

    useEffect(() => {
        if (isOpen === false) {
            return;
        }

        const onPointerDown = (event: MouseEvent) => {
            const container = containerRef.current;

            if (container != null && container.contains(event.target as Node) === false) {
                close({ returnFocus: false });
            }
        };

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                close({ returnFocus: true });
            }
        };

        // mousedown rather than click: a click that starts inside and ends outside is a drag,
        // not a dismissal, and the panel's picker rows are wide enough for that to happen.
        document.addEventListener("mousedown", onPointerDown);
        document.addEventListener("keydown", onKeyDown);

        return () => {
            document.removeEventListener("mousedown", onPointerDown);
            document.removeEventListener("keydown", onKeyDown);
        };
    }, [isOpen, close]);

    useEffect(() => {
        if (isOpen) {
            wasOpen.current = true;

            if (initialFocus === "container") {
                menuRef.current?.focus();
            } else {
                const firstFocusable = menuRef.current?.querySelector<HTMLElement>(focusableSelector);
                firstFocusable?.focus();
            }

            return;
        }

        if (wasOpen.current && shouldReturnFocus.current) {
            triggerRef.current?.focus();
        }

        wasOpen.current = false;
        shouldReturnFocus.current = false;
    }, [isOpen, initialFocus]);

    return {
        isOpen,
        containerRef,
        triggerRef,
        menuRef,
        triggerId,
        menuId,
        toggle,
        close
    };
};

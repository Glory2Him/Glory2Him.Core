import { useCallback, useEffect, useState } from "react";

export type Theme = "light" | "dark" | "auto";

// The Blogzine theme switcher, ported from the Blazor project's App.razor inline script. The
// first-paint script in index.html still applies the stored theme before React loads (so there
// is no flash), but its DOMContentLoaded wiring never finds the React-rendered
// [data-bs-theme-value] buttons — this hook owns the toggle instead: it reads/writes the
// 'theme' localStorage key, stamps data-bs-theme on <html> ('auto' resolves against
// prefers-color-scheme), and tracks OS scheme changes while 'auto' is selected.
const getStoredTheme = (): Theme | null =>
    localStorage.getItem("theme") as Theme | null;

const getPreferredTheme = (): Theme =>
    getStoredTheme() ?? "light";

const applyTheme = (theme: Theme): void => {
    if (theme === "auto"
        && window.matchMedia("(prefers-color-scheme: dark)").matches) {
        document.documentElement.setAttribute("data-bs-theme", "dark");
    } else {
        document.documentElement.setAttribute("data-bs-theme", theme);
    }
};

export const useTheme = (): { theme: Theme, setTheme: (theme: Theme) => void } => {
    const [theme, setThemeState] = useState<Theme>(getPreferredTheme);

    const setTheme = useCallback((nextTheme: Theme) => {
        localStorage.setItem("theme", nextTheme);
        applyTheme(nextTheme);
        setThemeState(nextTheme);
    }, []);

    useEffect(() => {
        applyTheme(theme);

        const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

        const onSchemeChange = () => {
            const storedTheme = getStoredTheme();

            if (storedTheme !== "light" && storedTheme !== "dark") {
                applyTheme(getPreferredTheme());
            }
        };

        mediaQuery.addEventListener("change", onSchemeChange);

        return () => mediaQuery.removeEventListener("change", onSchemeChange);
    }, [theme]);

    return { theme, setTheme };
};

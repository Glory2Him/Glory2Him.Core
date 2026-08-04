import { RefObject, useEffect, useRef } from "react";

// Initializes a Plyr media player (window.Plyr, loaded globally in index.html) on the element
// the returned ref is attached to. Mirrors the Blazor project's wwwroot/assets/js/functions.js,
// section "17 VIDEO PLAYER", which sets Plyr up over the template's .player-* elements — here
// each player owns its element via the ref, and the instance is destroyed on unmount so SPA
// navigation never leaks players.
export const usePlyr = <T extends HTMLElement = HTMLElement>(
    options?: PlyrOptions
): RefObject<T | null> => {
    const playerRef = useRef<T>(null);
    const optionsRef = useRef(options);
    optionsRef.current = options;

    useEffect(() => {
        const element = playerRef.current;

        if (!element || !window.Plyr) {
            return;
        }

        const instance = new window.Plyr(element, optionsRef.current ?? {});

        return () => instance.destroy();
    }, []);

    return playerRef;
};

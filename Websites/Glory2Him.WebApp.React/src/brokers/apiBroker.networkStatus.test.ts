import axios from 'axios';
import { describe, expect, it, vi } from 'vitest';
import {
    NETWORK_REACHABLE_EVENT,
    NETWORK_UNREACHABLE_EVENT,
    markNetworkReachable,
    markNetworkUnreachableIfUnreachable
} from './apiBroker';

// navigator.onLine only reports whether a network adapter is connected, not whether this origin
// is actually reachable — these two functions are what apiBroker.ts wires into axios's response
// interceptor so useOnlineStatus.ts can react to real request outcomes instead.
describe('apiBroker network-status signalling', () => {
    const sameOriginUrl = `${window.location.origin}/api/frontend-configurations`;
    const crossOriginUrl = 'https://third-party.example/widget';

    it('should announce reachability on a successful same-origin response', () => {
        // given
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const response = { status: 200, data: {}, config: { url: sameOriginUrl } } as never;

        // when
        const result = markNetworkReachable(response);

        // then
        expect(result).toBe(response);
        expect(dispatchSpy).toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_REACHABLE_EVENT }));
    });

    // GetAsyncAbsolute takes a caller-supplied absolute URI, so a same-origin API is not the
    // only thing this interceptor could ever see — a third party's own reachability says
    // nothing about ours.
    it('should stay quiet for a successful cross-origin response', () => {
        // given
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const response = { status: 200, data: {}, config: { url: crossOriginUrl } } as never;

        // when
        markNetworkReachable(response);

        // then
        expect(dispatchSpy).not.toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_REACHABLE_EVENT }));
    });

    it('should announce unreachability when a same-origin request never got a response', async () => {
        // given
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const networkError = Object.assign(new Error('Network Error'), {
            isAxiosError: true,
            response: undefined,
            config: { url: sameOriginUrl }
        });

        // when
        await expect(markNetworkUnreachableIfUnreachable(networkError)).rejects.toBe(networkError);

        // then
        expect(dispatchSpy).toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_UNREACHABLE_EVENT }));
    });

    it('should stay quiet for a cross-origin request that never got a response', async () => {
        // given
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const networkError = Object.assign(new Error('Network Error'), {
            isAxiosError: true,
            response: undefined,
            config: { url: crossOriginUrl }
        });

        // when
        await expect(markNetworkUnreachableIfUnreachable(networkError)).rejects.toBe(networkError);

        // then
        expect(dispatchSpy).not.toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_UNREACHABLE_EVENT }));
    });

    // A cancelled request (component unmount, a superseded call) never gets a response either,
    // but that means "something else superseded this call", not "the network is down".
    it('should stay quiet for a cancelled request', async () => {
        // given
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const cancelError = Object.assign(new axios.CanceledError('canceled'), {
            config: { url: sameOriginUrl }
        });

        // when
        await expect(markNetworkUnreachableIfUnreachable(cancelError)).rejects.toBe(cancelError);

        // then
        expect(dispatchSpy).not.toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_UNREACHABLE_EVENT }));
    });

    it('should stay quiet for an HTTP error the server actually answered', async () => {
        // given: a 404/500 proves the network works, unlike a request that got no response at all
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const httpError = Object.assign(new Error('Request failed with status code 404'), {
            isAxiosError: true,
            response: { status: 404, data: {} },
            config: { url: sameOriginUrl }
        });

        // when
        await expect(markNetworkUnreachableIfUnreachable(httpError)).rejects.toBe(httpError);

        // then
        expect(dispatchSpy).not.toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_UNREACHABLE_EVENT }));
    });

    it('should stay quiet for a non-axios error', async () => {
        // given
        const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
        const otherError = new Error('not an axios error');

        // when
        await expect(markNetworkUnreachableIfUnreachable(otherError)).rejects.toBe(otherError);

        // then
        expect(dispatchSpy).not.toHaveBeenCalledWith(
            expect.objectContaining({ type: NETWORK_UNREACHABLE_EVENT }));
    });

    it('should wire both functions into axios as its response interceptor', () => {
        // given: proves the module actually registers the interceptor at import time, not just
        // that the exported functions behave correctly in isolation
        const handlers = (axios.interceptors.response as unknown as {
            handlers: Array<{ fulfilled: unknown; rejected: unknown } | null>
        }).handlers;

        // then
        expect(handlers.some((handler) =>
            handler?.fulfilled === markNetworkReachable
            && handler?.rejected === markNetworkUnreachableIfUnreachable)).toBe(true);
    });
});

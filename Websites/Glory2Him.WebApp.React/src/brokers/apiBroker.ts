import axios, { AxiosResponse } from 'axios';

// navigator.onLine (useOnlineStatus.ts) only reports whether a network adapter is connected,
// not whether this origin is actually reachable. A response reaching the app at all — even an
// error response — proves the network works; a request that never got one (no `error.response`)
// is the real signal of connectivity loss. Exported as named functions rather than an inline
// interceptor so a test can invoke them directly without faking real HTTP traffic.
export const NETWORK_REACHABLE_EVENT = 'g2h-network-reachable';
export const NETWORK_UNREACHABLE_EVENT = 'g2h-network-unreachable';

// This interceptor is registered on the shared axios module, so it sees every request the app
// makes — not just the same-origin ones GetAsync/PostAsync/etc. build. GetAsyncAbsolute exists
// for a caller-supplied absolute URI, which could be cross-origin; a third-party outage there
// says nothing about whether OUR origin is reachable, so only same-origin requests get to
// report connectivity.
const isSameOriginUrl = (url: string | undefined): boolean => {
    if (!url) {
        return false;
    }

    try {
        return new URL(url, window.location.origin).origin === window.location.origin;
    } catch {
        return false;
    }
};

export const markNetworkReachable = (response: AxiosResponse): AxiosResponse => {
    if (isSameOriginUrl(response.config.url)) {
        window.dispatchEvent(new Event(NETWORK_REACHABLE_EVENT));
    }

    return response;
};

export const markNetworkUnreachableIfUnreachable = (error: unknown): Promise<never> => {
    // A cancelled request (AbortController/CancelToken) never gets a response either, but that
    // means "something else superseded this call," not "the network is down" — axios.isCancel
    // is the documented way to tell the two apart. No caller passes a signal through ApiBroker
    // today, so this only guards against the first one that does.
    if (axios.isAxiosError(error) && !error.response && !axios.isCancel(error)
        && isSameOriginUrl(error.config?.url)) {
        window.dispatchEvent(new Event(NETWORK_UNREACHABLE_EVENT));
    }

    return Promise.reject(error);
};

axios.interceptors.response.use(markNetworkReachable, markNetworkUnreachableIfUnreachable);

// Cookie-based authentication (ASP.NET Core Identity) — the browser sends the
// auth cookie automatically on same-origin requests, so no token handling is
// required here. In dev, Vite proxies /api to the ASP.NET Core host.
class ApiBroker {
    private config = { withCredentials: true };

    public async GetAsync(queryFragment: string): Promise<AxiosResponse> {
        return axios.get(queryFragment, this.config);
    }

    public async GetAsyncAbsolute(absoluteUri: string): Promise<AxiosResponse> {
        return axios.get(absoluteUri, this.config);
    }

    public async PostAsync(relativeUrl: string, data: unknown): Promise<AxiosResponse> {
        return axios.post(relativeUrl, data, this.config);
    }

    public async PostFormAsync(relativeUrl: string, data: FormData): Promise<AxiosResponse> {
        const headers = { "Content-Type": 'multipart/form-data' };
        return axios.post(relativeUrl, data, { ...this.config, headers });
    }

    public async PutAsync(relativeUrl: string, data: unknown): Promise<AxiosResponse> {
        return axios.put(relativeUrl, data, this.config);
    }

    public async DeleteAsync(relativeUrl: string): Promise<AxiosResponse> {
        return axios.delete(relativeUrl, this.config);
    }
}

export default ApiBroker;

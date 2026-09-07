import axios, { AxiosResponse } from 'axios';

// navigator.onLine (useOnlineStatus.ts) only reports whether a network adapter is connected,
// not whether this origin is actually reachable. A response reaching the app at all — even an
// error response — proves the network works; a request that never got one (no `error.response`)
// is the real signal of connectivity loss. Exported as named functions rather than an inline
// interceptor so a test can invoke them directly without faking real HTTP traffic.
export const NETWORK_REACHABLE_EVENT = 'g2h-network-reachable';
export const NETWORK_UNREACHABLE_EVENT = 'g2h-network-unreachable';

export const markNetworkReachable = (response: AxiosResponse): AxiosResponse => {
    window.dispatchEvent(new Event(NETWORK_REACHABLE_EVENT));

    return response;
};

export const markNetworkUnreachableIfUnreachable = (error: unknown): Promise<never> => {
    if (axios.isAxiosError(error) && !error.response) {
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

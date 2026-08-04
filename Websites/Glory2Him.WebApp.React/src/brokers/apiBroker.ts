import axios, { AxiosResponse } from 'axios';

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

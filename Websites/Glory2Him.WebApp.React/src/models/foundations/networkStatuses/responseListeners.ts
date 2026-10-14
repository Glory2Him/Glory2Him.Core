import { AxiosResponse } from 'axios';

export interface ResponseListeners {
    onResponse: (response: AxiosResponse) => void;
    onFailure: (error: unknown) => void;
}

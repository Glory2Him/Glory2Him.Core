import { useEffect } from 'react';

// React counterpart of Blazor's <PageTitle> — keeps the browser tab in step with the page.
export const useDocumentTitle = (title: string): void => {
    useEffect(() => {
        document.title = title;
    }, [title]);
};

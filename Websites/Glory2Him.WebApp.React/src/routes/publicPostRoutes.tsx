import { RouteObject } from 'react-router-dom';
import { Author } from '../pages/author';
import { Categories } from '../pages/categories';
import { Home } from '../pages/home';
import { JournalMasonry } from '../pages/journalMasonry';
import { PostGrid } from '../pages/postGrid';
import { PostList } from '../pages/postList';
import { PostSingle } from '../pages/postSingle';
import { Tag } from '../pages/tag';

// The public journal routes, mirroring the Blazor @page directives one for one. The orchestrator
// spreads these into the Root route's children in App.tsx.
export const publicPostRoutes: RouteObject[] = [
    { index: true, element: <Home /> },
    { path: 'Author', element: <Author /> },
    { path: 'Categories', element: <Categories /> },
    { path: 'Tag', element: <Tag /> },
    { path: 'Post-Grid', element: <PostGrid /> },
    { path: 'Post-List', element: <PostList /> },
    { path: 'Post-Grid-Masonry-Filter', element: <JournalMasonry /> },
    { path: 'Post-Single', element: <PostSingle /> },
    { path: 'Post-Single/:slug', element: <PostSingle /> },
];

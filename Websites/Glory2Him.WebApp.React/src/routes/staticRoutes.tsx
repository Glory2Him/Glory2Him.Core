import { Navigate, RouteObject } from 'react-router-dom';
import { About } from '../pages/about';
import { BibleReference } from '../pages/bibleReference';
import { BibleReader } from '../pages/bibleReader';
import { Contact } from '../pages/contact';
import { NotFound } from '../pages/notFound';
import { Search } from '../pages/search';
import { SearchResult } from '../pages/searchResult';
import { StyleGuide } from '../pages/styleGuide';

// The static (non-secured) pages, mirroring the Blazor @page routes one for one.
// Spread these into the root route's children; the "*" catch-all must stay last.
export const staticRoutes: RouteObject[] = [
    { path: 'About-Us', element: <About /> },
    { path: 'Contact-Us', element: <Contact /> },
    { path: 'BibleReferences', element: <BibleReference /> },
    { path: 'BibleReferences/BibleReader', element: <BibleReader /> },
    // The full-chapter page used to live at Full-Chapter; keep old links working.
    {
        path: 'BibleReferences/Full-Chapter',
        element: <Navigate to="/BibleReferences/BibleReader" replace />,
    },
    { path: 'Search', element: <Search /> },
    { path: 'Search-Result', element: <SearchResult /> },
    { path: 'Style-Guide', element: <StyleGuide /> },
    { path: 'Not-Found', element: <NotFound /> },
    { path: '*', element: <NotFound /> },
];

import { Navigate, RouteObject } from 'react-router-dom';
import { Author } from '../pages/author';
import { Categories } from '../pages/categories';
import { Contribute } from '../pages/contribute';
import { Home } from '../pages/home';
import { JournalMasonry } from '../pages/journalMasonry';
import { PostDetail } from '../pages/postDetail';
import { PostGrid } from '../pages/postGrid';
import { PostList } from '../pages/postList';
import { MyPosts } from '../pages/myPosts';
import { Posts } from '../pages/posts';
import { SecuredRoute } from '../components/securitys/securedRoutes';
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
    // REST puts the collection first and plural, so the contribution surface is a member of
    // `posts` rather than a verb hung off a singular noun. The literal segment is declared before
    // the parameter one for the reader's sake — React Router ranks a static segment above a
    // dynamic one whatever the order — so /posts/contribute is the form and /posts/{id} is an
    // item.
    // The collection itself: every contribution, searched and scrolled. Declared before
    // its two members for the reader's sake - React Router ranks a static segment above a
    // dynamic one whatever the order.
    { path: 'posts', element: <Posts /> },
    { path: 'posts/contribute', element: <Contribute /> },

    // The caller's own contributions, in the public layout — lowercase like the rest of the
    // posts family. Secured with no role list: there is no "my" for a visitor, and any
    // authenticated reader owns whatever they contributed.
    {
        path: 'myposts',
        element:
            <SecuredRoute>
                <MyPosts />
            </SecuredRoute>,
    },
    {
        // One of MY posts — where /posts/contribute lands a fresh submission, so the
        // contributor reads their draft on their own surface rather than the public one.
        // The same detail page serves it: the caller-scoped read already shows an owner
        // their own row at any status.
        path: 'myposts/:contentItemId',
        element:
            <SecuredRoute>
                <PostDetail />
            </SecuredRoute>,
    },
    { path: 'posts/:contentItemId', element: <PostDetail /> },

    // The route this page used to answer on. Kept so links already in the wild — and the
    // sample pages that hard-code it — still land somewhere.
    { path: 'post/contribute', element: <Navigate to="/posts/contribute" replace /> },
];

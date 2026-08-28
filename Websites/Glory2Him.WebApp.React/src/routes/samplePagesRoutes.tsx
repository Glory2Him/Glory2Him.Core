import { ComponentType, ReactElement, Suspense, lazy } from 'react';
import { RouteObject } from 'react-router-dom';
import SidebarLayout from '../components/layouts/sidebarLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import { Spinner } from '../components/coreUI/spinner';
import securityPoints from '../securityMatrix';

// The demos are admin-only reference pages, so they are code-split out of the main
// bundle: each page loads on first visit (React.lazy), keeping the public bundle lean.
const lazyNamed = <T extends Record<string, ComponentType>, K extends keyof T & string>(
    loader: () => Promise<T>,
    exportName: K) =>
    lazy(async () => ({ default: (await loader())[exportName] }));

const AssociationPanelDoc = lazyNamed(() => import('../pages/samplePages/components/associationPanelDoc'), 'AssociationPanelDoc');
const BibleReferenceAssociationPanelDoc = lazyNamed(() => import('../pages/samplePages/components/bibleReferenceAssociationPanelDoc'), 'BibleReferenceAssociationPanelDoc');
const TagAssociationPanelDoc = lazyNamed(() => import('../pages/samplePages/components/tagAssociationPanelDoc'), 'TagAssociationPanelDoc');
const BibleReferenceFullChapterSample = lazyNamed(() => import('../pages/samplePages/bibleReferences/bibleReferenceFullChapterSample'), 'BibleReferenceFullChapterSample');
const BibleReferencePartialSample = lazyNamed(() => import('../pages/samplePages/bibleReferences/bibleReferencePartialSample'), 'BibleReferencePartialSample');
const DashboardSample = lazyNamed(() => import('../pages/samplePages/dashboardSample'), 'DashboardSample');
const HomeBlogClassicSample = lazyNamed(() => import('../pages/samplePages/home/homeBlogClassicSample'), 'HomeBlogClassicSample');
const HomeBlogPodcastSample = lazyNamed(() => import('../pages/samplePages/home/homeBlogPodcastSample'), 'HomeBlogPodcastSample');
const HomeBlogTechSample = lazyNamed(() => import('../pages/samplePages/home/homeBlogTechSample'), 'HomeBlogTechSample');
const HomeDefaultSample = lazyNamed(() => import('../pages/samplePages/home/homeDefaultSample'), 'HomeDefaultSample');
const HomeMagazineSample = lazyNamed(() => import('../pages/samplePages/home/homeMagazineSample'), 'HomeMagazineSample');
const LifestyleSample = lazyNamed(() => import('../pages/samplePages/lifestyleSample'), 'LifestyleSample');
const AboutSample = lazyNamed(() => import('../pages/samplePages/pages/aboutSample'), 'AboutSample');
const ContactSample = lazyNamed(() => import('../pages/samplePages/pages/contactSample'), 'ContactSample');
const Error404Sample = lazyNamed(() => import('../pages/samplePages/pages/error404Sample'), 'Error404Sample');
const OfflineSample = lazyNamed(() => import('../pages/samplePages/pages/offlineSample'), 'OfflineSample');
const SigninSample = lazyNamed(() => import('../pages/samplePages/pages/signinSample'), 'SigninSample');
const SignupSample = lazyNamed(() => import('../pages/samplePages/pages/signupSample'), 'SignupSample');
const PaginationStylesSample = lazyNamed(() => import('../pages/samplePages/post/paginationStylesSample'), 'PaginationStylesSample');
const PodcastSingleSample = lazyNamed(() => import('../pages/samplePages/post/podcastSingleSample'), 'PodcastSingleSample');
const PostCardSample = lazyNamed(() => import('../pages/samplePages/post/postCardSample'), 'PostCardSample');
const PostGrid4ColSample = lazyNamed(() => import('../pages/samplePages/post/postGrid4ColSample'), 'PostGrid4ColSample');
const PostGridMasonryFilterSample = lazyNamed(() => import('../pages/samplePages/post/postGridMasonryFilterSample'), 'PostGridMasonryFilterSample');
const PostGridMasonrySample = lazyNamed(() => import('../pages/samplePages/post/postGridMasonrySample'), 'PostGridMasonrySample');
const PostGridSample = lazyNamed(() => import('../pages/samplePages/post/postGridSample'), 'PostGridSample');
const PostListSample = lazyNamed(() => import('../pages/samplePages/post/postListSample'), 'PostListSample');
const PostMixedLargeThenGridSample = lazyNamed(() => import('../pages/samplePages/post/postMixedLargeThenGridSample'), 'PostMixedLargeThenGridSample');
const PostOverlaySample = lazyNamed(() => import('../pages/samplePages/post/postOverlaySample'), 'PostOverlaySample');
const PostSingleCardSample = lazyNamed(() => import('../pages/samplePages/post/postSingleCardSample'), 'PostSingleCardSample');
const PostSingleClassicSample = lazyNamed(() => import('../pages/samplePages/post/postSingleClassicSample'), 'PostSingleClassicSample');
const PostSingleMagazineSample = lazyNamed(() => import('../pages/samplePages/post/postSingleMagazineSample'), 'PostSingleMagazineSample');
const PostSingleMinimalSample = lazyNamed(() => import('../pages/samplePages/post/postSingleMinimalSample'), 'PostSingleMinimalSample');
const PostSingleReviewSample = lazyNamed(() => import('../pages/samplePages/post/postSingleReviewSample'), 'PostSingleReviewSample');
const PostSingleVideoSample = lazyNamed(() => import('../pages/samplePages/post/postSingleVideoSample'), 'PostSingleVideoSample');
const PostTypesSample = lazyNamed(() => import('../pages/samplePages/post/postTypesSample'), 'PostTypesSample');
const SamplePagesIndex = lazyNamed(() => import('../pages/samplePages/samplePagesIndex'), 'SamplePagesIndex');

// The Blogzine layout demos, mirroring the Blazor SamplePages routes exactly. Every page is
// Administrators-only ([Authorize(Roles = "Administrators")] in Blazor). The index sits in
// the admin shell (SidebarLayout, as its @layout directive did) so "Back to Sample Pages"
// lands back in the admin area; the demos themselves render full width inside the site
// chrome, exactly as their Blazor counterparts did with the default layout.
const secured = (element: ReactElement): ReactElement => (
    <SecuredRoute allowedRoles={securityPoints.admin.view}>
        <Suspense fallback={<div className="text-center py-5"><Spinner /></div>}>
            {element}
        </Suspense>
    </SecuredRoute>
);

export const samplePagesRoutes: RouteObject[] = [
    {
        // The component reference pages keep the admin shell rather than rendering full width:
        // they are documentation you read across, so the tree stays on the left.
        element: <SidebarLayout />,
        children: [
            { path: 'SamplePages', element: secured(<SamplePagesIndex />) },

            {
                path: 'SamplePages/Components/Association-Panel',
                element: secured(<AssociationPanelDoc />),
            },
            {
                path: 'SamplePages/Components/Tag-Association-Panel',
                element: secured(<TagAssociationPanelDoc />),
            },
            {
                path: 'SamplePages/Components/Bible-Reference-Association-Panel',
                element: secured(<BibleReferenceAssociationPanelDoc />),
            },
        ],
    },
    {
        path: 'SamplePages/Home/Default',
        element: secured(<HomeDefaultSample />),
    },
    {
        path: 'SamplePages/Home/Magazine',
        element: secured(<HomeMagazineSample />),
    },
    {
        path: 'SamplePages/Home/Blog-Classic',
        element: secured(<HomeBlogClassicSample />),
    },
    {
        path: 'SamplePages/Home/Blog-Tech',
        element: secured(<HomeBlogTechSample />),
    },
    {
        path: 'SamplePages/Home/Blog-Podcast',
        element: secured(<HomeBlogPodcastSample />),
    },
    {
        path: 'SamplePages/Pages/About',
        element: secured(<AboutSample />),
    },
    {
        path: 'SamplePages/Pages/Contact',
        element: secured(<ContactSample />),
    },
    {
        path: 'SamplePages/Pages/Error-404',
        element: secured(<Error404Sample />),
    },
    {
        path: 'SamplePages/Pages/Signin',
        element: secured(<SigninSample />),
    },
    {
        path: 'SamplePages/Pages/Signup',
        element: secured(<SignupSample />),
    },
    {
        path: 'SamplePages/Pages/Offline',
        element: secured(<OfflineSample />),
    },
    {
        path: 'SamplePages/Post/Post-Grid/Post-Grid',
        element: secured(<PostGridSample />),
    },
    {
        path: 'SamplePages/Post/Post-Grid/Post-Grid-4-Col',
        element: secured(<PostGrid4ColSample />),
    },
    {
        path: 'SamplePages/Post/Post-Grid/Post-Grid-Masonry',
        element: secured(<PostGridMasonrySample />),
    },
    {
        path: 'SamplePages/Post/Post-Grid/Post-Grid-Masonry-Filter',
        element: secured(<PostGridMasonryFilterSample />),
    },
    {
        path: 'SamplePages/Post/Post-Grid/Post-Mixed-Large-Then-Grid',
        element: secured(<PostMixedLargeThenGridSample />),
    },
    {
        path: 'SamplePages/Post/Post-List',
        element: secured(<PostListSample />),
    },
    {
        path: 'SamplePages/Post/Post-Card',
        element: secured(<PostCardSample />),
    },
    {
        path: 'SamplePages/Post/Post-Overlay',
        element: secured(<PostOverlaySample />),
    },
    {
        path: 'SamplePages/Post/Post-Types',
        element: secured(<PostTypesSample />),
    },
    {
        path: 'SamplePages/Post/Post-Single-Magazine',
        element: secured(<PostSingleMagazineSample />),
    },
    {
        path: 'SamplePages/Post/Post-Single-Classic',
        element: secured(<PostSingleClassicSample />),
    },
    {
        path: 'SamplePages/Post/Post-Single-Minimal',
        element: secured(<PostSingleMinimalSample />),
    },
    {
        path: 'SamplePages/Post/Post-Single-Card',
        element: secured(<PostSingleCardSample />),
    },
    {
        path: 'SamplePages/Post/Post-Single-Review',
        element: secured(<PostSingleReviewSample />),
    },
    {
        path: 'SamplePages/Post/Post-Single-Video',
        element: secured(<PostSingleVideoSample />),
    },
    {
        path: 'SamplePages/Post/Podcast-Single',
        element: secured(<PodcastSingleSample />),
    },
    {
        path: 'SamplePages/Post/Pagination-Styles',
        element: secured(<PaginationStylesSample />),
    },
    {
        path: 'SamplePages/BibleReferences/BibleReference-Single-verse',
        element: secured(<BibleReferencePartialSample />),
    },
    {
        path: 'SamplePages/BibleReferences/BibleReference-Full-Chapter',
        element: secured(<BibleReferenceFullChapterSample />),
    },
    {
        path: 'SamplePages/Lifestyle',
        element: secured(<LifestyleSample />),
    },
    {
        path: 'SamplePages/Dashboard',
        element: secured(<DashboardSample />),
    },
];

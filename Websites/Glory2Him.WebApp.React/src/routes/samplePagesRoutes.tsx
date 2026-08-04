import { ReactElement } from 'react';
import { RouteObject } from 'react-router-dom';
import SidebarLayout from '../components/layouts/sidebarLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import securityPoints from '../securityMatrix';
import { BibleReferenceFullChapterSample } from '../pages/samplePages/bibleReferences/bibleReferenceFullChapterSample';
import { BibleReferencePartialSample } from '../pages/samplePages/bibleReferences/bibleReferencePartialSample';
import { DashboardSample } from '../pages/samplePages/dashboardSample';
import { HomeBlogClassicSample } from '../pages/samplePages/home/homeBlogClassicSample';
import { HomeBlogPodcastSample } from '../pages/samplePages/home/homeBlogPodcastSample';
import { HomeBlogTechSample } from '../pages/samplePages/home/homeBlogTechSample';
import { HomeDefaultSample } from '../pages/samplePages/home/homeDefaultSample';
import { HomeMagazineSample } from '../pages/samplePages/home/homeMagazineSample';
import { LifestyleSample } from '../pages/samplePages/lifestyleSample';
import { AboutSample } from '../pages/samplePages/pages/aboutSample';
import { ContactSample } from '../pages/samplePages/pages/contactSample';
import { Error404Sample } from '../pages/samplePages/pages/error404Sample';
import { OfflineSample } from '../pages/samplePages/pages/offlineSample';
import { SigninSample } from '../pages/samplePages/pages/signinSample';
import { SignupSample } from '../pages/samplePages/pages/signupSample';
import { PaginationStylesSample } from '../pages/samplePages/post/paginationStylesSample';
import { PodcastSingleSample } from '../pages/samplePages/post/podcastSingleSample';
import { PostCardSample } from '../pages/samplePages/post/postCardSample';
import { PostGrid4ColSample } from '../pages/samplePages/post/postGrid4ColSample';
import { PostGridMasonryFilterSample } from '../pages/samplePages/post/postGridMasonryFilterSample';
import { PostGridMasonrySample } from '../pages/samplePages/post/postGridMasonrySample';
import { PostGridSample } from '../pages/samplePages/post/postGridSample';
import { PostListSample } from '../pages/samplePages/post/postListSample';
import { PostMixedLargeThenGridSample } from '../pages/samplePages/post/postMixedLargeThenGridSample';
import { PostOverlaySample } from '../pages/samplePages/post/postOverlaySample';
import { PostSingleCardSample } from '../pages/samplePages/post/postSingleCardSample';
import { PostSingleClassicSample } from '../pages/samplePages/post/postSingleClassicSample';
import { PostSingleMagazineSample } from '../pages/samplePages/post/postSingleMagazineSample';
import { PostSingleMinimalSample } from '../pages/samplePages/post/postSingleMinimalSample';
import { PostSingleReviewSample } from '../pages/samplePages/post/postSingleReviewSample';
import { PostSingleVideoSample } from '../pages/samplePages/post/postSingleVideoSample';
import { PostTypesSample } from '../pages/samplePages/post/postTypesSample';
import { SamplePagesIndex } from '../pages/samplePages/samplePagesIndex';

// The Blogzine layout demos, mirroring the Blazor SamplePages routes exactly. Every page is
// Administrators-only ([Authorize(Roles = "Administrators")] in Blazor). The index sits in
// the admin shell (SidebarLayout, as its @layout directive did) so "Back to Sample Pages"
// lands back in the admin area; the demos themselves render full width inside the site
// chrome, exactly as their Blazor counterparts did with the default layout.
const secured = (element: ReactElement): ReactElement => (
    <SecuredRoute allowedRoles={securityPoints.admin.view}>
        {element}
    </SecuredRoute>
);

export const samplePagesRoutes: RouteObject[] = [
    {
        element: <SidebarLayout />,
        children: [
            { path: 'SamplePages', element: secured(<SamplePagesIndex />) },
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

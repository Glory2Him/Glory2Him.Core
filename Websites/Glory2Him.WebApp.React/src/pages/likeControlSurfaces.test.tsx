import { ReactElement } from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Home } from './home';
import { MyPostDetail } from './myPostDetail';
import { MyPosts } from './myPosts';
import { PostDetail } from './postDetail';
import { Posts } from './posts';
import { ContentItemModerationPage } from './admin/contentItemModerationPage';

import {
    ContentItemModerationDetailPage
} from './admin/contentItemModerationDetailPage';

import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs } from '../tests/testAuth';
import { testContentItemSetting } from '../tests/testContentItemSettings';

// THE INVENTORY. Turning a type's ShowReactions on is supposed to be SUFFICIENT — there must be
// no second switch, held by a page, that an administrator cannot find and no setting can
// override (§DOM6.5). That claim is only true if every surface that renders a content item card
// passes the handlers, so it is asserted once per surface rather than once per rule: a future
// page that forgets is exactly what this file is here to catch.
//
// Series is the fixture, seeded with reactionsAllowed and showReactions BOTH false
// (ContentItemSettingSeedData.cs) and flipped on here — the administrator's edit, made in a
// test. One assertion per surface, deliberately not a matrix: what varies between the seven is
// the surface, and nothing else.
const authState = createAuthState();
let contentItem: ContentItem | undefined;

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemSearchPageSize: 8,

    contentItemService: {
        useSearchContentItems: () => ({
            data: {
                pages: [{
                    items: contentItem == null ? [] : [contentItem],
                    pageIndex: 0,
                    pageSize: 8,
                    hasNextPage: false
                }]
            },
            isLoading: false,
            isError: false,
            hasNextPage: false,
            isFetchingNextPage: false,
            fetchNextPage: vi.fn()
        }),

        useGetContentItemById: () => ({
            data: contentItem,
            isLoading: false,
            isError: false,
            refetch: vi.fn()
        }),

        useModifyContentItem: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useRemoveContentItem: () => ({ mutateAsync: vi.fn(), isPending: false })
    }
}));

// THE ADMINISTRATOR'S EDIT: Series ships with the facet pair off, and this is it turned on.
const seriesSetting = testContentItemSetting(ContentType.Series, 'Series', {
    reactionsAllowed: true,
    showReactions: true
});

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [seriesSetting], isLoading: false, isError: false }),
        useGetEffectiveSettingsFor: () =>
            ({ data: [seriesSetting], isLoading: false, isError: false }),
        useCreateOrUpdateContentItemSettingOverride: () =>
            ({ mutateAsync: vi.fn(), isPending: false }),
        useHardRemoveContentItemSetting: () => ({ mutateAsync: vi.fn(), isPending: false })
    }
}));

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({
            data: [{
                id: 'reaction-1',
                name: 'Amen',
                unicodeEmoji: '👍',
                isPublished: true,
                approvalStatus: 2,
                isDeleted: false
            }]
        })
    }
}));

vi.mock('../services/foundations/contributorService', () => ({
    contributorService: {
        useGetContributorById: () => ({ data: undefined })
    }
}));

// The moderation detail page assembles an approval round beside the card. None of it is this
// file's subject; it is mocked so the page renders at all, because an unwrapped useQuery in a
// harness with no QueryClientProvider would fail for a reason that is not the surface's.
vi.mock('../services/foundations/approvalService', () => ({
    approvalService: {
        useGetApprovalVerdict: () => ({ data: undefined, isLoading: false, refetch: vi.fn() }),
        useGetApprovalReviews: () => ({ data: [], isLoading: false, refetch: vi.fn() }),
        useGetReviewerCandidates: () => ({ data: [], refetch: vi.fn() }),
        useGetReviewRequests: () => ({ data: [], refetch: vi.fn() }),
        useGetReviewerDisplayNames: () => ({ data: [], refetch: vi.fn() }),
        useCastApprovalReview: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useDecideApproval: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useResetApproval: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useRequestReview: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useWithdrawReviewRequest: () => ({ mutateAsync: vi.fn(), isPending: false })
    }
}));

vi.mock('../services/foundations/aiReviewerService', () => ({
    aiReviewerService: {
        useGetAIReviewerStatus: () => ({
            data: {
                isOffered: true,
                isRequested: false,
                isAIReviewCompleted: false,
                isAIReviewCommentsPresent: false
            },
            refetch: vi.fn()
        }),

        useAssignAIReviewer: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useWithdrawAIReviewer: () => ({ mutateAsync: vi.fn(), isPending: false })
    }
}));

vi.mock('../services/foundations/approvalCommentService', () => ({
    approvalCommentService: {
        useGetApprovalComments: () => ({ data: [], isLoading: false, refetch: vi.fn() }),
        useAddApprovalComment: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useModifyApprovalComment: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useRemoveApprovalComment: () => ({ mutateAsync: vi.fn(), isPending: false }),
        useResolveApprovalComment: () => ({ mutateAsync: vi.fn(), isPending: false })
    }
}));

// WHO WROTE IT decides what a reader may do with it, so the public surfaces read an item
// somebody ELSE contributed — the signed-in account is always user-1, and an item created by
// user-1 opens every ownership gate and proves nothing about a reader.
const seriesItemBy = (createdBy: string): ContentItem => ({
    id: 'series-1',
    contentType: ContentType.Series,
    title: 'Walking through Philippians',
    author: null,
    content: 'Eight weeks in the letter of joy.',
    shareabilityBasis: ShareabilityBasis.Owned,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: '2026-07-03T00:00:00Z',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy,
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: createdBy,
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
});

const renderSurface = (
    page: ReactElement,
    routePath: string,
    initialUrl: string) =>
    render(
        <MemoryRouter initialEntries={[initialUrl]}>
            <AuthProvider>
                <Routes>
                    <Route path={routePath} element={page} />
                </Routes>
            </AuthProvider>
        </MemoryRouter>);

// Every surface in the table: the page, the route it is read through, who is reading it, and
// whose item they are reading.
const surfaces: ReadonlyArray<{
    surface: string;
    page: ReactElement;
    routePath: string;
    initialUrl: string;
    roles: Array<string>;
    contributedBy: string;
}> = [
    {
        surface: '/',
        page: <Home />,
        routePath: '/',
        initialUrl: '/',
        roles: ['Users'],
        contributedBy: 'contributor-9'
    },
    {
        surface: '/posts',
        page: <Posts />,
        routePath: '/posts',
        initialUrl: '/posts',
        roles: ['Users'],
        contributedBy: 'contributor-9'
    },
    {
        surface: '/posts/{id}',
        page: <PostDetail />,
        routePath: '/posts/:contentItemId',
        initialUrl: '/posts/series-1',
        roles: ['Users'],
        contributedBy: 'contributor-9'
    },
    {
        // MY OWN, and only ever my own: /myposts is a caller-scoped read, so the contributor
        // here is the reader by definition rather than by fixture convenience.
        surface: '/myposts',
        page: <MyPosts />,
        routePath: '/myposts',
        initialUrl: '/myposts',
        roles: ['Users'],
        contributedBy: 'user-1'
    },
    {
        surface: '/myposts/{id}',
        page: <MyPostDetail />,
        routePath: '/myposts/:contentItemId',
        initialUrl: '/myposts/series-1',
        roles: ['Users'],
        contributedBy: 'user-1'
    },
    {
        surface: 'the moderation queue',
        page: <ContentItemModerationPage />,
        routePath: '/Admin/Posts',
        initialUrl: '/Admin/Posts',
        roles: ['Administrators'],
        contributedBy: 'contributor-9'
    },
    {
        surface: 'the moderation detail page',
        page: <ContentItemModerationDetailPage />,
        routePath: '/Admin/Posts/:contentItemId',
        initialUrl: '/Admin/Posts/series-1',
        roles: ['Administrators'],
        contributedBy: 'contributor-9'
    }
];

describe('The like control across every surface that renders a content item card', () => {
    beforeEach(() => {
        contentItem = undefined;
    });

    it.each(surfaces)(
        'should offer the like control on every surface once the setting allows reactions '
        + '($surface)',
        ({ page, routePath, initialUrl, roles, contributedBy }) => {
            // given
            contentItem = seriesItemBy(contributedBy);
            signInAs(authState, roles);

            // when
            renderSurface(page, routePath, initialUrl);

            // then
            expect(screen.getByRole('button', { name: /Like/ })).toBeInTheDocument();
        });
});

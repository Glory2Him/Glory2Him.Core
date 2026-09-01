import { ReactElement } from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Home } from './home';
import { MyPosts } from './myPosts';
import { ContentItemModerationPage } from './admin/contentItemModerationPage';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs, signOut } from '../tests/testAuth';

import {
    ContentItemPage
} from '../models/foundations/contentItems/contentItemSearchQuery';

import {
    ContentItemSearchCriteria
} from '../models/components/contentItems/contentItemSearchItem';

// Three surfaces over one family, and what this suite pins is exactly what DIFFERS between them:
// which read each page feeds the panel from, and what each pins onto it. Everything the pages
// share — projection, URL round trip, paging — is posts.test.tsx's subject.
const authState = createAuthState();
let searchedOptions: Record<string, unknown> | null = null;
let pages: ContentItemPage[] = [];

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemSearchPageSize: 8,

    contentItemService: {
        useSearchContentItems: (
            criteria: ContentItemSearchCriteria,
            options: Record<string, unknown>) => {
            void criteria;
            searchedOptions = options;

            return {
                data: { pages },
                isLoading: false,
                isError: false,
                hasNextPage: false,
                isFetchingNextPage: false,
                fetchNextPage: vi.fn()
            };
        }
    }
}));

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({ data: [] })
    }
}));

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [] }),
        useGetEffectiveSettingsFor: () => ({ data: [] })
    }
}));

const contentItemFor = (overrides: Partial<ContentItem> = {}): ContentItem => ({
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    title: 'Grace for the ordinary Tuesday',
    author: 'Miriam Vale',
    content: 'Grace is not a one-time event.',
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
    createdBy: 'user-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'user-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null,
    ...overrides
});

const renderPage = (page: ReactElement, initialUrl = '/') =>
    render(
        <MemoryRouter initialEntries={[initialUrl]}>
            <AuthProvider>{page}</AuthProvider>
        </MemoryRouter>);

describe('The content item feed pages', () => {
    beforeEach(() => {
        searchedOptions = null;
        pages = [{ items: [contentItemFor()], pageIndex: 0, pageSize: 8, hasNextPage: false }];
        signOut(authState);
    });

    describe('Home', () => {
        // §14.1 by construction: the front page builds on the caller-INDEPENDENT read, so no
        // role change anywhere can leak a draft onto it.
        it('should feed the panel from the public read', () => {
            // when
            renderPage(<Home />);

            // then
            expect(searchedOptions).toEqual(expect.objectContaining({ scope: 'public' }));
        });

        // The button is a courtesy, never a boundary — the server re-decides against the stored
        // row — but a visitor with no account has nothing to edit, so they get no button.
        it('should offer Edit to a signed-in reader and not to a visitor', () => {
            // given / when: a visitor
            const rendered = renderPage(<Home />);

            // then
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();

            // given / when: signed in
            rendered.unmount();
            signInAs(authState, ['Users']);
            renderPage(<Home />);

            // then
            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
        });

        it('should keep the verse of the day above the feed', () => {
            // when
            renderPage(<Home />);

            // then
            expect(screen.getByText(/Verse of the day/i)).toBeInTheDocument();

            expect(screen.getByRole('button', { name: 'Grace for the ordinary Tuesday' }))
                .toBeInTheDocument();
        });
    });

    describe('MyPosts', () => {
        it('should pin the read to the signed-in account', () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderPage(<MyPosts />, '/myposts');

            // then: user-1 is the id signInAs mints
            expect(searchedOptions).toEqual(
                expect.objectContaining({ scope: 'caller', submittedById: 'user-1' }));
        });

        // The page must never ask for everybody's rows while the identity is still arriving.
        it('should hold the read while the account id has not resolved', () => {
            // when
            renderPage(<MyPosts />, '/myposts');

            // then
            expect(searchedOptions).toEqual(expect.objectContaining({ enabled: false }));
        });

        // The whole point of "my posts": the reader sees their own rows wearing their status.
        it('should show the caller their own draft wearing its badge', () => {
            // given
            signInAs(authState, ['Users']);

            pages = [{
                items: [contentItemFor({
                    id: 'draft-1',
                    title: 'When the answer is wait',
                    approvalStatus: ApprovalStatus.Draft,
                    isPublished: false,
                    publishDate: null
                })],
                pageIndex: 0,
                pageSize: 8,
                hasNextPage: false
            }];

            // when
            renderPage(<MyPosts />, '/myposts');

            // then: the badge AND the corner ribbon — /myposts opts into ribbons, so the
            // status arrives twice, each through its own affordance.
            expect(screen.getAllByText('Draft')).toHaveLength(2);

            expect(document.querySelector('.g2h-approval-ribbon'))
                .toHaveAttribute('data-approval-status', 'Draft');
        });
    });

    describe('ContentItemModerationPage', () => {
        it('should pin the queue to the statuses a moderator acts on', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderPage(<ContentItemModerationPage />, '/Admin/Posts');

            // then: Draft + Submitted — the third clause (approved with unapproved
            // associations) is blocked on #318 and recorded on the issue, not approximated.
            expect(searchedOptions).toEqual(
                expect.objectContaining({
                    scope: 'caller',
                    approvalStatuses: [ApprovalStatus.Draft, ApprovalStatus.Submitted]
                }));
        });

        it('should render in the admin chrome with its breadcrumb', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderPage(<ContentItemModerationPage />, '/Admin/Posts');

            // then
            expect(screen.getByRole('heading', { name: 'Posts awaiting moderation', level: 1 }))
                .toBeInTheDocument();
        });
    });
});

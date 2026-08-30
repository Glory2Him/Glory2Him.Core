import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PostDetail } from './postDetail';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentItemSetting } from '../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { createAuthState, signInAs, signOut } from '../tests/testAuth';

const authState = createAuthState();
let contentItem: ContentItem | undefined;
let isLoading = false;
let isError = false;
let requestedId = '';

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemService: {
        useGetContentItemById: (contentItemId: string) => {
            requestedId = contentItemId;

            return { data: contentItem, isLoading, isError };
        }
    }
}));

const testimonySetting: ContentItemSetting = {
    id: '11111111-1111-1111-1111-111111111111',
    contentType: ContentType.Testimony,
    contentItemId: null,
    contentTypeName: 'Testimony',
    contentTypeDescription: 'Your walk with Him',
    contentTypeIconCssClass: 'bi-chat-heart',
    sortOrder: 0,
    hasTitle: true,
    hasAuthor: false,
    isAvailableAsGeneralUserContribution: true,
    tagsAllowed: true,
    showTags: true,
    reactionsAllowed: true,
    showReactions: true,
    linksAllowed: false,
    showLinks: true,
    attachmentsAllowed: false,
    showAttachments: true,
    commentsAllowed: true,
    showComments: true,
    bibleReferenceAllowed: true,
    showBibleReferences: true,
    limitReactionsToLoveOnly: false,
    createdBy: 'system-seed',
    createdWhen: '2026-08-28T12:21:18.308+00:00',
    updatedBy: 'system-seed',
    updatedWhen: '2026-08-28T12:21:18.308+00:00',
    deletedBy: null,
    deletedWhen: null,
    isDeleted: false,
    deletionReason: null
};

// An override for THIS item, and a quote whose type carries no title at all - the page's heading
// has to resolve both exactly as the panel does.
const testimonyOverride: ContentItemSetting = {
    ...testimonySetting,
    id: '22222222-2222-2222-2222-222222222222',
    contentItemId: 'content-item-1',
    contentTypeName: 'A Testimony, Retitled'
};

const quoteSetting: ContentItemSetting = {
    ...testimonySetting,
    id: '33333333-3333-3333-3333-333333333333',
    contentType: ContentType.Quote,
    contentTypeName: 'Quote',
    hasTitle: false
};

let contentItemSettings: ContentItemSetting[] = [];

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: contentItemSettings, isLoading: false, isError: false })
    }
}));

// signInAs mints userId 'user-1', so this row belongs to the reader in every test below — the
// hardest case for "editing is disabled here", since the owner is the one account that could
// otherwise amend it at any status.
const ownedItem: ContentItem = {
    id: 'content-item-1',
    contentType: ContentType.Testimony,
    title: 'He kept me through the night shift',
    author: null,
    content: 'The whole testimony, as it happened.',
    shareabilityBasis: 0,
    sharePermission: null,
    contentHash: 'hash',
    groupId: 'group-1',
    version: 1,
    publishDate: null,
    isPublished: false,
    approvalStatus: 0,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'user-1',
    createdWhen: '2026-08-30T10:22:41.237+00:00',
    updatedBy: 'user-1',
    updatedWhen: '2026-08-30T10:22:41.237+00:00',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
};

const renderPage = () =>
    render(
        <MemoryRouter initialEntries={['/posts/content-item-1']}>
            <AuthProvider>
                <Routes>
                    <Route path="/posts/:contentItemId" element={<PostDetail />} />
                </Routes>
            </AuthProvider>
        </MemoryRouter>);

describe('PostDetail', () => {
    beforeEach(() => {
        signOut(authState);
        contentItem = ownedItem;
        contentItemSettings = [testimonySetting, quoteSetting];
        isLoading = false;
        isError = false;
    });

    it('should read the item named by the route', () => {
        // when
        renderPage();

        // then
        expect(requestedId).toBe('content-item-1');
    });

    it('should render the item in the read surface', () => {
        // when
        renderPage();

        // then
        expect(screen.getByRole('heading', { name: 'He kept me through the night shift' }))
            .toBeInTheDocument();

        expect(screen.getByText('The whole testimony, as it happened.')).toBeInTheDocument();
        expect(screen.getByText('Testimony')).toBeInTheDocument();
        expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
    });

    it('should give the page its own heading rather than starting at the panel', () => {
        // when
        renderPage();

        // then: one h1 naming the item, and the panel does not repeat it underneath
        expect(screen.getByRole('heading',
            { name: 'He kept me through the night shift', level: 1 })).toBeInTheDocument();

        expect(screen.getAllByRole('heading', { name: 'He kept me through the night shift' }))
            .toHaveLength(1);
    });

    it('should fall back to the content type name for a type that carries no title', () => {
        // given
        contentItem = { ...ownedItem, title: null };

        // when
        renderPage();

        // then
        expect(screen.getByRole('heading', { name: 'Testimony', level: 1 })).toBeInTheDocument();
    });

    it('should not shout a title the panel deliberately hides', () => {
        // given: a type whose effective setting carries no title, on a row that still has one -
        // the panel hides it, so the page must not promote it to the h1
        contentItem = { ...ownedItem, contentType: ContentType.Quote, title: 'A stored title' };

        // when
        renderPage();

        // then
        expect(screen.queryByRole('heading', { name: 'A stored title' })).not.toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Quote', level: 1 })).toBeInTheDocument();
    });

    it('should prefer an item override when naming the page', () => {
        // given
        contentItemSettings = [testimonySetting, testimonyOverride];
        contentItem = { ...ownedItem, title: null };

        // when
        renderPage();

        // then: the override for THIS item wins over the content type default
        expect(screen.getByRole('heading', { name: 'A Testimony, Retitled', level: 1 }))
            .toBeInTheDocument();
    });

    it('should name the type rather than a literal before the settings arrive', () => {
        // given
        contentItemSettings = [];
        contentItem = { ...ownedItem, title: null };

        // when
        renderPage();

        // then: the fixed enum label, never "Contribution"
        expect(screen.getByRole('heading', { name: 'Testimony', level: 1 })).toBeInTheDocument();
    });

    it('should offer no editing to the reader who contributed it', () => {
        // given: isEditingAllowed is left off, which is the switch ahead of every role check
        signInAs(authState);

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
    });

    it('should offer no editing to an administrator either', () => {
        // given
        signInAs(authState, ['Administrators']);

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
    });

    it('should say so rather than render an empty page when the item cannot be read', () => {
        // given: the read is [AllowAnonymous] but filtered per caller, so "not found" and
        // "not yours to read" are the same answer here
        isError = true;
        contentItem = undefined;

        // when
        renderPage();

        // then
        expect(screen.getByRole('alert')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /Back to the journal/ })).toBeInTheDocument();
    });
});

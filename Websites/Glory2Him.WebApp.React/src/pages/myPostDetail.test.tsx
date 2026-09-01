import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MyPostDetail } from './myPostDetail';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs } from '../tests/testAuth';

// The contributor's own detail surface: the way back to their list, the item on the left, and
// the association surfaces beside it. The reads are mocked at their own boundary; what this
// suite pins is the LAYOUT contract the page owns — the back button, the 7/5 split, and the
// three right-column panels.
const authState = createAuthState();
let contentItem: ContentItem | undefined;

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemService: {
        useGetContentItemById: () => ({
            data: contentItem,
            isLoading: false,
            isError: false
        })
    }
}));

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [] }),
        useGetEffectiveSettingsFor: () => ({ data: [] })
    }
}));

const draftQuote: ContentItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    title: null,
    author: 'D. L. Moody',
    content: 'Character is what you are in the dark.',
    shareabilityBasis: ShareabilityBasis.PublicDomain,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: null,
    isPublished: false,
    approvalStatus: ApprovalStatus.Draft,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'user-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'user-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
};

const renderPage = (initialEntry: Parameters<typeof MemoryRouter>[0]['initialEntries'] extends
    ReadonlyArray<infer T> | undefined ? T : never = '/myposts/quote-1') =>
    render(
        <MemoryRouter initialEntries={[initialEntry]}>
            <AuthProvider>
                <MyPostDetail />
            </AuthProvider>
        </MemoryRouter>);

describe('MyPostDetail', () => {
    beforeEach(() => {
        contentItem = draftQuote;
        signInAs(authState, ['Users']);
    });

    it('should offer the way back to my posts', () => {
        // when
        renderPage();

        // then
        expect(screen.getByRole('link', { name: /Back to my posts/ }))
            .toHaveAttribute('href', '/myposts');
    });

    // A redirect that carried its origin gets a TRUE back — a filtered list returns filtered.
    it('should walk back to the origin a redirect carried in state', () => {
        // when
        render(
            <MemoryRouter
                initialEntries={[{
                    pathname: '/myposts/quote-1',
                    state: { from: '/myposts?type=Quote' }
                }]}>
                <AuthProvider>
                    <MyPostDetail />
                </AuthProvider>
            </MemoryRouter>);

        // then
        expect(screen.getByRole('link', { name: /Back to my posts/ }))
            .toHaveAttribute('href', '/myposts?type=Quote');
    });

    it('should stand the item in the seven beside a five', () => {
        // when
        const { container } = renderPage();

        // then: the layout contract itself — 7 for the item, 5 for what sits beside it
        expect(container.querySelector('.col-lg-7 h1')).toBeInTheDocument();
        expect(container.querySelector('.col-lg-5')).toBeInTheDocument();
    });

    it('should stand the association and sharing surfaces in the five', () => {
        // when
        const { container } = renderPage();
        const rightColumn = container.querySelector('.col-lg-5') as HTMLElement;

        // then
        expect(rightColumn.textContent).toContain('Tags');
        expect(rightColumn.textContent).toContain('Bible references');
        expect(rightColumn.textContent).toContain('Have something to share?');
    });

    it('should render the item itself on the left', () => {
        // when
        renderPage();

        // then: a quote leads with its content — the hero face may append the author, so
        // the match is a fragment rather than the whole line
        expect(screen.getByText(/Character is what you are in the dark\./))
            .toBeInTheDocument();
    });
});

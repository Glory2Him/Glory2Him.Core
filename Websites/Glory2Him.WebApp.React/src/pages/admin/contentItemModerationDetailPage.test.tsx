import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemModerationDetailPage } from './contentItemModerationDetailPage';
import { AuthProvider } from '../../components/securitys/authProvider';
import { ContentItem } from '../../models/foundations/contentItems/contentItem';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs } from '../../tests/testAuth';

// ONE ITEM UNDER MODERATION, in the admin shell. The reads are mocked at their own boundary;
// what this suite pins is what the PAGE owns — the way back to the queue, the moderated face it
// asks the card to wear, and the 7/5 split: what is being judged on the left with the facts
// attached to it, who is judging it on the right.
const authState = createAuthState();
let contentItem: ContentItem | undefined;

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../../services/foundations/contentItemService', () => ({
    contentItemService: {
        useGetContentItemById: () => ({
            data: contentItem,
            isLoading: false,
            isError: false
        })
    }
}));

vi.mock('../../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [] }),
        useGetEffectiveSettingsFor: () => ({ data: [] })
    }
}));

vi.mock('../../services/foundations/contributorService', () => ({
    contributorService: {
        useGetContributorById: () => ({ data: undefined })
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

// Back NAVIGATES rather than links, so the address it reaches is the only evidence of where
// it went — and returning to the queue as the moderator left it is the whole point of it.
const LocationProbe = () => {
    const location = useLocation();

    return <span data-testid="location">{location.pathname}{location.search}</span>;
};

const renderPage = (state?: { from: string }) =>
    render(
        <MemoryRouter
            initialEntries={[{ pathname: '/Admin/Posts/quote-1', state }]}>
            <AuthProvider>
                <ContentItemModerationDetailPage />
            </AuthProvider>
            <LocationProbe />
        </MemoryRouter>);

const landedOn = (): string | null => screen.getByTestId('location').textContent;

describe('ContentItemModerationDetailPage', () => {
    beforeEach(() => {
        contentItem = draftQuote;
        signInAs(authState, ['Administrators']);
    });

    it('should render the item in the admin chrome with its breadcrumb', () => {
        // when
        renderPage();

        // then
        expect(screen.getByText(/Character is what you are in the dark\./))
            .toBeInTheDocument();

        expect(screen.getByRole('link', { name: 'Posts' }))
            .toHaveAttribute('href', '/Admin/Posts');
    });

    it('should walk back to the bare queue when no origin was carried', async () => {
        // given
        renderPage();

        // when
        await userEvent.click(screen.getByRole('button', { name: /Back to Posts/ }));

        // then
        expect(landedOn()).toBe('/Admin/Posts');
    });

    /// A moderator part-way through a filtered queue must come back to IT, not to an unfiltered
    /// first page — which is why the origin travels in router state rather than being guessed
    /// at from history.
    it('should walk back to the filtered queue a redirect carried in state', async () => {
        // given
        renderPage({ from: '/Admin/Posts?type=Quote' });

        // when
        await userEvent.click(screen.getByRole('button', { name: /Back to Posts/ }));

        // then
        expect(landedOn()).toBe('/Admin/Posts?type=Quote');
    });

    /// THE MODERATED FACE. showModerationSection says the surface IS moderation, so the card
    /// offers no Edit of its own — there is nowhere for it to lead from the page it is already
    /// on — and the ribbon names the status in the corner.
    it('should wear the ribbon and offer no edit of its own', () => {
        // when
        const { container } = renderPage();

        // then
        expect(container.querySelector('.g2h-approval-ribbon'))
            .toHaveAttribute('data-approval-status', 'Draft');

        expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument();
    });

    /// One card must not state the same fact twice: the ribbon already names the status, so the
    /// pill beside the type chip stays off.
    it('should not repeat the status as a pill beside the ribbon', () => {
        // when
        const { container } = renderPage();

        // then
        expect(container.querySelectorAll('[data-approval-status]')).toHaveLength(1);
    });

    it('should stand the item in the seven beside a five', () => {
        // when
        const { container } = renderPage();

        // then: the layout contract itself
        expect(container.querySelector('.col-lg-7')).toBeInTheDocument();
        expect(container.querySelector('.col-lg-5')).toBeInTheDocument();
    });

    /// Tags and references are facts ABOUT the thing being judged, so they belong under it in
    /// its own column — not beside it, and not inside the card, whose own sections are off.
    it('should stand both association surfaces below the item in the seven', () => {
        // when
        const { container } = renderPage();
        const leftColumn = container.querySelector('.col-lg-7') as HTMLElement;

        // then
        expect(screen.getByRole('heading', { name: 'Tags' })).toBeInTheDocument();

        expect(screen.getByRole('heading', { name: 'Bible references' }))
            .toBeInTheDocument();

        expect(leftColumn.textContent).toContain('Tags');
        expect(leftColumn.textContent).toContain('Bible references');
    });

    /// WHO IS JUDGING IT, beside what is being judged.
    it('should stand the review round in the five', () => {
        // when
        const { container } = renderPage();
        const rightColumn = container.querySelector('.col-lg-5') as HTMLElement;

        // then
        expect(rightColumn.textContent).toContain('Approval Reviews');
        expect(rightColumn.textContent).toContain('Review Outcome');
    });

    /// The panel's gates read the STORED row — the item's owner and its status — so the page
    /// has to hand over both faithfully. signInAs mints 'user-1', so the two cases below differ
    /// in exactly one thing: who submitted the item under review.
    it('should offer the round to an administrator who does not own the submission', () => {
        // given: submitted by somebody else, and open
        contentItem = {
            ...draftQuote,
            createdBy: 'another-user',
            approvalStatus: ApprovalStatus.Submitted
        };

        // when
        renderPage();

        // then
        expect(screen.getByRole('button', { name: 'Vote...' })).toBeInTheDocument();
    });

    /// HR-2: nobody reviews their own submission, an administrator included. The page passes
    /// the STORED owner, so the panel can refuse it — a projection could not carry that.
    it('should refuse the vote to an administrator who owns the submission', () => {
        // given: the same open round, submitted by the viewer themselves
        contentItem = {
            ...draftQuote,
            createdBy: 'user-1',
            approvalStatus: ApprovalStatus.Submitted
        };

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: 'Vote...' })).not.toBeInTheDocument();
    });

    it('should tell the reader honestly when the item cannot be read', () => {
        // given
        contentItem = undefined;

        // when
        renderPage();

        // then
        expect(screen.getByRole('alert')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /Back to Posts/ })).toBeInTheDocument();
    });
});

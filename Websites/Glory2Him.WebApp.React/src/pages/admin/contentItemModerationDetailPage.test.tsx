import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemModerationDetailPage } from './contentItemModerationDetailPage';
import { AuthProvider } from '../../components/securitys/authProvider';
import { ContentItem } from '../../models/foundations/contentItems/contentItem';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ApprovalDecision } from '../../models/components/approvals/approvalReviewItem';
import { ApprovalStatus } from '../../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs } from '../../tests/testAuth';
import { testContentItemSetting } from '../../tests/testContentItemSettings';

import {
    ApprovalReview,
    ApprovalReviewRequest,
    ApprovalVerdict,
    ReviewerCandidate
} from '../../models/foundations/approvals/approval';

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

const modifiedWith = vi.fn();
const removedWith = vi.fn();

// THE ROUND'S WRITES. Each is captured as the request the page composed, which is the whole
// contract between a click on the panel and the endpoint behind it — the panel raises an event
// with what the viewer chose, and the page is what turns that into an id, a scope and a row.
const toastErrorSpy = vi.fn();
const toastSuccessSpy = vi.fn();

vi.mock('../../brokers/toastBroker.error', () => ({
    toastError: (message: string) => toastErrorSpy(message)
}));

vi.mock('../../brokers/toastBroker.success', () => ({
    toastSuccess: (message: string) => toastSuccessSpy(message)
}));

const castWith = vi.fn();
const resetWith = vi.fn().mockResolvedValue({ approvalId: 'approval-1' });
const decidedWith = vi.fn();
const requestedWith = vi.fn();
const withdrawnWith = vi.fn();

vi.mock('../../services/foundations/contentItemService', () => ({
    contentItemService: {
        useGetContentItemById: () => ({
            data: contentItem,
            isLoading: false,
            isError: false
        }),

        useModifyContentItem: () => ({
            mutateAsync: modifiedWith,
            isPending: false
        }),

        useRemoveContentItem: () => ({
            mutateAsync: removedWith,
            isPending: false
        })
    }
}));

// The editor SHAPES ITSELF from the setting (§6.4) — which fields exist at all is the type's
// call — so a settings-less mock would open a form with nothing in it and prove nothing.
const quoteSetting =
    testContentItemSetting(ContentType.Quote, 'Quote', { hasTitle: false });

vi.mock('../../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [quoteSetting] }),
        useGetEffectiveSettingsFor: () => ({ data: [quoteSetting] })
    }
}));

vi.mock('../../services/foundations/contributorService', () => ({
    contributorService: {
        useGetContributorById: () => ({ data: undefined })
    }
}));

// The APPROVAL ROUND, mocked at the service boundary like every other read on this page. The
// hook above it does the chaining; what this suite cares about is that the page asks for the
// round of the item in the URL and hands what comes back to the panel.
let approvalVerdict: ApprovalVerdict | undefined;
let approvalReviews: ApprovalReview[] = [];
let reviewRequests: ApprovalReviewRequest[] = [];
let reviewerCandidates: ReviewerCandidate[] = [];
let verdictAskedFor: ReadonlyArray<string> = [];

vi.mock('../../services/foundations/approvalService', () => ({
    approvalService: {
        useGetApprovalVerdict: (entityType: string, entityId: string) => {
            verdictAskedFor = [entityType, entityId];

            return { data: approvalVerdict, isLoading: false };
        },
        useGetApprovalReviews: () => ({ data: approvalReviews, isLoading: false }),
        useGetReviewerCandidates: () => ({ data: reviewerCandidates }),
        useGetReviewRequests: () => ({ data: reviewRequests }),
        useGetReviewerDisplayNames: () => ({
            data: [{ userId: 'user-john', displayName: 'John' }]
        }),

        useCastApprovalReview: () => ({ mutateAsync: castWith, isPending: false }),
        useDecideApproval: () => ({ mutateAsync: decidedWith, isPending: false }),
        useResetApproval: () => ({ mutateAsync: resetWith, isPending: false }),
        useRequestReview: () => ({ mutateAsync: requestedWith, isPending: false }),
        useWithdrawReviewRequest: () => ({ mutateAsync: withdrawnWith, isPending: false })
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

// Rendered THROUGH its route, not beside one: the page reads the item's id off the URL, and a
// bare element would hand it an empty string while every mocked read answered anyway — a harness
// that passed whether or not the id was threaded at all.
const renderPage = (state?: { from: string }) =>
    render(
        <MemoryRouter
            initialEntries={[{ pathname: '/Admin/Posts/quote-1', state }]}>
            <AuthProvider>
                <Routes>
                    <Route
                        path="/Admin/Posts/:contentItemId"
                        element={<ContentItemModerationDetailPage />} />

                    {/* The queue itself is another page's subject; it is declared here only so
                        that walking back lands somewhere rather than on an unmatched route. */}
                    <Route path="/Admin/Posts" element={null} />
                </Routes>
            </AuthProvider>
            <LocationProbe />
        </MemoryRouter>);

const landedOn = (): string | null => screen.getByTestId('location').textContent;

// The panel's own confirmation wears the same word as the affordance that opened it, so the
// two are told apart by position rather than by name — the dialog renders after the form.
const confirmDeleteButton = (): HTMLElement => {
    const deleteButtons = screen.getAllByRole('button', { name: /Delete/ });

    return deleteButtons[deleteButtons.length - 1];
};

const submittedVerdict: ApprovalVerdict = {
    approvalId: 'approval-1',
    entityType: 0,
    entityId: 'quote-1',
    approvalStatus: ApprovalStatus.Submitted,
    blockReasons: [
        { code: 1, message: 'At least 3 approving review(s) is required by reviewers.' }
    ],
    isBlocked: true,
    isBypassAllowedForCurrentUser: false,
    canApprove: false,
    approvalCount: 1,
    requiredNumberOfApprovals: 3,
    unresolvedApprovalCommentCount: 0
};

describe('ContentItemModerationDetailPage', () => {
    beforeEach(() => {
        contentItem = draftQuote;
        approvalVerdict = undefined;
        approvalReviews = [];
        reviewRequests = [];
        reviewerCandidates = [];
        verdictAskedFor = [];
        modifiedWith.mockReset();
        modifiedWith.mockResolvedValue(undefined);
        removedWith.mockReset();
        removedWith.mockResolvedValue(undefined);

        for (const write of [castWith, decidedWith, requestedWith, withdrawnWith]) {
            write.mockReset();
            write.mockResolvedValue({ approvalId: 'approval-1' });
        }

        toastErrorSpy.mockReset();
        toastSuccessSpy.mockReset();

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

    /// THE MODERATED FACE. showModerationSection puts the card's one action under the
    /// moderation tier and labels it Edit; the ribbon names the status in the corner.
    it('should wear the ribbon and offer the moderator its edit', () => {
        // when
        const { container } = renderPage();

        // then
        expect(container.querySelector('.g2h-approval-ribbon'))
            .toHaveAttribute('data-approval-status', 'Draft');

        expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();
    });

    /// EDITING HAPPENS IN PLACE. This page is already the destination, so the affordance opens
    /// the editor here rather than navigating anywhere.
    it('should open the editor in place when the moderator takes Edit', async () => {
        // given
        renderPage();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

        // then: the stored content, seeded into a form, and the page has not moved
        expect(screen.getByDisplayValue('Character is what you are in the dark.'))
            .toBeInTheDocument();

        expect(landedOn()).toBe('/Admin/Posts/quote-1');
    });

    it('should put the item back as it was when the edit is cancelled', async () => {
        // given
        renderPage();
        await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

        // when
        await userEvent.click(screen.getByRole('button', { name: /Cancel/ }));

        // then
        expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();
        expect(modifiedWith).not.toHaveBeenCalled();
    });

    /// THE WHOLE ROW GOES BACK, with the edit over the top. PUT api/ContentItems binds a
    /// ContentItem and pins the non-content fields by COMPARISON against storage, so a partial
    /// would arrive as a default in every field the form does not carry — and default is a
    /// legal value for most of them.
    it('should send the stored row with the edit laid over it', async () => {
        // given
        renderPage();
        await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

        const contentBox = screen.getByDisplayValue('Character is what you are in the dark.');
        await userEvent.clear(contentBox);
        await userEvent.type(contentBox, 'Character is what you are in the dark, always.');

        // when
        await userEvent.click(screen.getByRole('button', { name: /Save/ }));

        // then
        expect(modifiedWith).toHaveBeenCalledTimes(1);

        expect(modifiedWith).toHaveBeenCalledWith(expect.objectContaining({
            // the edit
            content: 'Character is what you are in the dark, always.',

            // and the identity and audit the row arrived with, untouched
            id: 'quote-1',
            groupId: 'group-1',
            contentHash: 'hash-1',
            version: 1,
            createdBy: 'user-1'
        }));
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

    /// A TAKEDOWN LEAVES NOWHERE TO STAND: the row this page is about is gone, so staying on
    /// its address would show a removed item.
    it('should take the item down and go back to the queue', async () => {
        // given
        renderPage({ from: '/Admin/Posts?type=Quote' });
        await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

        // when: the panel confirms for itself, and its dialog's button wears the same word
        await userEvent.click(screen.getByRole('button', { name: /Delete/ }));
        await userEvent.click(confirmDeleteButton());

        // then
        expect(removedWith).toHaveBeenCalledWith({ contentItemId: 'quote-1' });
        expect(landedOn()).toBe('/Admin/Posts?type=Quote');
    });

    /// A takedown that FAILED has removed nothing, so the moderator stays where they are with
    /// the item still in front of them rather than being sent to a queue that still holds it.
    it('should keep the moderator on the item when the takedown fails', async () => {
        // given
        removedWith.mockRejectedValue(new Error('refused'));
        renderPage();
        await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

        // when
        await userEvent.click(screen.getByRole('button', { name: /Delete/ }));
        await userEvent.click(confirmDeleteButton());

        // then
        expect(landedOn()).toBe('/Admin/Posts/quote-1');
    });

    /// THE ROUND IS READ, not invented. The page asks for the approval of the item in the URL
    /// and hands what comes back to the panel — the verdict's reasons, the votes cast, and who
    /// is still being waited on.
    it('should ask for the round of the item in the url', () => {
        // when
        renderPage();

        // then
        expect(verdictAskedFor).toEqual(['ContentItem', 'quote-1']);
    });

    it('should show the round the approval endpoints answer with', () => {
        // given
        contentItem = { ...draftQuote, approvalStatus: ApprovalStatus.Submitted };
        approvalVerdict = submittedVerdict;

        approvalReviews = [{
            id: 'review-1',
            approvalId: 'approval-1',
            statusId: ApprovalStatus.Approved,
            comment: '',
            createdBy: 'user-john',
            createdWhen: '2026-07-02T00:00:00Z',
            updatedBy: 'user-john',
            updatedWhen: '2026-07-02T00:00:00Z',
            isDeleted: false
        }];

        reviewRequests = [{
            id: 'request-1',
            approvalId: 'approval-1',
            requestedUserId: 'user-mary',
            requestedUserDisplayName: 'Mary Adeyemi',
            isDeleted: false
        }];

        // when
        renderPage();

        // then: the vote that was cast, named by the display-name read
        expect(screen.getByText('John')).toBeInTheDocument();
        expect(screen.getByText('Approved')).toBeInTheDocument();

        // the invitation still outstanding
        expect(screen.getByText('Mary Adeyemi')).toBeInTheDocument();
        expect(screen.getByText('Requested')).toBeInTheDocument();

        // and the verdict's own reason, verbatim
        expect(screen.getByText(
            'At least 3 approving review(s) is required by reviewers.')).toBeInTheDocument();
    });

    /// A post with no approval row 404s, and so does a caller outside the moderation tier
    /// (§14.5 rule 1) — both leave the verdict undefined. Nothing is then claimed about the
    /// round: no block reasons, and no approve, because the verdict is the only thing entitled
    /// to say whether approving is allowed. Reject survives, which is the panel's own rule —
    /// a direct reject needs no conditions and no bypass (§12.5.3 rule 13).
    it('should claim nothing about a round the verdict would not answer for', async () => {
        // given: submitted, so nothing but the missing verdict is holding the controls back
        contentItem = { ...draftQuote, approvalStatus: ApprovalStatus.Submitted };
        approvalVerdict = undefined;

        // when
        renderPage();

        // then: no reasons invented
        expect(screen.queryByText('Approval is blocked')).not.toBeInTheDocument();

        // and approving is refused while rejecting stands
        await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));

        expect(screen.getByRole('button', { name: /Approve this item/ })).toBeDisabled();
        expect(screen.getByRole('button', { name: /Reject this item/ })).toBeEnabled();
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

    // ── THE WRITES ─────────────────────────────────────────────────────────────────
    //
    // The panel raises what the viewer CHOSE; the page is what turns that into a request the
    // endpoint understands. So each of these asserts the request composed, against the verdict
    // and the item the page holds — an id the panel never sees, a scope it never names.
    describe('the writes', () => {
        const openRoundByAnotherAuthor = () => {
            contentItem = {
                ...draftQuote,
                createdBy: 'another-user',
                approvalStatus: ApprovalStatus.Submitted
            };

            approvalVerdict = submittedVerdict;
        };

        /// A first vote is a POST: no standing review, so nothing to amend. The approval id
        /// comes off the verdict, which is the only read that knows it.
        it('should cast a first vote against the approval the verdict named', async () => {
            // given
            openRoundByAnotherAuthor();
            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Vote...' }));
            await userEvent.click(screen.getByRole('button', { name: /I am happy with this item/ }));

            // then
            expect(castWith).toHaveBeenCalledTimes(1);

            expect(castWith).toHaveBeenCalledWith({
                approvalId: 'approval-1',
                vote: ApprovalStatus.Approved,
                standingReview: undefined
            });
        });

        /// A changed vote amends the viewer's OWN row (§7.7 rule 1) — matched by account id,
        /// never by name, and handed over whole so the foundation can check its audit fields.
        // A reset dismisses every review and hands the round back to the same reviewers. A
        // dismissal writes only StatusId, so the row still comes back on the reviews read — and
        // treating it as the viewer's standing review aims an amend at a row the server refuses
        // (§7.7 rule 7: a dismissed review is closed, the reviewer files a NEW one). Before this
        // was fixed, every reviewer on a reset round was refused every vote they tried to cast,
        // permanently.
        it('should file a new review rather than amend a dismissed one', async () => {
            // given: the viewer's only review on this round has been dismissed
            openRoundByAnotherAuthor();

            approvalReviews = [{
                id: 'review-mine',
                approvalId: 'approval-1',
                statusId: ApprovalStatus.Dismissed,
                comment: '',
                createdBy: 'user-1',
                createdWhen: '2026-07-02T00:00:00Z',
                updatedBy: 'user-1',
                updatedWhen: '2026-07-02T00:00:00Z',
                isDeleted: false
            }];

            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Vote...' }));
            await userEvent.click(screen.getByRole('button', { name: /I am happy with this item/ }));

            // then: a POST, not a PUT — no standing review is claimed
            expect(castWith).toHaveBeenCalledWith({
                approvalId: 'approval-1',
                vote: ApprovalStatus.Approved,
                standingReview: undefined
            });
        });

        it('should amend the standing review when the viewer changes their vote', async () => {
            // given
            openRoundByAnotherAuthor();

            const standing = {
                id: 'review-mine',
                approvalId: 'approval-1',
                statusId: ApprovalStatus.Rejected,
                comment: '',
                createdBy: 'user-1',
                createdWhen: '2026-07-02T00:00:00Z',
                updatedBy: 'user-1',
                updatedWhen: '2026-07-02T00:00:00Z',
                isDeleted: false
            };

            approvalReviews = [standing];
            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Rejected' }));
            await userEvent.click(screen.getByRole('button', { name: /I am happy with this item/ }));

            // then
            expect(castWith).toHaveBeenCalledWith({
                approvalId: 'approval-1',
                vote: ApprovalStatus.Approved,
                standingReview: standing
            });
        });

        it('should send a plain rejection against the item in the url', async () => {
            // given
            openRoundByAnotherAuthor();
            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));
            await userEvent.click(screen.getByRole('button', { name: /Reject this item/ }));
            await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

            // then
            expect(decidedWith).toHaveBeenCalledWith({
                entityType: 'ContentItem',
                entityId: 'quote-1',
                decision: ApprovalDecision.Reject,
                isBypassRequested: false,
                bypassReason: ''
            });
        });

        /// The bypass is a REQUEST with its reason; what lands on the row is the outcome's to
        /// say. The page forwards both exactly as the panel gathered them.
        it('should send a bypass approve with the reason the moderator gave', async () => {
            // given
            openRoundByAnotherAuthor();
            approvalVerdict = { ...submittedVerdict, isBypassAllowedForCurrentUser: true };
            renderPage();

            // when
            await userEvent.click(screen.getByRole('checkbox'));

            await userEvent.type(
                screen.getByLabelText('Reason for bypassing the approval requirements'),
                'Verified against the printed edition.');

            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));
            await userEvent.click(screen.getByRole('button', { name: /Approve this item/ }));
            await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

            // then
            expect(decidedWith).toHaveBeenCalledWith({
                entityType: 'ContentItem',
                entityId: 'quote-1',
                decision: ApprovalDecision.Approve,
                isBypassRequested: true,
                bypassReason: 'Verified against the printed edition.'
            });
        });

        it('should ask the chosen candidate to review the item in the url', async () => {
            // given
            openRoundByAnotherAuthor();
            reviewerCandidates = [{ userId: 'user-mary', displayName: 'Mary Adeyemi' }];
            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            await userEvent.click(screen.getByRole('button', { name: /Mary/ }));

            // then
            expect(requestedWith).toHaveBeenCalledWith({
                entityType: 'ContentItem',
                entityId: 'quote-1',
                requestedUserId: 'user-mary'
            });
        });

        it('should withdraw an outstanding request when its row is picked again', async () => {
            // given
            openRoundByAnotherAuthor();
            reviewerCandidates = [{ userId: 'user-mary', displayName: 'Mary Adeyemi' }];

            reviewRequests = [{
                id: 'request-1',
                approvalId: 'approval-1',
                requestedUserId: 'user-mary',
                requestedUserDisplayName: 'Mary Adeyemi',
                isDeleted: false
            }];

            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            await userEvent.click(screen.getByRole('button', { name: /Mary/ }));

            // then
            expect(withdrawnWith).toHaveBeenCalledWith({
                entityType: 'ContentItem',
                entityId: 'quote-1',
                requestedUserId: 'user-mary'
            });

            expect(requestedWith).not.toHaveBeenCalled();
        });

        /// A refusal is an ANSWER (§14.5): the server says why, and that reason — not a generic
        /// failure — is what the moderator reads.
        it('should show the reason the server gave when a write is refused', async () => {
            // given
            openRoundByAnotherAuthor();

            decidedWith.mockRejectedValue({
                isAxiosError: true,
                response: { data: { message: 'Reviewers record verdicts but do not decide approvals.' } }
            });

            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));
            await userEvent.click(screen.getByRole('button', { name: /Reject this item/ }));
            await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

            // then
            expect(toastErrorSpy).toHaveBeenCalledWith(
                'Reviewers record verdicts but do not decide approvals.');

            expect(toastSuccessSpy).not.toHaveBeenCalled();
        });
    });
});

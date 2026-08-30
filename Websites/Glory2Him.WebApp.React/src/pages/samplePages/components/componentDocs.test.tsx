import { screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AssociationPanelDoc } from './associationPanelDoc';
import { ContentItemDetailPanelDoc } from './contentItemDetailPanelDoc';
import { BibleReferenceAssociationPanelDoc } from './bibleReferenceAssociationPanelDoc';
import { ReviewPanelDoc } from './reviewPanelDoc';
import { TagAssociationPanelDoc } from './tagAssociationPanelDoc';
import {
    createAuthState,
    renderWithAuth,
    signInAs,
    signOut
} from '../../../tests/testAuth';

const authState = createAuthState();

vi.mock('../../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

// The reference pages are Administrators-only, so the demos are exercised as one — which is
// also the only way the approve / deny pair appears at all.
describe('Component reference pages', () => {
    beforeEach(() => {
        signOut(authState);
        signInAs(authState, ['Administrators']);
    });

    describe('AssociationPanelDoc', () => {
        it('should document the component and run it live', () => {
            // when
            renderWithAuth(<AssociationPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Association Panel', level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText('src/components/associations/associationPanel.tsx'))
                .toBeInTheDocument();

            // the live panel, not a screenshot of one
            expect(screen.getByRole('link', { name: '#creation' })).toBeInTheDocument();
            expect(screen.getByPlaceholderText('Start typing a tag…')).toBeInTheDocument();
        });

        it('should offer the moderation pair on a submission the reader does not own', () => {
            // when
            renderWithAuth(<AssociationPanelDoc />);

            // then
            expect(screen.getByRole('button', { name: 'Approve awaiting-someone-elses' }))
                .toBeInTheDocument();

            expect(screen.getByRole('button', { name: 'Reject awaiting-someone-elses' }))
                .toBeInTheDocument();
        });

        it('should give the reader a withdrawal rather than a verdict on their own submission', () => {
            // when
            renderWithAuth(<AssociationPanelDoc />);

            // then: an administrator who contributed it still cannot wave it through
            expect(screen.getByRole('button', { name: 'Remove awaiting-mine' })).toBeInTheDocument();

            expect(screen.queryByRole('button', { name: /Approve awaiting-mine/ }))
                .not.toBeInTheDocument();
        });

        it('should not leave a bare section for the theme to pad', () => {
            // when
            const { container } = renderWithAuth(<AssociationPanelDoc />);

            // then: the theme pads every bare <section> by 3.5rem/2.8rem, which stacked into
            // roughly 100px of dead space between headings. The only section left on the page is
            // the panel itself, which neutralises that padding with its own class.
            const sections = Array.from(container.querySelectorAll('section'));

            sections.forEach((section) =>
                expect(section).toHaveClass('g2h-association-panel'));
        });

        it('should mark each matrix cell with a tick, a cross or a coloured action', () => {
            // when
            const { container } = renderWithAuth(<AssociationPanelDoc />);

            // then: read whole cells — the glyph sits in its own aria-hidden span beside the
            // word, so the text is split across nodes and getByText cannot see it
            const cells = Array.from(container.querySelectorAll('td'))
                .map((cell) => cell.textContent ?? '');

            expect(cells).toContain('✅ Yes');
            expect(cells).toContain('❌ No');
            expect(cells).toContain('🔴 Remove');
            expect(cells).toContain('🟡 Reject + 🟢 Approve');
            expect(cells).toContain('🔴 Remove + 🟡 Reject + 🟢 Approve');
        });

        it('should carry the code samples and the props table', () => {
            // when
            const { container } = renderWithAuth(<AssociationPanelDoc />);

            // then
            // getAllByText: the gate props are named in the prose as well as the table
            expect(container.querySelectorAll('pre code').length).toBeGreaterThan(0);
            expect(screen.getAllByText('viewAllRoles').length).toBeGreaterThan(0);
            expect(screen.getAllByText('showModerationActions').length).toBeGreaterThan(0);
        });
    });

    describe('TagAssociationPanelDoc', () => {
        it('should document the component and run it with its own defaults', () => {
            // when
            renderWithAuth(<TagAssociationPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Tag Association Panel', level: 1 }))
                .toBeInTheDocument();

            // the hash prefix and the search href are the component's, not the page's
            expect(screen.getByRole('link', { name: '#miracles' }))
                .toHaveAttribute('href', '/Search?q=miracles');

            expect(screen.getByText('Think a tag is missing? Suggest one and help others find this post.'))
                .toBeInTheDocument();
        });
    });

    describe('BibleReferenceAssociationPanelDoc', () => {
        it('should document the component and run it with its own defaults', () => {
            // when
            renderWithAuth(<BibleReferenceAssociationPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Bible Reference Association Panel', level: 1 }))
                .toBeInTheDocument();

            // A multi-part citation addresses its opening chapter — the label keeps the full
            // "Joshua 10:8, 12-13" while the href resolves to what the route can parse.
            expect(screen.getByRole('link', { name: /Joshua 10:8, 12-13/ }))
                .toHaveAttribute('href', '/BibleReferences/JOS.10');
        });

        it('should show the book on an approved chip and the hourglass on a waiting one', () => {
            // when
            renderWithAuth(<BibleReferenceAssociationPanelDoc />);

            // then
            const approvedChip = screen.getByRole('link', { name: /Joshua 10:8, 12-13/ })
                .closest('span.g2h-association-chip');

            const pendingChip = screen.getByRole('link', { name: /Romans 3:23/ })
                .closest('span.g2h-association-chip');

            expect(approvedChip?.querySelector('i.bi-book')).toBeInTheDocument();
            expect(pendingChip?.querySelector('i.bi-hourglass-split')).toBeInTheDocument();
        });
    });

    describe('ContentItemDetailPanelDoc', () => {
        it('should document the component and name its source', () => {
            // when
            renderWithAuth(<ContentItemDetailPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Content Item Detail Panel', level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText('src/components/contentItems/contentItemDetailPanel.tsx'))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'Security posture' }))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'Props' })).toBeInTheDocument();
        });

        it('should run the add demo rather than picture one', () => {
            // when
            renderWithAuth(<ContentItemDetailPanelDoc />);

            // then: the picker is the settings the page handed over, running for real
            expect(screen.getAllByRole('button', { name: /Testimony/ }).length)
                .toBeGreaterThan(0);

            expect(screen.getAllByRole('button', { name: 'Submit for review' }))
                .toHaveLength(2);
        });

        it('should mark up the validation demo from the API messages it was given', () => {
            // when
            renderWithAuth(<ContentItemDetailPanelDoc />);

            // then: two fields named, and the message that names no field summarised
            expect(screen.getAllByText('Text is required')).toHaveLength(2);

            expect(screen.getByText('A content item already exists with the same content.'))
                .toBeInTheDocument();
        });

        it('should demonstrate the effective-setting resolution, not merely describe it', () => {
            // when
            renderWithAuth(<ContentItemDetailPanelDoc />);

            // then
            expect(screen.getByRole('heading',
                { name: 'Settings resolve here — most specific wins' })).toBeInTheDocument();

            // Two demos over the SAME item and the same props but one extra row. Scoped to each
            // demo card, because three other demos on the page render the same byline.
            const demoBody = (title: string): HTMLElement =>
                screen.getByText(title).closest('.card')?.querySelector('.card-body') as HTMLElement;

            expect(within(demoBody('Live — default only')).getByText('By Grace Abara'))
                .toBeInTheDocument();

            expect(within(demoBody(
                'Live — the same item, with its override in the collection'))
                .queryByText('By Grace Abara')).not.toBeInTheDocument();
        });

        it('should show the actions only where isEditingAllowed lets the roles decide', () => {
            // when
            renderWithAuth(<ContentItemDetailPanelDoc />);

            // then: three read demos, and only the two that throw the switch offer anything —
            // and of those, the Approved one is terminal to an administrator, so it keeps the
            // takedown and loses the amendment
            expect(screen.getAllByRole('button', { name: /Edit/ })).toHaveLength(1);
            expect(screen.getAllByRole('button', { name: /Delete/ })).toHaveLength(2);
        });
    });

    describe('ReviewPanelDoc', () => {
        it('should document the component and name its source', () => {
            // when
            renderWithAuth(<ReviewPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Review Panel', level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText('src/components/approvals/reviewPanel.tsx'))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'Security posture' }))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'Props' })).toBeInTheDocument();
        });

        /// The page exists to show the states that actually differ, so the demos must RUN
        /// rather than merely render a heading.
        it('should run the blocked-round demo with its reasons and bypass', () => {
            // when
            renderWithAuth(<ReviewPanelDoc />);

            // then: every block reason is rendered, not just the first
            expect(screen.getByText(
                'At least 3 approving review(s) is required by reviewers.')).toBeInTheDocument();

            expect(screen.getByText('A rejected review is blocking approval.'))
                .toBeInTheDocument();

            expect(screen.getByText('All review comments must be resolved.')).toBeInTheDocument();

            // the bypass is offered because the demo verdict allows it
            expect(screen.getAllByRole('checkbox').length).toBeGreaterThan(0);
        });

        it('should show controls only on the demos whose state allows them', () => {
            // when
            renderWithAuth(<ReviewPanelDoc />);

            // then: the DECISION control appears on two demos only - the blocked round and the
            // unblocked one. The read-only demo pins its role props empty, the decided demo is
            // terminal, and the voted-reviewer and picker demos pin decisionRoles empty because
            // they are about reviewing rather than deciding.
            expect(screen.getAllByRole('button', { name: 'Set approval status' }))
                .toHaveLength(2);

            // The cog follows the VOTE tier, not the decision tier, so it appears on all four
            // demos that leave voteRoles to compose: the two decision demos plus the two that
            // exist to show reviewing.
            expect(screen.getAllByRole('button', { name: 'Request a review' }))
                .toHaveLength(4);
        });
    });
});

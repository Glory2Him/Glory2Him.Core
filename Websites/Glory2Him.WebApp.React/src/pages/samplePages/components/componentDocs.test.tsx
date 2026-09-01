import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AssociationPanelDoc } from './associationPanelDoc';
import { ContentItemPanelDoc } from './contentItemPanelDoc';
import { ContentItemSearchBarPanelDoc } from './contentItemSearchBarPanelDoc';
import { ContentItemResultsPanelDoc } from './contentItemResultsPanelDoc';
import { ContentItemAddPanelDoc } from './contentItemAddPanelDoc';
import { ContentItemEditPanelDoc } from './contentItemEditPanelDoc';
import { ContentItemDefaultPanelDoc } from './contentItemDefaultPanelDoc';
import { ContentItemQuotesPanelDoc } from './contentItemQuotesPanelDoc';
import { ContentItemVerseImagePanelDoc } from './contentItemVerseImagePanelDoc';
import { ContentItemListPanelDoc } from './contentItemListPanelDoc';
import { SharingPanelDoc } from './sharingPanelDoc';
import { BibleReferenceAssociationPanelDoc } from './bibleReferenceAssociationPanelDoc';
import { ReviewPanelDoc } from './reviewPanelDoc';
import { TagAssociationPanelDoc } from './tagAssociationPanelDoc';

import {
    ApprovalStatus
} from '../../../models/components/contentItems/contentItemFormItem';

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

    describe('ContentItemPanelDoc', () => {
        it('should document the merged panel and name its source', () => {
            // when
            renderWithAuth(<ContentItemPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Content Item Panel', level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText('src/components/contentItems/contentItemPanel.tsx'))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'The family' }))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'What the consumer owns' }))
                .toBeInTheDocument();
        });

        it('should run the add demo rather than picture one', () => {
            // when
            renderWithAuth(<ContentItemPanelDoc />);

            // then: the picker is the settings the page handed over, running for real
            expect(screen.getAllByRole('button', { name: /Devotional/ }).length)
                .toBeGreaterThan(0);

            expect(screen.getByRole('button', { name: 'Submit for review' }))
                .toBeInTheDocument();
        });

        it('should render the view demo through the view template, ribbon and all', () => {
            // when
            renderWithAuth(<ContentItemPanelDoc />);

            // then: the demo element renders as the card the feeds show
            expect(screen.getByText('NASA Proves The Bible Is True')).toBeInTheDocument();

            const ribbon = document.querySelector('.g2h-approval-ribbon');
            expect(ribbon).not.toBeNull();
            expect(ribbon!.getAttribute('data-approval-status')).toBe('Draft');
        });

        it('should step the demo into another viewer through the security context', async () => {
            // given: the playground opens as the submitter (owner) — Edit shows
            renderWithAuth(<ContentItemPanelDoc />);

            expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();

            // when: the reader becomes a reviewer who is not the owner
            await userEvent.click(
                screen.getByRole('radio', { name: 'I am a reviewer (not owner)' }));

            // then: the ownership gate closes Edit and the moderation tier gets the shield
            expect(screen.queryByRole('button', { name: 'Edit' }))
                .not.toBeInTheDocument();

            expect(screen.getByRole('button', { name: 'Moderate' })).toBeInTheDocument();
        });

        it('should wear whichever status the ribbon radio picks', async () => {
            // given: the playground opens on Draft
            renderWithAuth(<ContentItemPanelDoc />);

            expect(document.querySelector('.g2h-approval-ribbon'))
                .toHaveAttribute('data-approval-status', 'Draft');

            // when
            await userEvent.click(
                screen.getByRole('radio', { name: 'Rejected (red)' }));

            // then
            expect(document.querySelector('.g2h-approval-ribbon'))
                .toHaveAttribute('data-approval-status', 'Rejected');
        });

        it('should stamp the view demo as the reader\u2019s own and edit in place', async () => {
            // given: the demo item carries the signed-in account id, so the real
            // ownership gate opens for whoever reads the page
            renderWithAuth(<ContentItemPanelDoc />);

            // when: Edit is taken on the view demo
            await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

            // then: the card became the editor — anchored on the seeded Title field,
            // because the view card carries a bookmark control also named Save
            expect(screen.getByLabelText(/Title/))
                .toHaveValue('NASA Proves The Bible Is True');

            // The add demo above carries a Cancel of its own; the editor's is the last.
            const cancels = screen.getAllByRole('button', { name: 'Cancel' });
            await userEvent.click(cancels[cancels.length - 1]);

            // and Cancel brings the card back
            expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();
        });
    });

    describe('ContentItemAddPanelDoc', () => {
        it('should open and close the surface through the contribution gates', async () => {
            // given: the board opens on the design's own position — any signed-in reader
            renderWithAuth(<ContentItemAddPanelDoc />);

            expect(screen.getByText('What are you sharing?')).toBeInTheDocument();

            // when: a grant the demo reader does not hold
            await userEvent.click(screen.getByRole('radio',
                { name: 'addRoles = Contributors — which this reader lacks' }));

            // then
            expect(screen.queryByText('What are you sharing?')).not.toBeInTheDocument();

            expect(screen.getByText('Contributions are not open to this account.'))
                .toBeInTheDocument();

            // when: the same grant, now held
            await userEvent.click(screen.getByRole('radio',
                { name: 'addRoles = Contributors — and this reader holds it' }));

            // then
            expect(screen.getByText('What are you sharing?')).toBeInTheDocument();

            // when: a ReadOnly block, which is asked FIRST and outranks the grant (#366)
            await userEvent.click(screen.getByRole('radio',
                { name: 'ContentItem-ReadOnly — the block outranks the grant' }));

            // then
            expect(screen.getByText('Contributions are not open to this account.'))
                .toBeInTheDocument();
        });

        it('should open the Submit as row on whichever default the board picks', async () => {
            // given
            renderWithAuth(<ContentItemAddPanelDoc />);

            expect(screen.getByLabelText(/Submit as/))
                .toHaveValue(String(ApprovalStatus.Submitted));

            // when
            await userEvent.click(screen.getByRole('radio',
                { name: 'Draft — a surface that files drafts' }));

            // then
            expect(screen.getByLabelText(/Submit as/))
                .toHaveValue(String(ApprovalStatus.Draft));
        });
    });

    describe('the family reference pages', () => {
        // One smoke per page: the heading, the source path, and the control board —
        // every page in the tree documents its own component with its own switches.
        const pages = [
            {
                name: 'Content Item Search Bar Panel',
                path: 'src/components/contentItems/contentItemSearchBarPanel.tsx',
                page: <ContentItemSearchBarPanelDoc />
            },
            {
                name: 'Content Item Results Panel',
                path: 'src/components/contentItems/contentItemResultsPanel.tsx',
                page: <ContentItemResultsPanelDoc />
            },
            {
                name: 'Content Item Add Panel',
                path: 'src/components/contentItems/contentItemAddPanel.tsx',
                page: <ContentItemAddPanelDoc />
            },
            {
                name: 'Content Item Edit Panel',
                path: 'src/components/contentItems/contentItemEditPanel.tsx',
                page: <ContentItemEditPanelDoc />
            },
            {
                name: 'Content Item Default Panel',
                path: 'src/components/contentItems/contentItemDefaultPanel.tsx',
                page: <ContentItemDefaultPanelDoc />
            },
            {
                name: 'Content Item Quotes Panel',
                path: 'src/components/contentItems/contentItemQuotesPanel.tsx',
                page: <ContentItemQuotesPanelDoc />
            },
            {
                name: 'Content Item Verse Image Panel',
                path: 'src/components/contentItems/contentItemVerseImagePanel.tsx',
                page: <ContentItemVerseImagePanelDoc />
            }
        ];

        it.each(pages)('should document $name with a live control board', (entry) => {
            // when
            renderWithAuth(entry.page);

            // then
            expect(screen.getByRole('heading', { name: entry.name, level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText(entry.path)).toBeInTheDocument();

            // the control board: at least one switch, each driving the demo beside it
            expect(screen.getAllByRole('switch').length).toBeGreaterThan(0);
        });

        it('should flip a section switch and re-render the template live', async () => {
            // given: the default template page, tags showing
            renderWithAuth(<ContentItemDefaultPanelDoc />);

            expect(screen.getByText('#creation')).toBeInTheDocument();

            // when: the reader flips showTagSection off
            await userEvent.click(screen.getByRole('switch', { name: /^showTagSection/ }));

            // then: the demo re-rendered with the prop changed
            expect(screen.queryByText('#creation')).not.toBeInTheDocument();
        });
    });

    describe('ContentItemListPanelDoc', () => {
        it('should document the family and name its source', () => {
            // when
            renderWithAuth(<ContentItemListPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Content Item List Panel', level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText('src/components/contentItems/contentItemListPanel.tsx'))
                .toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'The family' })).toBeInTheDocument();
            expect(screen.getByRole('heading', { name: 'Props' })).toBeInTheDocument();
        });

        it('should run the two templates rather than picture them', () => {
            // when
            renderWithAuth(<ContentItemListPanelDoc />);

            // then: a quote shown whole through the override, a story through the default
            expect(screen.getAllByText(new RegExp('coincidences happen')).length)
                .toBeGreaterThan(0);

            // The mixed-page and filter demos both render the story, so present, not unique.
            expect(screen.getAllByRole('button', { name: 'NASA Proves The Bible Is True' })
                .length).toBeGreaterThan(0);
        });

        // §6.4 per card, on one collection: the second quote's item-level override carries
        // limitReactionsToLoveOnly, so its choices are one where the first's are four.
        it('should narrow the choices on the item its override belongs to', async () => {
            // given
            renderWithAuth(<ContentItemListPanelDoc />);
            const likeButtons = screen.getAllByRole('button', { name: /Like/ });

            // when
            await userEvent.click(likeButtons[0]);

            // then
            expect(screen.getAllByRole('menuitem')).toHaveLength(4);

            // when: close it, open the love-only card's choices
            await userEvent.click(likeButtons[0]);
            await userEvent.click(likeButtons[1]);

            // then
            expect(screen.getAllByRole('menuitem')).toHaveLength(1);
            expect(screen.getByRole('menuitem', { name: 'Love' })).toBeInTheDocument();
        });

        it('should react for real rather than describing the event', async () => {
            // given
            renderWithAuth(<ContentItemListPanelDoc />);

            // when
            await userEvent.click(screen.getAllByRole('button', { name: /Like/ })[0]);
            await userEvent.click(screen.getByRole('menuitem', { name: 'Amen' }));

            // then
            expect(screen.getByText('onReactionSelected(quote-1, Amen)')).toBeInTheDocument();

            // and the choice the reader made is marked when the choices reopen
            await userEvent.click(screen.getAllByRole('button', { name: /Like/ })[0]);

            expect(screen.getByRole('menuitem', { name: 'Amen' }))
                .toHaveAttribute('aria-pressed', 'true');
        });

        it('should offer the full advanced options', async () => {
            // given
            renderWithAuth(<ContentItemListPanelDoc />);

            // when: the page carries two live bars now (the criteria demo and the
            // playground), so the first is the one exercised
            await userEvent.click(
                screen.getAllByRole('button', { name: 'Advanced search options' })[0]);

            // then: the whole grid — Category | Author, Submitted by | Shareability, Tags
            expect(screen.getAllByLabelText('Category').length).toBeGreaterThan(0);
            expect(screen.getAllByLabelText('Author').length).toBeGreaterThan(0);
            expect(screen.getAllByLabelText('Submitted by').length).toBeGreaterThan(0);
            expect(screen.getAllByLabelText('Shareability').length).toBeGreaterThan(0);

            expect(screen.getAllByLabelText('Type a tag and press Enter').length)
                .toBeGreaterThan(0);
        });
    });

    describe('SharingPanelDoc', () => {
        it('should document the component and name its source', () => {
            // when
            renderWithAuth(<SharingPanelDoc />);

            // then
            expect(screen.getByRole('heading', { name: 'Sharing Panel', level: 1 }))
                .toBeInTheDocument();

            expect(screen.getByText('src/components/contentItems/sharingPanel.tsx'))
                .toBeInTheDocument();

            expect(screen.getByRole('heading',
                { name: 'It adapts to its container, not the viewport' })).toBeInTheDocument();

            expect(screen.getByRole('heading', { name: 'Props' })).toBeInTheDocument();
        });

        it('should run the panel live in both faces and reworded', async () => {
            // given: two default-worded demos (wide and narrow) plus the reworded one
            renderWithAuth(<SharingPanelDoc />);

            expect(screen.getAllByRole('button', { name: /Submit a contribution/ }))
                .toHaveLength(2);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Share your story/ }));

            // then
            expect(screen.getByText('onSubmit() — reworded')).toBeInTheDocument();
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

import { ReactElement, ReactNode } from 'react';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemFormPanel } from './contentItemFormPanel';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemFormItem,
    defaultShareabilityBasis,
    ShareabilityBasis
} from '../../models/components/contentItems/contentItemFormItem';

import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../securitys/authProvider';

import {
    createAuthState,
    renderWithAuth,
    signInAs,
    signOut
} from '../../tests/testAuth';

// renderWithAuth owns the wrapper, and RenderResult.rerender replaces the WHOLE tree - so a test
// that changes a prop has to hand the wrapper back each time or it would remount and lose state.
const wrapped = (ui: ReactNode): ReactElement => (
    <MemoryRouter initialEntries={['/Secured/Page']}>
        <AuthProvider>{ui}</AuthProvider>
    </MemoryRouter>
);

const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

// signInAs mints userId 'user-1', which is what CreatedBy carries for that reader's own items.
const ViewerId = 'user-1';
const OtherId = 'another-user';

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription: `A ${contentTypeName.toLowerCase()}`,
        contentTypeIconCssClass: 'bi-chat-quote',
        // The seed gives each type the order its enum member is numbered with, so a fixture
        // that does the same puts the tiles in the order the real picker shows them.
        sortOrder: contentType,
        hasTitle: true,
        hasAuthor: true,
        isAvailableAsGeneralUserContribution: true,
        tagsAllowed: true,
        showTags: true,
        reactionsAllowed: true,
        showReactions: true,
        linksAllowed: true,
        showLinks: true,
        attachmentsAllowed: true,
        showAttachments: true,
        commentsAllowed: true,
        showComments: true,
        bibleReferenceAllowed: true,
        showBibleReferences: true,
        limitReactionsToLoveOnly: false,
        createdBy: OtherId,
        createdWhen: '2026-01-01T00:00:00+00:00',
        updatedBy: OtherId,
        updatedWhen: '2026-01-01T00:00:00+00:00',
        deletedBy: null,
        deletedWhen: null,
        isDeleted: false,
        deletionReason: null,
        ...overrides
    });

const storySetting = settingFor(ContentType.Story, 'Story');
const devotionalSetting = settingFor(ContentType.Devotional, 'Devotional');
const settings = [storySetting, devotionalSetting];

const itemWith = (overrides: Partial<ContentItemFormItem> = {}): ContentItemFormItem => ({
    id: 'content-item-1',
    contentType: ContentType.Story,
    title: 'He carried me',
    author: 'Anon',
    content: 'The whole story, as it happened.',

    // NOT an owned basis, deliberately. An owned basis suppresses the Author field on its own
    // (the submitter is the author), and most of the tests below are about the SETTING's hasAuthor
    // — so a shared fixture that hid the field for the other reason would make them pass or fail
    // for a question they are not asking. The ownership rule has its own describe block.
    shareabilityBasis: ShareabilityBasis.PermissionGranted,
    sharePermission: '',
    createdBy: ViewerId,
    approvalStatus: ApprovalStatus.Draft,
    ...overrides
});

describe('ContentItemFormPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    describe('add mode', () => {
        it('should render the picker, the fields and the submit pair for a signed-in reader', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('What are you sharing?')).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Story/ })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Devotional/ })).toBeInTheDocument();
            expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
            expect(screen.getByLabelText(/Author/)).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Submit for review' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
        });

        it('should offer a way in rather than a form when nobody is signed in', () => {
            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('link', { name: /Login to contribute/ }))
                .toHaveAttribute('href', '/Account/Login?returnUrl=%2FSecured%2FPage');

            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });

        it('should say contributions are closed rather than render an empty panel for a blocked account', () => {
            // given: the global block role
            signInAs(authState, ['ReadOnly']);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Contributions are not open to this account.'))
                .toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });

        it('should block the whole surface for the entity-type block role', () => {
            // given
            signInAs(authState, ['ContentItem-ReadOnly']);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Contributions are not open to this account.'))
                .toBeInTheDocument();
        });

        it('should close one tile and leave the rest of the form live for a narrow block', () => {
            // given: blocked from devotionals only — stories stay open
            signInAs(authState, ['ContentItem-Devotional-ReadOnly']);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Devotional/ })).toBeDisabled();
            expect(screen.getByRole('button', { name: /Story/ })).toBeEnabled();
            expect(screen.getByRole('button', { name: 'Submit for review' })).toBeInTheDocument();
            expect(screen.getByText('Not open to this account')).toBeInTheDocument();
        });

        it('should select the first type that is open rather than the first listed', async () => {
            // given: the FIRST setting is the blocked one
            signInAs(authState, ['ContentItem-Story-ReadOnly']);
            const onAdded = vi.fn();

            // when
            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            await userEvent.type(screen.getByLabelText(/Devotional/), 'A word for today');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then
            expect(onAdded).toHaveBeenCalledWith(
                expect.objectContaining({ contentType: ContentType.Devotional }));
        });

        it('should raise onAdded with what was typed against the chosen type', async () => {
            // given
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));
            await userEvent.type(screen.getByLabelText(/Title/), 'Morning');
            await userEvent.type(screen.getByLabelText(/Devotional/), 'A word for today');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then
            // objectContaining: the emitted projection also carries its winning setting,
            // constructed from the collection the form was shaped with.
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                contentType: ContentType.Devotional,
                title: 'Morning',

                // Untouched, and the form opens on an owned basis — so the field was showing the
                // contributor's own name, and what it showed is what is filed.
                author: 'Tester',
                content: 'A word for today',
                shareabilityBasis: defaultShareabilityBasis,

                // Mandatory under the permission default the form opens on.
                sharePermission: 'By email from the author'
            }));
        });

        it('should ask for the permission detail only once permission is the basis', async () => {
            // given
            signInAs(authState);
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // when: a basis that rests on nobody's permission
            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PublicDomain));

            // then
            expect(screen.queryByLabelText(/Permission details/)).not.toBeInTheDocument();

            // when
            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PermissionGranted));

            // then
            expect(screen.getByLabelText(/Permission details/)).toBeInTheDocument();
        });

        it('should ask for the permission detail when the contributor grants their own', async () => {
            // given: an owned basis is still a PERMISSION basis when the contributor is the one
            // granting it, so the detail field belongs to it too
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/Permission details/)).toBeInTheDocument();
        });

        it('should shape the fields from the chosen type settings', async () => {
            // given: a quote carries neither a title nor an author of its own
            signInAs(authState);

            const quoteSetting = settingFor(ContentType.Quote, 'Quote', {
                hasTitle: false,
                hasAuthor: false
            });

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={[storySetting, quoteSetting]} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Quote/ }));

            // then
            expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
            expect(screen.queryByLabelText(/Author/)).not.toBeInTheDocument();
            expect(screen.getByLabelText(/Quote/)).toBeInTheDocument();
        });

        it('should order the tiles by the settings own sortOrder, not by the order handed over', () => {
            // given: the rows arrive in the reverse of the order they should be offered in
            signInAs(authState);

            const quote = settingFor(ContentType.Quote, 'Quote', { sortOrder: 0 });
            const story = settingFor(ContentType.Story, 'Story', { sortOrder: 1 });
            const testimony = settingFor(ContentType.Testimony, 'Testimony', { sortOrder: 2 });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={[testimony, story, quote]} />);

            // then
            const tiles = screen.getAllByRole('button', { name: /Quote|Story|Testimony/ });

            expect(tiles.map((tile) => tile.textContent?.startsWith('Quote')
                ? 'Quote'
                : tile.textContent?.startsWith('Story') ? 'Story' : 'Testimony'))
                .toEqual(['Quote', 'Story', 'Testimony']);

            // and the type it lands on is the FIRST in that order, not the first row given
            expect(screen.getByRole('button', { name: /Quote/ }))
                .toHaveAttribute('aria-pressed', 'true');
        });

        it('should say so when no type is on offer', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={[]} />);

            // then
            expect(screen.getByText('Contributions are not open for any content type right now.'))
                .toBeInTheDocument();
        });

        it('should raise onCancelled rather than clearing anything itself', async () => {
            // given
            signInAs(authState);
            const onCancelled = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    onCancelled={onCancelled} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            // then
            expect(onCancelled).toHaveBeenCalledTimes(1);
        });
    });

    describe('the edit face and its gates', () => {
        // The read face left with the merge — the view templates own it — so an item IS
        // the editor here, and every old read-surface action gate is now the gate on the
        // editor itself: who may hold it (Save), and who may destroy from it (Delete).
        it('should refuse the editor while isEditingAllowed is off, roles regardless', () => {
            // given: the owner, who is also an administrator — every gate below would pass
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={settings} />);

            // then: no editor, and a refusal rather than a silent different surface
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.getByRole('alert')).toBeInTheDocument();
        });

        it('should give the owner the editor and the takedown on their own item', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Delete/ })).toBeInTheDocument();
        });

        it('should keep the owner editing their own item after it is decided', () => {
            // given: amending a terminal item forks a new version (§3.4 rule 16) — the
            // CONSUMER decides that, but the affordance stays
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ approvalStatus: ApprovalStatus.Approved })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
        });

        it('should offer the publisher tier an edit on a live item and none on a decided one', () => {
            // given
            signInAs(authState, ['ContentItem-Story-Publishers']);

            // when
            const submitted = renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        createdBy: OtherId,
                        approvalStatus: ApprovalStatus.Submitted
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
            submitted.unmount();

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        createdBy: OtherId,
                        approvalStatus: ApprovalStatus.Approved
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
        });

        it('should never offer the reviewer tier the editor — a reviewer reviews', () => {
            // given
            signInAs(authState, ['Reviewers', 'ContentItem-Reviewers',
                'ContentItem-Story-Reviewers']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should withhold removal from the publisher tier — a takedown is not moderation', () => {
            // given
            signInAs(authState, ['Publishers']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should give an administrator removal on somebody else\'s item', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Delete/ })).toBeInTheDocument();
        });

        it('should leave a plain reader with no editor at all', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should strip the editor from an owner blocked for that content type', () => {
            // given: the block outranks [OWNER] (#366)
            signInAs(authState, ['ContentItem-Story-ReadOnly']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should confirm before it raises onRemoved', async () => {
            // given
            signInAs(authState);
            const onRemoved = vi.fn();
            const contentItem = itemWith();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={contentItem}
                    isEditingAllowed
                    onRemoved={onRemoved}
                    contentItemSettingCollection={settings} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Delete/ }));

            // then: the dialog is up and nothing has fired
            expect(screen.getByText('Are you sure?')).toBeInTheDocument();
            expect(onRemoved).not.toHaveBeenCalled();

            // when
            await userEvent.click(
                within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }));

            // then
            expect(onRemoved).toHaveBeenCalledWith(contentItem);
        });

        it('should raise nothing when the confirmation is refused', async () => {
            // given
            signInAs(authState);
            const onRemoved = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    onRemoved={onRemoved}
                    contentItemSettingCollection={settings} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Delete/ }));

            await userEvent.click(
                within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancel' }));

            // then
            expect(onRemoved).not.toHaveBeenCalled();
        });
    });

    describe('the mandatory permission detail', () => {
        // The one client-side rule the panel decides itself: a permission basis with no
        // permission named is refused here rather than posted. Everything else stays the
        // server's to judge.
        it('should refuse to submit a permission basis with no detail and say why', async () => {
            // given: the form opens on the permission default
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            await userEvent.type(screen.getByLabelText(/^Story/), 'The whole story.');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then
            expect(onAdded).not.toHaveBeenCalled();

            expect(screen.getByText(new RegExp('required for this sharing basis')))
                .toBeInTheDocument();

            expect(screen.getByLabelText(/Permission details/))
                .toHaveAttribute('aria-invalid', 'true');
        });

        it('should clear the refusal the moment the reader answers', async () => {
            // given
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            await userEvent.type(screen.getByLabelText(/^Story/), 'The whole story.');
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // when
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');

            // then: the message is gone before any resubmit
            expect(screen.queryByText(new RegExp('required for this sharing basis')))
                .not.toBeInTheDocument();

            // and the same submit now goes through
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                sharePermission: 'By email from the author'
            }));
        });

        it('should hold both permission members to the rule', async () => {
            // given: the OTHER permission member — somebody else's permission
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            await userEvent.type(screen.getByLabelText(/^Story/), 'The whole story.');

            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PermissionGranted));

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then
            expect(onAdded).not.toHaveBeenCalled();
        });

        it('should ask nothing extra of a public domain basis', async () => {
            // given
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            await userEvent.type(screen.getByLabelText(/^Story/), 'The whole story.');

            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PublicDomain));

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then: no detail exists to demand — the field is not even rendered
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                shareabilityBasis: ShareabilityBasis.PublicDomain,
                sharePermission: ''
            }));
        });

        it('should hold an edit to the same rule as an add', async () => {
            // given: a stored row on a permission basis whose note is empty
            signInAs(authState);
            const onModified = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    onModified={onModified}
                    contentItemSettingCollection={settings} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            // then
            expect(onModified).not.toHaveBeenCalled();

            expect(screen.getByText(new RegExp('required for this sharing basis')))
                .toBeInTheDocument();
        });
    });

    describe('the approval-status ribbon', () => {
        it('should wear no ribbon unless the surface opted in', () => {
            signInAs(authState);

            const { container } = renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ approvalStatus: ApprovalStatus.Submitted })}
                    contentItemSettingCollection={settings} />);

            expect(container.querySelector('.g2h-approval-ribbon')).toBeNull();
        });

        it('should wear the status member name for the stylesheet to colour', () => {
            signInAs(authState);

            const { container } = renderWithAuth(
                <ContentItemFormPanel
                    showApprovalStatusRibbon
                    contentItem={itemWith({ approvalStatus: ApprovalStatus.Submitted })}
                    contentItemSettingCollection={settings} />);

            const ribbon = container.querySelector('.g2h-approval-ribbon');
            expect(ribbon).not.toBeNull();
            expect(ribbon!.getAttribute('data-approval-status')).toBe('Submitted');
            expect(ribbon!.textContent).toBe('Submitted');
        });

        it('should wear none in add mode — no item, no status', () => {
            signInAs(authState);

            const { container } = renderWithAuth(
                <ContentItemFormPanel
                    showApprovalStatusRibbon
                    contentItemSettingCollection={settings} />);

            expect(container.querySelector('.g2h-approval-ribbon')).toBeNull();
        });
    });

    describe('the self-contained projection', () => {
        // A list surface hands its element over WITH its winning setting; re-resolving
        // from a collection that may not hold the override would silently un-override it.
        it('should let the embedded winner beat the collection for its own item', () => {
            signInAs(authState);

            // The collection's Story default says hasTitle; the element's own winner says
            // not — the element wins, so the editor offers no Title field.
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        contentItemSetting: settingFor(ContentType.Story, 'Story override', {
                            id: 'override-row',
                            contentItemId: 'content-item-1',
                            hasTitle: false
                        })
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
        });

        it('should emit a projection carrying the winner it was shaped with', async () => {
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    onAdded={onAdded} />);

            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));
            await userEvent.type(screen.getByLabelText(/Devotional/), 'A word for today');

            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');

            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then: the consumer can hand this straight to a detail surface
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                contentItemSetting: expect.objectContaining({
                    contentType: ContentType.Devotional
                })
            }));
        });
    });

    describe('edit mode', () => {
        it('should seed the editor from the item', () => {
            // given
            signInAs(authState);

            // when: an item IS the editor — there is no Edit affordance to take here, that
            // is ContentItemPanel's view face
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/Title/)).toHaveValue('He carried me');
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
        });

        it('should freeze the content type rather than offering the picker', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then: create-only (§12.4.1 rule 7a)
            expect(screen.queryByText('What are you sharing?')).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Devotional/ })).not.toBeInTheDocument();
            expect(screen.getByText('Type')).toBeInTheDocument();
        });

        it('should raise onModified with the amendments over the original identity', async () => {
            // given
            signInAs(authState);
            const onModified = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    onModified={onModified}
                    contentItemSettingCollection={settings} />);

            // when
            await userEvent.clear(screen.getByLabelText(/Title/));
            await userEvent.type(screen.getByLabelText(/Title/), 'He carried me still');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            // then
            expect(onModified).toHaveBeenCalledWith(expect.objectContaining({
                id: 'content-item-1',
                contentType: ContentType.Story,
                createdBy: ViewerId,
                title: 'He carried me still'
            }));
        });

        it('should reseed the original values and tell the page on Cancel', async () => {
            // given
            signInAs(authState);
            const onCancelled = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    onCancelled={onCancelled}
                    contentItemSettingCollection={settings} />);

            await userEvent.clear(screen.getByLabelText(/Title/));
            await userEvent.type(screen.getByLabelText(/Title/), 'Something else');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            // then: the abandoned edit is gone, and the PAGE decides which face comes next
            expect(onCancelled).toHaveBeenCalledTimes(1);
            expect(screen.getByLabelText(/Title/)).toHaveValue('He carried me');
        });
    });

    describe('validation readback', () => {
        it('should mark each field the API named and show its message', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    validationIssues={{
                        Content: ['Text is required'],
                        Title: ['Text is required']
                    }} />);

            // then
            expect(screen.getByLabelText(/Story/)).toHaveClass('is-invalid');
            expect(screen.getByLabelText(/Title/)).toHaveClass('is-invalid');
            expect(screen.getAllByText('Text is required')).toHaveLength(2);
        });

        it('should match the API field names whatever their casing', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    validationIssues={{ content: ['Text is required'] }} />);

            // then
            expect(screen.getByLabelText(/Story/)).toHaveClass('is-invalid');
        });

        it('should summarise an issue it cannot place on a field rather than dropping it', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    validationIssues={{
                        ContentHash: ['A content item already exists with the same content.']
                    }} />);

            // then
            expect(screen.getByText('Please fix the following and try again:'))
                .toBeInTheDocument();

            expect(screen.getByText('A content item already exists with the same content.'))
                .toBeInTheDocument();
        });

        it('should summarise a ContentType message in edit, where there is no picker', () => {
            // given: the picker is the only place a ContentType message renders, and edit mode
            // has no picker - the message must reach the summary rather than vanish
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings}
                    validationIssues={{ ContentType: ['Value is invalid'] }} />);

            // then
            expect(screen.getByText('Please fix the following and try again:'))
                .toBeInTheDocument();

            expect(screen.getByText('Value is invalid')).toBeInTheDocument();
        });

        it('should place a ContentType message on the picker in add', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    validationIssues={{ ContentType: ['Value is invalid'] }} />);

            // then: it lands on the picker, so it is NOT repeated in the summary
            expect(screen.getByText('Value is invalid')).toBeInTheDocument();

            expect(screen.queryByText('Please fix the following and try again:'))
                .not.toBeInTheDocument();
        });

        it('should summarise a message for a field the setting does not render', () => {
            // given: a quote has no title, so a Title message has no input to attach to
            signInAs(authState);

            const quoteSetting = settingFor(ContentType.Quote, 'Quote', { hasTitle: false });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={[quoteSetting]}
                    validationIssues={{ Title: ['Text is required'] }} />);

            // then
            expect(screen.getByText('Please fix the following and try again:'))
                .toBeInTheDocument();

            expect(screen.getByText('Text is required')).toBeInTheDocument();
        });

        it('should attach each message to its field for a screen reader', () => {
            // given: is-invalid is a colour; the association is what a screen reader follows
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    validationIssues={{ Content: ['Text is required'] }} />);

            // then
            const content = screen.getByLabelText(/^Story/);

            expect(content).toHaveAttribute('aria-invalid', 'true');

            expect(document.getElementById(content.getAttribute('aria-describedby') ?? ''))
                .toHaveTextContent('Text is required');
        });

        it('should leave a clean field unmarked', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/^Story/)).not.toHaveAttribute('aria-invalid');
            expect(screen.getByLabelText(/^Story/)).not.toHaveAttribute('aria-describedby');
        });

        it('should list two unplaced fields sharing one message without colliding', () => {
            // given: the server's messages are shared literals, so two fields carrying the same
            // sentence is the norm - React treats same-keyed siblings as undefined behaviour
            signInAs(authState);

            const quoteSetting = settingFor(ContentType.Quote, 'Quote', {
                hasTitle: false,
                hasAuthor: false
            });

            const consoleError = vi.spyOn(console, 'error').mockImplementation(() => { });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={[quoteSetting]}
                    validationIssues={{
                        Title: ['Text is required'],
                        Author: ['Text is required']
                    }} />);

            // then: both are listed, and React raised no key warning
            expect(screen.getAllByText('Text is required')).toHaveLength(2);

            expect(consoleError.mock.calls.map((call) => String(call[0])).join(' '))
                .not.toMatch(/same key/i);

            consoleError.mockRestore();
        });

        it('should carry the readback into the edit surface too', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings}
                    validationIssues={{ Content: ['Text is required'] }} />);

            // then
            expect(screen.getByLabelText(/Story/)).toHaveClass('is-invalid');
        });
    });

    describe('field shaping', () => {
        const quoteSetting = settingFor(ContentType.Quote, 'Quote', {
            hasTitle: false,
            hasAuthor: false
        });

        const quoteCarryingBoth = itemWith({
            contentType: ContentType.Quote,
            title: 'A title the type no longer has',
            author: 'An author the type no longer has'
        });

        it('should hide both fields in the editor for the same setting', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={quoteCarryingBoth}
                    isEditingAllowed
                    contentItemSettingCollection={[quoteSetting]} />);

            // then
            expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
            expect(screen.queryByLabelText(/Author/)).not.toBeInTheDocument();
            expect(screen.getByLabelText(/Quote/)).toBeInTheDocument();
        });

        it('should carry a hidden field through a save rather than erasing it', async () => {
            // given: hiding a field is a RENDERING rule, so it must never be a destructive one -
            // a setting changed after the item was written would otherwise silently blank it
            signInAs(authState);
            const onModified = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={quoteCarryingBoth}
                    isEditingAllowed
                    onModified={onModified}
                    contentItemSettingCollection={[quoteSetting]} />);

            // when
            await userEvent.clear(screen.getByLabelText(/Quote/));
            await userEvent.type(screen.getByLabelText(/Quote/), 'Amended words');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            // then
            expect(onModified).toHaveBeenCalledWith(expect.objectContaining({
                content: 'Amended words',
                title: 'A title the type no longer has',
                author: 'An author the type no longer has'
            }));
        });

        it('should fall back to what the item carries when no setting resolves at all', () => {
            // given: no row for this content type, so there is no flag to obey — the fields
            // the row carries values for render
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[]} />);

            // then
            expect(screen.getByLabelText(/Title/)).toHaveValue('He carried me');
            expect(screen.getByLabelText(/Author/)).toHaveValue('Anon');
        });

        it('should let a resolved false beat what the item carries', () => {
            // given: the flag is present, so presence is not consulted
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={quoteCarryingBoth}
                    isEditingAllowed
                    contentItemSettingCollection={[quoteSetting]} />);

            // then
            expect(screen.queryByLabelText(/Author/)).not.toBeInTheDocument();
        });
    });

    describe('what a hidden field submits', () => {
        // Ordered BEHIND Story, so the panel opens on the type that has a title to type into -
        // these tests are about abandoning a typed title, so the title has to exist first.
        const quoteSetting = settingFor(ContentType.Quote, 'Quote', {
            hasTitle: false,
            hasAuthor: false,
            sortOrder: 2
        });

        it('should not post a title typed under a type the reader then abandoned', async () => {
            // given: a title is typed while Story is selected, then Quote is chosen - the Quote
            // setting has no title, so the input disappears
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={[storySetting, quoteSetting]}
                    onAdded={onAdded} />);

            await userEvent.type(screen.getByLabelText(/Title/), 'A story title');
            await userEvent.type(screen.getByLabelText(/Author/), 'Someone Else');

            // when
            await userEvent.click(screen.getByRole('button', { name: /Quote/ }));
            await userEvent.type(screen.getByLabelText(/Quote/), 'The quote itself');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then: the reader cannot see them, the type is create-only, and the read surface
            // would never show them again - so they must not be stored
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                contentType: ContentType.Quote,
                content: 'The quote itself',
                title: '',
                author: ''
            }));
        });

        it('should keep what was typed when the reader picks the type back again', async () => {
            // given: dropping the value on SUBMIT must not mean losing it while still typing
            signInAs(authState);

            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={[storySetting, quoteSetting]} />);

            await userEvent.type(screen.getByLabelText(/Title/), 'A story title');

            // when
            await userEvent.click(screen.getByRole('button', { name: /Quote/ }));
            await userEvent.click(screen.getByRole('button', { name: /Story/ }));

            // then
            expect(screen.getByLabelText(/Title/)).toHaveValue('A story title');
        });

        it('should not post a permission note the reader has just withdrawn', async () => {
            // given: the note is hidden by the reader's OWN answer, not by policy - so unlike a
            // title it drops rather than being carried, in both directions
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    onAdded={onAdded} />);

            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PermissionGranted));

            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'Emailed by the author');

            await userEvent.type(screen.getByLabelText(/^Story/), 'The story itself');

            // when: they change their mind and release it as their own, which rests on nobody's
            // permission at all
            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.OwnedPublicDomain));

            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then: a permission claim must not be filed against an item released outright
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                sharePermission: ''
            }));
        });

        it('should drop a stored permission note when an amendment withdraws the basis', async () => {
            // given
            signInAs(authState);
            const onModified = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.PermissionGranted,
                        sharePermission: 'Emailed by the author'
                    })}
                    isEditingAllowed
                    onModified={onModified}
                    contentItemSettingCollection={settings} />);

            // when
            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.OwnedPublicDomain));

            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            // then
            expect(onModified).toHaveBeenCalledWith(expect.objectContaining({
                shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                sharePermission: ''
            }));
        });

        it('should keep the note when the basis still says permission was granted', async () => {
            // given
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItemSettingCollection={settings}
                    onAdded={onAdded} />);

            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PermissionGranted));

            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'Emailed by the author');

            await userEvent.type(screen.getByLabelText(/^Story/), 'The story itself');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                shareabilityBasis: ShareabilityBasis.PermissionGranted,
                sharePermission: 'Emailed by the author'
            }));
        });

        it('should still carry a hidden field that was already on the row', async () => {
            // given: the edit half of the same rule - hiding is a rendering rule, never a
            // destructive one
            signInAs(authState);
            const onModified = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        contentType: ContentType.Quote,
                        title: 'A title the type no longer has'
                    })}
                    isEditingAllowed
                    onModified={onModified}
                    contentItemSettingCollection={[quoteSetting]} />);

            // when
            await userEvent.clear(screen.getByLabelText(/Quote/));
            await userEvent.type(screen.getByLabelText(/Quote/), 'Amended');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            // then
            expect(onModified).toHaveBeenCalledWith(expect.objectContaining({
                content: 'Amended',
                title: 'A title the type no longer has'
            }));
        });
    });

    describe('effective setting resolution', () => {
        // §6.4 / §12.5.2 rules 1-2: the item-level override takes FULL precedence over the
        // content type default, and a soft-deleted row is out of resolution altogether (§6.6).
        const overrideFor = (
            contentItemId: string,
            overrides: Partial<ContentItemSetting> = {}): ContentItemSetting =>
            settingFor(ContentType.Story, 'Story override', {
                id: `override-${contentItemId}`,
                contentItemId,
                ...overrides
            });

        it('should let an item override beat the content type default', () => {
            // given: the default says a story has an author, this item's override says it has none
            signInAs(authState);
            const override = overrideFor('content-item-1', { hasAuthor: false });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[storySetting, override]} />);

            // then
            expect(screen.queryByLabelText(/Author/)).not.toBeInTheDocument();
            expect(screen.getByLabelText(/Title/)).toBeInTheDocument();
        });

        it('should read the override wherever it sits in the collection', () => {
            // given: order must not decide the outcome
            signInAs(authState);
            const override = overrideFor('content-item-1', { hasAuthor: false });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[override, storySetting]} />);

            // then
            expect(screen.queryByLabelText(/Author/)).not.toBeInTheDocument();
        });

        it('should never apply the override of one item to another item', () => {
            // given: an override for a DIFFERENT story
            signInAs(authState);
            const override = overrideFor('content-item-99', { hasAuthor: false });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[storySetting, override]} />);

            // then: the default still applies
            expect(screen.getByLabelText(/Author/)).toBeInTheDocument();
        });

        it('should exclude a soft-deleted row from resolution', () => {
            // given
            signInAs(authState);
            const override = overrideFor('content-item-1', { hasAuthor: false, isDeleted: true });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[storySetting, override]} />);

            // then: the withdrawn override is not policy, so the default applies
            expect(screen.getByLabelText(/Author/)).toBeInTheDocument();
        });

        it('should fall back to the default when no override matches', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[storySetting]} />);

            // then: the default's own name on the frozen-type chip, not the enum label —
            // scoped to the chip, because the content label says the same word
            expect(document.querySelector('.g2h-content-item-chip'))
                .toHaveTextContent('Story');
        });

        it('should offer the defaults alone in the picker, never an override', () => {
            // given
            signInAs(authState);
            const override = overrideFor('content-item-1', { contentTypeName: 'Story override' });

            // when
            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={[storySetting, override]} />);

            // then: an override belongs to one existing item and is never a contributable type
            expect(screen.getAllByRole('button', { name: /Story/ })).toHaveLength(1);

            expect(screen.queryByRole('button', { name: /Story override/ }))
                .not.toBeInTheDocument();
        });

        it('should offer only the types open to a general contribution', () => {
            // given
            signInAs(authState);

            const adminOnly = settingFor(ContentType.BlogPost, 'Blog Post', {
                isAvailableAsGeneralUserContribution: false
            });

            // when
            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={[storySetting, adminOnly]} />);

            // then
            expect(screen.getByRole('button', { name: /Story/ })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Blog Post/ })).not.toBeInTheDocument();
        });

        it('should say nothing is open when every row is closed to contribution', () => {
            // given
            signInAs(authState);

            const adminOnly = settingFor(ContentType.BlogPost, 'Blog Post', {
                isAvailableAsGeneralUserContribution: false
            });

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={[adminOnly]} />);

            // then
            expect(screen.getByText('Contributions are not open for any content type right now.'))
                .toBeInTheDocument();
        });

        it('should still shape the editor from a row the picker would not offer', () => {
            // given: a blog post is not a general contribution, but its row still shapes
            signInAs(authState);

            const blogSetting = settingFor(ContentType.BlogPost, 'Blog Post', {
                isAvailableAsGeneralUserContribution: false,
                hasAuthor: false
            });

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({ contentType: ContentType.BlogPost })}
                    isEditingAllowed
                    contentItemSettingCollection={[blogSetting]} />);

            // then
            expect(document.querySelector('.g2h-content-item-chip'))
                .toHaveTextContent('Blog Post');

            expect(screen.queryByLabelText(/Author/)).not.toBeInTheDocument();
        });
    });

    describe('surface control by the consumer', () => {
        // The mode prop and the in-place Edit entry belong to ContentItemPanel now — see
        // contentItemPanel.test.tsx, which pins that dispatch.
        it('should reseed the editor when a different item arrives', async () => {
            // given: React Router reuses one element across /posts/a and /posts/b, so the panel
            // must not carry one item's half-typed edit onto the next
            signInAs(authState);

            const view = renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            await userEvent.clear(screen.getByLabelText(/^Story/));
            await userEvent.type(screen.getByLabelText(/^Story/), 'Half a thought');

            // when
            view.rerender(wrapped(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        id: 'content-item-2',
                        content: 'A different story entirely.'
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />));

            // then
            expect(screen.getByLabelText(/^Story/)).toHaveValue('A different story entirely.');
        });
    });

    describe('add role overrides', () => {
        it('should withhold the form from a reader without the required add role', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    addRoles="Editors"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Contributions are not open to this account.'))
                .toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });

        it('should open the form to a reader who holds one of them', () => {
            // given
            signInAs(authState, ['Editors']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    addRoles="Editors, Administrators"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: 'Submit for review' })).toBeInTheDocument();
        });

        it('should let a block outrank an add role the reader holds', () => {
            // given: the block question is asked first (#366)
            signInAs(authState, ['Editors', 'ContentItem-ReadOnly']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    addRoles="Editors"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });
    });

    describe('presentation', () => {
        it('should not leave a bare section for the theme to pad', () => {
            // given
            signInAs(authState);

            // when
            const { container } = renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then: the theme pads every bare <section> by 3.5rem/2.8rem, which the panel's own
            // class neutralises
            const sections = Array.from(container.querySelectorAll('section'));

            sections.forEach((section) =>
                expect(section).toHaveClass('g2h-content-item-panel'));
        });

        it('should freeze the buttons while the consumer is persisting', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} isSubmitting />);

            // then
            expect(screen.getByRole('button', { name: 'Submit for review' })).toBeDisabled();
        });

        it('should show a loading line instead of a half-built form', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemFormPanel isLoading />);

            // then
            expect(screen.getByText('Loading…')).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });

    });

    // An owned basis says WHO wrote it but not what they want to be called for it, so it fills
    // the Author field in rather than taking it away — a contributor may publish under a pen
    // name, an initial, or a maiden name. The read surface then hides the column only when it
    // would be printing that one person twice.
    describe('an owned basis prefills the author', () => {
        // signInAs mints displayName 'Tester'.
        const ViewerName = 'Tester';

        it('should put the contributor\'s own name in the field on an untouched form', async () => {
            // given: the form opens on an owned basis
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/Author/)).toHaveValue(ViewerName);
        });

        it('should leave the field empty for a basis that names somebody else', async () => {
            // given
            signInAs(authState);
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // when
            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.PublicDomain));

            // then: the contributor is not the author here, so their name must not be sitting in
            // the box waiting to be submitted by somebody who did not read it
            expect(screen.getByLabelText(/Author/)).toHaveValue('');
        });

        it('should submit the prefilled name the contributor was shown and left alone', async () => {
            // given
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            // when
            await userEvent.type(screen.getByLabelText(/Title/), 'He carried me');
            await userEvent.type(screen.getByLabelText(/^Story/), 'The whole story.');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then: what the field showed is what gets filed
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                author: ViewerName
            }));
        });

        it('should let the contributor publish under another name', async () => {
            // given
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

            // when
            await userEvent.clear(screen.getByLabelText(/Author/));
            await userEvent.type(screen.getByLabelText(/Author/), 'A. Pilgrim');
            await userEvent.type(screen.getByLabelText(/^Story/), 'The whole story.');
            await userEvent.type(
                screen.getByLabelText(/Permission details/), 'By email from the author');
            await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

            // then
            expect(onAdded).toHaveBeenCalledWith(expect.objectContaining({
                author: 'A. Pilgrim'
            }));
        });

        it('should not overwrite a pen name when the basis changes', async () => {
            // given
            signInAs(authState);
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            await userEvent.clear(screen.getByLabelText(/Author/));
            await userEvent.type(screen.getByLabelText(/Author/), 'A. Pilgrim');

            // when: between the two owned options, both of which would prefill an empty field
            await userEvent.selectOptions(
                screen.getByLabelText(/How are you permitted to share this\?/),
                String(ShareabilityBasis.OwnedPublicDomain));

            // then
            expect(screen.getByLabelText(/Author/)).toHaveValue('A. Pilgrim');
        });

        it('should respect a field the contributor deliberately emptied', async () => {
            // given
            signInAs(authState);
            renderWithAuth(<ContentItemFormPanel contentItemSettingCollection={settings} />);

            // when
            await userEvent.clear(screen.getByLabelText(/Author/));

            // then: refilling it here would be the form arguing with the person using it
            expect(screen.getByLabelText(/Author/)).toHaveValue('');
        });

        it('should never overwrite an author already on the item', async () => {
            // given: an owned item somebody else contributed, carrying its own author. The
            // editor holds a role rather than being the owner — otherwise the panel refuses
            // `edit` back to `read` and there is no field to assert on.
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                        author: 'Grace Abara',
                        createdBy: OtherId
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then: prefilling here would sign her work with whoever opened the editor
            expect(screen.getByLabelText(/Author/)).toHaveValue('Grace Abara');
        });

        it('should prefill an amendment from the submitter, not from the editor', async () => {
            // given: a publisher amending somebody else's owned contribution that carries no
            // author yet
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                        author: '',
                        createdBy: OtherId
                    })}
                    isEditingAllowed
                    submittedByDisplayName="Grace Abara"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/Author/)).toHaveValue('Grace Abara');
        });
    });

    describe('the type chip', () => {
        it('should key the chip on the enum member name, never the editable display name', () => {
            // given: the administrator has renamed the type, which must not detach it from its
            // colour — the stylesheet keys off the member name for exactly this reason
            signInAs(authState);
            const renamed = settingFor(ContentType.Story, 'Testimonies of Grace');

            // when: the frozen-type chip in the editor
            const { container } = renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={[renamed]} />);

            // then
            const chip = container.querySelector('.g2h-content-item-chip');

            expect(chip).toHaveAttribute('data-content-type', 'Story');
            expect(chip).toHaveTextContent('Testimonies of Grace');
        });

        it('should re-key the chip when the type in play changes', async () => {
            // given
            signInAs(authState);

            const { container, rerender } = renderWithAuth(
                <ContentItemFormPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            expect(container.querySelector('.g2h-content-item-chip'))
                .toHaveAttribute('data-content-type', 'Story');

            // when
            rerender(wrapped(
                <ContentItemFormPanel
                    contentItem={itemWith({
                        id: 'content-item-2',
                        contentType: ContentType.Devotional
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />));

            // then: nothing is wired between the type and the colour — the attribute IS the
            // selector, so the cascade re-resolves on the next paint
            expect(container.querySelector('.g2h-content-item-chip'))
                .toHaveAttribute('data-content-type', 'Devotional');
        });

        it('should mark only the selected tile for the stylesheet to paint', async () => {
            // given
            signInAs(authState);

            const { container } = renderWithAuth(
                <ContentItemFormPanel contentItemSettingCollection={settings} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));

            // then
            const selected = Array.from(
                container.querySelectorAll('.g2h-content-item-type-selected'));

            expect(selected).toHaveLength(1);
            expect(selected[0]).toHaveAttribute('data-content-type', 'Devotional');
        });
    });
});

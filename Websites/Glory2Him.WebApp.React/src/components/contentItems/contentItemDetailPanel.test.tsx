import { ReactElement, ReactNode } from 'react';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemDetailPanel } from './contentItemDetailPanel';
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

describe('ContentItemDetailPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    describe('add mode', () => {
        it('should render the picker, the fields and the submit pair for a signed-in reader', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Contributions are not open to this account.'))
                .toBeInTheDocument();
        });

        it('should close one tile and leave the rest of the form live for a narrow block', () => {
            // given: blocked from devotionals only — stories stay open
            signInAs(authState, ['ContentItem-Devotional-ReadOnly']);

            // when
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={[storySetting, quoteSetting]} />);

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
                <ContentItemDetailPanel
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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={[]} />);

            // then
            expect(screen.getByText('Contributions are not open for any content type right now.'))
                .toBeInTheDocument();
        });

        it('should raise onCancelled rather than clearing anything itself', async () => {
            // given
            signInAs(authState);
            const onCancelled = vi.fn();

            renderWithAuth(
                <ContentItemDetailPanel
                    contentItemSettingCollection={settings}
                    onCancelled={onCancelled} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            // then
            expect(onCancelled).toHaveBeenCalledTimes(1);
        });
    });

    describe('read mode', () => {
        it('should default to read and render the item for anyone, anonymous included', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('heading', { name: 'He carried me' })).toBeInTheDocument();
            expect(screen.getByText('Author')).toBeInTheDocument();
            expect(screen.getByText('Anon')).toBeInTheDocument();
            expect(screen.getByText('The whole story, as it happened.')).toBeInTheDocument();
            expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
        });

        it('should show no action at all while isEditingAllowed is off, roles regardless', () => {
            // given: the owner, who is also an administrator — every gate below would pass
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should refuse a mode of edit back to read while isEditingAllowed is off', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.getByText('The whole story, as it happened.')).toBeInTheDocument();
        });

        it('should give the owner both actions on their own item', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Delete/ })).toBeInTheDocument();
        });

        it('should keep the owner editing their own item after it is decided', () => {
            // given: amending a terminal item forks a new version (§3.4 rule 16) — the CONSUMER
            // decides that, but the affordance stays
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ approvalStatus: ApprovalStatus.Approved })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
        });

        it('should offer the publisher tier an edit on a live item and none on a decided one', () => {
            // given
            signInAs(authState, ['ContentItem-Story-Publishers']);

            // when
            const submitted = renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        createdBy: OtherId,
                        approvalStatus: ApprovalStatus.Submitted
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
            submitted.unmount();

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        createdBy: OtherId,
                        approvalStatus: ApprovalStatus.Approved
                    })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
        });

        it('should never offer the reviewer tier an edit — a reviewer reviews', () => {
            // given
            signInAs(authState, ['Reviewers', 'ContentItem-Reviewers',
                'ContentItem-Story-Reviewers']);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should withhold removal from the publisher tier — a takedown is not moderation', () => {
            // given
            signInAs(authState, ['Publishers']);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should give an administrator removal on somebody else\'s item', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('button', { name: /Delete/ })).toBeInTheDocument();
        });

        it('should leave a plain reader with neither action', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ createdBy: OtherId })}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should strip both actions from an owner blocked for that content type', () => {
            // given: the block outranks [OWNER] (#366)
            signInAs(authState, ['ContentItem-Story-ReadOnly']);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
        });

        it('should confirm before it raises onRemoved', async () => {
            // given
            signInAs(authState);
            const onRemoved = vi.fn();
            const contentItem = itemWith();

            renderWithAuth(
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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

    describe('the self-contained projection', () => {
        // A list surface hands its element over WITH its winning setting; re-resolving
        // from a collection that may not hold the override would silently un-override it.
        it('should let the embedded winner beat the collection for its own item', () => {
            signInAs(authState);

            // The collection's Story default says hasTitle; the element's own winner says
            // not — the element wins, so no title heading renders.
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        contentItemSetting: settingFor(ContentType.Story, 'Story override', {
                            id: 'override-row',
                            contentItemId: 'content-item-1',
                            hasTitle: false
                        })
                    })}
                    contentItemSettingCollection={settings} />);

            expect(screen.queryByRole('heading', { name: 'He carried me' }))
                .not.toBeInTheDocument();
        });

        it('should emit a projection carrying the winner it was shaped with', async () => {
            signInAs(authState);
            const onAdded = vi.fn();

            renderWithAuth(
                <ContentItemDetailPanel
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
        it('should open the editor on Edit and announce the mode', async () => {
            // given
            signInAs(authState);
            const onModeChanged = vi.fn();

            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    onModeChanged={onModeChanged}
                    contentItemSettingCollection={settings} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));

            // then
            expect(onModeChanged).toHaveBeenCalledWith('edit');
            expect(screen.getByLabelText(/Title/)).toHaveValue('He carried me');
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
        });

        it('should freeze the content type rather than offering the picker', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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

        it('should return to read with the original values on Cancel', async () => {
            // given
            signInAs(authState);
            const onCancelled = vi.fn();

            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    isEditingAllowed
                    onCancelled={onCancelled}
                    contentItemSettingCollection={settings} />);

            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));
            await userEvent.clear(screen.getByLabelText(/Title/));
            await userEvent.type(screen.getByLabelText(/Title/), 'Something else');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            // then
            expect(onCancelled).toHaveBeenCalledTimes(1);
            expect(screen.getByRole('heading', { name: 'He carried me' })).toBeInTheDocument();

            // when: back into the editor
            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));

            // then: the abandoned edit is gone
            expect(screen.getByLabelText(/Title/)).toHaveValue('He carried me');
        });
    });

    describe('validation readback', () => {
        it('should mark each field the API named and show its message', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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

        it('should hide both fields in the read surface when the setting says the type has neither', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={quoteCarryingBoth}
                    contentItemSettingCollection={[quoteSetting]} />);

            // then: the values are still on the row - the setting decides what RENDERS
            expect(screen.queryByRole('heading', { name: /A title the type no longer has/ }))
                .not.toBeInTheDocument();

            expect(screen.queryByText(/An author the type no longer has/)).not.toBeInTheDocument();
            expect(screen.getByText('The whole story, as it happened.')).toBeInTheDocument();
        });

        it('should hide both fields in the editor for the same setting', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={quoteCarryingBoth}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={quoteCarryingBoth}
                    mode="edit"
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
            // when: no row for this content type, so there is no flag to obey
            renderWithAuth(
                <ContentItemDetailPanel contentItem={itemWith()} contentItemSettingCollection={[]} />);

            // then
            expect(screen.getByRole('heading', { name: 'He carried me' })).toBeInTheDocument();
            expect(screen.getByText('Anon')).toBeInTheDocument();
        });

        it('should let a resolved false beat what the item carries', () => {
            // when: the flag is present, so presence is not consulted
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={quoteCarryingBoth}
                    contentItemSettingCollection={[quoteSetting]} />);

            // then
            expect(screen.queryByText(/An author the type no longer has/)).not.toBeInTheDocument();
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.PermissionGranted,
                        sharePermission: 'Emailed by the author'
                    })}
                    mode="edit"
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        contentType: ContentType.Quote,
                        title: 'A title the type no longer has'
                    })}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
                    isEditingAllowed
                    contentItemSettingCollection={[storySetting, override]} />);

            // then: the withdrawn override is not policy, so the default applies
            expect(screen.getByLabelText(/Author/)).toBeInTheDocument();
        });

        it('should fall back to the default when no override matches', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={[storySetting]} />);

            // then: the default's own name, not the enum label
            expect(screen.getByText('Story')).toBeInTheDocument();
        });

        it('should offer the defaults alone in the picker, never an override', () => {
            // given
            signInAs(authState);
            const override = overrideFor('content-item-1', { contentTypeName: 'Story override' });

            // when
            renderWithAuth(
                <ContentItemDetailPanel contentItemSettingCollection={[storySetting, override]} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={[storySetting, adminOnly]} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={[adminOnly]} />);

            // then
            expect(screen.getByText('Contributions are not open for any content type right now.'))
                .toBeInTheDocument();
        });

        it('should still shape the read surface from a row the picker would not offer', () => {
            // given: a blog post is not a general contribution, but it still renders
            const blogSetting = settingFor(ContentType.BlogPost, 'Blog Post', {
                isAvailableAsGeneralUserContribution: false,
                hasAuthor: false
            });

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ contentType: ContentType.BlogPost })}
                    contentItemSettingCollection={[blogSetting]} />);

            // then
            expect(screen.getByText('Blog Post')).toBeInTheDocument();
            expect(screen.queryByText('Anon')).not.toBeInTheDocument();
        });
    });

    describe('surface control by the consumer', () => {
        it('should let a change to the mode prop overrule the surface the reader chose', async () => {
            // given: the pattern onModeChanged invites - the page tracks the mode and hands it
            // back. Without this, the reader's first click shadows the prop for the item's life.
            signInAs(authState);
            const contentItem = itemWith();

            const view = renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={contentItem}
                    mode="read"
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();

            // when: the page catches up with the reader, then later closes the editor itself
            view.rerender(wrapped(
                <ContentItemDetailPanel
                    contentItem={contentItem}
                    mode="edit"
                    isEditingAllowed
                    contentItemSettingCollection={settings} />));

            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();

            view.rerender(wrapped(
                <ContentItemDetailPanel
                    contentItem={contentItem}
                    mode="read"
                    isEditingAllowed
                    contentItemSettingCollection={settings} />));

            // then
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.getByText('The whole story, as it happened.')).toBeInTheDocument();
        });

        it('should reseed the editor when a different item arrives', async () => {
            // given: React Router reuses one element across /posts/a and /posts/b, so the panel
            // must not carry one item's half-typed edit onto the next
            signInAs(authState);

            const view = renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    mode="edit"
                    isEditingAllowed
                    contentItemSettingCollection={settings} />);

            await userEvent.clear(screen.getByLabelText(/^Story/));
            await userEvent.type(screen.getByLabelText(/^Story/), 'Half a thought');

            // when
            view.rerender(wrapped(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        id: 'content-item-2',
                        content: 'A different story entirely.'
                    })}
                    mode="edit"
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
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
                <ContentItemDetailPanel
                    addRoles="Editors"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });
    });

    describe('presentation', () => {
        it('should let a page suppress the item title it states itself', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    showItemTitle={false}
                    contentItemSettingCollection={settings} />);

            // then: the body still renders - only the duplicated heading is gone
            expect(screen.queryByRole('heading', { name: 'He carried me' }))
                .not.toBeInTheDocument();

            expect(screen.getByText('The whole story, as it happened.')).toBeInTheDocument();
        });

        it('should not leave a bare section for the theme to pad', () => {
            // given
            signInAs(authState);

            // when
            const { container } = renderWithAuth(
                <ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} isSubmitting />);

            // then
            expect(screen.getByRole('button', { name: 'Submit for review' })).toBeDisabled();
        });

        it('should show a loading line instead of a half-built form', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(<ContentItemDetailPanel isLoading />);

            // then
            expect(screen.getByText('Loading…')).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Submit for review' }))
                .not.toBeInTheDocument();
        });

        it('should render the item title at the heading level the consumer asked for', () => {
            // given: a page whose whole subject is this one item heads the document with it,
            // rather than duplicating the title above a chip the panel owns

            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    titleHeadingLevel="h1"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('heading', { name: 'He carried me', level: 1 }))
                .toBeInTheDocument();
        });

        it('should head the item at h3 when the consumer says nothing', () => {
            // when: a panel sitting among other content must not claim the document's h1
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByRole('heading', { name: 'He carried me', level: 3 }))
                .toBeInTheDocument();
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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/Author/)).toHaveValue(ViewerName);
        });

        it('should leave the field empty for a basis that names somebody else', async () => {
            // given
            signInAs(authState);
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
                <ContentItemDetailPanel contentItemSettingCollection={settings} onAdded={onAdded} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
            renderWithAuth(<ContentItemDetailPanel contentItemSettingCollection={settings} />);

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
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                        author: 'Grace Abara',
                        createdBy: OtherId
                    })}
                    mode="edit"
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
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                        author: '',
                        createdBy: OtherId
                    })}
                    mode="edit"
                    isEditingAllowed
                    submittedByDisplayName="Grace Abara"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByLabelText(/Author/)).toHaveValue('Grace Abara');
        });
    });

    describe('the author column on the read surface', () => {
        it('should say nothing twice when the author IS the submitter', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                        author: 'Louis Ferguson'
                    })}
                    submittedByDisplayName="Louis Ferguson"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Submitted by')).toBeInTheDocument();
            expect(screen.queryByText('Author')).not.toBeInTheDocument();
            expect(screen.getAllByText('Louis Ferguson')).toHaveLength(1);
        });

        it('should treat a difference in case as the same person', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ author: 'louis ferguson' })}
                    submittedByDisplayName="Louis Ferguson"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByText('Author')).not.toBeInTheDocument();
        });

        it('should show the pen name the contributor chose', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
                        author: 'A. Pilgrim'
                    })}
                    submittedByDisplayName="Louis Ferguson"
                    contentItemSettingCollection={settings} />);

            // then: it says something the submitter column does not
            expect(screen.getByText('Author')).toBeInTheDocument();
            expect(screen.getByText('A. Pilgrim')).toBeInTheDocument();
            expect(screen.getByText('Louis Ferguson')).toBeInTheDocument();
        });

        it('should show the author while the submitter is still unresolved', () => {
            // when: no submitter passed, so nothing is known to be duplicated
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ author: 'Anon' })}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Author')).toBeInTheDocument();
            expect(screen.getByText('Anon')).toBeInTheDocument();
        });
    });

    describe('the type chip', () => {
        it('should key the chip on the enum member name, never the editable display name', () => {
            // given: the administrator has renamed the type, which must not detach it from its
            // colour — the stylesheet keys off the member name for exactly this reason
            const renamed = settingFor(ContentType.Story, 'Testimonies of Grace');

            // when
            const { container } = renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={[renamed]} />);

            // then
            const chip = container.querySelector('.g2h-content-item-chip');

            expect(chip).toHaveAttribute('data-content-type', 'Story');
            expect(chip).toHaveTextContent('Testimonies of Grace');
        });

        it('should re-key the chip when the type in play changes', async () => {
            // given
            const { container, rerender } = renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={settings} />);

            expect(container.querySelector('.g2h-content-item-chip'))
                .toHaveAttribute('data-content-type', 'Story');

            // when
            rerender(wrapped(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        id: 'content-item-2',
                        contentType: ContentType.Devotional
                    })}
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
                <ContentItemDetailPanel contentItemSettingCollection={settings} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));

            // then
            const selected = Array.from(
                container.querySelectorAll('.g2h-content-item-type-selected'));

            expect(selected).toHaveLength(1);
            expect(selected[0]).toHaveAttribute('data-content-type', 'Devotional');
        });
    });

    describe('the read byline', () => {
        it('should name and picture the contributor the consumer resolved', () => {
            // when: the panel fetches nothing — CreatedBy is an account id, and the NAME behind it
            // is the consumer's to look up
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    submittedByDisplayName="Louis Ferguson"
                    submittedByImageUrl="Profile-Image/abc?v=1234"
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Submitted by')).toBeInTheDocument();
            expect(screen.getByText('Louis Ferguson')).toBeInTheDocument();

            expect(screen.getByRole('img', { name: 'Louis Ferguson' }))
                .toHaveAttribute('src', 'Profile-Image/abc?v=1234');
        });

        it('should show no contributor at all rather than a placeholder while one loads', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    contentItemSettingCollection={settings} />);

            // then: a name that is still arriving must not flash somebody else's under a
            // testimony
            expect(screen.queryByText('Submitted by')).not.toBeInTheDocument();
        });

        it('should state the licence rather than repeat who wrote it', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({
                        shareabilityBasis: ShareabilityBasis.OwnedPublicDomain
                    })}
                    contentItemSettingCollection={settings} />);

            // then: a reader wants to know what may be done with it; who wrote it is the two
            // columns to the left
            expect(screen.getByText('Shareability')).toBeInTheDocument();
            expect(screen.getByText('Public Domain')).toBeInTheDocument();
        });

        it('should name the retired basis plainly rather than claiming a licence for it', () => {
            // when: every item contributed before the basis was split carries this
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ shareabilityBasis: ShareabilityBasis.Owned })}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Own Work')).toBeInTheDocument();
        });

        it('should date the contribution from the row rather than from today', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ createdWhen: '2026-07-15T09:14:00+00:00' })}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText('Jul 15, 2026')).toBeInTheDocument();
        });

        it('should ignore a date it cannot parse rather than printing Invalid Date', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith({ createdWhen: 'not a date' })}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.queryByText(/Invalid Date/)).not.toBeInTheDocument();
        });
    });

    describe('the engagement figures', () => {
        it('should read the figures the consumer gathered', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    readingTimeMinutes={5}
                    reactionCount={257}
                    commentCount={4}
                    viewCount={2344}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText(/5 min read/)).toBeInTheDocument();
            expect(screen.getByText(/257 reactions/)).toBeInTheDocument();
            expect(screen.getByText(/4 comments/)).toBeInTheDocument();
            expect(screen.getByText(/2,344 Views/)).toBeInTheDocument();
        });

        it('should agree in number with the count it is reporting', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    reactionCount={1}
                    commentCount={1}
                    viewCount={1}
                    contentItemSettingCollection={settings} />);

            // then
            expect(screen.getByText(/1 reaction$/)).toBeInTheDocument();
            expect(screen.getByText(/1 comment$/)).toBeInTheDocument();
            expect(screen.getByText(/1 View$/)).toBeInTheDocument();
        });

        it('should leave out a figure it was given none of rather than reporting a zero', () => {
            // when: only one of the four is passed
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    commentCount={3}
                    contentItemSettingCollection={settings} />);

            // then: "0 reactions" asserts that nobody responded, which is a different statement
            // from a surface with nothing to report
            expect(screen.getByText(/3 comments/)).toBeInTheDocument();
            expect(screen.queryByText(/reaction/)).not.toBeInTheDocument();
            expect(screen.queryByText(/View/)).not.toBeInTheDocument();
            expect(screen.queryByText(/min read/)).not.toBeInTheDocument();
        });

        it('should report a genuine zero it was actually given', () => {
            // when
            renderWithAuth(
                <ContentItemDetailPanel
                    contentItem={itemWith()}
                    commentCount={0}
                    contentItemSettingCollection={settings} />);

            // then: undefined means "no figure", zero means "none yet" — and the two must not
            // collapse into each other
            expect(screen.getByText(/0 comments/)).toBeInTheDocument();
        });
    });
});

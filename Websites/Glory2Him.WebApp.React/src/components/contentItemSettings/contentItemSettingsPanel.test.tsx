import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import userEvent from '@testing-library/user-event';
import { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '../securitys/authProvider';
import { ContentItemSettingsPanel } from './contentItemSettingsPanel';
import { createAuthState, signInAs, signOut } from '../../tests/testAuth';
import { testContentItemSetting } from '../../tests/testContentItemSettings';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSetting
} from '../../models/foundations/contentItemSettings/contentItemSetting';

// THE SETTINGS SIDEBAR, both faces. Every fact the panel shows comes from the collection it is
// handed and the identity it is rendered under, so each test varies exactly one of those two.
//
// The auth double is here because the writes are identity decisions: Modify and Remove Override
// belong to Administrators and to nobody else. Render gates only — the foundation re-decides
// both against the stored row (§14.6), which is why no test here asserts a server outcome.
const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const renderPanel = (ui: ReactElement) =>
    render(
        <MemoryRouter initialEntries={['/Admin/Posts/item-1']}>
            <AuthProvider>{ui}</AuthProvider>
        </MemoryRouter>);

const contentItemId = 'item-1';

const typeDefault = (overrides: Partial<ContentItemSetting> = {}): ContentItemSetting =>
    testContentItemSetting(ContentType.Devotional, 'Devotional', {
        id: 'default-devotional',
        contentItemId: null,
        ...overrides
    });

const itemOverride = (overrides: Partial<ContentItemSetting> = {}): ContentItemSetting =>
    testContentItemSetting(ContentType.Devotional, 'Devotional', {
        id: 'override-devotional',
        contentItemId,
        ...overrides
    });

// The switch behind a label, found through the label's own text so a test never depends on the
// order the rows happen to render in.
const switchFor = (label: string): HTMLInputElement =>
    screen.getByText(label).parentElement!.querySelector('input')!;

describe('ContentItemSettingsPanel', () => {
    beforeEach(() => {
        signInAs(authState, ['Administrators']);
    });

    describe('resolving which settings apply', () => {
        it('should show the content type default when the item has no override', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault({ showComments: false })]} />);

            expect(switchFor('Comments are shown').checked).toBe(false);
            expect(screen.getByText('Default')).toBeInTheDocument();
        });

        it('should prefer the item override over the content type default', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[
                        typeDefault({ showComments: true }),
                        itemOverride({ showComments: false })
                    ]} />);

            expect(switchFor('Comments are shown').checked).toBe(false);
            expect(screen.getByText('Override')).toBeInTheDocument();
        });

        // ANOTHER ITEM'S OVERRIDE MUST NOT LAND HERE. A moderation surface can legitimately hold
        // a collection covering several items, and applying the wrong row would narrow an item
        // nobody narrowed.
        it('should ignore an override belonging to a different content item', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[
                        typeDefault({ showComments: true }),
                        itemOverride({ contentItemId: 'item-2', showComments: false })
                    ]} />);

            expect(switchFor('Comments are shown').checked).toBe(true);
            expect(screen.getByText('Default')).toBeInTheDocument();
        });

        it('should say so honestly when no setting applies at all', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[]} />);

            expect(screen.getByText(/No content settings apply/i)).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Modify' })).toBeNull();
        });

        it('should exclude a soft-deleted override and fall back to the default', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[
                        typeDefault({ showComments: true }),
                        itemOverride({ showComments: false, isDeleted: true })
                    ]} />);

            expect(switchFor('Comments are shown').checked).toBe(true);
            expect(screen.getByText('Default')).toBeInTheDocument();
        });
    });

    describe('the scope ribbon', () => {
        it('should name the default scope on the read face by default', () => {
            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            const ribbon = container.querySelector('.g2h-settings-ribbon');
            expect(ribbon).not.toBeNull();
            expect(ribbon!.getAttribute('data-setting-scope')).toBe('Default');
            expect(ribbon!.textContent).toBe('Default');
            expect(container.querySelector('.g2h-has-corner-ribbon')).not.toBeNull();
        });

        it('should name the override scope when the item carries its own row', () => {
            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]} />);

            const ribbon = container.querySelector('.g2h-settings-ribbon');
            expect(ribbon!.getAttribute('data-setting-scope')).toBe('Override');
            expect(ribbon!.textContent).toBe('Override');
        });

        // Turned off, the scope does not vanish — it moves to a badge. A reader must never have
        // to guess whether they are looking at the type default.
        it('should fall back to a badge when the ribbon is turned off', () => {
            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]}
                    showRibbon={false} />);

            expect(container.querySelector('.g2h-settings-ribbon')).toBeNull();
            expect(container.querySelector('.badge')!.textContent).toBe('Override');
        });

        it('should wear no ribbon on the modify face', async () => {
            const user = userEvent.setup();

            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));

            expect(container.querySelector('.g2h-settings-ribbon')).toBeNull();
        });
    });

    describe('who may write', () => {
        it('should offer no write to a signed-out reader', () => {
            signOut(authState);

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]} />);

            expect(screen.queryByRole('button', { name: 'Modify' })).toBeNull();
            expect(screen.queryByRole('button', { name: 'Remove Override' })).toBeNull();
        });

        it('should offer no write to a reviewer, who cannot administer settings', () => {
            signInAs(authState, ['Reviewers', 'Publishers']);

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]} />);

            expect(screen.queryByRole('button', { name: 'Modify' })).toBeNull();
            expect(screen.queryByRole('button', { name: 'Remove Override' })).toBeNull();
        });

        // A sanction outranks every grant (#366), so the ReadOnly role is asked before the
        // Administrators grant is honoured.
        it('should offer no write to a blocked administrator', () => {
            signInAs(authState, ['Administrators', 'ReadOnly']);

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]} />);

            expect(screen.queryByRole('button', { name: 'Modify' })).toBeNull();
        });

        // With no item there is nothing to override, so there is nothing to write either.
        it('should offer no write when no content item is named', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            expect(screen.queryByRole('button', { name: 'Modify' })).toBeNull();
            expect(screen.getByText('Default')).toBeInTheDocument();
        });
    });

    describe('Remove Override', () => {
        it('should not offer removal against a content type default', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            expect(screen.getByRole('button', { name: 'Modify' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Remove Override' })).toBeNull();
        });

        it('should raise the override row when removal is taken', async () => {
            const user = userEvent.setup();
            const onOverrideRemoved = vi.fn();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]}
                    onOverrideRemoved={onOverrideRemoved} />);

            await user.click(screen.getByRole('button', { name: 'Remove Override' }));

            expect(onOverrideRemoved).toHaveBeenCalledTimes(1);
            expect(onOverrideRemoved.mock.calls[0][0].id).toBe('override-devotional');
        });
    });

    // The theme's own .card carries no border, so "leave the class off" was never a border and
    // the switch moved nothing. Asserted in BOTH directions for that reason: a test that only
    // checked the off state passed the whole time the on state did nothing.
    describe('showBorder', () => {
        it('should draw the card bordered by default', () => {
            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            const card = container.querySelector('.card')!;
            expect(card).toHaveClass('border');
            expect(card).not.toHaveClass('border-0');
        });

        it('should take the border away when the surface turns it off', () => {
            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]}
                    showBorder={false} />);

            const card = container.querySelector('.card')!;
            expect(card).toHaveClass('border-0');
            expect(card).not.toHaveClass('border');
        });

        it('should carry the same answer onto the modify face', async () => {
            const user = userEvent.setup();

            const { container } = renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]}
                    showBorder={false} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));

            expect(container.querySelector('.card')!).toHaveClass('border-0');
        });
    });

    describe('the read face is read-only', () => {
        it('should render every switch disabled', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            expect(switchFor('Comments are shown').disabled).toBe(true);
            expect(switchFor('Tags can be added').disabled).toBe(true);
            expect(switchFor('Limit reactions to love only').disabled).toBe(true);
        });
    });

    describe('modifying', () => {
        it('should seed the form from the values the read face displayed', async () => {
            const user = userEvent.setup();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[
                        typeDefault({ showComments: true }),
                        itemOverride({ showComments: false, showTags: false })
                    ]} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));

            expect(switchFor('Comments are shown').checked).toBe(false);
            expect(switchFor('Tags are shown').checked).toBe(false);
            expect(switchFor('Comments are shown').disabled).toBe(false);
        });

        it('should land straight on the modify face when the mode says so', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    mode="modify"
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]} />);

            expect(screen.getByRole('button', { name: 'Save settings' })).toBeInTheDocument();
        });

        it('should notify the consumer when Modify is taken', async () => {
            const user = userEvent.setup();
            const onModify = vi.fn();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]}
                    onModify={onModify} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));

            expect(onModify).toHaveBeenCalledTimes(1);
        });

        // THE HEART OF IT: saving from a default-seeded form must create an OVERRIDE, so the row
        // that goes out carries the item's id and NOT the default's own identity — sending that
        // back would rewrite the policy for every item of the type.
        it('should save the edits as an override when the form was seeded from the default',
            async () => {
                const user = userEvent.setup();
                const onModified = vi.fn();

                renderPanel(
                    <ContentItemSettingsPanel
                        contentItemId={contentItemId}
                        contentType={ContentType.Devotional}
                        contentItemSettingCollection={[typeDefault({ showComments: true })]}
                        onModified={onModified} />);

                await user.click(screen.getByRole('button', { name: 'Modify' }));
                await user.click(switchFor('Comments are shown'));
                await user.click(screen.getByRole('button', { name: 'Save settings' }));

                expect(onModified).toHaveBeenCalledTimes(1);

                const saved: ContentItemSetting = onModified.mock.calls[0][0];
                expect(saved.contentItemId).toBe(contentItemId);
                expect(saved.id).toBe('');
                expect(saved.showComments).toBe(false);
            });

        it('should save onto the existing override when one was already in force', async () => {
            const user = userEvent.setup();
            const onModified = vi.fn();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[
                        typeDefault(),
                        itemOverride({ showComments: true })
                    ]}
                    onModified={onModified} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));
            await user.click(switchFor('Comments are shown'));
            await user.click(screen.getByRole('button', { name: 'Save settings' }));

            const saved: ContentItemSetting = onModified.mock.calls[0][0];
            expect(saved.id).toBe('override-devotional');
            expect(saved.contentItemId).toBe(contentItemId);
            expect(saved.showComments).toBe(false);
        });

        // The foundation refuses an add with no ContentTypeName, a description over its ceiling
        // or a negative SortOrder, so a create that dropped the fields this form does not edit
        // would be a 400 rather than a narrower policy.
        it('should carry every field the form does not edit through to the saved row',
            async () => {
                const user = userEvent.setup();
                const onModified = vi.fn();

                const seed = typeDefault({
                    contentTypeName: 'Devotional',
                    contentTypeDescription: 'A daily devotional',
                    contentTypeIconCssClass: 'bi-book',
                    sortOrder: 30,
                    hasTitle: true,
                    hasAuthor: false,
                    maxTitleLength: 120,
                    isAvailableAsGeneralUserContribution: true
                });

                renderPanel(
                    <ContentItemSettingsPanel
                        contentItemId={contentItemId}
                        contentType={ContentType.Devotional}
                        contentItemSettingCollection={[seed]}
                        onModified={onModified} />);

                await user.click(screen.getByRole('button', { name: 'Modify' }));
                await user.click(screen.getByRole('button', { name: 'Save settings' }));

                const saved: ContentItemSetting = onModified.mock.calls[0][0];
                expect(saved.contentTypeName).toBe('Devotional');
                expect(saved.contentTypeDescription).toBe('A daily devotional');
                expect(saved.contentTypeIconCssClass).toBe('bi-book');
                expect(saved.sortOrder).toBe(30);
                expect(saved.hasTitle).toBe(true);
                expect(saved.hasAuthor).toBe(false);
                expect(saved.maxTitleLength).toBe(120);
                expect(saved.isAvailableAsGeneralUserContribution).toBe(true);
            });

        it('should return to the read face once a save is committed', async () => {
            const user = userEvent.setup();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]}
                    onModified={vi.fn()} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));
            await user.click(screen.getByRole('button', { name: 'Save settings' }));

            expect(screen.getByRole('button', { name: 'Modify' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Save settings' })).toBeNull();
        });
    });

    describe('Reset', () => {
        it('should revert uncommitted edits to the values the read face displayed', async () => {
            const user = userEvent.setup();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[
                        typeDefault(),
                        itemOverride({ showComments: false, showTags: true })
                    ]} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));
            await user.click(switchFor('Comments are shown'));
            await user.click(switchFor('Tags are shown'));

            expect(switchFor('Comments are shown').checked).toBe(true);
            expect(switchFor('Tags are shown').checked).toBe(false);

            await user.click(screen.getByRole('button', { name: 'Reset' }));

            expect(switchFor('Comments are shown').checked).toBe(false);
            expect(switchFor('Tags are shown').checked).toBe(true);
        });

        it('should stay on the modify face and notify the consumer', async () => {
            const user = userEvent.setup();
            const onReset = vi.fn();

            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault()]}
                    onReset={onReset} />);

            await user.click(screen.getByRole('button', { name: 'Modify' }));
            await user.click(screen.getByRole('button', { name: 'Reset' }));

            expect(onReset).toHaveBeenCalledTimes(1);
            expect(screen.getByRole('button', { name: 'Save settings' })).toBeInTheDocument();
        });
    });

    describe('while the consumer is persisting', () => {
        it('should freeze the buttons so one click is one write', () => {
            renderPanel(
                <ContentItemSettingsPanel
                    contentItemId={contentItemId}
                    contentType={ContentType.Devotional}
                    contentItemSettingCollection={[typeDefault(), itemOverride()]}
                    isSubmitting />);

            expect(screen.getByRole('button', { name: 'Modify' })).toBeDisabled();
            expect(screen.getByRole('button', { name: 'Remove Override' })).toBeDisabled();
        });
    });
});

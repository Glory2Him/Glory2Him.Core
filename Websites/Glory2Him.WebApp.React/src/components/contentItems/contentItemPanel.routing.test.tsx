import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ContentItemPanel } from './contentItemPanel';
import { AuthProvider } from '../securitys/authProvider';
import { createAuthState, signOut } from '../../tests/testAuth';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemReactionOption,
    ContentItemSearchItem,
    ShareabilityBasis
} from '../../models/components/contentItems/contentItemSearchItem';

// THE CARD COMPOSES NO ROUTE (rule 3.2.4). A file of its own, because contentItemPanel.test.tsx
// doubles the router's navigate: here React Router stands as it ships, with NO router over the
// render and no double of `useNavigate` or `useLocation` — each throws outside a router, so a
// card that reads either one fails to render or to raise the hook.
const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const quoteSetting: ContentItemSetting = {
    id: 'setting-quote',
    contentType: ContentType.Quote,
    contentItemId: null,
    contentTypeName: 'Quotes',
    contentTypeDescription: 'Quotes',
    contentTypeIconCssClass: 'bi-chat-quote',
    sortOrder: ContentType.Quote,
    hasTitle: false,
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
    createdBy: 'seed',
    createdWhen: '2026-01-01T00:00:00Z',
    updatedBy: 'seed',
    updatedWhen: '2026-01-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    isDeleted: false,
    deletionReason: null
};

const quoteItem: ContentItemSearchItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    contentItemSetting: quoteSetting,
    author: 'William Temple',
    content: "When I pray, coincidences happen; when I don't, they don't",
    submittedById: 'account-bryan',
    submittedByName: 'Bryan',
    shareabilityBasis: ShareabilityBasis.PublicDomain,
    publishedDate: new Date(2026, 6, 18),
    reactionSummary: [
        { label: 'Amen', glyph: '👍', count: 85 },
        { label: 'Love', glyph: '❤️', count: 43 }
    ]
};

const reactionOptions: ReadonlyArray<ContentItemReactionOption> = [
    { id: 'reaction-amen', label: 'Amen', glyph: '👍' },
    { id: 'reaction-love', label: 'Love', glyph: '❤️', isLove: true }
];

describe('ContentItemPanel', () => {
    describe('the signed-out reader', () => {
        it('should compose no sign-in route of its own', async () => {
            // given
            signOut(authState);
            const onReactionSelected = vi.fn();

            render(
                <AuthProvider>
                    <ContentItemPanel
                        contentItem={quoteItem}
                        reactionOptions={reactionOptions}
                        onReactionSelected={onReactionSelected} />
                </AuthProvider>);

            // when
            await userEvent.click(screen.getByRole('button', { name: /Like/ }));
            await userEvent.click(screen.getByRole('menuitem', { name: 'Love' }));

            // then
            expect(onReactionSelected).toHaveBeenCalledWith(
                quoteItem, expect.objectContaining({ label: 'Love' }));
        });
    });
});

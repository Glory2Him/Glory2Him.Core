import { ReactNode } from 'react';
import { renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useContentItemEngagement } from './useContentItemEngagement';
import { AuthProvider } from '../components/securitys/authProvider';
import { ApprovalStatus } from '../models/components/associations/associationItem';
import { ContentItemSearchItem } from '../models/components/contentItems/contentItemSearchItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { Reaction } from '../models/foundations/reactions/reaction';
import { ReactionSummariesRead } from '../services/foundations/associationService';
import { createAuthState, signInAs } from '../tests/testAuth';

// The hook's own business is which summaries it asks for, for whom, and what it puts on each
// card. The foundation services below it are mocked as the page tests mock them, so each test
// says exactly what the read answered and asserts what the hook did with it.
const authState = createAuthState();

const useGetReactionSummaries = vi.fn<(
    contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>,
    readerId: string | null | undefined) => ReactionSummariesRead>();

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../services/foundations/associationService', () => ({
    associationService: {
        useGetReactionSummaries: (
            contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>,
            readerId: string | null | undefined) =>
            useGetReactionSummaries(contentItemIdPages, readerId)
    }
}));

const love: Reaction = {
    id: 'reaction-love',
    name: 'Love',
    unicodeEmoji: '❤️',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
};

const joy: Reaction = {
    id: 'reaction-joy',
    name: 'Joy',
    unicodeEmoji: '😊',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
};

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        // The vocabulary's order is neither the summaries' order nor the alphabet's, so a card
        // that took its counts' order from either would be told apart.
        useGetApprovedReactions: () => ({ data: [joy, love] })
    }
}));

const noSummaries: ReactionSummariesRead = { summaries: {}, isLoading: false, isError: false };

const cardFor = (id: string): ContentItemSearchItem => ({
    id,
    contentType: ContentType.Quote,
    content: `The words of ${id}.`
});

const wrapper = ({ children }: { children: ReactNode }) => (
    <AuthProvider>{children}</AuthProvider>
);

const renderEngagement = (contentItemIdPages?: ReadonlyArray<ReadonlyArray<string>>) =>
    renderHook(() => useContentItemEngagement(contentItemIdPages), { wrapper });

describe('useContentItemEngagement', () => {
    beforeEach(() => {
        useGetReactionSummaries.mockReset();
        useGetReactionSummaries.mockReturnValue(noSummaries);
        signInAs(authState);
    });

    it('should read the summaries of the pages it is handed', () => {
        const contentItemIdPages = [['item-1', 'item-2'], ['item-3']];

        renderEngagement(contentItemIdPages);

        expect(useGetReactionSummaries).toHaveBeenCalled();

        for (const [handedPages] of useGetReactionSummaries.mock.calls) {
            expect(handedPages).toStrictEqual([['item-1', 'item-2'], ['item-3']]);
        }
    });

    it("should put each summary's counts on its item in the view's names and the summary's order", () => {
        useGetReactionSummaries.mockReturnValue({
            summaries: {
                'item-1': {
                    contentItemId: 'item-1',
                    reactions: [
                        { reactionId: 'reaction-love', name: 'Love', unicodeEmoji: '❤️', count: 3 },
                        { reactionId: 'reaction-joy', name: 'Joy', unicodeEmoji: '😊', count: 1 }
                    ],
                    viewerReactionId: null,
                    viewerReactionName: null
                }
            },
            isLoading: false,
            isError: false
        });

        const { result } = renderEngagement([['item-1']]);
        const [card] = result.current.withReactions([cardFor('item-1')]);

        expect(card.reactionSummary).toStrictEqual([
            { label: 'Love', glyph: '❤️', count: 3 },
            { label: 'Joy', glyph: '😊', count: 1 }
        ]);
    });
});

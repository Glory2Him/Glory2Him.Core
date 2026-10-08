import { ReactNode } from 'react';
import { renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useContentItemEngagement } from './useContentItemEngagement';
import { AuthProvider } from '../components/securitys/authProvider';
import { ApprovalStatus } from '../models/components/associations/associationItem';
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
        useGetApprovedReactions: () => ({ data: [love, joy] })
    }
}));

const noSummaries: ReactionSummariesRead = { summaries: {}, isLoading: false, isError: false };

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
});

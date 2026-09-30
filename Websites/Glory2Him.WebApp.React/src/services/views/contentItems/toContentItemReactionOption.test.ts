import { describe, expect, it } from 'vitest';
import { ApprovalStatus } from '../../../models/components/associations/associationItem';
import { Reaction } from '../../../models/foundations/reactions/reaction';

import { toContentItemReactionOption } from './toContentItemReactionOption';

const reactionWith = (id: string, name: string, unicodeEmoji: string): Reaction => ({
    id,
    name,
    unicodeEmoji,
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
});

describe('toContentItemReactionOption', () => {
    it("should carry each reaction's id on its option", () => {
        // given
        const vocabulary: ReadonlyArray<Reaction> = [
            reactionWith('7b2d90c1-4e6a-4f3b-8d21-000000000001', 'Amen', '👍'),
            reactionWith('7b2d90c1-4e6a-4f3b-8d21-000000000002', 'Love', '❤️'),
            reactionWith('7b2d90c1-4e6a-4f3b-8d21-000000000003', 'Joy', '😄')
        ];

        // when
        const options = vocabulary.map(toContentItemReactionOption);

        // then
        expect(options).toEqual([
            {
                id: '7b2d90c1-4e6a-4f3b-8d21-000000000001',
                label: 'Amen',
                glyph: '👍',
                isLove: false
            },
            {
                id: '7b2d90c1-4e6a-4f3b-8d21-000000000002',
                label: 'Love',
                glyph: '❤️',
                isLove: true
            },
            {
                id: '7b2d90c1-4e6a-4f3b-8d21-000000000003',
                label: 'Joy',
                glyph: '😄',
                isLove: false
            }
        ]);
    });
});

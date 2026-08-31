import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ContentItemSettingBroker from './apiBroker.contentItemSettings';

// The overrides read behind the §6.4 per-item resolution: a surface hands its panel the defaults
// PLUS the overrides of exactly the items it shows, and this is the query that fetches the
// second half. What matters is the batching — one request per chunk, never one per card, and
// never an or-chain long enough to breach IIS's query-string limit.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);

const requestedUrls = (): string[] =>
    getAsync.mock.calls.map((call) => decodeURIComponent(call[0] as string));

describe('ContentItemSettingBroker.GetOverridesForContentItemsAsync', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: [] } as never);
    });

    it('should ask nothing at all for no items', async () => {
        // when
        const overrides =
            await new ContentItemSettingBroker().GetOverridesForContentItemsAsync([]);

        // then
        expect(overrides).toEqual([]);
        expect(getAsync).not.toHaveBeenCalled();
    });

    it('should or-chain the ids into one filter', async () => {
        // when
        await new ContentItemSettingBroker()
            .GetOverridesForContentItemsAsync(['item-1', 'item-2']);

        // then
        expect(getAsync).toHaveBeenCalledTimes(1);

        expect(requestedUrls()[0]).toContain(
            '$filter=contentItemId eq item-1 or contentItemId eq item-2');
    });

    // Repeated guids exhaust IIS's 2048-character query string at roughly 45 — a refusal, not a
    // truncation — so a long list travels as several requests rather than one long one.
    it('should chunk a long list rather than build one long query string', async () => {
        // given
        const ids = Array.from({ length: 45 }, (_unused, index) => `item-${index}`);

        // when
        await new ContentItemSettingBroker().GetOverridesForContentItemsAsync(ids);

        // then: 45 ids at a chunk of 20 is three requests, none oversized
        expect(getAsync).toHaveBeenCalledTimes(3);

        requestedUrls().forEach((url) => expect(url.length).toBeLessThan(2048));
    });

    it('should pool every chunk into one collection', async () => {
        // given
        getAsync
            .mockResolvedValueOnce({ data: [{ id: 'override-1' }] } as never)
            .mockResolvedValueOnce({ data: [{ id: 'override-2' }] } as never);

        const ids = Array.from({ length: 21 }, (_unused, index) => `item-${index}`);

        // when
        const overrides =
            await new ContentItemSettingBroker().GetOverridesForContentItemsAsync(ids);

        // then
        expect(overrides.map((setting) => setting.id)).toEqual(['override-1', 'override-2']);
    });
});

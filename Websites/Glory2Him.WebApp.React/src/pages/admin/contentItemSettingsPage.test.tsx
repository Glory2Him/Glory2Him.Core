import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentItemSettingQuery } from '../../models/foundations/contentItemSettings/contentItemSettingQuery';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ContentItemSettingsPage } from './contentItemSettingsPage';

// The query the page hands the service is the whole contract between the filter controls and
// the server-side $filter/$skip/$top the broker builds from it — nothing about it shows on
// screen, so it is asserted directly.
let lastQuery: ContentItemSettingQuery | undefined;
let hasNextPage = false;
let settings: ContentItemSetting[] = [];

vi.mock('../../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetContentItemSettings: (query: ContentItemSettingQuery) => {
            lastQuery = query;

            return {
                data: { items: settings, page: query.page, pageSize: query.pageSize, hasNextPage },
                isLoading: false,
                isError: false,
                isFetching: false
            };
        }
    }
}));

const createSetting = (overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
    id: '11111111-1111-1111-1111-111111111111',
    contentType: ContentType.Quote,
    contentItemId: null,
    contentTypeName: 'Quote',
    contentTypeDescription: 'Words that stirred you',
    contentTypeIconCssClass: 'bi-quote',
    sortOrder: 0,
    hasTitle: false,
    hasAuthor: true,
    isAvailableAsGeneralUserContribution: true,
    tagsAllowed: true,
    showTags: true,
    reactionsAllowed: true,
    showReactions: true,
    linksAllowed: false,
    showLinks: true,
    attachmentsAllowed: false,
    showAttachments: true,
    commentsAllowed: true,
    showComments: true,
    bibleReferenceAllowed: true,
    showBibleReferences: true,
    limitReactionsToLoveOnly: false,
    createdBy: 'system-seed',
    createdWhen: '2026-08-28T12:21:18.308+00:00',
    updatedBy: 'system-seed',
    updatedWhen: '2026-08-28T12:21:18.308+00:00',
    deletedBy: null,
    deletedWhen: null,
    isDeleted: false,
    deletionReason: null,
    ...overrides
});

const renderPage = () =>
    render(
        <MemoryRouter>
            <ContentItemSettingsPage />
        </MemoryRouter>);

describe('ContentItemSettingsPage', () => {
    beforeEach(() => {
        lastQuery = undefined;
        hasNextPage = false;
        settings = [];
    });

    it('should ask for the first page unfiltered', () => {
        // when
        renderPage();

        // then
        expect(lastQuery).toEqual({
            searchTerm: '',
            contentType: undefined,
            scope: 'All',
            page: 1,
            pageSize: 10
        });
    });

    it('should tell a type default apart from an item override', () => {
        // given
        settings = [
            createSetting(),
            createSetting({
                id: '22222222-2222-2222-2222-222222222222',
                contentType: ContentType.BlogPost,
                contentTypeName: 'Blog Post',
                contentItemId: '33333333-3333-3333-3333-333333333333'
            })
        ];

        // when
        renderPage();

        // then
        expect(screen.getByText('Type default')).toBeInTheDocument();
        expect(screen.getByText('Item override')).toBeInTheDocument();
        expect(screen.getByText('33333333-3333-3333-3333-333333333333')).toBeInTheDocument();
    });

    it('should show the fixed content type member beside the row-editable name', () => {
        // given
        settings = [createSetting({
            contentType: ContentType.BibleStudy,
            contentTypeName: 'Digging Deeper'
        })];

        // when
        renderPage();

        // then: the filter dropdown carries the member label too, so this looks in the table
        const table = within(screen.getByRole('table'));
        expect(table.getByText('Digging Deeper')).toBeInTheDocument();
        expect(table.getByText('Bible Study')).toBeInTheDocument();
    });

    it('should title each feature glyph with what it says about the setting', () => {
        // given: shown but closed to new ones, and the reverse
        settings = [createSetting({
            showTags: true,
            tagsAllowed: false,
            showComments: false,
            commentsAllowed: true
        })];

        // when
        renderPage();

        // then
        expect(screen.getByTitle('Tags shown')).toBeInTheDocument();
        expect(screen.getByTitle('Tags can not be added')).toBeInTheDocument();
        expect(screen.getByTitle('Comments hidden')).toBeInTheDocument();
        expect(screen.getByTitle('Comments can be added')).toBeInTheDocument();
    });

    it('should keep every feature on one row rather than a column each', () => {
        // given
        settings = [createSetting()];

        // when
        renderPage();

        // then: six feature columns made the table wider than the content area, which pushed
        // the row's own Manage button behind a horizontal scrollbar
        const headers = screen.getAllByRole('columnheader').map((header) => header.textContent);
        expect(headers).toEqual(['Content type', '', '']);
        expect(screen.getByRole('button', { name: 'Manage' })).toBeInTheDocument();
    });

    it('should carry the chosen content type into the query', async () => {
        // given
        renderPage();

        // when
        await userEvent.selectOptions(
            screen.getByLabelText('Content type'),
            String(ContentType.Testimony));

        // then
        expect(lastQuery?.contentType).toBe(ContentType.Testimony);
    });

    it('should carry the chosen scope into the query', async () => {
        // given
        renderPage();

        // when
        await userEvent.selectOptions(screen.getByLabelText('Scope'), 'Override');

        // then
        expect(lastQuery?.scope).toBe('Override');
    });

    it('should debounce the search term rather than query on every keystroke', async () => {
        // given
        renderPage();

        // when
        await userEvent.type(screen.getByLabelText('Search content item settings'), 'quote');

        // then
        expect(lastQuery?.searchTerm).toBe('');
        await waitFor(() => expect(lastQuery?.searchTerm).toBe('quote'));
    });

    it('should return to the first page when the filters narrow', async () => {
        // given
        hasNextPage = true;
        settings = [createSetting()];
        renderPage();
        await userEvent.click(screen.getByRole('button', { name: 'Next' }));
        expect(lastQuery?.page).toBe(2);

        // when
        await userEvent.selectOptions(screen.getByLabelText('Scope'), 'Default');

        // then
        expect(lastQuery?.page).toBe(1);
    });

    it('should offer no pager while a single page holds everything', () => {
        // given
        settings = [createSetting()];

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: 'Next' })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Previous' })).not.toBeInTheDocument();
    });

    it('should say that filters matched nothing rather than that there is nothing', async () => {
        // given
        renderPage();

        // when
        await userEvent.selectOptions(screen.getByLabelText('Scope'), 'Override');

        // then
        expect(screen.getByText('No content item settings match these filters.')).toBeInTheDocument();
    });
});
